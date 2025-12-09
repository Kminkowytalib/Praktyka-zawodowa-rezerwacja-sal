using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rezarwacja_Sal.Models
{
    public enum ReservationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3
    }

    public class Reservation : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Wybierz salę.")]
        [Display(Name = "Sala")]
        public int RoomId { get; set; }


        public Room? Room { get; set; }

        [Required(ErrorMessage = "Podaj datę rozpoczęcia.")]
        [Display(Name = "Początek")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "Podaj datę zakończenia.")]
        [Display(Name = "Koniec")]
        public DateTime EndAt { get; set; }

        [Required(ErrorMessage = "Podaj tytuł.")]
        [StringLength(200, ErrorMessage = "Tytuł może mieć maksymalnie 200 znaków.")]
        [Display(Name = "Tytuł")]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000, ErrorMessage = "Notatki mogą mieć maksymalnie 4000 znaków.")]
        [Display(Name = "Notatki")]
        public string? Notes { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartAt >= EndAt)
            {
                yield return new ValidationResult("Data rozpoczęcia musi być wcześniejsza niż data zakończenia.", new[] { nameof(StartAt), nameof(EndAt) });
            }

           
            if (StartAt < DateTime.UtcNow.AddMinutes(-1))
            {
                yield return new ValidationResult("Rezerwacje powinny dotyczyć czasu w przyszłości.", new[] { nameof(StartAt) });
            }
        }
    }
}
