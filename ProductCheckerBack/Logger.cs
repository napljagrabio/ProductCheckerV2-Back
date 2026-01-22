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
        public static Tool Tool { get; set; }

        static void InitializeTool()
        {
            if (Tool == null)
            {
                LoggingDbContext db = new LoggingDbContext();
                Tool = db.Tools.First(tool => tool.Name == Configuration.GetToolName());
            }
        }

        public static void Log(Payload payload, string message, string stackTrace)
        {
            InitializeTool();
            LoggingDbContext db = new LoggingDbContext();
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
