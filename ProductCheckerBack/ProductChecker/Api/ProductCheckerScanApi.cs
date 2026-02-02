using ProductCheckerBack.Models;
using ProductCheckerBack.ProductChecker.Api.Response;
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

        public async Task<ProductCheckerScanResponse> Scan(object payload, string listing, string progress, string endpoint)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("scan/product-checker", payload);
                var raw = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Done {progress}] Listing Id: {listing} | Raw Result: {raw ?? ""} via {endpoint}");
                var envelope = JsonSerializer.Deserialize<ScanEnvelope>(raw, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
                var result = envelope?.Data?.FirstOrDefault();

                if (result != null)
                {
                    UpdateListingStatus(result);
                }

                return result;
            }
            catch (Exception)
            {
                return null;
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
