
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using SchoolNewspaperBlazorApp.Components;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;
using SchoolNewspaperBlazorApp.Repository;
using SchoolNewspaperBlazorApp.Service;
using System.Security.Claims;

namespace SchoolNewspaperBlazorApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Połączenie z SQL Server
            builder.Services.AddDbContextFactory<NewspaperDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString(
                        "NewspaperConnectionString")));

            // Repozytoria
            builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
            builder.Services.AddScoped<IFileRepository, FileRepository>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();

            // Serwisy
            builder.Services.AddScoped<IArticleService, ArticleService>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.Cookie.Name = "SchoolNewspaper.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                    options.SlidingExpiration = true;
                }
                );
            var app = builder.Build();


            // Konfiguracja obsługi błędów
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.MapPost("/auth/login", async (
     HttpContext context,
     IAuthService authService) =>
            {
                var form = await context.Request.ReadFormAsync();
                string email = form["Email"].ToString();
                string password = form["Password"].ToString();
                bool success = await authService.LoginAsync(email, password);
     if (!success)
                {
                    return Results.Redirect("/");

                }
                var claims = new List<Claim>
     {
         new Claim(ClaimTypes.Name,email)
     };
                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    principal);
                return Results.Redirect("/adminPanel");
            });


            app.MapPost("/auth/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return Results.Redirect("/login");
            });

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    @"C:\Users\!Uczen2026\source\repos\SchoolNewspaperBlazorApp\Images"),
                RequestPath = "/Images"
            });

            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
