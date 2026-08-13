using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductCheckerBack.Models
{
    [Table("link_checker_helpers")]
    public class ApiEndpoint
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("key")]
        public string? Key { get; set; }

        [Column("value")]
        public string? Value { get; set; }
    }
}