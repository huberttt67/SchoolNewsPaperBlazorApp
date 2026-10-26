
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class TeacherRepository : ITeacherRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<TeacherRepository> _logger;

        public TeacherRepository(
            IDbContextFactory<NewspaperDbContext> contextFactory,
            ILogger<TeacherRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<Teacher>> GetAllAsync()
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            return await db.Teachers
                .AsNoTracking()
                .OrderBy(t => t.FullName)
                .ToListAsync();
        }

        public async Task<Teacher?> GetByIdAsync(int id)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            return await db.Teachers
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(Teacher teacher)
        {
            try
            {
                await using var db = await _contextFactory.CreateDbContextAsync();

                teacher.FullName = teacher.FullName.Trim();
                teacher.Subject = string.IsNullOrWhiteSpace(teacher.Subject)
                    ? null
                    : teacher.Subject.Trim();

                teacher.IsDeleted = false;

                db.Teachers.Add(teacher);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nie udało się dodać nauczyciela.");
                throw;
            }
        }
    }
}
