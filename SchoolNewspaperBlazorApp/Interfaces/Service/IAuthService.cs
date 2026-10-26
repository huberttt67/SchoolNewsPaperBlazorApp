using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IAuthService
    {
        Task AddUserAsync(string login, string password, string email);

        Task<bool> LoginAsync(string email, string password);

        Task<List<UserListItem>> GetUsersAsync();

        Task UpdateUserProfileAsync(
            int userId,
            string login,
            string email,
            string firstName,
            string lastName);

        Task ChangeUserPasswordAsync(
            int userId,
            string currentPassword,
            string newPassword);

        // Administrator (ManageUsers) ustawia hasło TYMCZASOWE istniejącemu kontu.
        // Użytkownik musi je zmienić przy następnym logowaniu.
        Task SetTemporaryPasswordAsync(int userId, string temporaryPassword);

        // Wymuszona zmiana hasła tymczasowego po pierwszym logowaniu.
        // Zwraca null przy sukcesie albo kod błędu:
        // "current" (błędne hasło tymczasowe), "short" (za krótkie), "same" (takie samo).
        Task<string?> ChangeTemporaryPasswordAsync(
            int userId,
            string currentPassword,
            string newPassword);
    }
}