using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductCheckerBack.ErrorLogging
{
    internal class Payload
    {
        public long CrawlQueryId { get; set; }
        public long ProductCheckerRequestId { get; set; }
        public long ListingId { get; set; }
        public long SourceUrlId { get; set; }
        public long? SourceUrlJobId {  get; set; }
        public long? SourceUrlResultId { get; set; }
    }
}
