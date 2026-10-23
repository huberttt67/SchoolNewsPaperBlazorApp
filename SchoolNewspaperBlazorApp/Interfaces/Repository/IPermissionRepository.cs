using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IPermissionRepository
    {
        Task<List<Permission>> GetAllAsync();

        Task<Permission?> GetByIdAsync(int id);

        Task<Permission?> GetByNameAsync(string permissionName);

        Task<List<User>> GetAllUsersAsync();

        // Lista kont, którymi można zarządzać: aktywne konta z pominięciem
        // konta wskazanego w excludedUserId (zalogowany) i kont Master.
        // Filtr jest wykonywany w zapytaniu do bazy danych.
        Task<List<User>> GetAssignableUsersAsync(int excludedUserId);

        // To samo kryterium co w GetAssignableUsersAsync, dla pojedynczego konta.
        Task<bool> IsUserAssignableAsync(int targetUserId, int excludedUserId);

        Task<List<EnumPermission>> GetUserPermissionsAsync(int userId);

        Task SetUserPermissionsAsync(
            int userId,
            List<EnumPermission> permissions);

        Task AddUserPermissionAsync(int userId, EnumPermission permission);

        Task RemoveUserPermissionAsync(int userId, EnumPermission permission);

        Task<int> CountUsersWithPermissionAsync(EnumPermission permission);

        Task DeleteUserAsync(int userId);
    }
}