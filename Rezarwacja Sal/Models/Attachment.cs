using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rezarwacja_Sal.Models
{
    public class Attachment
    {
        public int Id { get; set; }

        [Required]
        public int ReservationId { get; set; }

        [Required]
        [StringLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(260)]
        public string StoredFileName { get; set; } = string.Empty; 

        [Required]
        [StringLength(200)]
        public string ContentType { get; set; } = "application/octet-stream";

        public long SizeBytes { get; set; }

        [StringLength(450)]
        public string? UploadedByUserId { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

      
        [StringLength(500)]
        public string RelativePath { get; set; } = string.Empty; 
    }
}
