using ProductCheckerBack.Models;
using ProductCheckerBack.ProductChecker;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProductCheckerBack.Models.ProductChecker;

namespace ProductCheckerBack.RequestState.DefaultStateHandler
{
    internal class CheckProductAvailability : IHandler
    {
        private readonly StorageClearer _storageClearer = new StorageClearer();

        public IHandler NextHandler { get; set; }

        public void Process(ProductCheckerDbContext productCheckerDbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false)
        {
            ProcessAsync(productCheckerDbContext, productCheckerService, errors, onlyErrors)
                .GetAwaiter()
                .GetResult();
        }

        private bool IsNotSupportedListing(ProductListings listing)
        {
            if (listing == null || string.IsNullOrEmpty(listing.Platform))
            {
                return false;
            }

            return listing.Platform.Contains("Not Supported", StringComparison.OrdinalIgnoreCase);
        }

        private void FinalizeRequest(ProductCheckerDbContext productCheckerDbContext, ProductCheckerService productCheckerService, List<string> errors)
        {
            if (productCheckerService.GetErrorProductListings().Count == productCheckerService.GetAllProductListings().Count)
            {
                productCheckerService.MarkAsFailed(errors);
            }
            else if (productCheckerService.GetErrorProductListings().Count == 0)
            {
                productCheckerService.MarkAsSuccess();
            }
            else
            {
                productCheckerService.MarkAsCompletedWithIssues(errors);
            }

            if (productCheckerService.Request != null)
            {
                productCheckerService.Request.UpdatedAt = DateTime.UtcNow.AddHours(8);
            }

            Console.Clear();

            NextHandler?.Process(productCheckerDbContext, productCheckerService, errors);
        }

        private async Task ProcessAsync(ProductCheckerDbContext productCheckerDbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false)
        {
            var allListings = productCheckerService.GetOrganizedListings(onlyErrors) ?? new List<ProductListings>();
            const string NotSupportedMessage = "Not supported platform in Product Checker";

            if (allListings.Count == 0)
            {
                errors.Add("Request has no product listings to process.");
                FinalizeRequest(productCheckerDbContext, productCheckerService, errors);
                return;
            }

            var unsupportedListings = allListings
                .Where(IsNotSupportedListing)
                .ToList();

            if (unsupportedListings.Count > 0)
            {
                var checkedDate = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss");
                foreach (var listing in unsupportedListings)
                {
                    listing.UrlStatus = "Error";
                    listing.Note = NotSupportedMessage;
                    listing.ErrorDetail = string.Empty;
                    listing.CheckedDate = checkedDate;
                }

                productCheckerDbContext.SaveChanges();
            }

            var supportedListings = allListings
                .Where(listing => !IsNotSupportedListing(listing))
                .ToList();
            var totalListingCount = allListings.Count;

            if (supportedListings.Count == 0)
            {
                errors.Add("Request listings has no supported platforms.");
                FinalizeRequest(productCheckerDbContext, productCheckerService, errors);
                return;
            }

            var activeEndpoints = EndpointProvider.GetActiveEndpoints();

            if (activeEndpoints.Count == 0)
            {
                errors.Add("No active Product Checker endpoints available.");
                FinalizeRequest(productCheckerDbContext, productCheckerService, errors);
                return;
            }

            var results = new BlockingCollection<ScanTaskResult>();
            var endpointQueue = new ConcurrentQueue<string>(activeEndpoints);
            var endpointSignal = new SemaphoreSlim(activeEndpoints.Count, activeEndpoints.Count);
            var tasks = new List<Task>(allListings.Count);
            var totalCount = supportedListings.Count;
            var startedCount = 0;
            var completedCount = 0;
            var clearStorageCounter = 0;
            var listingById = supportedListings.ToDictionary(listing => listing.Id);
            var currentRequest = productCheckerService.Request;
            var yieldToHighPriority = false;
            const string HighPriorityCancelMessage = "Cancelled request cause of prioritizing high prio requests";

            var resultSaver = new ScanResultSaver(productCheckerDbContext, listingById);
            var saveTask = resultSaver.RunAsync(results);

            foreach (var listing in supportedListings)
            {
                if (currentRequest != null && currentRequest.Priority != 1)
                {
                    using var priorityDbContext = new ProductCheckerDbContext();
                    var hasHighPriority = priorityDbContext.Requests
                        .AsNoTracking()
                        .Any(req =>
                            (req.Status == RequestStatus.PENDING || req.Status == RequestStatus.PROCESSING) &&
                            req.Priority == 1 &&
                            req.Id != currentRequest.Id);

                    if (hasHighPriority)
                    {
                        yieldToHighPriority = true;
                        break;
                    }
                }
                await endpointSignal.WaitAsync().ConfigureAwait(false);
                if (!endpointQueue.TryDequeue(out var endpoint))
                {
                    endpointSignal.Release();
                    errors.Add("No endpoint available for processing.");
                    break;
                }

                var listingDbId = listing.Id;
                var listingId = listing.ListingId;
                var caseNumber = listing.CaseNumber;
                var url = listing.Url;
                var platform = listing.Platform;

                tasks.Add(Task.Run(async () =>
                {
                    var started = Interlocked.Increment(ref startedCount);
                    Console.WriteLine($"[Scan] Start {started}/{totalCount} listing {listingId} via {endpoint}");
                    try
                    {
                        var client = ProductCheckerClient.ForApi(endpoint);
                        var payload = new
                        {
                            listing_id = listingId,
                            case_number = caseNumber,
                            url = url,
                            availability = false
                        };

                        var response = await client.ProductCheckerScanApi.Scan(payload, listingId, $"{started}/{totalCount}", endpoint).ConfigureAwait(false);
                        results.Add(new ScanTaskResult(listingDbId, listingId, caseNumber, url, platform, response, null));
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ScanTaskResult(listingDbId, listingId, caseNumber, url, platform, null, ex.ToString()));
                    }
                    finally
                    {
                        var done = Interlocked.Increment(ref completedCount);
                        var clearCount = Interlocked.Increment(ref clearStorageCounter);
                        if (clearCount >= Configuration.GetClearStorageThreshold() &&
                            Interlocked.Exchange(ref clearStorageCounter, 0) >= Configuration.GetClearStorageThreshold())
                        {
                            await _storageClearer.TryClearStorageAsync(errors).ConfigureAwait(false);
                        }
                        endpointQueue.Enqueue(endpoint);
                        endpointSignal.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            results.CompleteAdding();
            await saveTask.ConfigureAwait(false);

            if (yieldToHighPriority)
            {
                productCheckerService.MarkAsPending(true);
                if (currentRequest != null)
                {
                    currentRequest.UpdatedAt = DateTime.UtcNow.AddHours(8);
                    productCheckerDbContext.SaveChanges();
                }
                return;
            }

            FinalizeRequest(productCheckerDbContext, productCheckerService, errors);
        }
    }
}
