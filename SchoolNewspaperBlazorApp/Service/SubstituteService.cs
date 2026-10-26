
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class SubstituteService : ISubstituteService
    {
        private readonly ISubstituteRepository _repository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly ILogger<SubstituteService> _logger;

        public SubstituteService(
            ISubstituteRepository repository,
            ITeacherRepository teacherRepository,
            ILogger<SubstituteService> logger)
        {
            _repository = repository;
            _teacherRepository = teacherRepository;
            _logger = logger;
        }

        public Task<List<Substitute>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public async Task AddAsync(
            DateTime substituteDate,
            int lessonNumber,
            TimeSpan startTime,
            TimeSpan endTime,
            int teacherId,
            string reason,
            string? notes)
        {
            if (lessonNumber < 1 || lessonNumber > 20)
                throw new ArgumentException("Numer lekcji musi być z zakresu 1–20.");

            if (startTime < TimeSpan.Zero ||
                endTime <= startTime ||
                endTime >= TimeSpan.FromDays(1))
            {
                throw new ArgumentException("Podaj prawidłową godzinę rozpoczęcia i zakończenia.");
            }

            if (teacherId <= 0)
                throw new ArgumentException("Wybierz nauczyciela z listy.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Podaj powód zastępstwa.");

            if (reason.Trim().Length > 300)
                throw new ArgumentException("Powód może mieć maksymalnie 300 znaków.");

            if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > 500)
                throw new ArgumentException("Uwagi mogą mieć maksymalnie 500 znaków.");

            var teacher = await _teacherRepository.GetByIdAsync(teacherId);

            if (teacher is null)
                throw new ArgumentException("Wybrany nauczyciel nie istnieje.");

            try
            {
                await _repository.AddAsync(new Substitute
                {
                    SubstituteDate = substituteDate.Date,
                    LessonNumber = lessonNumber,
                    StartTime = startTime,
                    EndTime = endTime,
                    TeacherId = teacherId,
                    Reason = reason.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes)
                        ? null
                        : notes.Trim()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dodawania zastępstwa.");
                throw;
            }
        }

        public async Task RemoveAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Nieprawidłowy identyfikator zastępstwa.");

            try
            {
                await _repository.RemoveAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas usuwania zastępstwa o ID {Id}.",
                    id);

                throw;
            }
        }
    }
}
