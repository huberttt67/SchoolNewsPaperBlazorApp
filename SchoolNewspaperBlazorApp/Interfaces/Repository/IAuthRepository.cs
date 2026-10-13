using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public class AuthRepository
    {
        public interface IAuthRepository
        {
            Task<User?> GetByLoginAsync(string login);
            Task AddUserAsync(User user);



        }
    }
}
