using System.Collections.Generic;
using System.Linq;

namespace ProductCheckerBack.RequestState.DefaultStateHandler
{
    internal static class EndpointProvider
    {
        public static List<string> GetActiveEndpoints()
        {
            using var db = new ProductCheckerDbContext();
            return db.Ports
                .Where(port => port.Status == 1 && !string.IsNullOrWhiteSpace(port.Api))
                .Select(port => port.Api!.Trim())
                .ToList();
        }
    }
}
