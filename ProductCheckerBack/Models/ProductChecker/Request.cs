using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductCheckerBack.Models.ProductChecker
{
    [Table("requests")]
    public class Request
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("request_info_id")]
        public int RequestInfoId { get; set; }

        [Column("status")]
        public RequestStatus Status { get; set; } = RequestStatus.PENDING;

        [Column("request_ended")]
        public DateTime? RequestEnded { get; set; }

        [Column("errors")]
        public IList<string>? Errors { get; set; }

        [Column("rescan_info_id")]
        public int? RescanInfoId { get; set; } 

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        [ForeignKey("RequestInfoId")]
        public virtual RequestInfo RequestInfo { get; set; }
    }

    public enum RequestStatus
    {
        PENDING,
        PROCESSING,
        SUCCESS,
        FAILED,
        COMPLETED_WITH_ISSUES
    }
}
