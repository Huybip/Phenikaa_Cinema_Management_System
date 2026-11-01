using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("Tickets")]
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        public int BookingSeatId { get; set; }

        [Required]
        [StringLength(50)]
        public string TicketCode { get; set; }

        public DateTime IssuedAtUtc { get; set; }

        public bool IsUsed { get; set; }

        [ForeignKey(nameof(BookingSeatId))]
        public virtual BookingSeat BookingSeat { get; set; }
    }
}