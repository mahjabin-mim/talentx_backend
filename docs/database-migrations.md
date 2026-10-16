# Database & Migrations

## Users Table

The `Users` table is mapped from `Domain.Entities.User` using EF Core configuration in `Infrastructure.Configurations.UserConfiguration`.

### Columns

- `Id` (GUID, PK, required)
- `Fullname` (varchar(120), nullable)
- `Email` (varchar(254), required, unique)
- `Password` (varchar(200), required, hashed with BCrypt)
- `ProfileImageURL` (varchar(500), nullable)
- `PhoneNumber` (varchar(20), nullable)
- `IsActive` (bool, required, default true)
- `EmailConfirmed` (bool, required, default false)
- `EmailVerificationCode` (varchar(128), nullable, SHA-256 hash)
- `EmailVerificationCodeExpiryTime` (timestamp with time zone, nullable)
- `ForgetPasswordVerificationCode` (varchar(128), nullable, SHA-256 hash)
- `ForgotPasswordVerificationCodeExpiryTime` (timestamp with time zone, nullable)
- `RefreshToken` (varchar(128), nullable, SHA-256 hash)
- `RefreshTokenExpiryTime` (timestamp with time zone, nullable)
- `PreviousRefreshToken` (varchar(128), nullable, SHA-256 hash)
- `PreviousRefreshTokenExpiryTime` (timestamp with time zone, nullable)
- `CreatedBy` (varchar(100), nullable)
- `CreatedOn` (timestamp with time zone, required)
- `LastModifiedBy` (varchar(100), nullable)
- `LastModifiedOn` (timestamp with time zone, nullable)
- `DeletedOn` (timestamp with time zone, nullable)
- `DeletedBy` (varchar(100), nullable)
- `Deleted` (bool, required, default false)
- `UserRole` (int, nullable, enum: Admin=5, Employer=10, Talent=15)
- `TokenVersion` (int, required, default 0)

### Relationships

- One-to-one with `Employers` table via `Employer.UserId` foreign key

### Indexes

- Unique index on `Email`

## Employers Table

The `Employers` table is mapped from `Domain.Entities.Employer` using EF Core configuration in `Infrastructure.Configurations.EmployerConfiguration`.

### Columns

- `Id` (GUID, PK, required)
- `UserId` (GUID, required, unique, FK → `Users.Id`)
- `CompanyName` (varchar(160), required)

### Relationships

- One-to-one with `Users` table via `UserId` foreign key
- One-to-many with `Jobs` table (Employer has many Jobs)

### Notes

- No audit columns (CreatedBy, CreatedOn, etc.) after migration `20260413042140_RemoveEmployerAuditColumns`
- CompanyName is auto-generated from user's fullname if not provided during job creation

## Jobs Table

The `Jobs` table is mapped from `Domain.Entities.Job` using EF Core configuration in `Infrastructure.Configurations.JobConfiguration`.

### Columns

- `Id` (GUID, PK, required)
- `EmployerId` (GUID, required, FK → `Employers.Id`)
- `Title` (varchar(160), required)
- `Description` (varchar(4000), required)
- `Responsibilities` (varchar(4000), required)
- `SkillRequirements` (varchar(4000), required)
- `EmploymentType` (int, required, enum: FullTime=0, PartTime=1, Contract=2, Temporary=3, Internship=4)
- `WorkArrangement` (int, required, enum: Remote=0, Onsite=1, Hybrid=2)
- `ExperienceLevel` (int, required, enum: Entry=0, Junior=1, MidLevel=2, Senior=3, Lead=4, Principal=5)
- `Status` (int, required, enum: Draft=0, Active=1, Closed=2, Expired=3; default `Draft`)
- `ExperienceMinYears` (int, required)
- `ExperienceMaxYears` (int, required)
- `SalaryMin` (numeric(18,2), required)
- `SalaryMax` (numeric(18,2), required)
- `CountryCode` (varchar(3), required)
- `City` (varchar(120), required)
- `Timezone` (varchar(64), required)
- `CurrencyCode` (varchar(3), required)
- `NumberOfPositions` (int, required, default 1)
- `ApplicationDeadline` (timestamp with time zone, required)
- `CreatedBy` (varchar(100), nullable)
- `CreatedOn` (timestamp with time zone, required)
- `LastModifiedBy` (varchar(100), nullable)
- `LastModifiedOn` (timestamp with time zone, nullable)
- `DeletedOn` (timestamp with time zone, nullable)
- `DeletedBy` (varchar(100), nullable)
- `Deleted` (bool, required, default false)

