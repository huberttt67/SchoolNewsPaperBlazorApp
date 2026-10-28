using System.ComponentModel.DataAnnotations;

namespace SchoolNewspaperBlazorApp.Data
{
    // Sala lekcyjna - lista sal działa tak samo jak lista nauczycieli.
    public class Room
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}