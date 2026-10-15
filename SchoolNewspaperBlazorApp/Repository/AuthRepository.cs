
using Microsoft.EntityFrameworkCore;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;

        public AuthRepository(
            IDbContextFactory<NewspaperDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<User?> GetByLoginAsync(string login)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .FirstOrDefaultAsync(u => u.Login == login);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddUserAsync(User user)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();

            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (existingUser == null)
            {
                throw new InvalidOperationException(
                    "Nie znaleziono użytkownika do aktualizacji.");
            }

            existingUser.Login = user.Login;
            existingUser.Email = user.Email;
            existingUser.Haslo = user.Haslo;

            await context.SaveChangesAsync();
        }
    }
}
