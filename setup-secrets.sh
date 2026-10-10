#!/usr/bin/env bash
# Sets the development secrets (DB connection string + JWT key) with .NET User Secrets.
# Usage (from the solution folder): ./setup-secrets.sh
set -e
read -rsp "PostgreSQL password for user 'postgres': " PG_PASSWORD; echo
JWT_KEY=$(head -c 48 /dev/urandom | base64 | tr -d '\n')
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=DentalClinicDb;Username=postgres;Password=${PG_PASSWORD}" --project DentalClinic.API
dotnet user-secrets set "Jwt:Key" "${JWT_KEY}" --project DentalClinic.API
echo "Done. Run the API with: dotnet run --project DentalClinic.API"
