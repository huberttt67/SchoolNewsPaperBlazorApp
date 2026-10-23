using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(
            IDbContextFactory<NewspaperDbContext> contextFactory,
            ILogger<AuthRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        // Filtr Soft Delete: konta usunięte logicznie nie są zwracane,
        // więc nie mogą się zalogować.
        public async Task<User?> GetByLoginAsync(string login)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                login = login.Trim();

                return await context.Users
                    .FirstOrDefaultAsync(u => u.Login == login);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas wyszukiwania użytkownika po loginie.");
                throw;
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                // Adresy e-mail porównujemy bez rozróżniania wielkości liter,
                // żeby Jan@szkola.pl i jan@szkola.pl były tym samym kontem.
                var normalizedEmail = email.Trim().ToLower();

                return await context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas wyszukiwania użytkownika po adresie e-mail.");
                throw;
            }
        }

        public async Task AddUserAsync(User user)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();

            // Nowe konto zawsze jest aktywne.
            user.IsDeleted = false;

            await context.Users.AddAsync(user);

            try
            {
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Dodano użytkownika {UserId}.",
                    user.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Ostatnia linia obrony: unikalny indeks w bazie danych
                // zatrzyma też dwa równoczesne zapisy tego samego e-maila.
                _logger.LogWarning(
                    "Odrzucono dodanie użytkownika: adres e-mail już istnieje.");

                throw new InvalidOperationException(
                    "Konto z tym adresem email już istnieje.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dodawania użytkownika.");
                throw;
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            // 2601 = duplikat w unikalnym indeksie, 2627 = naruszenie UNIQUE constraint.
            return ex.InnerException is Microsoft.Data.SqlClient.SqlException sql
                && (sql.Number == 2601 || sql.Number == 2627);
        }

        public async Task UpdateUserAsync(User user)
        {
            var found = false;

            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                // Usunięte logicznie konto nie jest znajdowane, a IsDeleted
                // nie jest kopiowane – aktualizacja nie przywraca konta.
                var existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                if (existingUser != null)
                {
                    found = true;

                    existingUser.Login = user.Login;
                    existingUser.Email = user.Email;
                    existingUser.Haslo = user.Haslo;

                    await context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Zaktualizowano użytkownika {UserId}.",
                        user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas aktualizacji użytkownika {UserId}.",
                    user.Id);
                throw;
            }

            if (!found)
            {
                _logger.LogWarning(
                    "Próba aktualizacji nieistniejącego użytkownika {UserId}.",
                    user.Id);

                throw new InvalidOperationException(
                    "Nie znaleziono użytkownika do aktualizacji.");
            }
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                return await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania użytkownika {UserId}.",
                    id);
                throw;
            }
        }

        // Filtr „bez siebie i bez Master” jest częścią zapytania SQL.
        // Master widzi wszystkich aktywnych użytkowników.
        public async Task<List<UserListItem>> GetUsersForViewerAsync(
            int viewerId,
            bool viewerIsMaster)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                IQueryable<User> query = context.Users.AsNoTracking();

                if (!viewerIsMaster)
                {
                    query = query
                        .Where(u => u.Id != viewerId)
                        .Where(u => !u.UserPermissions.Any(
                            up => up.Permission.Name == EnumPermission.Master));
                }

                var users = await query
                    .OrderBy(u => u.Login)
                    .Select(u => new UserListItem
                    {
                        Id = u.Id,
                        Login = u.Login,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        IsMaster = u.UserPermissions.Any(
                            up => up.Permission.Name == EnumPermission.Master)
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Pobrano listę użytkowników dla panelu ({Count}).",
                    users.Count);

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania listy użytkowników dla panelu.");
                throw;
            }
        }

        public async Task<bool> IsMasterAsync(int userId)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                return await context.UserPermissions
                    .AsNoTracking()
                    .AnyAsync(up =>
                        up.UserId == userId &&
                        up.Permission.Name == EnumPermission.Master);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas sprawdzania roli Master użytkownika {UserId}.",
                    userId);
                throw;
            }
        }

        public async Task UpdateProfileAsync(
            int userId,
            string login,
            string email,
            string firstName,
            string lastName)
        {
            var found = false;

            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    found = true;

                    user.Login = login;
                    user.Email = email;
                    user.FirstName = firstName;
                    user.LastName = lastName;

                    await context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Zaktualizowano dane profilu użytkownika {UserId}.",
                        userId);
                }
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(
                    "Odrzucono zmianę profilu użytkownika {UserId}: adres e-mail już istnieje.",
                    userId);

                throw new InvalidOperationException(
                    "Konto z tym adresem email już istnieje.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas aktualizacji profilu użytkownika {UserId}.",
                    userId);
                throw;
            }

            if (!found)
            {
                _logger.LogWarning(
                    "Próba zmiany profilu nieistniejącego użytkownika {UserId}.",
                    userId);

                throw new InvalidOperationException(
                    "Nie znaleziono użytkownika.");
            }
        }

        public async Task SetPasswordHashAsync(int userId, string passwordHash)
        {
            var found = false;

            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    found = true;
                    user.Haslo = passwordHash;
                    await context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Zmieniono hasło użytkownika {UserId}.",
                        userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas zmiany hasła użytkownika {UserId}.",
                    userId);
                throw;
            }

            if (!found)
            {
                _logger.LogWarning(
                    "Próba zmiany hasła nieistniejącego użytkownika {UserId}.",
                    userId);

                throw new InvalidOperationException(
                    "Nie znaleziono użytkownika.");
            }
        }
    }
}