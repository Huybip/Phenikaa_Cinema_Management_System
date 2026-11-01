using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("BookingPromotions")]
    public class BookingPromotion
    {
        public int BookingId { get; set; }

        public int PromotionId { get; set; }

        public decimal DiscountAmount { get; set; }

        [ForeignKey(nameof(BookingId))]
        public virtual Booking Booking { get; set; }

        [ForeignKey(nameof(PromotionId))]
        public virtual Promotion Promotion { get; set; }
    }
}