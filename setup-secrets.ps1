# Sets the development secrets (DB connection string + JWT key) with .NET User Secrets.
# Secrets are stored outside the project folder and never enter the repository.
# Usage (from the solution folder):  powershell -ExecutionPolicy Bypass -File setup-secrets.ps1
param([string]$PgPassword)

if (-not $PgPassword) {
    $secure = Read-Host "PostgreSQL password for user 'postgres'" -AsSecureString
    $PgPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
}

$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=DentalClinicDb;Username=postgres;Password=$PgPassword" --project DentalClinic.API
dotnet user-secrets set "Jwt:Key" "$jwtKey" --project DentalClinic.API

Write-Host "Done. Run the API with: dotnet run --project DentalClinic.API"
