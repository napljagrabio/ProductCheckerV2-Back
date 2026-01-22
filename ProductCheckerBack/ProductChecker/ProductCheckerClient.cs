using System;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using ProductCheckerBack.ProductChecker.Api;

namespace ProductCheckerBack.ProductChecker
{
    internal class ProductCheckerClient
    {
        private static readonly ConcurrentDictionary<string, ProductCheckerClient> _instances = new();
        private readonly HttpClient _httpClient;

        public ProductCheckerScanApi ProductCheckerScanApi { get; private set; }

        private ProductCheckerClient(string api)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(api)
            };
            ProductCheckerScanApi = new ProductCheckerScanApi(_httpClient);
        }

        public static ProductCheckerClient ForApi(string api)
        {
            if (string.IsNullOrWhiteSpace(api))
                throw new ArgumentException("API base URL is required.", nameof(api));

            return _instances.GetOrAdd(api, key => new ProductCheckerClient(key));
        }
    }
}
