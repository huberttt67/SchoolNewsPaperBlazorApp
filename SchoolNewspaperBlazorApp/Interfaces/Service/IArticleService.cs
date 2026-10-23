using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IArticleService
    {
        Task<int> AddArticleAsync(string title, string text, string author, int id, ArticleType type);

        Task<List<Article>> GetAllArticlesAsync();

        Task<List<Article>> GetArticlesByTypeAsync(ArticleType type);

        Task<Article> GetArticleByIdAsync(int id);

        Task RemoveArticleByIdAsync(int id);

        Task UpdateArticleAsync(
            int id,
            string title,
            string text,
            string author,
            int imageId,
            ArticleType type);
    }
}