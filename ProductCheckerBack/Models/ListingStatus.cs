using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProductCheckerBack.Models
{
    [Table("listing_status")]
    internal class ListingStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        [JsonIgnore]
        public ulong Id { get; set; }

        [Column("listing_id")]
        public long ListingId { get; set; }

        [Column("status")]
        public Status Status { get; set; }

        [Column("checked_by_product_checker")]
        public int CheckedByProductChecker { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(8);
    }

    enum Status
    {
        AVAILABLE,
        NOT_AVAILABLE
    }
}
