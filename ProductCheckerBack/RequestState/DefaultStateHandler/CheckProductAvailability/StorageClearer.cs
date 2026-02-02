using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ProductCheckerBack.ProductChecker.Api;

namespace ProductCheckerBack.RequestState.DefaultStateHandler
{
    internal sealed class StorageClearer
    {
        private static readonly SemaphoreSlim ClearStorageGate = new SemaphoreSlim(1, 1);

        public async Task TryClearStorageAsync(List<string> errors)
        {
            ServerStorageClearerApi? storageClearerApi = null;
            HttpClient? storageHttpClient = null;
            List<string> storageClearerUrls = [];

            using (var db = new ProductCheckerDbContext())
            {
                storageClearerUrls = db.ApiEndpoints
                    .Where(s => s.Key == "server_storage_clearer_url")
                    .Select(s => s.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!.Trim())
                    .ToList();
            }

            if (storageClearerUrls.Count > 0)
            {
                storageHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
                storageClearerApi = new ServerStorageClearerApi(storageHttpClient);
            }

            if (storageClearerApi == null || storageClearerUrls.Count == 0)
            {
                return;
            }

            await ClearStorageGate.WaitAsync().ConfigureAwait(false);
            try
            {
                var clearTasks = storageClearerUrls
                    .Select(url => storageClearerApi.ClearStorage(url))
                    .ToArray();
                await Task.WhenAll(clearTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lock (errors)
                {
                    errors.Add($"Storage clear failed: {ex.Message}");
                }
            }
            finally
            {
                ClearStorageGate.Release();
                storageHttpClient?.Dispose();
            }
        }
    }
}
