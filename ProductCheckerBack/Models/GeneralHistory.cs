using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductCheckerBack.Models
{
    [Table("general_history")]
    [PrimaryKey(nameof(Id), nameof(CreatedAt))]
    internal class GeneralHistory
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public ulong Id { get; set; }

        [Column("ui_type")]
        [MaxLength(10)]
        public string UiType { get; set; } = string.Empty;

        [Column("qflag_id")]
        public uint? QflagId { get; set; }

        [Column("listing_id")]
        public ulong? ListingId { get; set; }

        [Column("seller_id")]
        public ulong? SellerId { get; set; }

        [Column("campaign_id")]
        public uint? CampaignId { get; set; }

        [Column("seller_account_id")]
        public ulong? SellerAccountId { get; set; }

        [Column("lawfirm_id")]
        public uint? LawfirmId { get; set; }

        [Column("case_id")]
        public uint? CaseId { get; set; }

        [Column("user_id")]
        public uint? UserId { get; set; }

        [Column("rauser_id")]
        public uint? RauserId { get; set; }

        [Column("action")]
        [MaxLength(255)]
        public string Action { get; set; } = string.Empty;

        [Column("field")]
        [MaxLength(255)]
        public string Field { get; set; } = string.Empty;

        [Column("value", TypeName = "text")]
        public string? Value { get; set; }

        [Column("text", TypeName = "text")]
        public string? Text { get; set; }

        [Column("created_at", TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }
    }
}
