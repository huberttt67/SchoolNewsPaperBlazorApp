
using Microsoft.AspNetCore.Identity;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        // Rejestracja użytkownika
        public async Task AddUserAsync(
            string login,
            string password,
            string email)
        {
            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Login, hasło i email są wymagane.");
            }

            login = login.Trim();
            email = email.Trim();

            var existingUser =
                await _authRepository.GetByEmailAsync(email);

            if (existingUser != null)
            {
                throw new InvalidOperationException(
                    "Konto z tym adresem email już istnieje.");
            }

            var existingLogin =
                await _authRepository.GetByLoginAsync(login);

            if (existingLogin != null)
            {
                throw new InvalidOperationException(
                    "Ten login jest już zajęty.");
            }

            var user = new User
            {
                Login = login,
                Email = email,
                Haslo = string.Empty
            };

            // Zapisujemy hash, nigdy nowe hasło w zwykłym tekście.
            user.Haslo = _passwordHasher.HashPassword(
                user,
                password);

            await _authRepository.AddUserAsync(user);
        }

        // Logowanie użytkownika
        public async Task<bool> LoginAsync(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrEmpty(password))
            {
                return false;
            }

            var user = await _authRepository.GetByEmailAsync(
                email.Trim());

            if (user == null || string.IsNullOrEmpty(user.Haslo))
            {
                return false;
            }

            // Sprawdzamy, czy hasło jest zapisanym hashem.
            PasswordVerificationResult result;

            try
            {
                result = _passwordHasher.VerifyHashedPassword(
                    user,
                    user.Haslo,
                    password);
            }
            catch (FormatException)
            {
                // Starsze hasło może być zapisane zwykłym tekstem.
                result = PasswordVerificationResult.Failed;
            }

            if (result == PasswordVerificationResult.Success ||
                result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.Haslo = _passwordHasher.HashPassword(
                        user,
                        password);

                    await _authRepository.UpdateUserAsync(user);
                }

                return true;
            }

            
            if (user.Haslo == password)
            {
                user.Haslo = _passwordHasher.HashPassword(
                    user,
                    password);

                await _authRepository.UpdateUserAsync(user);

                return true;
            }

            return false;
        }
    }
}
