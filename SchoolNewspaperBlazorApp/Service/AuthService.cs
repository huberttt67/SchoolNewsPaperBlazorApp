
using Microsoft.AspNetCore.Identity;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<AuthService> _logger;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthService(
            IAuthRepository authRepository,
            IPermissionService permissionService,
            ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _permissionService = permissionService;
            _logger = logger;
        }

        // Rejestracja użytkownika
        public async Task AddUserAsync(
            string login,
            string password,
            string email)
        {
            await _permissionService.RequirePermissionAsync(
                EnumPermission.ManageUsers);

            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Login, hasło i email są wymagane.");
            }

            login = login.Trim();
            email = email.Trim().ToLowerInvariant();

            if (!System.Net.Mail.MailAddress.TryCreate(email, out var parsed)
                || !string.Equals(
                    parsed.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Podaj poprawny adres email.");
            }

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

            // Nowe hasło zapisujemy wyłącznie jako hash.
            user.Haslo = _passwordHasher.HashPassword(
                user,
                password);

            await _authRepository.AddUserAsync(user);

            _logger.LogInformation(
                "Utworzono konto użytkownika {UserId}.",
                user.Id);
        }

        // Logowanie użytkownika
        public async Task<bool> LoginAsync(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrEmpty(password))
            {
                _logger.LogWarning(
                    "Odrzucono próbę logowania z pustymi danymi.");

                return false;
            }

            var user = await _authRepository.GetByEmailAsync(
                email.Trim());

            if (user == null || string.IsNullOrEmpty(user.Haslo))
            {
                _logger.LogWarning(
                    "Nieudane logowanie: nie znaleziono aktywnego konta.");

                return false;
            }

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
                _logger.LogWarning(
                    "Konto {UserId} ma hasło w starszym formacie – użyto weryfikacji zastępczej.",
                    user.Id);

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

                    _logger.LogInformation(
                        "Zaktualizowano format hasha hasła konta {UserId}.",
                        user.Id);
                }

                _logger.LogInformation(
                    "Użytkownik {UserId} zalogował się.",
                    user.Id);

                return true;
            }

            // Obsługa starszych kont z hasłem zapisanym jako zwykły tekst.
            if (user.Haslo == password)
            {
                user.Haslo = _passwordHasher.HashPassword(
                    user,
                    password);

                await _authRepository.UpdateUserAsync(user);

                _logger.LogInformation(
                    "Użytkownik {UserId} zalogował się; hasło ze starszego formatu zostało zahaszowane.",
                    user.Id);

                return true;
            }

            _logger.LogWarning(
                "Nieudane logowanie użytkownika {UserId}: błędne hasło.",
                user.Id);

            return false;
        }

        // ---------- Panel użytkowników ----------

        public async Task<List<UserListItem>> GetUsersAsync()
        {
            await _permissionService.RequirePermissionAsync(
                EnumPermission.ViewUsers,
                EnumPermission.ManageUsers);

            var viewerId =
                await _permissionService.GetCurrentUserIdAsync();

            var viewerIsMaster =
                await _permissionService.IsCurrentUserMasterAsync();

            var canManage =
                await _permissionService.HasPermissionAsync(
                    EnumPermission.ManageUsers);

            // Poza Masterem: bez własnego konta i bez kont Master.
            var users = await _authRepository.GetUsersForViewerAsync(
                viewerId,
                viewerIsMaster);

            foreach (var item in users)
            {
                item.CanEdit = canManage && IsEditAllowed(
                    viewerIsMaster,
                    viewerId,
                    item.Id,
                    item.IsMaster);
            }

            return users;
        }

        public async Task UpdateUserProfileAsync(
            int userId,
            string login,
            string email,
            string firstName,
            string lastName)
        {
            await AuthorizeManageAsync(userId);

            login = (login ?? string.Empty).Trim();
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            firstName = (firstName ?? string.Empty).Trim();
            lastName = (lastName ?? string.Empty).Trim();

            if (login.Length == 0 || email.Length == 0)
            {
                throw new ArgumentException(
                    "Login i email są wymagane.");
            }

            if (login.Length > 100 ||
                firstName.Length > 100 ||
                lastName.Length > 100)
            {
                throw new ArgumentException(
                    "Login, imię i nazwisko mogą mieć maksymalnie 100 znaków.");
            }

            if (!System.Net.Mail.MailAddress.TryCreate(email, out var parsed)
                || !string.Equals(
                    parsed.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Podaj poprawny adres email.");
            }

            var sameEmail =
                await _authRepository.GetByEmailAsync(email);

            if (sameEmail != null && sameEmail.Id != userId)
            {
                throw new InvalidOperationException(
                    "Konto z tym adresem email już istnieje.");
            }

            var sameLogin =
                await _authRepository.GetByLoginAsync(login);

            if (sameLogin != null && sameLogin.Id != userId)
            {
                throw new InvalidOperationException(
                    "Ten login jest już zajęty.");
            }

            await _authRepository.UpdateProfileAsync(
                userId,
                login,
                email,
                firstName,
                lastName);
        }

        // Użytkownik może zmienić wyłącznie własne hasło.
        // Zmiana wymaga podania prawidłowego obecnego hasła.
        // Master również nie może zmienić hasła innego użytkownika.
        public async Task ChangeUserPasswordAsync(
            int userId,
            string currentPassword,
            string newPassword)
        {
            var viewerId =
                await _permissionService.GetCurrentUserIdAsync();

            // Kontrola po stronie serwera — nie polegamy na samym UI.
            if (viewerId <= 0 ||
                userId <= 0 ||
                userId != viewerId)
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} próbował zmienić hasło konta {TargetUserId}.",
                    viewerId,
                    userId);

                throw new UnauthorizedAccessException(
                    "Możesz zmienić wyłącznie własne hasło.");
            }

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                throw new ArgumentException(
                    "Wpisz obecne hasło.");
            }

            if (string.IsNullOrWhiteSpace(newPassword) ||
                newPassword.Length < 8)
            {
                throw new ArgumentException(
                    "Nowe hasło musi mieć co najmniej 8 znaków.");
            }

            if (string.Equals(
                currentPassword,
                newPassword,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Nowe hasło musi różnić się od obecnego.");
            }

            var user = await _authRepository.GetByIdAsync(viewerId);

            if (user == null || string.IsNullOrEmpty(user.Haslo))
            {
                throw new InvalidOperationException(
                    "Nie znaleziono użytkownika lub jego hasła.");
            }

            bool currentPasswordIsValid;

            try
            {
                var verificationResult =
                    _passwordHasher.VerifyHashedPassword(
                        user,
                        user.Haslo,
                        currentPassword);

                currentPasswordIsValid =
                    verificationResult == PasswordVerificationResult.Success ||
                    verificationResult == PasswordVerificationResult.SuccessRehashNeeded;
            }
            catch (FormatException)
            {
                // Zgodność ze starszymi kontami, które mogą jeszcze
                // mieć hasło zapisane w postaci jawnego tekstu.
                currentPasswordIsValid = string.Equals(
                    user.Haslo,
                    currentPassword,
                    StringComparison.Ordinal);
            }

            if (!currentPasswordIsValid)
            {
                _logger.LogWarning(
                    "Nieudana próba zmiany hasła użytkownika {UserId}: błędne obecne hasło.",
                    viewerId);

                throw new UnauthorizedAccessException(
                    "Obecne hasło jest nieprawidłowe.");
            }

            // Zapisujemy wyłącznie hash nowego hasła.
            var newPasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    newPassword);

            await _authRepository.SetPasswordHashAsync(
                viewerId,
                newPasswordHash);

            _logger.LogInformation(
                "Użytkownik {UserId} zmienił własne hasło.",
                viewerId);
        }

        // Master: może edytować własne konto i konta niebędące Master.
        // Pozostali: nie mogą edytować własnego konta ani kont Master.
        private static bool IsEditAllowed(
            bool viewerIsMaster,
            int viewerId,
            int targetId,
            bool targetIsMaster)
        {
            var isSelf = targetId == viewerId;

            return viewerIsMaster
                ? (isSelf || !targetIsMaster)
                : (!isSelf && !targetIsMaster);
        }

        // Kontrola po stronie serwera dla każdej zmiany konta.
        private async Task<User> AuthorizeManageAsync(int targetUserId)
        {
            await _permissionService.RequirePermissionAsync(
                EnumPermission.ManageUsers);

            if (targetUserId <= 0)
            {
                throw new ArgumentException(
                    "Nieprawidłowy użytkownik.");
            }

            var viewerId =
                await _permissionService.GetCurrentUserIdAsync();

            var viewerIsMaster =
                await _permissionService.IsCurrentUserMasterAsync();

            var target =
                await _authRepository.GetByIdAsync(targetUserId);

            if (target == null)
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} wskazał nieistniejące konto {TargetUserId}.",
                    viewerId,
                    targetUserId);

                throw new InvalidOperationException(
                    "Nie znaleziono użytkownika.");
            }

            var targetIsMaster =
                await _authRepository.IsMasterAsync(targetUserId);

            if (!IsEditAllowed(
                viewerIsMaster,
                viewerId,
                targetUserId,
                targetIsMaster))
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} próbował edytować niedozwolone konto {TargetUserId}.",
                    viewerId,
                    targetUserId);

                throw new UnauthorizedAccessException(
                    "Nie masz uprawnień do edycji tego konta.");
            }

            return target;
        }
    }
}
