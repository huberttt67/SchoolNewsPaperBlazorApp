using SchoolNewspaperBlazorApp.Data;
using System.Threading.Tasks;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IAuthRepository
    {
        Task<User?> GetByLoginAsync(string login);
        Task<User?> GetByEmailAsync(string email);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
    }
}