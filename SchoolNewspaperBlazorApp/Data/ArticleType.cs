﻿namespace SchoolNewspaperBlazorApp.Data
{
    /// <summary>
    /// Rodzaj artykułu – każdy rodzaj ma na stronie głównej własną zakładkę.
    /// Wartości liczbowe zapisują się w bazie jako int, więc nie zmieniaj ich kolejności
    /// ani numerów; nowe rodzaje dopisuj zawsze na końcu.
    /// </summary>
    public enum ArticleType
    {
        Nowosci = 0,
        Wydarzenia = 1,
        Ogloszenia = 2,
        Substitute = 3,
        Competitions = 4,
        Admissions = 5,
        Exams = 6,
        StudentCouncil = 7
    }

    public static class ArticleTypeExtensions
    {
        /// <summary>Nazwa wyświetlana użytkownikowi.</summary>
        public static string Label(this ArticleType type) => type switch
        {
            ArticleType.Nowosci => "Nowości",
            ArticleType.Wydarzenia => "Wydarzenia",
            ArticleType.Ogloszenia => "Ogłoszenia",
            ArticleType.Substitute => "Zastępstwa",
            ArticleType.Competitions => "Zawody",
            ArticleType.Admissions => "Rekrutacja",
            ArticleType.Exams => "Egzaminy",
            ArticleType.StudentCouncil => "Samorząd uczniowski",
            _ => type.ToString()
        };

        /// <summary>Krótki opis pod nagłówkiem zakładki.</summary>
        public static string Description(this ArticleType type) => type switch
        {
            ArticleType.Nowosci => "Świeże wiadomości od redakcji gazetki.",
            ArticleType.Wydarzenia => "Uroczystości, konkursy, wycieczki i inne szkolne wydarzenia.",
            ArticleType.Ogloszenia => "Ważne komunikaty dla uczniów, rodziców i nauczycieli.",
            ArticleType.Substitute => "Zmiany w planie lekcji i informacje o zastępstwach.",
            ArticleType.Competitions => "Zawody sportowe, turnieje i rywalizacje z udziałem naszych uczniów.",
            ArticleType.Admissions => "Wszystko o naborze do szkoły: terminy, zasady i dokumenty.",
            ArticleType.Exams => "Harmonogramy, informacje i wskazówki dotyczące egzaminów.",
            ArticleType.StudentCouncil => "Działania, inicjatywy i komunikaty samorządu uczniowskiego.",
            _ => string.Empty
        };

        /// <summary>Komunikat widoczny, gdy w zakładce nie ma jeszcze artykułów.</summary>
        public static string EmptyText(this ArticleType type) => type switch
        {
            ArticleType.Nowosci => "Nie ma jeszcze żadnych nowości.",
            ArticleType.Wydarzenia => "Nie zaplanowano jeszcze żadnych wydarzeń.",
            ArticleType.Ogloszenia => "Nie ma obecnie żadnych ogłoszeń.",
            ArticleType.Substitute => "Brak informacji o zastępstwach.",
            ArticleType.Competitions => "Brak informacji o zawodach.",
            ArticleType.Admissions => "Brak informacji o rekrutacji.",
            ArticleType.Exams => "Brak informacji o egzaminach.",
            ArticleType.StudentCouncil => "Samorząd uczniowski nie opublikował jeszcze żadnych wpisów.",
            _ => "Brak artykułów w tej kategorii."
        };

        /// <summary>
        /// Wartość parametru w adresie URL (małymi literami), np. /?typ=studentcouncil.
        /// Odczyt nie rozróżnia wielkości liter, więc działa też /?typ=StudentCouncil.
        /// </summary>
        public static string Slug(this ArticleType type) =>
            type.ToString().ToLowerInvariant();

        public static bool TryParseSlug(string? slug, out ArticleType type)
        {
            foreach (var candidate in Enum.GetValues<ArticleType>())
            {
                if (string.Equals(candidate.Slug(), slug, StringComparison.OrdinalIgnoreCase))
                {
                    type = candidate;
                    return true;
                }
            }

            type = ArticleType.Nowosci;
            return false;
        }
    }
}