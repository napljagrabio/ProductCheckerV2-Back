using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductCheckerBack.Models
{
    [Table("link_checker_schedule_executions")]
    public class Execution
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public long Id { get; set; }

        [Column("link_checker_schedule_id")]
        public long ScheduleId { get; set; }

        [Column("status")]
        public ExecutionStatus Status { get; set; } = ExecutionStatus.PENDING;

        [Column("execution_started")]
        public DateTime? ExecutionStarted { get; set; }

        [Column("execution_ended")]
        public DateTime? ExecutionEnded { get; set; }

        [Column("errors")]
        public IList<string>? Errors { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<ExecutionListing> ExecutionListings { get; set; } = new List<ExecutionListing>();
    }

    public enum ExecutionStatus
    {
        PENDING,
        PROCESSING,
        SUCCESS,
        FAILED,
        COMPLETED_WITH_ISSUES
    }
}
