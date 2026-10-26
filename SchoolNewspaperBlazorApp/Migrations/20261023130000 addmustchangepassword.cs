using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolNewspaperBlazorApp.Migrations
{
    /// <summary>
    /// Znacznik hasła tymczasowego (do zmiany po pierwszym logowaniu).
    /// Istniejące konta dostają false, więc nic się dla nich nie zmienia.
    /// </summary>
    [DbContext(typeof(SchoolNewspaperBlazorApp.Data.NewspaperDbContext))]
    [Migration("20261023130000_AddMustChangePassword")]
    public partial class AddMustChangePassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");
        }
    }
}