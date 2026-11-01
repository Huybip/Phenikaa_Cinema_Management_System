using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("Rooms")]
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        [StringLength(100)]
        public string RoomName { get; set; }

        public int Capacity { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public virtual ICollection<Seat> Seats { get; set; }
            = new HashSet<Seat>();

        public virtual ICollection<Showtime> Showtimes { get; set; }
            = new HashSet<Showtime>();
    }
}