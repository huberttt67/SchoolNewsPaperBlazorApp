using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class FileRepository : IFileRepository
    {
        // Fabryka daje osobny kontekst na każde zapytanie, więc wiele
        // komponentów może jednocześnie pobierać zdjęcia bez konfliktu.
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<FileRepository> _logger;

        public FileRepository(
            IDbContextFactory<NewspaperDbContext> contextFactory,
            ILogger<FileRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<int> GetLastFileId()
        {
            try
            {
                // Id pliku jest nadawane ręcznie (ValueGeneratedNever), więc
                // wyliczamy je z UWZGLĘDNIENIEM usuniętych logicznie rekordów.
                // Bez IgnoreQueryFilters() nowy plik mógłby dostać Id
                // istniejącego, ale usuniętego rekordu (konflikt klucza głównego).
                await using var context = await _contextFactory.CreateDbContextAsync();

                var id = await context.Files
                    .IgnoreQueryFilters()
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
                return id + 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas ustalania kolejnego Id pliku.");
                throw;
            }
        }

        public async Task<MediaFile> GetFileByIdAsync(int id)
        {
            MediaFile? file;

            try
            {
                // Filtr Soft Delete pomija usunięte pliki.
                await using var context = await _contextFactory.CreateDbContextAsync();

                file = await context.Files.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas pobierania pliku {FileId}.",
                    id);
                throw;
            }

            if (file == null)
            {
                _logger.LogWarning(
                    "Nie znaleziono pliku o Id {FileId} (nie istnieje lub został usunięty).",
                    id);

                throw new Exception("[FileRepository]Brak zdjecia w bazie danych");
            }

            return file;
        }

        public async Task AddFileAsync(Data.MediaFile file)
        {
            try
            {
                file.IsDeleted = false;

                await using var context = await _contextFactory.CreateDbContextAsync();

                await context.Files.AddAsync(file);
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Dodano plik multimedialny {FileId}.",
                    file.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Błąd podczas dodawania pliku multimedialnego {FileId}.",
                    file.Id);
                throw;
            }
        }

    }
}
