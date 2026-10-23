<#
 Przygotowuje repozytorium Git (galaz "dev") i otwiera projekt w VS Code Insiders.
 Skrypt NIE wykonuje commita ani pusha - zrobisz to w VS Code (Source Control).

 Uruchomienie (PowerShell, w folderze z tym plikiem):
   powershell -ExecutionPolicy Bypass -File .\Przygotuj-do-GitHub-Insiders.ps1
 Opcjonalnie z adresem repozytorium:
   .\Przygotuj-do-GitHub-Insiders.ps1 -RepoUrl https://github.com/TWOJA-NAZWA/TWOJE-REPO.git
#>
param(
    [string]$RepoUrl = ""
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "Nie znaleziono Git. Zainstaluj Git (https://git-scm.com) i uruchom skrypt ponownie." -ForegroundColor Red
    exit 1
}

# Ochrona przed przypadkowym .git skopiowanym z innego repozytorium:
# jesli folder .git istnieje, ale katalog glowny repo to NIE ten folder, przerywamy.
if (Test-Path ".git") {
    $top = (git rev-parse --show-toplevel 2>$null)
    $here = (Resolve-Path ".").Path
    if (-not $top -or ((Resolve-Path $top).Path -ne $here)) {
        Write-Host "Folder .git w tym katalogu jest nieprawidlowy (nie nalezy do tego projektu)." -ForegroundColor Red
        Write-Host "Usun go recznie (to tylko metadane Git, nie kod projektu) i uruchom skrypt ponownie." -ForegroundColor Yellow
        exit 1
    }
}
else {
    git init | Out-Null
    Write-Host "Zainicjowano nowe repozytorium Git." -ForegroundColor Green
}

# Galaz dev (bez utraty zmian)
git checkout -B dev 2>$null | Out-Null
Write-Host "Aktywna galaz: dev" -ForegroundColor Green

# Adres zdalnego repozytorium GitHub (origin)
$origin = (git remote get-url origin 2>$null)
if (-not $origin) {
    if (-not $RepoUrl) {
        $RepoUrl = Read-Host "Podaj adres repozytorium GitHub (np. https://github.com/uzytkownik/repo.git) albo zostaw puste, aby dodac go pozniej"
    }
    if ($RepoUrl) {
        git remote add origin $RepoUrl
        Write-Host "Dodano origin: $RepoUrl" -ForegroundColor Green
    }
    else {
        Write-Host "UWAGA: nie ustawiono adresu repozytorium GitHub - Push nie bedzie mozliwy," -ForegroundColor Yellow
        Write-Host "dopoki nie wykonasz: git remote add origin <adres-repozytorium>" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Istniejacy origin: $origin" -ForegroundColor Cyan
}

git add -A
Write-Host "Zmiany dodane do poczekalni (git add)." -ForegroundColor Green

$insiders = Get-Command code-insiders -ErrorAction SilentlyContinue
if ($insiders) {
    code-insiders .
    Write-Host "Otwarto projekt w VS Code Insiders." -ForegroundColor Green
}
else {
    Write-Host "Nie znaleziono polecenia 'code-insiders'. Otworz folder recznie w VS Code Insiders." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Dalej w VS Code Insiders: Source Control -> wpisz opis -> Commit -> Sync/Push (galaz dev)." -ForegroundColor Cyan
Write-Host "Przy pierwszym pushu: git push -u origin dev" -ForegroundColor Cyan
