using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class ArticleService : IArticleService
    {
        private readonly IArticleRepository _articleRepository;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<ArticleService> _logger;

        public ArticleService(
            IArticleRepository articleRepository,
            IPermissionService permissionService,
            ILogger<ArticleService> logger)
        {
            _articleRepository = articleRepository;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<int> AddArticleAsync(
            string title,
            string text,
            string author,
            int fileId,
            ArticleType type,
            string? tags = null,
            bool isPinned = false)
        {
            await _permissionService.RequirePermissionAsync(
                EnumPermission.CreateArticles);

            var article = new Article
            {
                Title = title,
                Text = text,
                Author = author,
                PublishDate = DateTime.Now,
                FileId = fileId,
                Type = type,
                Tags = ArticleExtensions.NormalizeTags(tags),
                IsPinned = isPinned
            };

            var articleId = await _articleRepository.AddArticleAsync(article);

            _logger.LogInformation(
                "Dodano artykuł {ArticleId} (rodzaj: {ArticleType}).",
                articleId,
                type);

            return articleId;
        }

        public async Task<List<Article>> GetArticlesByTypeAsync(ArticleType type)
        {
            return await _articleRepository.GetArticlesByTypeAsync(type);
        }

        public async Task<List<Article>> GetAllArticlesAsync()
        {
            return await _articleRepository.GetAllArticlesAsync();
        }

        public async Task<Article> GetArticleByIdAsync(int id)
        {
            Article article = await _articleRepository.GetArticleByIdAsync(id);

            if (article == null)
            {
                throw new Exception($"Article with ID {id} not found.");
            }

            return article;
        }

        public async Task RemoveArticleByIdAsync(int id)
        {
            await _permissionService.RequirePermissionAsync(
                EnumPermission.DeleteArticles);

            if (id > 0)
            {
                await _articleRepository.RemoveArticleByIdAsync(id);

                _logger.LogInformation(
                    "Usunięto artykuł {ArticleId} (Soft Delete).",
                    id);
            }
            else
            {
                _logger.LogWarning(
                    "Pominięto usuwanie artykułu o nieprawidłowym Id {ArticleId}.",
                    id);
            }
        }

        public async Task UpdateArticleAsync(
            int id,
            string title,
            string text,
            string author,
            int imageId,
            ArticleType type,
            string? tags = null,
            bool isPinned = false)
        {
            await _permissionService.RequirePermissionAsync(
                EnumPermission.EditArticles);

            if (id <= 0)
            {
                _logger.LogWarning(
                    "Odrzucono aktualizację artykułu o nieprawidłowym Id {ArticleId}.",
                    id);

                throw new ArgumentException("Nieprawidłowe ID artykułu.");
            }

            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(text) ||
                string.IsNullOrWhiteSpace(author))
            {
                throw new ArgumentException(
                    "Tytuł, treść i autor są wymagane.");
            }

            var article = await _articleRepository.GetArticleByIdAsync(id);

            if (article == null)
            {
                throw new Exception($"Nie znaleziono artykułu o ID {id}.");
            }

            article.Title = title;
            article.Text = text;
            article.Author = author;
            article.Type = type;
            article.Tags = ArticleExtensions.NormalizeTags(tags);
            article.IsPinned = isPinned;

            // Zmień zdjęcie tylko wtedy, gdy przekazano nowe ID.
            if (imageId > 0)
            {
                article.FileId = imageId;
            }

            await _articleRepository.UpdateArticleAsync(article);

            _logger.LogInformation(
                "Zaktualizowano artykuł {ArticleId}.",
                id);
        }
    }
}