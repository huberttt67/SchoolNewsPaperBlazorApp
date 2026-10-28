using Microsoft.AspNetCore.Components.Forms;

namespace SchoolNewspaperBlazorApp.Data
{
    /// <summary>
    /// Wykadrowane zdjęcie trzymane w pamięci. Implementuje IBrowserFile,
    /// dzięki czemu FileService zapisuje je dokładnie tak samo jak zdjęcie
    /// wybrane z dysku.
    /// </summary>
    public sealed class CroppedBrowserFile : IBrowserFile
    {
        private readonly byte[] _data;

        public CroppedBrowserFile(string name, byte[] data, string contentType = "image/jpeg")
        {
            Name = name;
            _data = data;
            ContentType = contentType;
        }

        /// <summary>Mała miniatura (data URL) do podglądu w galerii.</summary>
        public string? Thumbnail { get; init; }

        public string Name { get; }

        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public long Size => _data.LongLength;

        public string ContentType { get; }

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            if (Size > maxAllowedSize)
                throw new IOException($"Plik przekracza dozwolony rozmiar ({maxAllowedSize} B).");

            return new MemoryStream(_data, writable: false);
        }
    }
}