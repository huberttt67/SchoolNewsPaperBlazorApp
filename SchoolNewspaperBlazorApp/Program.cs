
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
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

            builder.Services
                .AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddCascadingAuthenticationState();

            builder.Services
                .AddAuthentication(
                    CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/login";

                    options.Cookie.Name = "SchoolNewspaper.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy =
                        CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;

                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AccessAdminPanel", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(
                        "permission",
                        EnumPermission.AccessAdminPanel.ToString());
                });
            });

            builder.Services.AddDbContextFactory<NewspaperDbContext>(
                options => options.UseSqlServer(
                    builder.Configuration.GetConnectionString(
                        "NewspaperConnectionString")));

            builder.Services.AddScoped<
                IArticleRepository, ArticleRepository>();

            builder.Services.AddScoped<
                IFileRepository, FileRepository>();

            builder.Services.AddScoped<
                IAuthRepository, AuthRepository>();

            builder.Services.AddScoped<
                IPermissionRepository, PermissionRepository>();

            builder.Services.AddScoped<
                IArticleService, ArticleService>();

            builder.Services.AddScoped<
                IFileService, FileService>();

            builder.Services.AddScoped<
                IAuthService, AuthService>();

            // DODANE: serwis zarządzania uprawnieniami.
            builder.Services.AddScoped<
                IPermissionService, PermissionService>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Zachowaj dostęp do zdjęć ze starszej lokalizacji projektu,
            // jeśli folder Images istnieje obok pliku projektu.
            var legacyImagesPath = Path.Combine(builder.Environment.ContentRootPath, "Images");
            if (Directory.Exists(legacyImagesPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(legacyImagesPath),
                    RequestPath = "/Images"
                });
            }


            app.UseAuthentication();
            app.UseAuthorization();

            app.MapPost("/auth/login", async (
                HttpContext context,
                IAuthService authService,
                IAuthRepository authRepository,
                IPermissionRepository permissionRepository) =>
            {
                var form = await context.Request.ReadFormAsync();

                var email = form["Email"].ToString().Trim();
                var password = form["Password"].ToString();

                var valid = await authService.LoginAsync(email, password);

                if (!valid)
                {
                    return Results.Redirect("/login?error=invalid");
                }

                var user =
                    await authRepository.GetByEmailAsync(email);

                if (user is null)
                {
                    return Results.Redirect("/login?error=invalid");
                }

                var permissions =
                    await permissionRepository.GetUserPermissionsAsync(
                        user.Id);

                var claims = new List<Claim>
                {
                    new(
                        ClaimTypes.NameIdentifier,
                        user.Id.ToString()),

                    new(
                        ClaimTypes.Name,
                        user.Login),

                    new(
                        ClaimTypes.Email,
                        user.Email)
                };

                foreach (var permission in permissions)
                {
                    claims.Add(
                        new Claim("permission", permission.ToString()));
                }

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal);

                return Results.Redirect("/adminPanel?toast=login");
            });

            app.MapPost("/auth/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return Results.Redirect("/login?toast=logout");
            });

            app.MapGet("/auth/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return Results.Redirect("/login?toast=logout");
            });

            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
