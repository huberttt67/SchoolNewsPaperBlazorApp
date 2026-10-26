
using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface ITeacherService
    {
        Task<List<Teacher>> GetAllAsync();

        Task AddAsync(string fullName, string? subject);
    }
}
