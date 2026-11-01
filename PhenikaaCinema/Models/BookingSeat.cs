using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Sockets;

namespace PhenikaaCinema.Models
{
    [Table("BookingSeats")]
    public class BookingSeat
    {
        [Key]
        public int BookingSeatId { get; set; }

        public int BookingId { get; set; }

        public int ShowtimeId { get; set; }

        public int SeatId { get; set; }

        public decimal SeatPrice { get; set; }

        public bool IsActiveReservation { get; set; }

        [Required]
        [StringLength(20)]
        public string ReservationStatus { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        [ForeignKey(nameof(BookingId))]
        public virtual Booking Booking { get; set; }

        [ForeignKey(nameof(ShowtimeId))]
        public virtual Showtime Showtime { get; set; }

        [ForeignKey(nameof(SeatId))]
        public virtual Seat Seat { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }
            = new HashSet<Ticket>();
    }
}