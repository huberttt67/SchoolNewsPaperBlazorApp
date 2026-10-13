namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    
        public interface IAuthService
        {
        Task AddUserAsync(string login, string password, string email);
        Task LoginAsync(string login, string email, string haslo);

        
    }
    
}
