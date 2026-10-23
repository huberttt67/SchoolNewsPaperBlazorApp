using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolNewspaperBlazorApp.Migrations
{
    /// <summary>
    /// Wymusza w bazie danych unikalność adresu e-mail użytkownika.
    /// </summary>
    [DbContext(typeof(SchoolNewspaperBlazorApp.Data.NewspaperDbContext))]
    [Migration("20261021100000_UniqueUserEmail")]
    public partial class UniqueUserEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Jeśli w bazie są już zdublowane adresy, unikalny indeks nie powstanie.
            // Zamiast niejasnego błędu SQL pokazujemy czytelną instrukcję.
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM [Users]
    GROUP BY LOWER([Email])
    HAVING COUNT(*) > 1)
BEGIN
    THROW 50001, 'W tabeli Users istnieją konta z tym samym adresem e-mail. Usuń duplikaty (SELECT LOWER(Email), COUNT(*) FROM Users GROUP BY LOWER(Email) HAVING COUNT(*) > 1) i uruchom migrację ponownie.', 1;
END");

            // nvarchar(max) nie może być kluczem indeksu, więc skracamy kolumnę.
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);
        }
    }
}
