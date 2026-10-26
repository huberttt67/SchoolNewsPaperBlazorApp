
using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface ITeacherRepository
    {
        Task<List<Teacher>> GetAllAsync();

        Task<Teacher?> GetByIdAsync(int id);

        Task AddAsync(Teacher teacher);
    }
}
