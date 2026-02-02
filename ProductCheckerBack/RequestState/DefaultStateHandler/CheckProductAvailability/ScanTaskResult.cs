using ProductCheckerBack.ProductChecker.Api.Response;

namespace ProductCheckerBack.RequestState.DefaultStateHandler
{
    internal sealed class ScanTaskResult
    {
        public int ListingDbId { get; }
        public string ListingId { get; }
        public string? CaseNumber { get; }
        public string Url { get; }
        public string Platform { get; }
        public ProductCheckerScanResponse? Status { get; }
        public string? Error { get; }
        public string? ErrorMessage { get; }

        public ScanTaskResult(
            int listingDbId,
            string listingId,
            string? caseNumber,
            string url,
            string platform,
            ProductCheckerScanResponse? status,
            string? error)
        {
            ListingDbId = listingDbId;
            ListingId = listingId;
            CaseNumber = caseNumber;
            Url = url;
            Platform = platform;
            Status = status;
            Error = error?.ToString();
            ErrorMessage = ScanErrorParser.TryParseErrorMessage(status?.ErrorDetails);
        }
    }
}
