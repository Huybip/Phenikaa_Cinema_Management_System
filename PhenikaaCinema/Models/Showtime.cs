using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("Showtimes")]
    public class Showtime
    {
        [Key]
        public int ShowtimeId { get; set; }

        public int MovieId { get; set; }

        public int RoomId { get; set; }

        public DateTime StartTimeUtc { get; set; }

        public DateTime EndTimeUtc { get; set; }

        public decimal BasePrice { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        [ForeignKey(nameof(MovieId))]
        public virtual Movie Movie { get; set; }

        [ForeignKey(nameof(RoomId))]
        public virtual Room Room { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; }
            = new HashSet<Booking>();

        public virtual ICollection<BookingSeat> BookingSeats { get; set; }
            = new HashSet<BookingSeat>();
    }
}