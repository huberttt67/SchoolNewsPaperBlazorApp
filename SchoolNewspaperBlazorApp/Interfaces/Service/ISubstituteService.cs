
using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface ISubstituteService
    {
        Task<List<Substitute>> GetAllAsync();

        Task AddAsync(
            DateTime substituteDate,
            int lessonNumber,
            TimeSpan startTime,
            TimeSpan endTime,
            int teacherId,
            string reason,
            string? notes);

        Task RemoveAsync(int id);
    }
}
