using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductCheckerBack.Models
{
    [Table("link_checker_workers")]
    public class Port
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("api")]
        public string? Api { get; set; }

        [Column("status")]
        public int Status { get; set; }
    }
}