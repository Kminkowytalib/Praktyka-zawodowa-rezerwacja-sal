using System.ComponentModel.DataAnnotations;

namespace Rezarwacja_Sal.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 100000)]
        public int Capacity { get; set; }

        [StringLength(300)]
        public string? Location { get; set; }

        [StringLength(2000)]
        public string? Equipment { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
