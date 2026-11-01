using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhenikaaCinema.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(30)]
        public string RoleName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        public virtual ICollection<User> Users { get; set; }
            = new HashSet<User>();
    }
}