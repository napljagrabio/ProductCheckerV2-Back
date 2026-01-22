using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductCheckerBack.Models.ProductChecker
{
    [Table("request_infos")]
    public class RequestInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("user")]
        [MaxLength(255)]
        public string User { get; set; }

        [Column("file_name")]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Request> Requests { get; set; }

        // Add this navigation property
        public virtual ICollection<ProductListings> ProductListings { get; set; }
    }
}