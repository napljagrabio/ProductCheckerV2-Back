using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ProductCheckerBack.Models
{
    [Table("listings")]
    public class Listing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("campaign_id")]
        public long? CampaignId { get; set; }

        [Column("platform_id")]
        public int PlatformId { get; set; }

        [Column("case_id")]
        public int? CaseId { get; set; }

        [Column("url")]
        public string Url { get; set; } = string.Empty;

        [ForeignKey(nameof(PlatformId))]
        public virtual Platform Platform { get; set; } = null!;

        [ForeignKey(nameof(CaseId))]
        public virtual Case? Case { get; set; }
    }
}
