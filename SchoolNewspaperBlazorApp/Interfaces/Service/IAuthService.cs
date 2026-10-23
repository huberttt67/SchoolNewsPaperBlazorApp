
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
    }
}
