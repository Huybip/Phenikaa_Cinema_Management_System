using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("Bookings")]
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        [StringLength(30)]
        public string BookingCode { get; set; }

        public int? CustomerUserId { get; set; }

        public int CreatedByUserId { get; set; }

        public int ShowtimeId { get; set; }

        public DateTime BookingTimeUtc { get; set; }

        public DateTime? HoldExpiresAtUtc { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal? FinalAmount { get; set; }

        [ForeignKey(nameof(CustomerUserId))]
        public virtual User Customer { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public virtual User CreatedBy { get; set; }

        [ForeignKey(nameof(ShowtimeId))]
        public virtual Showtime Showtime { get; set; }

        public virtual ICollection<BookingSeat> BookingSeats { get; set; }
            = new HashSet<BookingSeat>();

        public virtual ICollection<Payment> Payments { get; set; }
            = new HashSet<Payment>();

        public virtual ICollection<BookingPromotion> BookingPromotions { get; set; }
            = new HashSet<BookingPromotion>();
    }
}