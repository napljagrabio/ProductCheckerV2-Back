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
            Console.WriteLine($"[Request {progress}] Listing Id: {listingId} -> {endpoint}");

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
                var listingStatus = new ListingStatus
                {
                    ListingId = result.ListingId,
                    Status = result.Availability ? Status.AVAILABLE : Status.NOT_AVAILABLE,
                    CheckedByProductChecker = 1
                };
                db.ListingStatus.Add(listingStatus);
                db.SaveChanges();
            }
        }
    }
}
