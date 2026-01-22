using ProductCheckerBack.Models;
using ProductCheckerBack.ProductChecker;
using ProductCheckerBack.ProductChecker.Api.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCheckerBack.RequestState.SuccessStateHandler
{
    internal class CheckProductAvailability : IHandler
    {
        private sealed class ScanTaskResult
        {
            public int ListingDbId { get; }
            public string ListingId { get; }
            public string? CaseNumber { get; }
            public string Url { get; }
            public string Platform { get; }
            public ProductCheckerScanResponse? Status { get; }
            public Exception? Error { get; }

            public ScanTaskResult(
                int listingDbId,
                string listingId,
                string? caseNumber,
                string url,
                string platform,
                ProductCheckerScanResponse? status,
                Exception? error)
            {
                ListingDbId = listingDbId;
                ListingId = listingId;
                CaseNumber = caseNumber;
                Url = url;
                Platform = platform;
                Status = status;
                Error = error;
            }
        }

        public IHandler NextHandler { get; set; }

        public void Process(ProductCheckerV2DbContext productCheckerV2DbContext, ProductCheckerService productCheckerService, List<string> errors)
        {
            ProcessAsync(productCheckerV2DbContext, productCheckerService, errors)
                .GetAwaiter()
                .GetResult();
        }

        private async Task ProcessAsync(ProductCheckerV2DbContext productCheckerV2DbContext, ProductCheckerService productCheckerService, List<string> errors)
        {
            var allListings = productCheckerService.GetOrganizedListings();
            if (allListings.Count == 0)
            {
                errors.Add("Request has no product listings to process.");
                return;
            }

            //List<string> activeEndpoints;
            //using (var db = new ProductCheckerDbContext())
            //{
            //    activeEndpoints = db.Ports
            //        .Where(port => port.Status == 0 && !string.IsNullOrWhiteSpace(port.Api))
            //        .Select(port => port.Api!.Trim())
            //        .ToList();
            //}
            List<string> activeEndpoints = [
                    "http://172.31.11.95:8011",
                    "http://172.31.11.95:8012",
                    "http://172.31.11.95:8013"
                ];

            if (activeEndpoints.Count == 0)
            {
                errors.Add("No active Product Checker endpoints available.");
                productCheckerService.MarkAsFailed(errors);
                return;
            }

            var results = new BlockingCollection<ScanTaskResult>();
            var endpointQueue = new ConcurrentQueue<string>(activeEndpoints);
            var endpointSignal = new SemaphoreSlim(activeEndpoints.Count, activeEndpoints.Count);
            var tasks = new List<Task>(allListings.Count);
            var totalCount = allListings.Count;
            var startedCount = 0;
            var completedCount = 0;
            var failedCount = 0;
            var listingById = allListings.ToDictionary(listing => listing.Id);

            var saveTask = Task.Run(() =>
            {
                foreach (var result in results.GetConsumingEnumerable())
                {
                    if (!listingById.TryGetValue(result.ListingDbId, out var listing))
                    {
                        continue;
                    }

                    if (productCheckerV2DbContext.Entry(listing).State == EntityState.Detached)
                    {
                        productCheckerV2DbContext.ProductListings.Attach(listing);
                    }

                    var checkedDate = result.Status?.DateChecked;
                    listing.CheckedDate = string.IsNullOrWhiteSpace(checkedDate)
                        ? DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss")
                        : checkedDate;

                    if (result.Error != null || result.Status == null || !string.IsNullOrWhiteSpace(result.Status.ErrorDetails))
                    {
                        listing.UrlStatus = "Error";
                        listing.ErrorDetail = result.Error?.ToString() ?? result.Status?.ErrorDetails ?? "";
                        listing.Note = result.Status?.Notes ?? string.Empty;
                        errors.Add($"Listing {result.ListingId} failed.");
                        productCheckerV2DbContext.SaveChanges();
                        continue;
                    }

                    listing.UrlStatus = result.Status.Availability ? "Available" : "Not Available";
                    listing.ErrorDetail = result.Status.ErrorDetails;
                    listing.Note = result.Status.Notes;
                    productCheckerV2DbContext.SaveChanges();
                }
            });

            foreach (var listing in allListings)
            {
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

                        var response = await client.ProductCheckerScanApi.Scan(payload).ConfigureAwait(false);
                        results.Add(new ScanTaskResult(listingDbId, listingId, caseNumber, url, platform, response, null));
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failedCount);
                        results.Add(new ScanTaskResult(listingDbId, listingId, caseNumber, url, platform, null, ex));
                    }
                    finally
                    {
                        var done = Interlocked.Increment(ref completedCount);
                        var failed = failedCount;
                        Console.WriteLine($"[Scan] Done {done}/{totalCount} listing {listingId} via {endpoint} (failed {failed})");
                        endpointQueue.Enqueue(endpoint);
                        endpointSignal.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            results.CompleteAdding();
            await saveTask.ConfigureAwait(false);
            
            if (errors.Count > 0)
            {
                if (errors.Count == totalCount)
                {
                    productCheckerService.MarkAsFailed(errors);
                }
                else 
                {
                    productCheckerService.MarkAsCompletedWithIssues(errors);
                }
            }
            else
            {
                productCheckerService.MarkAsSuccess();
            }

            if (productCheckerService.Request != null)
            {
                productCheckerService.Request.UpdatedAt = DateTime.UtcNow.AddHours(8);
            }

            NextHandler?.Process(productCheckerV2DbContext, productCheckerService, errors);
        }
    }
}
