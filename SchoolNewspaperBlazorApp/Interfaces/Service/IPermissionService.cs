using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IPermissionService
    {
        Task<List<Permission>> GetAllPermissionsAsync();

        Task<List<User>> GetAllUsersAsync();

        Task<List<EnumPermission>> GetUserPermissionsAsync(int userId);

        Task SetUserPermissionsAsync(
            int userId,
            List<EnumPermission> permissions);

        Task AddUserPermissionAsync(int userId, EnumPermission permission);

        Task RemoveUserPermissionAsync(int userId, EnumPermission permission);

        // Usuwa konto użytkownika. Tylko Master; nie wolno usunąć siebie
        // ani ostatniego administratora głównego.
        Task DeleteUserAsync(int userId);

        // Rzuca UnauthorizedAccessException, jeśli zalogowany użytkownik
        // nie ma żadnego z podanych uprawnień (Master = pełny dostęp).
        Task RequirePermissionAsync(params EnumPermission[] anyOf);

        Task<int> GetCurrentUserIdAsync();

        Task<bool> IsCurrentUserMasterAsync();

        // true, jeśli zalogowany ma dane uprawnienie (Master = zawsze true).
        Task<bool> HasPermissionAsync(EnumPermission permission);
    }
}