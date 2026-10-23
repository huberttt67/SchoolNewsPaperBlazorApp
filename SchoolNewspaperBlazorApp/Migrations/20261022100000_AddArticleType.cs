using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolNewspaperBlazorApp.Migrations
{
    /// <summary>
    /// Dodaje rodzaj artykułu (Nowości / Wydarzenia / Ogłoszenia).
    /// Istniejące artykuły dostają wartość 0, czyli „Nowości”.
    /// </summary>
    [DbContext(typeof(SchoolNewspaperBlazorApp.Data.NewspaperDbContext))]
    [Migration("20261022100000_AddArticleType")]
    public partial class AddArticleType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Articles");
        }
    }
}
