﻿using Microsoft.AspNetCore.Components.Forms;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class FileService : IFileService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IPermissionService _permissionService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;
        private string UploadPath => Path.Combine(_environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "Images");
        public FileService(
            IFileRepository fileRepository,
            IPermissionService permissionService,
            IWebHostEnvironment environment,
            ILogger<FileService> logger)
        {
            _fileRepository = fileRepository;
            _permissionService = permissionService;
            _environment = environment;
            _logger = logger;
        }
        public async Task<string> GetPreviewAsync(IBrowserFile file)
        {
            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            byte[] bytes = memoryStream.ToArray();
            return $"data:{file.ContentType};base64,{Convert.ToBase64String(bytes)}";
        }
        public async Task<int> UploadImage(IBrowserFile file)
        {
            await _permissionService.RequirePermissionAsync(
                EnumPermission.CreateArticles,
                EnumPermission.EditArticles);

            //Tworzenie folderu
            Directory.CreateDirectory(UploadPath);
            //Pobieranie extension z pliku czyli np .png
            string extension = Path.GetExtension(file.Name);
            //Generowanie nazwy + extension np. plik.png
            string fileName = $"{Guid.NewGuid()}{extension}";
            // łaczenie uploadPath i fileName C:\Users\Praktyka\source\repos\SchoolNewspaperBlazorApp\Images\plik.png
            string fullPath = Path.Combine(UploadPath, fileName);

            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var fileStream = new FileStream(
                fullPath, //pelny link odnosnika
                FileMode.Create //ustawienie tworzenia
                );
            await stream.CopyToAsync(fileStream);
            var id = await _fileRepository.GetLastFileId();
            var mediaFile = new MediaFile
            {
                Id = id,
                FileName = fileName,
                FileType = extension
            };
            await _fileRepository.AddFileAsync(mediaFile);

            _logger.LogInformation(
                "Zapisano plik multimedialny {FileId} (typ: {FileType}).",
                id,
                extension);

            return id;

        }
        public async Task UploadGalleryImageAsync(int articleId, IBrowserFile file)
        {
            await _permissionService.RequirePermissionAsync(
                EnumPermission.CreateArticles,
                EnumPermission.EditArticles);

            if (articleId <= 0) throw new ArgumentException("Nieprawidłowy artykuł.");
            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Odrzucono plik o niedozwolonym typie ({ContentType}) dla galerii artykułu {ArticleId}.",
                    file.ContentType,
                    articleId);

                throw new InvalidOperationException("Można dodawać tylko pliki graficzne.");
            }

            var galleryPath = Path.Combine(UploadPath, "Gallery", articleId.ToString());
            Directory.CreateDirectory(galleryPath);
            var extension = Path.GetExtension(file.Name);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(galleryPath, fileName);
            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var output = new FileStream(fullPath, FileMode.Create);
            await stream.CopyToAsync(output);

            _logger.LogInformation(
                "Dodano zdjęcie do galerii artykułu {ArticleId}.",
                articleId);
        }

        public Task<List<string>> GetGalleryImagesAsync(int articleId)
        {
            var galleryPath = Path.Combine(UploadPath, "Gallery", articleId.ToString());
            if (!Directory.Exists(galleryPath)) return Task.FromResult(new List<string>());
            var names = Directory.GetFiles(galleryPath)
                .Where(path => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }
                    .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .Select(path => Path.GetFileName(path))
                .ToList();
            return Task.FromResult(names);
        }

        public async Task DeleteGalleryImageAsync(int articleId, string fileName)
        {
            await _permissionService.RequirePermissionAsync(
                EnumPermission.EditArticles);

            if (articleId <= 0 || string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Nieprawidłowe zdjęcie.");

            // Tylko sama nazwa pliku – blokuje ścieżki typu ..\..\plik.
            if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Odrzucono nieprawidłową nazwę pliku galerii dla artykułu {ArticleId}.",
                    articleId);

                throw new ArgumentException("Nieprawidłowa nazwa pliku.");
            }

            var fullPath = Path.Combine(UploadPath, "Gallery", articleId.ToString(), fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);

                _logger.LogInformation(
                    "Usunięto zdjęcie z galerii artykułu {ArticleId}.",
                    articleId);
            }
            else
            {
                _logger.LogWarning(
                    "Nie znaleziono zdjęcia galerii do usunięcia (artykuł {ArticleId}).",
                    articleId);
            }

            await Task.CompletedTask;
        }

        public async Task<string> GetFileNameByIdAsync(int id)
        {
            var file = await _fileRepository.GetFileByIdAsync(id);
            return file.FileName;
        }
    }
}