### Relationships

- Many-to-one with `Employers` table via `EmployerId` foreign key

### Indexes

- `IX_Jobs_EmployerId` - For employer-specific queries
- `IX_Jobs_Status` - For status-based filtering
- `IX_Jobs_ApplicationDeadline` - For expiration queries

### Field Semantics

- `Description`: Full job description (what the job entails)
- `Responsibilities`: Specific duties and responsibilities
- `SkillRequirements`: Required technical and soft skills
- For remote jobs: `CountryCode`, `City` are empty; `Timezone` is populated
- For on-site/hybrid jobs: `CountryCode`, `City` are populated; `Timezone` is empty
- Soft delete: `Deleted=true` hides jobs without physical removal

## Enums Reference

### UserRole

```csharp
public enum UserRole
{
    Admin = 5,
    Employer = 10,
    Talent = 15
}
```

### JobStatus

```csharp
public enum JobStatus
{
    Draft = 0,
    Active = 1,
    Closed = 2,
    Expired = 3
}
```

Status transitions:

- `Draft` → `Active`: Via publish or status update action
- `Active` → `Draft`: Via deactivate action
- `Active` → `Closed`: Via close action
- `Active`/`Draft` → `Expired`: Automatic when deadline passes

### EmploymentType

```csharp
public enum EmploymentType
{
    FullTime = 0,
    PartTime = 1,
    Contract = 2,
    Temporary = 3,
    Internship = 4
}
```

### WorkArrangement

```csharp
public enum WorkArrangement
{
    Remote = 0,
    Onsite = 1,
    Hybrid = 2
}
```

### ExperienceLevel

```csharp
public enum ExperienceLevel
{
    Entry = 0,
    Junior = 1,
    MidLevel = 2,
    Senior = 3,
    Lead = 4,
    Principal = 5
}
```

## Auth Security Semantics

### Token Storage and Rotation

- `RefreshToken` is stored as SHA-256 hash, never plaintext
- `PreviousRefreshToken` tracks the previously valid token for grace period handling
- Both current and previous tokens are checked during refresh to handle race conditions
- On successful refresh, current token becomes previous, new token is generated
- `TokenVersion` is embedded as a claim in access tokens
- On logout, `TokenVersion` is incremented and both refresh tokens are cleared
- Access tokens with stale `TokenVersion` are rejected by JWT validation middleware

### Password Security

- Passwords are hashed using BCrypt before storage
- Password validation requires: minimum 8 characters, uppercase, lowercase, digit, special character
- OAuth users may not have passwords if they only authenticate via Google

### Verification Codes

- `EmailVerificationCode` and `ForgetPasswordVerificationCode` are SHA-256 hashed
- Both codes have expiry timestamps for time-limited validity

## Runtime Database Management

### Automatic Migrations

