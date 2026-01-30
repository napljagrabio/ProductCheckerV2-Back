using ProductCheckerBack.ErrorLogging;
using ProductCheckerBack.Models.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductCheckerBack
{
    internal class Logger
    {
        private static readonly Dictionary<string, Tool> ToolsByEnvironment = new Dictionary<string, Tool>(StringComparer.OrdinalIgnoreCase);
        public static Tool Tool { get; private set; }

        private static Tool GetToolForCurrentEnvironment()
        {
            var environment = Configuration.GetCurrentEnvironment();
            if (ToolsByEnvironment.TryGetValue(environment, out var tool))
            {
                Tool = tool;
                return tool;
            }

            using var db = new LoggingDbContext();
            tool = db.Tools.First(t => t.Name == Configuration.GetToolName());
            ToolsByEnvironment[environment] = tool;
            Tool = tool;
            return tool;
        }

        public static void Log(Payload payload, string message, string stackTrace)
        {
            var tool = GetToolForCurrentEnvironment();
            using var db = new LoggingDbContext();
            db.Logs.Add(new ErrorLog()
            {
                ToolId = tool.Id,
                Payload = payload,
                Message = message,
                StackTrace = stackTrace
            });
            db.SaveChanges();
        }
    }
}
