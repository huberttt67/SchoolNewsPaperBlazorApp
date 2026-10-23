﻿using Microsoft.EntityFrameworkCore;

namespace SchoolNewspaperBlazorApp.Data
{
    public class NewspaperDbContext : DbContext
    {
        public NewspaperDbContext(
            DbContextOptions<NewspaperDbContext> options)
            : base(options)
        {
        }

        public DbSet<Article> Articles => Set<Article>();

        public DbSet<MediaFile> Files => Set<MediaFile>();

        public DbSet<User> Users => Set<User>();

        public DbSet<Permission> Permissions => Set<Permission>();

        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.Entity<MediaFile>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            mb.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            mb.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId);

            mb.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId);

            mb.Entity<Permission>()
                .HasIndex(p => p.Name)
                .IsUnique();

            // Jeden adres e-mail = jedno konto (egzekwowane przez bazę danych).
            mb.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(256);

            // Soft Delete: unikalność e-maila dotyczy tylko aktywnych kont,
            // dzięki temu e-mail usuniętego logicznie konta można użyć ponownie.
            mb.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // Soft Delete: zwykłe zapytania nie zwracają usuniętych rekordów.
            // Dostęp do nich możliwy tylko jawnie przez IgnoreQueryFilters().
            mb.Entity<Article>().HasQueryFilter(a => !a.IsDeleted);
            mb.Entity<MediaFile>().HasQueryFilter(f => !f.IsDeleted);
            mb.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

            // Uprawnienia usuniętego logicznie konta są niewidoczne
            // (konto bez uprawnień nie ma dostępu do panelu).
            mb.Entity<UserPermission>()
                .HasQueryFilter(up => !up.User.IsDeleted);
        }
    }
}