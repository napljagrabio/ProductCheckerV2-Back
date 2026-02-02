using System.Text.Json;

namespace ProductCheckerBack.RequestState.DefaultStateHandler
{ 
    internal static class ScanErrorParser
    {
        public static string? TryParseErrorMessage(string? errorDetails)
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
    }
}
