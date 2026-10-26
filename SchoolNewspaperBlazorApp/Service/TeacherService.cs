
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class TeacherService : ITeacherService
    {
        private readonly ITeacherRepository _repository;
        private readonly ILogger<TeacherService> _logger;

        public TeacherService(
            ITeacherRepository repository,
            ILogger<TeacherService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<List<Teacher>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public async Task AddAsync(string fullName, string? subject)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Podaj imię i nazwisko nauczyciela.");

            if (fullName.Trim().Length > 120)
                throw new ArgumentException("Imię i nazwisko może mieć maksymalnie 120 znaków.");

            if (!string.IsNullOrWhiteSpace(subject) && subject.Trim().Length > 100)
                throw new ArgumentException("Nazwa przedmiotu może mieć maksymalnie 100 znaków.");

            try
            {
                await _repository.AddAsync(new Teacher
                {
                    FullName = fullName.Trim(),
                    Subject = string.IsNullOrWhiteSpace(subject)
                        ? null
                        : subject.Trim()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dodawania nauczyciela.");
                throw;
            }
        }
    }
}
