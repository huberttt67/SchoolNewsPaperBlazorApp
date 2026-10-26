using System.ComponentModel.DataAnnotations;

namespace SchoolNewspaperBlazorApp.Data
{
    public class User
    {
        public int Id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string Haslo { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        // Soft Delete: false = aktywny, true = usunięty logicznie.
        public bool IsDeleted { get; set; }

        // true = hasło jest tymczasowe (nadane przez administratora)
        // i trzeba je zmienić zaraz po zalogowaniu.
        public bool MustChangePassword { get; set; }

        public ICollection<UserPermission> UserPermissions { get; set; }
            = new List<UserPermission>();
    }
}