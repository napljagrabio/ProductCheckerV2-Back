using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductCheckerBack.Models
{
    [Table("cases")]
    public class Case
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("case_number")]
        public string? CaseNumber { get; set; }
    }
}