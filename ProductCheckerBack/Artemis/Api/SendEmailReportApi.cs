using System.Net.Http.Json;

namespace ProductCheckerBack.Artemis.Api
{
    internal class SendEmailReportApi
    {
        private HttpClient _httpClient { get; set; }

        public SendEmailReportApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<HttpResponseMessage> Execute(long executionId)
        {
            var requestUri = new Uri($"{_httpClient.BaseAddress.AbsoluteUri.TrimEnd('/')}/api/link-checker/send-report");
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
            var payload = new { execution_id = executionId };

            httpRequestMessage.Content = JsonContent.Create(payload);

            return _httpClient.SendAsync(httpRequestMessage);
        }
    }
}
