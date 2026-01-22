using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProductCheckerBack.Models
{
    [Table("listing_status")]
    internal class ListingStatus
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        [JsonIgnore]
        public int Id { get; set; }

        [Column("listing_id")]
        public long ListingId { get; set; }

        [Column("status")]
        public Status Status { get; set; }

        [Column("checked_by_product_checker")]
        public int CheckedByProductChecker { get; set; }
    }

    enum Status
    {
        AVAILABLE,
        NOT_AVAILABLE
    }
}
