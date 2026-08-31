using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductCheckerBack.Models
{
    [Table("link_checker_schedule_execution_results")]
    public class ExecutionListing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public long Id { get; set; }

        [Column("link_checker_schedule_execution_id")]
        public long ExecutionId { get; set; }

        [Column("listing_id")]
        public long ListingId { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string UrlStatus { get; set; }

        [Column("checked_date")]
        [MaxLength(50)]
        public string CheckedDate { get; set; }

        [Column("error_detail")]
        public string ErrorDetail { get; set; }

        [Column("note")]
        [MaxLength(255)]
        public string Note { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(8);

        [ForeignKey("ExecutionId")]
        public virtual Execution Execution { get; set; }

        [ForeignKey(nameof(ListingId))]
        public virtual Listing Listing { get; set; }

        [NotMapped]
        public string? CaseNumber => Listing?.Case?.CaseNumber;

        [NotMapped]
        public string Url => Listing?.Url ?? string.Empty;

        [NotMapped]
        public string Platform => Listing?.Platform?.Name ?? "Unknown Platform";
    }
}
