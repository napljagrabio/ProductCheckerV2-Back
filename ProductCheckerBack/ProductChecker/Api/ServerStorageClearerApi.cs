using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProductCheckerBack.ProductChecker.Api
{
    internal class ServerStorageClearerApi
    {
        private readonly HttpClient _httpClient;

        public ServerStorageClearerApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task ClearStorage(string api)
        {
            if (string.IsNullOrWhiteSpace(api))
            {
                throw new ArgumentException("API base URL must be provided.", nameof(api));
            }

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(api)
            };

            using var response = await _httpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
        }
    }
}
