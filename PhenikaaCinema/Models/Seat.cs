using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("Seats")]
    public class Seat
    {
        [Key]
        public int SeatId { get; set; }

        public int RoomId { get; set; }

        [Required]
        [StringLength(5)]
        public string RowLabel { get; set; }

        public int SeatNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string SeatType { get; set; }

        public decimal ExtraPrice { get; set; }

        public bool IsActive { get; set; }

        [ForeignKey(nameof(RoomId))]
        public virtual Room Room { get; set; }

        public virtual ICollection<BookingSeat> BookingSeats { get; set; }
            = new HashSet<BookingSeat>();
    }
}