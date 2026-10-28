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

            // Blokada po 5 błędnych próbach logowania.
            builder.Services.AddSingleton<LoginAttemptTracker>();

            // Moduł zastępstw.
            builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
            builder.Services.AddScoped<ISubstituteRepository, SubstituteRepository>();
            builder.Services.AddScoped<ITeacherService, TeacherService>();
            builder.Services.AddScoped<ISubstituteService, SubstituteService>();

            var app = builder.Build();

            // Sale: tworzy tabelę Rooms i kolumnę Substitutes.RoomId, jeśli ich
            // jeszcze nie ma (bezpieczne przy każdym starcie, nie rusza danych).
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var factory = scope.ServiceProvider
                        .GetRequiredService<IDbContextFactory<NewspaperDbContext>>();
                    using var db = factory.CreateDbContext();

                    db.Database.ExecuteSqlRaw(
                        "IF OBJECT_ID(N'[Rooms]', N'U') IS NULL " +
                        "CREATE TABLE [Rooms] (" +
                        "[Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, " +
                        "[Name] nvarchar(50) NOT NULL, " +
                        "[IsDeleted] bit NOT NULL DEFAULT 0)");

                    db.Database.ExecuteSqlRaw(
                        "IF COL_LENGTH(N'Substitutes', N'RoomId') IS NULL " +
                        "ALTER TABLE [Substitutes] ADD [RoomId] int NULL");

                    db.Database.ExecuteSqlRaw(
                        "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys " +
                        "WHERE name = N'FK_Substitutes_Rooms_RoomId') " +
                        "ALTER TABLE [Substitutes] ADD CONSTRAINT " +
                        "[FK_Substitutes_Rooms_RoomId] FOREIGN KEY ([RoomId]) " +
                        "REFERENCES [Rooms] ([Id])");
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Nie udało się przygotować tabeli sal.");
                }
            }

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

            // Użytkownik z hasłem tymczasowym może otworzyć wyłącznie stronę
            // zmiany hasła, wylogowanie i zasoby techniczne Blazora.
            app.Use(async (context, next) =>
            {
                if (context.User.Identity?.IsAuthenticated == true &&
                    context.User.HasClaim("must_change_password", "1"))
                {
                    var path = context.Request.Path;

                    var allowed =
                        path.StartsWithSegments("/zmiana-hasla") ||
                        path.StartsWithSegments("/auth") ||
                        path.StartsWithSegments("/_framework") ||
                        path.StartsWithSegments("/_blazor");

                    if (!allowed)
                    {
                        context.Response.Redirect("/zmiana-hasla");
                        return;
                    }
                }

                await next();
            });

            app.UseAuthorization();

            app.MapPost("/auth/login", async (
                HttpContext context,
                IAuthService authService,
                IAuthRepository authRepository,
                IPermissionRepository permissionRepository,
                LoginAttemptTracker attempts) =>
            {
                var form = await context.Request.ReadFormAsync();

                var email = form["Email"].ToString().Trim();
                var password = form["Password"].ToString();
                var ip = context.Connection.RemoteIpAddress?.ToString();

                var locked = attempts.GetLockRemaining(email, ip);

                if (locked is { } remaining)
                {
                    var minutes = (int)Math.Ceiling(remaining.TotalMinutes);
                    return Results.Redirect($"/login?toast=locked&min={minutes}");
                }

                var valid = await authService.LoginAsync(email, password);

                if (!valid)
                {
                    if (attempts.RegisterFailure(email, ip))
                    {
                        var min = (int)LoginAttemptTracker.LockDuration.TotalMinutes;
                        return Results.Redirect($"/login?toast=locked&min={min}");
                    }

                    return Results.Redirect("/login?error=invalid");
                }

                attempts.Reset(email, ip);

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

                // Hasło tymczasowe: do czasu zmiany dostępna jest tylko
                // strona zmiany hasła (wymusza to middleware poniżej).
                if (user.MustChangePassword)
                {
                    claims.Add(new Claim("must_change_password", "1"));
                }

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal);

                if (user.MustChangePassword)
                {
                    return Results.Redirect("/zmiana-hasla");
                }

                return Results.Redirect("/adminPanel?toast=login");
            });

            app.MapPost("/auth/change-password", async (
                HttpContext context,
                IAuthService authService) =>
            {
                if (context.User.Identity?.IsAuthenticated != true)
                {
                    return Results.Redirect("/login");
                }

                var idValue = context.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(idValue, out var userId))
                {
                    return Results.Redirect("/login");
                }

                var form = await context.Request.ReadFormAsync();

                var current = form["CurrentPassword"].ToString();
                var newPassword = form["NewPassword"].ToString();
                var confirm = form["ConfirmPassword"].ToString();

                if (newPassword != confirm)
                {
                    return Results.Redirect("/zmiana-hasla?error=mismatch");
                }

                var error = await authService.ChangeTemporaryPasswordAsync(
                    userId,
                    current,
                    newPassword);

                if (error is not null)
                {
                    return Results.Redirect($"/zmiana-hasla?error={error}");
                }

                // Odświeżamy ciasteczko bez znacznika „hasło tymczasowe”.
                var claims = context.User.Claims
                    .Where(c => c.Type != "must_change_password")
                    .ToList();

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

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