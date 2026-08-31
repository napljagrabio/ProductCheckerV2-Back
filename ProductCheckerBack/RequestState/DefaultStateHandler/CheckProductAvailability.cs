using ProductCheckerBack.Models;
using ProductCheckerBack.ProductChecker;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProductCheckerBack.ProductChecker.Api;
using ProductCheckerBack.Artemis;
using System.Net.Http;

namespace ProductCheckerBack.ExecutionState.DefaultStateHandler
{
    internal class CheckProductAvailability : IHandler
    {
        private readonly StorageClearer _storageClearer = new StorageClearer();
        private bool _retriedOnce = false;

        public IHandler NextHandler { get; set; }

        public void Process(ArtemisDbContext productCheckerDbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false)
        {
            _retriedOnce = false;
            ValidateAndProcessExecution(productCheckerDbContext, productCheckerService, errors, onlyErrors)
                .GetAwaiter()
                .GetResult();
        }

        private bool IsNotSupportedListing(ExecutionListing listing)
        {
            if (listing == null || string.IsNullOrEmpty(listing.Platform))
            {
                return false;
            }

            return listing.Platform.Contains("Not Supported", StringComparison.OrdinalIgnoreCase);
        }

        private void FinalizeExecution(ArtemisDbContext productCheckerDbContext, ProductCheckerService productCheckerService, List<string> errors)
        {
            if (productCheckerService.GetErrorExecutionListings().Count == productCheckerService.GetAllExecutionListings().Count)
            {
                productCheckerService.MarkAsFailed(errors);
            }
            else if (productCheckerService.GetErrorExecutionListings().Count == 0)
            {
                productCheckerService.MarkAsSuccess();
            }
            else
            {
                productCheckerService.MarkAsCompletedWithIssues(errors);
            }

            var response = ArtemisClient.Instance.SendEmailReportApi.Execute(productCheckerService.Execution.Id).Result;
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Logger.Log(
                    new ErrorLogging.Payload
                    {
                        ExecutionId = productCheckerService.Execution.Id
                    },
                    $"[Product Checker] Failed To Send Email Report. HTTP {(int)response.StatusCode} {response.ReasonPhrase}. {responseBody}",
                    string.Empty);
            }

            Console.Clear();

            NextHandler?.Process(productCheckerDbContext, productCheckerService, errors);
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
                using (var db1 = new ArtemisDbContext())
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

        private async Task ValidateAndProcessExecution(ArtemisDbContext productCheckerDbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false)
        {
            var allListings = productCheckerService.GetOrganizedListings(onlyErrors) ?? new List<ExecutionListing>();
            const string NotSupportedMessage = "Not supported platform in Product Checker";

            if (allListings.Count == 0)
            {
                errors.Add("Execution has no product listings to process.");
                FinalizeExecution(productCheckerDbContext, productCheckerService, errors);
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
                errors.Add("Execution listings has no supported platforms.");
                FinalizeExecution(productCheckerDbContext, productCheckerService, errors);
                return;
            }

            var activeEndpoints = EndpointProvider.GetActiveEndpoints();
            if (activeEndpoints.Count == 0)
            {
                errors.Add("No active product checker endpoints are configured.");
                FinalizeExecution(productCheckerDbContext, productCheckerService, errors);
                return;
            }

            await ProcessAsync(productCheckerDbContext, errors, supportedListings, activeEndpoints)
                .ConfigureAwait(false);

            if (!_retriedOnce && productCheckerService.GetErrorExecutionListings().Count > 0)
            {
                var retryErrorListings = productCheckerService.GetOrganizedListings(true) ?? new List<ExecutionListing>();
                var errorSupportedListings = retryErrorListings
                .Where(listing => !IsNotSupportedListing(listing))
                .ToList();

                if (errorSupportedListings.Count > 0)
                {
                    _retriedOnce = true;
                    await ProcessAsync(productCheckerDbContext, errors, errorSupportedListings, activeEndpoints)
                        .ConfigureAwait(false);
                }
            }

            FinalizeExecution(productCheckerDbContext, productCheckerService, errors);
        }

        private async Task ProcessAsync(
            ArtemisDbContext productCheckerDbContext,
            List<string> errors,
            List<ExecutionListing> supportedListings,
            List<string> activeEndpoints)
        {
            var results = new BlockingCollection<ScanTaskResult>();
            var endpointQueue = new ConcurrentQueue<string>(activeEndpoints);
            var endpointSignal = new SemaphoreSlim(activeEndpoints.Count, activeEndpoints.Count);
            var totalCount = supportedListings.Count;
            var startedCount = 0;
            var listingById = supportedListings.ToDictionary(listing => listing.Id);
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
        }
    }
}
