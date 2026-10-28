using System.ComponentModel.DataAnnotations;

namespace SchoolNewspaperBlazorApp.Data
{
    public class Substitute
    {
        public int Id { get; set; }

        [Required]
        public DateTime SubstituteDate { get; set; } = DateTime.Today;

        [Range(1, 8, ErrorMessage = "Numer lekcji musi być od 1 do 8.")]
        public int LessonNumber { get; set; } = 1;

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        // Nauczyciel prowadzący zastępstwo.
        public int TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        // Nauczyciel, za którego odbywa się zastępstwo.
        public int? OriginalTeacherId { get; set; }
        public Teacher? OriginalTeacher { get; set; }

        // Sala, w której odbywa się zastępstwo (opcjonalnie).
        public int? RoomId { get; set; }
        public Room? Room { get; set; }

        // Klasa, której dotyczy zastępstwo, np. 3A.
        [Required]
        [MaxLength(20)]
        public string ClassName { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}