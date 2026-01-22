using System.Text.Json.Serialization;

namespace ProductCheckerBack.ProductChecker.Api.Response
{
    internal class ProductCheckerScanResponse
    {
        [JsonPropertyName("listing_id")]
        public long ListingId { get; set; }

        [JsonPropertyName("case_number")]
        public string CaseNumber { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("availability")]
        public bool Availability { get; set; }

        [JsonPropertyName("date_checked")]
        public string DateChecked { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("error_details")]
        public string ErrorDetails { get; set; }
    }
}
