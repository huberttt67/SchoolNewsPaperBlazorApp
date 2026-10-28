using System.ComponentModel.DataAnnotations;

namespace SchoolNewspaperBlazorApp.Data
{
    public class Article
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Text { get; set; }
        public required string Author { get; set; }
        public DateTime PublishDate { get; set; }
        public int FileId { get; set; }
        public ArticleType Type { get; set; } = ArticleType.Nowosci;

        // Tagi oddzielone przecinkami, np. "sport, konkurs".
        [MaxLength(300)]
        public string? Tags { get; set; }

        // Przypięty artykuł pokazuje się na górze strony głównej.
        public bool IsPinned { get; set; }

        // Licznik wyświetleń strony artykułu.
        public int ViewCount { get; set; }

        // Soft Delete: false = aktywny, true = usunięty logicznie.
        public bool IsDeleted { get; set; }

    }


}