# Zmiany: uprawnienia, ILogger, Soft Delete

## Migracja bazy danych (wymagana)

Dodano migrację `20261023100000_AddSoftDelete` (kolumna `IsDeleted` w tabelach
`Articles`, `Users`, `Files`; unikalny indeks e-maila obejmuje tylko aktywne konta).
Dane nie są usuwane ani zmieniane; istniejące rekordy dostają `IsDeleted = 0`.

    cd SchoolNewspaperBlazorApp
    dotnet ef database update

Migracja została napisana ręcznie (bez Designer.cs, tak jak istniejąca `AddArticleType`),
snapshot został uzupełniony. Jeśli wolisz wygenerować ją narzędziem:
usuń pliki `20261023100000_AddSoftDelete.cs`, wygeneruj `dotnet ef migrations add AddSoftDelete`
i zastosuj `dotnet ef database update`.

## GitHub / VS Code Insiders

    powershell -ExecutionPolicy Bypass -File .\Przygotuj-do-GitHub-Insiders.ps1

Skrypt ustawia gałąź `dev`, pyta o adres repozytorium GitHub (jeśli nie ma `origin`)
i otwiera VS Code Insiders. Commit i Push wykonujesz w VS Code.
