using ProductCheckerBack.ErrorLogging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductCheckerBack.Models.Logging
{
    [Table("error_logs")]
    internal class ErrorLog
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("tool_id")]
        public int ToolId { get; set; }
        public Payload Payload { get; set; }
        public string Message { get; set; }
        [Column("stack_trace")]
        public string StackTrace { get; set; }
    }
}
