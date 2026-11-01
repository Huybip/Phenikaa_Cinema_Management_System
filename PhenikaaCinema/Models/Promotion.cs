using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("Promotions")]
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; }

        [Required]
        [StringLength(30)]
        public string PromotionCode { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; }

        public decimal DiscountValue { get; set; }

        public DateTime StartAtUtc { get; set; }

        public DateTime EndAtUtc { get; set; }

        public int? MaxUsage { get; set; }

        public int UsedCount { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<BookingPromotion> BookingPromotions { get; set; }
            = new HashSet<BookingPromotion>();
    }
}