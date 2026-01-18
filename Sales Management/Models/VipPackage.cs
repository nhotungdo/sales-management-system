using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sales_Management.Models
{
    public class VipPackage
    {
        [Key]
        public int VipPackageId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(50)]
        public string? Tag { get; set; } // e.g., "Basic", "Most Popular"

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        public int DurationMonth { get; set; } = 12;

        public double DiscountPercent { get; set; }

        public string? Features { get; set; } // Stored as JSON or delimiter-separated

        [StringLength(50)]
        public string? Icon { get; set; } // Bootstrap icon class

        [StringLength(20)]
        public string? ColorCode { get; set; } // Bootstrap color suffix: warning, info, etc.

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
