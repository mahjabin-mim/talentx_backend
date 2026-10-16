# TalentX Backend

ASP.NET Core API for TalentX authentication, jobs, applications, and talent profile flows.

## Requirements

Install these before running the backend:

- .NET 10 SDK
- PostgreSQL running locally

Optional for extra features:

- `dotnet-ef` CLI tool for creating or applying migrations manually
- SMTP credentials if you want emails to be sent instead of logged
- AWS credentials and an S3 bucket if you want resume/profile uploads to work

## Tech Stack

- ASP.NET Core
- .NET 10
- Entity Framework Core 9
- PostgreSQL
- MediatR
- FluentValidation
- JWT authentication
- xUnit

## Setup After Cloning

### 1. Go to the backend project

```bash
cd talentx2026q1.backend
```

### 2. Restore packages

```bash
dotnet restore
```

### 3. Trust the local HTTPS development certificate

The API runs on `https://localhost:5000` in development.

```bash
dotnet dev-certs https --trust
```

### 4. Configure local settings

Update these files if your local setup is different:

- `app/Host/API/appsettings.json`
- `app/Host/API/appsettings.Development.json`

Important settings:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `Cors:AllowedOrigins`
- `Smtp` section
- `S3` section

Recommended local values:

- Backend URL: `https://localhost:5000`
- Frontend URL in CORS: `https://localhost:3000`
- PostgreSQL database name: `talentx2026q1`

Notes:

- The frontend expects the API base URL to be `https://localhost:5000/api/v1`.
- For local development, make sure `Cors:AllowedOrigins` includes the frontend URL.
- SMTP is optional for starting the API. If SMTP is not configured, email sending falls back to logging.
- S3 is optional for basic startup, but upload endpoints need a valid bucket and AWS credentials.

### 5. Make sure PostgreSQL is running

The default development connection string points to:

```text
Host=localhost;Port=5432;Database=talentx2026q1;Username=postgres;Password=postgres
```

You can either:

- create a local PostgreSQL database and user that match this connection string, or
- change `DefaultConnection` to values that match your machine

### 6. Start the API

```bash
dotnet run --project app/Host/API/Host.csproj
```

The API will start at:

- `https://localhost:5000`
- Swagger UI: `https://localhost:5000/swagger`

The app applies pending EF Core migrations automatically on startup.

## Common Commands

```bash
dotnet build TalentX.sln
dotnet test TalentX.sln
dotnet run --project app/Host/API/Host.csproj
```

## Manual Migrations

Install `dotnet-ef` if you do not already have it:

```bash
dotnet tool install --global dotnet-ef
```

Common migration commands:

```bash
dotnet ef migrations add MigrationName --project app/Migrators/PostgreSQL/PostgreSQL.csproj --startup-project app/Host/API/Host.csproj
dotnet ef database update --project app/Migrators/PostgreSQL/PostgreSQL.csproj --startup-project app/Host/API/Host.csproj
```

## Main API Endpoints

Auth:

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/oauth/google`
- `POST /api/v1/auth/role`

Jobs:

- `GET /api/v1/jobs/public`
- `GET /api/v1/jobs/{jobId}`
- `GET /api/v1/jobs/dashboard`
- `POST /api/v1/jobs/generate`
- `PATCH /api/v1/jobs/{jobId}/status`
- `DELETE /api/v1/jobs/{jobId}`

## Project Structure

```text
app/
  Core/
  Host/API/
  Migrators/PostgreSQL/

tests/
  IntegrationTests/
  UnitTests/
```

## Extra Documentation

See the `docs` folder for more detail:

- `docs/api-endpoints.md`
- `docs/database-migrations.md`
- `docs/testing.md`
- `docs/operational-concerns.md`
- `docs/application-layer-pattern.md`
