using ProductCheckerBack.Models;
using ProductCheckerBack.ProductChecker;
using ProductCheckerBack.ProductChecker.Api;
using ProductCheckerBack.ProductChecker.Api.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProductCheckerBack.Models.ProductChecker;

namespace ProductCheckerBack.RequestState.SuccessStateHandler
{
    internal class CheckProductAvailability : IHandler
    {
        private static readonly SemaphoreSlim ClearStorageGate = new SemaphoreSlim(1, 1);
        private sealed class ScanTaskResult
        {
            public int ListingDbId { get; }
            public string ListingId { get; }
            public string? CaseNumber { get; }
            public string Url { get; }
            public string Platform { get; }
            public ProductCheckerScanResponse? Status { get; }
            public Exception? Error { get; }
            public string? ErrorMessage { get; }

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
                ErrorMessage = CheckProductAvailability.TryParseErrorMessage(status?.ErrorDetails);
            }
        }

        public IHandler NextHandler { get; set; }

        public void Process(ProductCheckerV2DbContext productCheckerV2DbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false)
        {
            ProcessAsync(productCheckerV2DbContext, productCheckerService, errors, onlyErrors)
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

        private static string? TryParseErrorMessage(string? errorDetails)
        {
            if (string.IsNullOrWhiteSpace(errorDetails))
            {
                return null;
            }

            try
            {
                using var json = JsonDocument.Parse(errorDetails);
                if (json.RootElement.TryGetProperty("message", out var messageElement))
                {
                    var message = messageElement.GetString();
                    return string.IsNullOrWhiteSpace(message) ? null : message;
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private void FinalizeRequest(ProductCheckerV2DbContext productCheckerV2DbContext, ProductCheckerService productCheckerService, List<string> errors)
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

            NextHandler?.Process(productCheckerV2DbContext, productCheckerService, errors);
        }

        private async Task TryClearStorageAsync(List<string> errors)
        {
            ServerStorageClearerApi? storageClearerApi = null;
            HttpClient? storageHttpClient = null;
            List<string> storageClearerUrls = [];

            using (var db = new ProductCheckerDbContext())
            {
                storageClearerUrls = db.ApiEndpoints
                    .Where(s => s.Key == "server_storage_clearer_url")
                    .Select(s => s.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!.Trim())
                    .ToList();
            }

            if (storageClearerUrls.Count > 0)
            {
                storageHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
                storageClearerApi = new ServerStorageClearerApi(storageHttpClient);
            }

            if (storageClearerApi == null || storageClearerUrls.Count == 0)
            {
                return;
            }

            await ClearStorageGate.WaitAsync().ConfigureAwait(false);
            try
            {
                var clearTasks = storageClearerUrls
                    .Select(url => storageClearerApi.ClearStorage(url))
                    .ToArray();
                await Task.WhenAll(clearTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lock (errors)
                {
                    errors.Add($"Storage clear failed: {ex.Message}");
                }
            }
            finally
            {
                ClearStorageGate.Release();
                storageHttpClient?.Dispose();
            }
        }

        private async Task ProcessAsync(ProductCheckerV2DbContext productCheckerV2DbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false)
        {
            var allListings = new List<ProductListings>();
            const string NotSupportedMessage = "Not supported platform in Product Checker";

            if (onlyErrors)
            {
                allListings = productCheckerService.GetOrganizedListings(onlyErrors);
            }
            else
            {
                allListings = productCheckerService.GetOrganizedListings();
            }

            if (allListings.Count == 0)
            {
                errors.Add("Request has no product listings to process.");
                FinalizeRequest(productCheckerV2DbContext, productCheckerService, errors);
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

                productCheckerV2DbContext.SaveChanges();
            }

            var supportedListings = allListings
                .Where(listing => !IsNotSupportedListing(listing))
                .ToList();
            var totalListingCount = allListings.Count;

            if (supportedListings.Count == 0)
            {
                errors.Add("Request listings has no supported platforms.");
                FinalizeRequest(productCheckerV2DbContext, productCheckerService, errors);
                return;
            }

            List<string> activeEndpoints;
            using (var db = new ProductCheckerDbContext())
            {
               activeEndpoints = db.Ports
                   .Where(port => port.Status == 0 && !string.IsNullOrWhiteSpace(port.Api))
                   .Select(port => port.Api!.Trim())
                   .ToList();
            }

            if (activeEndpoints.Count == 0)
            {
                errors.Add("No active Product Checker endpoints available.");
                FinalizeRequest(productCheckerV2DbContext, productCheckerService, errors);
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
                        listing.Note = result.ErrorMessage ?? result.Status?.Notes ?? string.Empty;
                        lock (errors)
                        {
                            errors.Add($"Listing {result.ListingId} failed.");
                        }
                        productCheckerV2DbContext.SaveChanges();
                        continue;
                    }

                    listing.UrlStatus = result.Status.Availability ? "Available" : "Not Available";
                    listing.ErrorDetail = result.Status.ErrorDetails;
                    listing.Note = result.Status.Notes;
                    productCheckerV2DbContext.SaveChanges();
                }
            });

            foreach (var listing in supportedListings)
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
                        results.Add(new ScanTaskResult(listingDbId, listingId, caseNumber, url, platform, null, ex));
                    }
                    finally
                    {
                        var done = Interlocked.Increment(ref completedCount);
                        Console.WriteLine($"[Scan] Done {done}/{totalCount} listing {listingId} via {endpoint}");
                        var clearCount = Interlocked.Increment(ref clearStorageCounter);
                        if (clearCount >= Configuration.GetClearStorageThreshold() &&
                            Interlocked.Exchange(ref clearStorageCounter, 0) >= Configuration.GetClearStorageThreshold())
                        {
                            await TryClearStorageAsync(errors).ConfigureAwait(false);
                        }
                        endpointQueue.Enqueue(endpoint);
                        endpointSignal.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            results.CompleteAdding();
            await saveTask.ConfigureAwait(false);
            
            FinalizeRequest(productCheckerV2DbContext, productCheckerService, errors);
        }
    }
}
