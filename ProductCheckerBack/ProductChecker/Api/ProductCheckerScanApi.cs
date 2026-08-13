using ProductCheckerBack.Models;
using ProductCheckerBack.ProductChecker.Api.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductCheckerBack.ProductChecker.Api
{
    internal class ProductCheckerScanApi
    {
        private HttpClient _httpClient { get; set; }

        private sealed class ScanEnvelope
        {
            [JsonPropertyName("status")]
            public string Status { get; set; }

            [JsonPropertyName("data")]
            public List<ProductCheckerScanResponse> Data { get; set; }
        }

        public ProductCheckerScanApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProductCheckerScanResponse> Scan(object payload, long listingId, string progress, string endpoint)
        {
            Console.WriteLine($"[Execution {progress}] Listing Id: {listingId} -> {endpoint}");

            var response = await _httpClient.PostAsJsonAsync("scan/product-checker", payload);
            var raw = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Done {progress}] Listing Id: {listingId} | HTTP {(int)response.StatusCode} | Raw Result: {raw ?? ""} via {endpoint}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Product checker API returned HTTP {(int)response.StatusCode} for listing {listingId} via {endpoint}. Response: {raw}");
            }

            var envelope = JsonSerializer.Deserialize<ScanEnvelope>(raw, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });

            var result = envelope?.Data?.FirstOrDefault();
            if (result == null)
            {
                Console.WriteLine($"Product checker API returned no result for listing {listingId} via {endpoint}. Response: {raw}");
            }

            TryUpdateListingStatus(result, listingId, endpoint);
            return result;
        }

        private static void TryUpdateListingStatus(ProductCheckerScanResponse result, long listingId, string endpoint)
        {
            try
            {
                UpdateListingStatus(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warn] Failed to persist Artemis listing status for listing {listingId} via {endpoint}: {ex.Message}");
            }
        }

        private static void UpdateListingStatus(ProductCheckerScanResponse result)
        {
            using (var db = new ArtemisDbContext())
            {
                var status = result.Availability ? Status.AVAILABLE : Status.NOT_AVAILABLE;
                var previousListingStatus = db.ListingStatus
                    .Where(item => item.ListingId == result.ListingId)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefault();
                Status? previousStatus = previousListingStatus?.Status;

                if (previousStatus == status)
                {
                    return;
                }

                var statusText = GetStatusText(status);
                var listingStatus = new ListingStatus
                {
                    ListingId = result.ListingId,
                    Status = status,
                    CheckedByProductChecker = 1
                };

                var generalHistory = new GeneralHistory
                {
                    UiType = "admin",
                    ListingId = (ulong)result.ListingId,
                    UserId = 1068,
                    RauserId = 0,
                    Action = previousStatus.HasValue ? "update" : "insert",
                    Field = "listing_status.status",
                    Value = statusText,
                    Text = previousStatus.HasValue
                        ? $"[Product Checker] Updated <b>Listing Status</b> from <b>{GetStatusText(previousStatus.Value)}</b> to <b>{statusText}</b>"
                        : $"[Product Checker] Added <b>Listing Status</b> with the value of <b>{statusText}</b>",
                    CreatedAt = DateTime.UtcNow.AddHours(8)
                };

                db.ListingStatus.Add(listingStatus);
                db.GeneralHistory.Add(generalHistory);
                db.SaveChanges();
            }
        }

        private static string GetStatusText(Status status)
        {
            return status == Status.NOT_AVAILABLE ? "NOT AVAILABLE" : "AVAILABLE";
        }
    }
}
