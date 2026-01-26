using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProductCheckerBack.ProductChecker.Api
{
    internal class ServerRestarterApi
    {
        private readonly HttpClient _httpClient;

        public ServerRestarterApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task Restart(string api)
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
