
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class SubstituteRepository : ISubstituteRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<SubstituteRepository> _logger;

        public SubstituteRepository(
            IDbContextFactory<NewspaperDbContext> contextFactory,
            ILogger<SubstituteRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<Substitute>> GetAllAsync()
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            return await db.Substitutes
                .AsNoTracking()
                .Include(s => s.Teacher)
                .OrderBy(s => s.SubstituteDate)
                .ThenBy(s => s.LessonNumber)
                .ToListAsync();
        }

        public async Task<Substitute?> GetByIdAsync(int id)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            return await db.Substitutes
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(Substitute substitute)
        {
            try
            {
                await using var db = await _contextFactory.CreateDbContextAsync();

                substitute.IsDeleted = false;
                substitute.CreatedAt = DateTime.Now;

                db.Substitutes.Add(substitute);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nie udało się dodać zastępstwa.");
                throw;
            }
        }

        public async Task RemoveAsync(int id)
        {
            try
            {
                await using var db = await _contextFactory.CreateDbContextAsync();

                var substitute = await db.Substitutes
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (substitute is null)
                    return;

                // Usunięcie logiczne — rekord pozostaje w bazie.
                substitute.IsDeleted = true;

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Nie udało się logicznie usunąć zastępstwa o ID {Id}.",
                    id);

                throw;
            }
        }
    }
}
