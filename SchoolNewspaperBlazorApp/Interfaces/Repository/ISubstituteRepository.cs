
using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface ISubstituteRepository
    {
        Task<List<Substitute>> GetAllAsync();

        Task<Substitute?> GetByIdAsync(int id);

        Task AddAsync(Substitute substitute);

        Task RemoveAsync(int id);
    }
}
