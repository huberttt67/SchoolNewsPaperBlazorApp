using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using static SchoolNewspaperBlazorApp.Interfaces.Repository.AuthRepository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly NewspaperDbContext _context;

        public AuthRepository(NewspaperDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByLoginAsync(string login)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

    }
}
