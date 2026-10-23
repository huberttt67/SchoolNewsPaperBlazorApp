using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<ArticleRepository> _logger;

        public ArticleRepository(
            IDbContextFactory<NewspaperDbContext> contextFactory,
            ILogger<ArticleRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<int> AddArticleAsync(Article article)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                // Nowy artykuł zawsze jest aktywny.
                article.IsDeleted = false;

                await context.Articles.AddAsync(article);
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Dodano artykuł {ArticleId}.",
                    article.Id);

                return article.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dodawania artykułu.");
                throw;
            }
        }

        public async Task<List<Article>> GetAllArticlesAsync()
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                // Najnowsze artykuły na górze (usunięte logicznie pomija filtr EF).
                return await context.Articles
                    .OrderByDescending(a => a.PublishDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania listy artykułów.");
                throw;
            }
        }

        public async Task<List<Article>> GetArticlesByTypeAsync(ArticleType type)
        {
            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                // Tylko wybrany rodzaj, najnowsze na górze.
                return await context.Articles
                    .Where(a => a.Type == type)
                    .OrderByDescending(a => a.PublishDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania artykułów rodzaju {ArticleType}.",
                    type);
                throw;
            }
        }

        public async Task<Article> GetArticleByIdAsync(int id)
        {
            Article? article;

            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                // Filtr Soft Delete pomija usunięte artykuły.
                article = await context.Articles
                    .FirstOrDefaultAsync(a => a.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania artykułu {ArticleId}.",
                    id);
                throw;
            }

            if (article == null)
            {
                _logger.LogWarning(
                    "Nie znaleziono artykułu o Id {ArticleId} (nie istnieje lub został usunięty).",
                    id);

                throw new Exception(
                    $"Article with ID {id} not found.");
            }

            return article;
        }

        // Soft Delete: artykuł dostaje IsDeleted = true, rekord zostaje w bazie.
        public async Task RemoveArticleByIdAsync(int id)
        {
            var found = false;

            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                var article = await context.Articles
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (article != null)
                {
                    found = true;
                    article.IsDeleted = true;
                    await context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Artykuł {ArticleId} został usunięty logicznie (Soft Delete).",
                        id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas usuwania artykułu {ArticleId}.",
                    id);
                throw;
            }

            if (!found)
            {
                _logger.LogWarning(
                    "Próba usunięcia nieistniejącego artykułu {ArticleId}.",
                    id);

                throw new Exception(
                    $"Article with ID {id} not found.");
            }
        }

        public async Task UpdateArticleAsync(Article article)
        {
            var found = false;

            try
            {
                await using var context =
                    await _contextFactory.CreateDbContextAsync();

                // Filtr Soft Delete: usunięty artykuł nie zostanie znaleziony,
                // więc aktualizacja nie może go przypadkowo przywrócić.
                var existingArticle = await context.Articles
                    .FirstOrDefaultAsync(a => a.Id == article.Id);

                if (existingArticle != null)
                {
                    found = true;

                    existingArticle.Title = article.Title;
                    existingArticle.Text = article.Text;
                    existingArticle.Author = article.Author;
                    existingArticle.Type = article.Type;

                    if (article.FileId > 0)
                    {
                        existingArticle.FileId = article.FileId;
                    }

                    await context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Zaktualizowano artykuł {ArticleId}.",
                        article.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas aktualizacji artykułu {ArticleId}.",
                    article.Id);
                throw;
            }

            if (!found)
            {
                _logger.LogWarning(
                    "Próba aktualizacji nieistniejącego artykułu {ArticleId}.",
                    article.Id);

                throw new Exception(
                    $"Article with ID {article.Id} not found.");
            }
        }
    }
}
