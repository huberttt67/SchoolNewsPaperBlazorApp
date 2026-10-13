using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;
using static SchoolNewspaperBlazorApp.Interfaces.Repository.AuthRepository;

namespace SchoolNewspaperBlazorApp.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _AuthRepository;
        public AuthService(IAuthRepository AuthRepository)
        {
            _AuthRepository = AuthRepository;
        }

        public Task LoginAsync(string login, string email, string haslo)
        {
            throw new NotImplementedException();
        }
        public async Task AddUserAsync(string login, string password, string email)
        {
            var user = new User
            {
                Login = login,
                Haslo = password,
                Email = email
            };
            
            await _AuthRepository.AddUserAsync(user);
        }
    }
}

