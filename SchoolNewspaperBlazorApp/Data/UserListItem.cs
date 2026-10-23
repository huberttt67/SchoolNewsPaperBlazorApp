namespace SchoolNewspaperBlazorApp.Data
{
    /// <summary>
    /// Wiersz listy użytkowników w panelu (nie jest encją bazy danych).
    /// Nie zawiera hasła ani hasha.
    /// </summary>
    public class UserListItem
    {
        public int Id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public bool IsMaster { get; set; }

        // Ustawiane przez serwis: czy zalogowany może edytować to konto.
        public bool CanEdit { get; set; }
    }
}