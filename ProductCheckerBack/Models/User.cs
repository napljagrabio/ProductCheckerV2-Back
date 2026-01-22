using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductCheckerBack.Models
{
    [Table("users")]
    internal class User
    {
        internal enum AccessLevelOption
        {
            Researcher = 1,
            QA,
            Lawyer,
            Admin,
            CampaignLead,
            Client,
            SuperAdmin
        }

        [NotMapped]
        public static AccessLevelOption[] INTERNAL_USERS = [AccessLevelOption.Researcher, AccessLevelOption.QA, AccessLevelOption.Admin, AccessLevelOption.CampaignLead, AccessLevelOption.SuperAdmin];
        [NotMapped]
        public static AccessLevelOption[] EXTERNAL_USERS = [AccessLevelOption.Lawyer, AccessLevelOption.Client];

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("first_name")]
        public string FirstName { get; set; }
        [Column("last_name")]
        public string LastName { get; set; }
        [Column("user_id")]
        public string UserId { get; set; }
        [Column("status")]
        public int Status { get; set; }
        [Column("access_level")]
        public AccessLevelOption AccessLevel { get; set; }
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        public bool IsInternal()
        {
            return INTERNAL_USERS.Contains(AccessLevel);
        }
    }
}
