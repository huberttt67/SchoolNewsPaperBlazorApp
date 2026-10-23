using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IArticleRepository
    {
        Task<int> AddArticleAsync(Article article);
        Task<List<Article>> GetAllArticlesAsync();
        Task<List<Article>> GetArticlesByTypeAsync(ArticleType type);
        Task<Article> GetArticleByIdAsync(int id);
        Task RemoveArticleByIdAsync(int id);
        Task UpdateArticleAsync(Article article);
    }
}