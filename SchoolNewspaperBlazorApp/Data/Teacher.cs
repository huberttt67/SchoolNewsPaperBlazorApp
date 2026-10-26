using System.ComponentModel.DataAnnotations;

namespace SchoolNewspaperBlazorApp.Data
{
    public class Teacher
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Subject { get; set; }

        public bool IsDeleted { get; set; }
    }
}