using Microsoft.EntityFrameworkCore;

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

        public DbSet<Teacher> Teachers => Set<Teacher>();

        public DbSet<Substitute> Substitutes => Set<Substitute>();

        public DbSet<Room> Rooms => Set<Room>();

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

            // Jeden adres e-mail = jedno konto.
            mb.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(256);

            // Unikalny e-mail tylko dla aktywnych kont.
            mb.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // Usuwanie logiczne.
            mb.Entity<Article>()
                .HasQueryFilter(a => !a.IsDeleted);

            mb.Entity<Teacher>()
                .HasQueryFilter(t => !t.IsDeleted);

            mb.Entity<Substitute>()
                .HasQueryFilter(s => !s.IsDeleted);

            mb.Entity<Room>()
                .HasQueryFilter(r => !r.IsDeleted);

            mb.Entity<MediaFile>()
                .HasQueryFilter(f => !f.IsDeleted);

            mb.Entity<User>()
                .HasQueryFilter(u => !u.IsDeleted);

            // Nauczyciel prowadzący zastępstwo.
            mb.Entity<Substitute>()
                .HasOne(s => s.Teacher)
                .WithMany()
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Nauczyciel, za którego odbywa się zastępstwo.
            mb.Entity<Substitute>()
                .HasOne(s => s.OriginalTeacher)
                .WithMany()
                .HasForeignKey(s => s.OriginalTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sala, w której odbywa się zastępstwo.
            mb.Entity<Substitute>()
                .HasOne(s => s.Room)
                .WithMany()
                .HasForeignKey(s => s.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Uprawnienia usuniętego logicznie konta są niewidoczne.
            mb.Entity<UserPermission>()
                .HasQueryFilter(up => !up.User.IsDeleted);
        }
    }
}