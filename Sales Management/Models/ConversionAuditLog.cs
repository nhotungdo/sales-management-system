using System;
using System.ComponentModel.DataAnnotations;

namespace Sales_Management.Models
{
    public class ConversionAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public decimal VndAmount { get; set; }

        [Required]
        public decimal CentsAmount { get; set; }

        public decimal ConversionRate { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string IpAddress { get; set; }

        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }
    }
}
