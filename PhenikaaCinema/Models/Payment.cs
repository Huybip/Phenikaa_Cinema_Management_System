using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int BookingId { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; }

        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [StringLength(100)]
        public string TransactionCode { get; set; }

        public DateTime? PaidAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        [ForeignKey(nameof(BookingId))]
        public virtual Booking Booking { get; set; }
    }
}