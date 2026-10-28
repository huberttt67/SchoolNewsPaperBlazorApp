using System.Text.RegularExpressions;

namespace SchoolNewspaperBlazorApp.Data
{
    public static class ArticleExtensions
    {
        private static readonly Regex ImageMarker = new(
            @"\[\[IMG:[A-Za-z0-9._-]+(?:\|(?:left|right|center))?\]\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>Czas czytania w minutach (ok. 200 słów na minutę, minimum 1).</summary>
        public static int ReadingMinutes(this Article article)
        {
            var text = ImageMarker.Replace(article.Text ?? string.Empty, " ");

            var words = text.Split(
                new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;

            return Math.Max(1, (int)Math.Ceiling(words / 200.0));
        }

        /// <summary>Lista tagów artykułu.</summary>
        public static List<string> TagList(this Article article) =>
            SplitTags(article.Tags);

        public static List<string> SplitTags(string? tags) =>
            string.IsNullOrWhiteSpace(tags)
                ? new List<string>()
                : tags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

        /// <summary>Porządkuje tagi do zapisu: bez duplikatów, max 8 tagów po 30 znaków.</summary>
        public static string? NormalizeTags(string? tags)
        {
            var list = SplitTags(tags)
                .Select(t => t.Length > 30 ? t[..30].Trim() : t)
                .Take(8)
                .ToList();

            return list.Count == 0 ? null : string.Join(", ", list);
        }
    }
}