using ProductCheckerBack.ErrorLogging;
using ProductCheckerBack.Models.Logging;
using System.Linq;

namespace ProductCheckerBack
{
    internal class Logger
    {
        private static Tool? _tool;

        public static Tool Tool
        {
            get
            {
                if (_tool != null)
                {
                    return _tool;
                }

                using var db = new LoggingDbContext();
                _tool = db.Tools.First(t => t.Name == Configuration.GetToolName());
                return _tool;
            }
        }

        public static void Log(Payload payload, string message, string stackTrace)
        {
            using var db = new LoggingDbContext();
            db.Logs.Add(new ErrorLog()
            {
                ToolId = Tool.Id,
                Payload = payload,
                Message = message,
                StackTrace = stackTrace
            });
            db.SaveChanges();
        }
    }
}