The API applies EF Core migrations at startup via `Database.MigrateAsync()` in `Program.cs`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
}
```

- Only runs for relational database providers (PostgreSQL in production)
- Skipped for in-memory databases (used in integration tests)
- Ensures database is always up-to-date on application start

### DB Error Mapping

Database write exceptions are normalized by `ExceptionHandlingMiddleware` into API-friendly responses:

- `23505` (unique violation) → `409 Conflict`
- `23503` (foreign key violation) → `409 Conflict`
- `23502` (not null violation) → `400 Bad Request`
- `22001` (string data right truncation) → `400 Bad Request`
- Other PostgreSQL errors → `500 Internal Server Error`

## Migration Commands

Use EF Core migrations from project root:

### Add Migration

```bash
dotnet ef migrations add MigrationName \
  --project app/Core/Infrastructure/Infrastructure.csproj \
  --startup-project app/Host/API/Host.csproj \
  --output-dir Migrations
```

### Update Database

```bash
dotnet ef database update \
  --project app/Core/Infrastructure/Infrastructure.csproj \
  --startup-project app/Host/API/Host.csproj
```

### List Migrations

```bash
dotnet ef migrations list \
  --project app/Core/Infrastructure/Infrastructure.csproj \
  --startup-project app/Host/API/Host.csproj
```

### Remove Last Migration

```bash
dotnet ef migrations remove \
  --project app/Core/Infrastructure/Infrastructure.csproj \
  --startup-project app/Host/API/Host.csproj
```

## Migration History

### 20260413044216_AddJobResponsibilitiesAndSkillRequirements

- Adds `Jobs.Responsibilities` column (varchar(4000), required, default empty string for backfill)
- Adds `Jobs.SkillRequirements` column (varchar(4000), required, default empty string for backfill)

### 20260413042140_RemoveEmployerAuditColumns

Removes audit tracking columns from `Employers` table:

- `CreatedBy`
- `CreatedOn`
- `LastModifiedBy`
- `LastModifiedOn`
- `DeletedOn`
- `DeletedBy`
- `Deleted`

Rationale: Employer records are lightweight and rarely modified, audit trail via User table is sufficient.

### 20260412070103_AddJobNumberOfPositions

- Adds `Jobs.NumberOfPositions` column (int, required, default `1`)

### 20260412064126_AddEmployersAndJobs

Initial jobs feature migration:

- Creates `Employers` table
- Creates `Jobs` table
- Adds one-to-one relation `Employers.UserId → Users.Id`
- Adds one-to-many relation `Jobs.EmployerId → Employers.Id`
- Creates indexes on `Jobs` table: `IX_Jobs_EmployerId`, `IX_Jobs_Status`, `IX_Jobs_ApplicationDeadline`

## Connection String Configuration

PostgreSQL connection strings are configured in `appsettings.json` and `appsettings.Development.json`:

### Production

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=5432;Database=talentx2026q1;Username=postgres;Password=postgres"
    }
}
```

### Development

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=5432;Database=talentx2026q1_dev;Username=postgres;Password=postgres"
    }
}
```

### Environment Variables (Recommended for Production)

Override connection string using environment variable:

```bash
export ConnectionStrings__DefaultConnection="Host=prod-db.example.com;Port=5432;Database=talentx_prod;Username=app_user;Password=<secure-password>;SSL Mode=Require"
```

## Data Integrity Rules

### Cascading Deletes

- `Users` → `Employers`: Restrict (cannot delete user with employer profile)
- `Employers` → `Jobs`: Restrict (cannot delete employer with active jobs)

### Soft Delete Strategy

- `Users` and `Jobs` use soft delete (`Deleted = true`)
- Soft-deleted records are excluded from queries via repository filters
- Physical deletion is never performed to maintain audit trail
- `Employers` table has no soft delete (removed in migration 20260413042140)

### Automatic Behaviors

1. **Job Expiration**: Jobs are automatically marked as `Expired` when:
    - `ApplicationDeadline < CurrentTime`
    - Current status is `Active` or `Draft`
    - Occurs during dashboard fetch or job detail retrieval

2. **Employer Profile Creation**: When a user creates their first job:
    - If no employer profile exists, one is created automatically
    - CompanyName defaults to user's fullname (truncated to 160 chars)
    - If fullname is empty, defaults to "Employer Company"
