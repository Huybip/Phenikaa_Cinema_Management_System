using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("Movies")]
    public class Movie
    {
        [Key]
        public int MovieId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public int DurationMinutes { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(500)]
        public string PosterUrl { get; set; }

        [StringLength(500)]
        public string TrailerUrl { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<Showtime> Showtimes { get; set; }
            = new HashSet<Showtime>();

        public virtual ICollection<MovieGenre> MovieGenres { get; set; }
            = new HashSet<MovieGenre>();
    }
}