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
using ProductCheckerBack.ProductChecker.Api;

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

        private static bool HasHigherPriorityRequest(Request? currentRequest)
        {
            if (currentRequest == null || currentRequest.Priority == 1)
            {
                return false;
            }

            using var priorityDbContext = new ProductCheckerDbContext();
            return priorityDbContext.Requests
                .AsNoTracking()
                .Any(req =>
                    (req.Status == RequestStatus.PENDING || req.Status == RequestStatus.PROCESSING) &&
                    req.Priority == 1 &&
                    req.Id != currentRequest.Id);
        }

        private async Task ClearStorageAndRestartAsync(List<string> errors, int clearStorageThreshold)
        {
            Console.WriteLine($"[Storage] Reached threshold of {clearStorageThreshold} listings. Clearing storage.");
            await _storageClearer.TryClearStorageAsync(errors).ConfigureAwait(false);

            Console.WriteLine("[Storage] Pausing product checks for 10 seconds.");
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            try
            {
                List<string> restarterUrls = [];
                using (var db1 = new ProductCheckerDbContext())
                {
                    restarterUrls = db1.ApiEndpoints
                        .Where(s => s.Key == "server_restarter_url")
                        .Select(s => s.Value)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => value!.Trim())
                        .ToList();
                }

                if (restarterUrls.Count > 0)
                {
                    using var restarterHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
                    var restarterApi = new ServerRestarterApi(restarterHttpClient);
                    var restartTasks = restarterUrls
                        .Select(url => restarterApi.Restart(url))
                        .ToArray();
                    Console.WriteLine("[System] Restarting all servers, Please wait 15 seconds.");
                    await Task.WhenAll(restartTasks).ConfigureAwait(false);
                    await Task.Delay(10000);
                }
            }
            catch (Exception ex)
            {
            }
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

            if (supportedListings.Count == 0)
            {
                errors.Add("Request listings has no supported platforms.");
                FinalizeRequest(productCheckerDbContext, productCheckerService, errors);
                return;
            }

            var activeEndpoints = EndpointProvider.GetActiveEndpoints();
            if (activeEndpoints.Count == 0)
            {
                errors.Add("No active product checker endpoints are configured.");
                FinalizeRequest(productCheckerDbContext, productCheckerService, errors);
                return;
            }

            var results = new BlockingCollection<ScanTaskResult>();
            var endpointQueue = new ConcurrentQueue<string>(activeEndpoints);
            var endpointSignal = new SemaphoreSlim(activeEndpoints.Count, activeEndpoints.Count);
            var totalCount = supportedListings.Count;
            var startedCount = 0;
            var listingById = supportedListings.ToDictionary(listing => listing.Id);
            var currentRequest = productCheckerService.Request;
            var yieldToHighPriority = false;
            var stopProcessing = false;
            var nextListingIndex = 0;
            var clearStorageThreshold = Configuration.GetClearStorageThreshold();
            var shouldClearStorageBetweenBatches = clearStorageThreshold > 0;

            var resultSaver = new ScanResultSaver(productCheckerDbContext, listingById);
            var saveTask = resultSaver.RunAsync(results);

            while (nextListingIndex < supportedListings.Count && !stopProcessing)
            {
                var batchSize = shouldClearStorageBetweenBatches
                    ? Math.Min(clearStorageThreshold, supportedListings.Count - nextListingIndex)
                    : supportedListings.Count - nextListingIndex;
                var batchTasks = new List<Task>(batchSize);
                var listingsStartedInBatch = 0;

                while (listingsStartedInBatch < batchSize && nextListingIndex < supportedListings.Count)
                {
                    if (HasHigherPriorityRequest(currentRequest))
                    {
                        yieldToHighPriority = true;
                        stopProcessing = true;
                        break;
                    }

                    await endpointSignal.WaitAsync().ConfigureAwait(false);
                    if (!endpointQueue.TryDequeue(out var endpoint))
                    {
                        endpointSignal.Release();
                        errors.Add("No endpoint available for processing.");
                        stopProcessing = true;
                        break;
                    }

                    var listing = supportedListings[nextListingIndex];
                    nextListingIndex++;
                    listingsStartedInBatch++;

                    var listingDbId = listing.Id;
                    var listingId = listing.ListingId;
                    var caseNumber = listing.CaseNumber;
                    var url = listing.Url;
                    var platform = listing.Platform;

                    batchTasks.Add(Task.Run(async () =>
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
                            endpointQueue.Enqueue(endpoint);
                            endpointSignal.Release();
                        }
                    }));
                }

                await Task.WhenAll(batchTasks).ConfigureAwait(false);

                var hasMoreListings = nextListingIndex < supportedListings.Count;
                if (!stopProcessing &&
                    shouldClearStorageBetweenBatches &&
                    listingsStartedInBatch == clearStorageThreshold &&
                    hasMoreListings)
                {
                    await ClearStorageAndRestartAsync(errors, clearStorageThreshold).ConfigureAwait(false);
                }
            }

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
