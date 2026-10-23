using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _repository;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            IPermissionRepository repository,
            AuthenticationStateProvider authenticationStateProvider,
            ILogger<PermissionService> logger)
        {
            _repository = repository;
            _authenticationStateProvider = authenticationStateProvider;
            _logger = logger;
        }

        // ---------- Odczyt (tylko Master) ----------

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            await RequirePermissionAsync(EnumPermission.Master);
            return await _repository.GetAllAsync();
        }

        // Lista dostępna tylko dla Mastera, który widzi wszystkie aktywne konta.
        // Edycja uprawnień własnego konta i kont Master jest blokowana
        // po stronie serwera (ValidateTargetUserAsync).
        public async Task<List<User>> GetAllUsersAsync()
        {
            await RequirePermissionAsync(EnumPermission.Master);
            return await _repository.GetAllUsersAsync();
        }

        public async Task<List<EnumPermission>> GetUserPermissionsAsync(int userId)
        {
            await RequirePermissionAsync(EnumPermission.Master);
            return await _repository.GetUserPermissionsAsync(userId);
        }

        // ---------- Zmiany uprawnień ----------

        public async Task SetUserPermissionsAsync(
            int userId,
            List<EnumPermission> permissions)
        {
            await ValidateTargetUserAsync(userId);

            await _repository.SetUserPermissionsAsync(userId, permissions);
        }

        public async Task AddUserPermissionAsync(
            int userId,
            EnumPermission permission)
        {
            await ValidateTargetUserAsync(userId);

            await _repository.AddUserPermissionAsync(userId, permission);
        }

        public async Task RemoveUserPermissionAsync(
            int userId,
            EnumPermission permission)
        {
            await ValidateTargetUserAsync(userId);

            await _repository.RemoveUserPermissionAsync(userId, permission);
        }

        // ---------- Usuwanie użytkowników (Soft Delete) ----------

        public async Task DeleteUserAsync(int userId)
        {
            var (currentUserId, currentPermissions) = await GetCurrentUserAsync();

            if (!currentPermissions.Contains(EnumPermission.Master))
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} bez uprawnień próbował usunąć użytkownika {TargetUserId}.",
                    currentUserId,
                    userId);

                throw new UnauthorizedAccessException(
                    "Tylko administrator główny może usuwać użytkowników.");
            }

            if (userId <= 0)
            {
                throw new ArgumentException("Nieprawidłowy użytkownik.");
            }

            if (userId == currentUserId)
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} próbował usunąć własne konto.",
                    currentUserId);

                throw new UnauthorizedAccessException(
                    "Nie możesz usunąć własnego konta.");
            }

            // Zawsze musi zostać co najmniej jeden administrator główny.
            var targetPermissions =
                await _repository.GetUserPermissionsAsync(userId);

            if (targetPermissions.Contains(EnumPermission.Master))
            {
                var masterCount = await _repository
                    .CountUsersWithPermissionAsync(EnumPermission.Master);

                if (masterCount <= 1)
                {
                    _logger.LogWarning(
                        "Odrzucono usunięcie ostatniego administratora głównego ({TargetUserId}).",
                        userId);

                    throw new InvalidOperationException(
                        "Nie można usunąć ostatniego administratora głównego.");
                }
            }

            // Konta Master nie są zarządzane z tego miejsca (tak samo jak
            // na liście uprawnień), więc nie można ich usunąć z poziomu panelu.
            await ValidateTargetUserAsync(userId);

            await _repository.DeleteUserAsync(userId);

            _logger.LogInformation(
                "Użytkownik {UserId} usunął logicznie konto {TargetUserId}.",
                currentUserId,
                userId);
        }

        // ---------- Sprawdzanie uprawnień zalogowanego użytkownika ----------

        public async Task RequirePermissionAsync(params EnumPermission[] anyOf)
        {
            var (userId, permissions) = await GetCurrentUserAsync();

            if (permissions.Contains(EnumPermission.Master) ||
                anyOf.Any(permissions.Contains))
            {
                return;
            }

            _logger.LogWarning(
                "Odmowa dostępu: użytkownik {UserId} nie ma wymaganych uprawnień ({Required}).",
                userId,
                string.Join(", ", anyOf));

            throw new UnauthorizedAccessException(
                "Brak uprawnień do wykonania tej operacji.");
        }

        public async Task<int> GetCurrentUserIdAsync()
        {
            var (userId, _) = await GetCurrentUserAsync();
            return userId;
        }

        public async Task<bool> IsCurrentUserMasterAsync()
        {
            var (_, permissions) = await GetCurrentUserAsync();
            return permissions.Contains(EnumPermission.Master);
        }

        public async Task<bool> HasPermissionAsync(EnumPermission permission)
        {
            var (_, permissions) = await GetCurrentUserAsync();
            return permissions.Contains(EnumPermission.Master) ||
                   permissions.Contains(permission);
        }

        // Uprawnienia są za każdym razem czytane z bazy, więc odebranie
        // uprawnienia działa od razu, bez ponownego logowania.
        private async Task<(int UserId, HashSet<EnumPermission> Permissions)>
            GetCurrentUserAsync()
        {
            var state =
                await _authenticationStateProvider.GetAuthenticationStateAsync();

            var user = state.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(
                    "Musisz być zalogowany, aby wykonać tę operację.");
            }

            var idValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idValue, out var userId))
            {
                _logger.LogWarning(
                    "Nie udało się ustalić identyfikatora zalogowanego użytkownika.");

                throw new UnauthorizedAccessException(
                    "Nie udało się ustalić zalogowanego użytkownika.");
            }

            var permissions =
                (await _repository.GetUserPermissionsAsync(userId)).ToHashSet();

            return (userId, permissions);
        }

        // Każda operacja na uprawnieniach wybranego konta (odczyt, zapis,
        // usunięcie) przechodzi przez tę kontrolę po stronie serwera:
        //  - wykonuje ją wyłącznie Master,
        //  - nie można zarządzać własnym kontem,
        //  - nie można zarządzać kontem Master,
        //  - konto musi istnieć i nie może być usunięte logicznie.
        private async Task ValidateTargetUserAsync(int targetUserId)
        {
            var (currentUserId, currentPermissions) = await GetCurrentUserAsync();

            if (!currentPermissions.Contains(EnumPermission.Master))
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} bez uprawnień próbował zarządzać uprawnieniami konta {TargetUserId}.",
                    currentUserId,
                    targetUserId);

                throw new UnauthorizedAccessException(
                    "Tylko administrator główny może nadawać i odbierać uprawnienia.");
            }

            if (targetUserId <= 0)
            {
                throw new ArgumentException("Nieprawidłowy użytkownik.");
            }

            if (targetUserId == currentUserId)
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} próbował zarządzać uprawnieniami własnego konta.",
                    currentUserId);

                throw new UnauthorizedAccessException(
                    "Nie możesz zarządzać uprawnieniami własnego konta.");
            }

            // To samo kryterium, które filtruje listę użytkowników w zapytaniu SQL.
            var assignable = await _repository.IsUserAssignableAsync(
                targetUserId,
                currentUserId);

            if (assignable)
            {
                return;
            }

            var targetPermissions =
                await _repository.GetUserPermissionsAsync(targetUserId);

            if (targetPermissions.Contains(EnumPermission.Master))
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} próbował zarządzać kontem administratora głównego {TargetUserId}.",
                    currentUserId,
                    targetUserId);

                throw new UnauthorizedAccessException(
                    "Nie można zarządzać uprawnieniami administratora głównego.");
            }

            _logger.LogWarning(
                "Użytkownik {UserId} wskazał nieistniejące konto {TargetUserId}.",
                currentUserId,
                targetUserId);

            throw new InvalidOperationException("Nie znaleziono użytkownika.");
        }
    }
}