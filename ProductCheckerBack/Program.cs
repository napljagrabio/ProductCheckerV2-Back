using ProductCheckerBack.Models;
using Microsoft.EntityFrameworkCore;
using ProductCheckerBack.Models.ProductChecker;
using ProductCheckerBack.RequestState;
using ProductCheckerBack.ProductCheckerState;
using ProductCheckerBack.ProductChecker.Api;

namespace ProductCheckerBack
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            while (true)
            {
                try
                {
                    using (ProductCheckerV2DbContext db = new ProductCheckerV2DbContext())
                    {
                        var activeRequests = db.Requests
                            .Include(r => r.RequestInfo)
                            .ThenInclude(ri => ri.ProductListings)
                            .Where(request => request.Status == RequestStatus.PENDING ||
                                             request.Status == RequestStatus.PROCESSING)
                            .ToList();

                        foreach (var request in activeRequests)
                        {
                            ProductCheckerService productCheckerService = null;
                            try
                            {
                                productCheckerService = new ProductCheckerService(request, db);
                                if (productCheckerService.GetAllProductListings().Count == 0)
                                {
                                    productCheckerService.MarkAsCompletedWithIssues(["Request Failed: No Listings Found"]);
                                    continue;
                                }

                                try
                                {
                                    List<string> restarterUrls = [];
                                    using (var db1 = new ProductCheckerDbContext())
                                    {
                                        restarterUrls = db1.ApiEndpoints
                                            .Where(s => s.Key == "server_restarter_url")
                                            .Select(s => s.Value)
                                            .Where(value => !string.IsNullOrWhiteSpace(value))
                                            .Select(value => value!.Trim())
                                            .ToList();
                                    }

                                    if (restarterUrls.Count > 0)
                                    {
                                        using var restarterHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
                                        var restarterApi = new ServerRestarterApi(restarterHttpClient);
                                        var restartTasks = restarterUrls
                                            .Select(url => restarterApi.Restart(url))
                                            .ToArray();
                                        await Task.WhenAll(restartTasks).ConfigureAwait(false);
                                    }
                                }
                                catch (Exception ex)
                                {
                                }
                                GetRequestState(productCheckerService, db, request).Process(productCheckerService);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(
                                    new ErrorLogging.Payload()
                                    {
                                        ProductCheckerRequestId = request.Id
                                    },
                                    String.Concat(ex.Message, "\n\n\n---- Inner Exception Message ----\n", ex.InnerException?.Message),
                                    String.Concat(ex.StackTrace, "\n\n\n---- Inner Exception Stack Trace ----\n", ex.InnerException?.StackTrace)
                                );
                            }
                        }

                        if (activeRequests.Count == 0)
                        {
                            Thread.Sleep(Configuration.GetRefresh());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(
                        null,
                        String.Concat(ex.Message, "\n\n\n---- Inner Exception Message ----\n", ex.InnerException?.Message),
                        String.Concat(ex.StackTrace, "\n\n\n---- Inner Exception Stack Trace ----\n", ex.InnerException?.StackTrace)
                    );
                    Thread.Sleep(Configuration.GetRefresh());
                }
            }
        }
     
        static IRequestState GetRequestState(ProductCheckerService productCheckerService, ProductCheckerV2DbContext productCheckerV2DbContext, Request request)
        {
            return request.Status switch
            {
                RequestStatus.FAILED => new ErrorState(),
                RequestStatus.PROCESSING => new ProcessingState(productCheckerV2DbContext),
                _ => new SuccessState(productCheckerV2DbContext),
            };
        }
    }
}
