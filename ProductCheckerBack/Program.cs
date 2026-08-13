using ProductCheckerBack.Models;
using Microsoft.EntityFrameworkCore;
using ProductCheckerBack.ExecutionState;
using ProductCheckerBack.ProductCheckerState;
using ProductCheckerBack.ProductChecker.Api;
using ProductCheckerBack.ExecutionState.DefaultStateHandler;
using ProductCheckerBack.Artemis;

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
                    using (ArtemisDbContext db = new ArtemisDbContext())
                    {
                        var activeExecutions = db.Executions
                            .Include(r => r.ExecutionListings)
                            .ThenInclude(result => result.Listing)
                            .ThenInclude(listing => listing.Platform)
                            .Where(execution => execution.Status == ExecutionStatus.PENDING ||
                                             execution.Status == ExecutionStatus.PROCESSING)
                            .OrderBy(execution => execution.CreatedAt)
                            .ToList();

                        var nextExecutions = new List<Execution>();
                        var processingExecutions = activeExecutions
                            .Where(req => req.Status == ExecutionStatus.PROCESSING)
                            .OrderBy(req => req.CreatedAt)
                            .ToList();

                        if (processingExecutions.Count > 0)
                        {
                            nextExecutions = processingExecutions.Take(1).ToList();
                        }
                        else
                        {
                            nextExecutions = activeExecutions
                                .Where(req => req.Status == ExecutionStatus.PENDING)
                                .OrderBy(req => req.CreatedAt)
                                .Take(1)
                                .ToList();
                        }

                        var activeEndpoints = EndpointProvider.GetActiveEndpoints();

                        if (activeEndpoints.Count > 0)
                        {
                            foreach (var execution in nextExecutions)
                            {
                                ProductCheckerService productCheckerService = null;
                                try
                                {
                                    productCheckerService = new ProductCheckerService(execution, db);
                                    if (productCheckerService.GetAllExecutionListings().Count == 0)
                                    {
                                        productCheckerService.MarkAsCompletedWithIssues(["Execution Failed: No Listings Found"]);
                                        continue;
                                    }

                                    try
                                    {
                                        List<string> restarterUrls = [];
                                        using (var db1 = new ArtemisDbContext())
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
                                            await Task.Delay(10000);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    GetExecutionState(productCheckerService, db, execution).Process(productCheckerService);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(
                                        new ErrorLogging.Payload()
                                        {
                                            ExecutionId = execution.Id
                                        },
                                        String.Concat(ex.Message, "\n\n\n---- Inner Exception Message ----\n", ex.InnerException?.Message),
                                        String.Concat(ex.StackTrace, "\n\n\n---- Inner Exception Stack Trace ----\n", ex.InnerException?.StackTrace)
                                    );
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Server Sleeping... No active workers available.");
                            Thread.Sleep(Configuration.GetRefresh());
                        }

                        if (activeExecutions.Count == 0)
                        {
                            Console.WriteLine("Server Sleeping... No executions found.");
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
                    Console.WriteLine($"Server Sleeping... Error: {ex.InnerException?.Message ?? ex.Message}");
                    Thread.Sleep(Configuration.GetRefresh());
                }
            }
        }
     
        static IExecutionState GetExecutionState(ProductCheckerService productCheckerService, ArtemisDbContext artemisDbContext, Execution execution)
        {
            return execution.Status switch
            {
                ExecutionStatus.FAILED => new ErrorState(),
                ExecutionStatus.PROCESSING => new ProcessingState(artemisDbContext),
                _ => new SuccessState(artemisDbContext),
            };
        }
    }
}
