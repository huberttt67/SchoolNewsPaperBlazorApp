using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IAuthRepository
    {
        Task<User?> GetByLoginAsync(string login);

        Task<User?> GetByEmailAsync(string email);

        Task AddUserAsync(User user);

        Task UpdateUserAsync(User user);

        Task<User?> GetByIdAsync(int id);

        // Lista dla panelu użytkowników. Gdy viewerIsMaster == false, zapytanie SQL
        // pomija konto oglądającego i konta Master.
        Task<List<UserListItem>> GetUsersForViewerAsync(
            int viewerId,
            bool viewerIsMaster);

        Task<bool> IsMasterAsync(int userId);

        Task UpdateProfileAsync(
            int userId,
            string login,
            string email,
            string firstName,
            string lastName);

        Task SetPasswordHashAsync(int userId, string passwordHash);
    }
}