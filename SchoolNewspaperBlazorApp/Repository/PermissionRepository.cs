using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<PermissionRepository> _logger;

        public PermissionRepository(
            IDbContextFactory<NewspaperDbContext> contextFactory,
            ILogger<PermissionRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<Permission>> GetAllAsync()
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                await EnsurePermissionsExistAsync(context);

                var permissions = await context.Permissions
                    .AsNoTracking()
                    .OrderBy(p => p.Id)
                    .ToListAsync();

                _logger.LogInformation(
                    "Pobrano listę uprawnień ({Count}).",
                    permissions.Count);

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania listy uprawnień.");
                throw;
            }
        }

        public async Task<Permission?> GetByIdAsync(int id)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                return await context.Permissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania uprawnienia o Id {PermissionId}.",
                    id);
                throw;
            }
        }

        public async Task<Permission?> GetByNameAsync(string permissionName)
        {
            if (!Enum.TryParse<EnumPermission>(
                    permissionName,
                    true,
                    out var permission))
            {
                _logger.LogWarning(
                    "Nieznana nazwa uprawnienia: {PermissionName}.",
                    permissionName);
                return null;
            }

            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                return await context.Permissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Name == permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania uprawnienia {Permission}.",
                    permission);
                throw;
            }
        }

        // Wszystkie aktywne konta (bez usuniętych logicznie).
        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                var users = await context.Users
                    .AsNoTracking()
                    .OrderBy(u => u.Login)
                    .ToListAsync();

                _logger.LogInformation(
                    "Pobrano listę użytkowników ({Count}).",
                    users.Count);

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania listy użytkowników.");
                throw;
            }
        }

        // Filtr (bez konta zalogowanego i bez kont Master) jest częścią
        // zapytania SQL – nie da się go obejść zmianą interfejsu.
        public async Task<List<User>> GetAssignableUsersAsync(int excludedUserId)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                var users = await context.Users
                    .AsNoTracking()
                    .Where(u => u.Id != excludedUserId)
                    .Where(u => !u.UserPermissions.Any(
                        up => up.Permission.Name == EnumPermission.Master))
                    .OrderBy(u => u.Login)
                    .ToListAsync();

                _logger.LogInformation(
                    "Pobrano listę użytkowników do zarządzania uprawnieniami ({Count}).",
                    users.Count);

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania listy użytkowników do zarządzania uprawnieniami.");
                throw;
            }
        }

        public async Task<bool> IsUserAssignableAsync(
            int targetUserId,
            int excludedUserId)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                return await context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == targetUserId)
                    .Where(u => u.Id != excludedUserId)
                    .Where(u => !u.UserPermissions.Any(
                        up => up.Permission.Name == EnumPermission.Master))
                    .AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas sprawdzania, czy użytkownik {UserId} może być zarządzany.",
                    targetUserId);
                throw;
            }
        }

        public async Task<List<EnumPermission>> GetUserPermissionsAsync(
            int userId)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                return await context.UserPermissions
                    .AsNoTracking()
                    .Where(up => up.UserId == userId)
                    .Select(up => up.Permission.Name)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania uprawnień użytkownika {UserId}.",
                    userId);
                throw;
            }
        }

        public async Task SetUserPermissionsAsync(
            int userId,
            List<EnumPermission> permissions)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                var userExists = await context.Users
                    .AnyAsync(u => u.Id == userId);

                if (!userExists)
                {
                    _logger.LogWarning(
                        "Próba zapisu uprawnień nieistniejącego użytkownika {UserId}.",
                        userId);

                    throw new InvalidOperationException(
                        "Nie znaleziono użytkownika.");
                }

                await EnsurePermissionsExistAsync(context);

                var selectedPermissions = permissions
                    .Where(p => p != EnumPermission.None)
                    .Distinct()
                    .ToList();

                var permissionRecords = await context.Permissions
                    .Where(p => selectedPermissions.Contains(p.Name))
                    .ToListAsync();

                if (permissionRecords.Count != selectedPermissions.Count)
                {
                    _logger.LogWarning(
                        "Nie znaleziono wszystkich uprawnień wybranych dla użytkownika {UserId}.",
                        userId);

                    throw new InvalidOperationException(
                        "Nie znaleziono wszystkich wybranych uprawnień.");
                }

                var currentPermissions = await context.UserPermissions
                    .Where(up => up.UserId == userId)
                    .ToListAsync();

                var selectedIds = permissionRecords
                    .Select(p => p.Id)
                    .ToHashSet();

                // Różnica zamiast „usuń wszystko i dodaj od nowa” –
                // nie powstaje konflikt kluczy w jednym SaveChanges.
                var toRemove = currentPermissions
                    .Where(up => !selectedIds.Contains(up.PermissionId))
                    .ToList();

                var currentIds = currentPermissions
                    .Select(up => up.PermissionId)
                    .ToHashSet();

                var toAdd = permissionRecords
                    .Where(p => !currentIds.Contains(p.Id))
                    .ToList();

                context.UserPermissions.RemoveRange(toRemove);

                foreach (var permission in toAdd)
                {
                    context.UserPermissions.Add(new UserPermission
                    {
                        UserId = userId,
                        PermissionId = permission.Id
                    });
                }

                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Zapisano uprawnienia użytkownika {UserId}: dodano {Added}, odebrano {Removed}.",
                    userId,
                    toAdd.Count,
                    toRemove.Count);
            }
            catch (InvalidOperationException)
            {
                // Błędy walidacji zostały już zalogowane powyżej.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas zapisu uprawnień użytkownika {UserId}.",
                    userId);
                throw;
            }
        }

        public async Task AddUserPermissionAsync(
            int userId,
            EnumPermission permission)
        {
            if (permission == EnumPermission.None)
            {
                return;
            }

            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                var userExists = await context.Users
                    .AnyAsync(u => u.Id == userId);

                if (!userExists)
                {
                    _logger.LogWarning(
                        "Próba nadania uprawnienia {Permission} nieistniejącemu użytkownikowi {UserId}.",
                        permission,
                        userId);

                    throw new InvalidOperationException(
                        "Nie znaleziono użytkownika.");
                }

                await EnsurePermissionsExistAsync(context);

                var permissionId = await context.Permissions
                    .Where(p => p.Name == permission)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();

                if (permissionId == 0)
                {
                    _logger.LogWarning(
                        "Brak uprawnienia {Permission} w bazie danych.",
                        permission);

                    throw new InvalidOperationException(
                        "Nie znaleziono uprawnienia w bazie danych.");
                }

                var alreadyAssigned = await context.UserPermissions
                    .AnyAsync(up =>
                        up.UserId == userId &&
                        up.PermissionId == permissionId);

                if (alreadyAssigned)
                {
                    return;
                }

                context.UserPermissions.Add(new UserPermission
                {
                    UserId = userId,
                    PermissionId = permissionId
                });

                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Nadano uprawnienie {Permission} użytkownikowi {UserId}.",
                    permission,
                    userId);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas nadawania uprawnienia {Permission} użytkownikowi {UserId}.",
                    permission,
                    userId);
                throw;
            }
        }

        public async Task RemoveUserPermissionAsync(
            int userId,
            EnumPermission permission)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                var records = await context.UserPermissions
                    .Where(up =>
                        up.UserId == userId &&
                        up.Permission.Name == permission)
                    .ToListAsync();

                if (records.Count == 0)
                {
                    return;
                }

                context.UserPermissions.RemoveRange(records);
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Odebrano uprawnienie {Permission} użytkownikowi {UserId}.",
                    permission,
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas odbierania uprawnienia {Permission} użytkownikowi {UserId}.",
                    permission,
                    userId);
                throw;
            }
        }

        // Liczy tylko aktywne konta (filtr Soft Delete na UserPermission).
        public async Task<int> CountUsersWithPermissionAsync(
            EnumPermission permission)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                return await context.UserPermissions
                    .AsNoTracking()
                    .Where(up => up.Permission.Name == permission)
                    .Select(up => up.UserId)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas zliczania użytkowników z uprawnieniem {Permission}.",
                    permission);
                throw;
            }
        }

        // Soft Delete: konto dostaje IsDeleted = true. Rekord użytkownika
        // i jego przypisania uprawnień zostają w bazie.
        public async Task DeleteUserAsync(int userId)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning(
                        "Próba usunięcia nieistniejącego użytkownika {UserId}.",
                        userId);

                    throw new InvalidOperationException(
                        "Nie znaleziono użytkownika.");
                }

                user.IsDeleted = true;

                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Użytkownik {UserId} został usunięty logicznie (Soft Delete).",
                    userId);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas usuwania użytkownika {UserId}.",
                    userId);
                throw;
            }
        }

        private async Task EnsurePermissionsExistAsync(
            NewspaperDbContext context)
        {
            var existing = await context.Permissions.ToListAsync();
            var existingNames = existing.Select(p => p.Name).ToList();

            var descriptions = new Dictionary<EnumPermission, string>
            {
                [EnumPermission.AccessAdminPanel] =
                    "Dostęp do panelu administracyjnego",

                [EnumPermission.ViewArticles] =
                    "Wyświetlanie artykułów",

                [EnumPermission.CreateArticles] =
                    "Dodawanie artykułów",

                [EnumPermission.EditArticles] =
                    "Edytowanie artykułów",

                [EnumPermission.DeleteArticles] =
                    "Usuwanie artykułów",

                [EnumPermission.ViewUsers] =
                    "Wyświetlanie użytkowników",

                [EnumPermission.ManageUsers] =
                    "Dodawanie i zarządzanie użytkownikami",

                [EnumPermission.ViewAnnouncements] =
                    "Wyświetlanie ogłoszeń",

                [EnumPermission.CreateAnnouncements] =
                    "Dodawanie ogłoszeń",

                [EnumPermission.EditAnnouncements] =
                    "Edytowanie ogłoszeń",

                [EnumPermission.DeleteAnnouncements] =
                    "Usuwanie ogłoszeń",

                [EnumPermission.CreateSubstitutions] =
                    "Dodawanie zastępstw i nauczycieli",

                [EnumPermission.EditSubstitutions] =
                    "Edytowanie zastępstw i nauczycieli",

                [EnumPermission.DeleteSubstitutions] =
                    "Usuwanie zastępstw i nauczycieli",

                [EnumPermission.Master] =
                    "Administrator główny (pełny dostęp)"
            };

            // Odświeżamy opisy i nazwy istniejących wierszy (np. po zmianie
            // nazwy uprawnienia w kodzie), żeby w bazie nie zostały stare.
            var refreshed = false;

            foreach (var row in existing)
            {
                if (descriptions.TryGetValue(row.Name, out var description) &&
                    (row.Description != description ||
                     row.PermissionName != row.Name.ToString()))
                {
                    row.Description = description;
                    row.PermissionName = row.Name.ToString();
                    refreshed = true;
                }
            }

            if (refreshed)
            {
                await context.SaveChangesAsync();
            }

            var missingPermissions = descriptions
                .Where(item => !existingNames.Contains(item.Key))
                .Select(item => new Permission
                {
                    Name = item.Key,
                    PermissionName = item.Key.ToString(),
                    Description = item.Value
                })
                .ToList();

            if (missingPermissions.Count > 0)
            {
                context.Permissions.AddRange(missingPermissions);
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Uzupełniono brakujące uprawnienia w bazie ({Count}).",
                    missingPermissions.Count);
            }
        }
    }
}