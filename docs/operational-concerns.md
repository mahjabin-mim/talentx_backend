# Operational Concerns

## 1. Token Security and Revocation

### Token Architecture

- **Access tokens** carry `token_version` claim for revocation support
- **Refresh tokens** are SHA-256 hashed before storage (never stored in plaintext)
- **Previous refresh tokens** are tracked for race condition handling during token rotation

### Token Validation Flow

1. JWT middleware validates access token signature and expiration
2. Custom token version validation checks `token_version` claim against `Users.TokenVersion`
3. Mismatched versions result in `401 Unauthorized` response
4. User must re-authenticate to obtain new tokens

### Refresh Token Rotation

- Each refresh request generates new refresh token and invalidates old one
- Previous token is stored temporarily to handle race conditions
- Both current and previous tokens are accepted during grace period
- On successful refresh: current becomes previous, new token is issued

### Logout and Revocation

- Logout clears both `RefreshToken` and `PreviousRefreshToken`
- `TokenVersion` is incremented to invalidate all existing access tokens
- User must re-authenticate to obtain new tokens
- Existing access tokens become invalid immediately after logout

### Token Expiry Configuration

Configured in `appsettings.json`:

```json
{
  "Jwt": {
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 30
  }
}
```

**Production Recommendations**:
- Access tokens: 15 minutes (balance between security and UX)
- Refresh tokens: 7-30 days (adjust based on security requirements)

## 2. Secrets and Configuration Management

### Required Secrets

- **JWT Signing Key** (`Jwt:Key`)
  - Minimum 256 bits (32 characters) for HS256
  - Use cryptographically secure random generation
  - Rotate every 90 days in production
  
- **Database Credentials** (`ConnectionStrings:DefaultConnection`)
  - Use separate credentials for different environments
  - Never commit to source control
  
- **OAuth Credentials** (when implemented)
  - Google OAuth client ID and secret
  - Other OAuth provider credentials

### Configuration Priority

.NET configuration sources (highest to lowest priority):

1. Command-line arguments
2. Environment variables
3. User secrets (Development only)
4. appsettings.{Environment}.json
5. appsettings.json

### Environment Variables

Override settings using environment variables:

```bash
# Connection string
export ConnectionStrings__DefaultConnection="Host=prod-db;Port=5432;Database=talentx_prod;Username=app_user;Password=<secure>"

# JWT configuration
export Jwt__Key="<secure-256-bit-key>"
export Jwt__Issuer="https://api.talentx.com"
export Jwt__Audience="https://app.talentx.com"

# CORS
export Cors__AllowedOrigins__0="https://app.talentx.com"
export Cors__AllowedOrigins__1="https://admin.talentx.com"
```

### Secret Rotation Procedures

**JWT Signing Key Rotation**:
1. Generate new signing key
2. Update configuration with new key
3. Deploy to all API instances
4. Monitor for authentication errors
5. All users will need to re-authenticate after old tokens expire

**Database Password Rotation**:
1. Create new database user with same permissions
2. Update connection string to use new credentials
3. Deploy application update
4. Verify connectivity and health
5. Revoke old database user credentials

### Never Commit to Source Control

- Production connection strings
- JWT signing keys
- OAuth client secrets
- API keys and service credentials
- User secrets files (`secrets.json`)

Use `.gitignore`:

```
appsettings.Production.json
**/secrets.json
.env
.env.local
```

## 3. Database and Migrations

### Automatic Migration Bootstrap

The application automatically applies pending migrations on startup:

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
}
```

**Behavior**:
- Only runs for relational database providers (PostgreSQL)
- Skipped for in-memory providers (integration tests)
- Runs before the API starts accepting requests
- Application startup fails if migration fails

### Pre-Deployment Migration Validation

**Before deploying to production**:

1. **Review migration history**:
   ```bash
   dotnet ef migrations list --project app/Core/Infrastructure/Infrastructure.csproj --startup-project app/Host/API/Host.csproj
   ```

2. **Generate SQL script** (for review):
   ```bash
   dotnet ef migrations script --project app/Core/Infrastructure/Infrastructure.csproj --startup-project app/Host/API/Host.csproj --output migration.sql
   ```

3. **Test in staging environment** first:
   - Deploy to staging
   - Verify automatic migration succeeds
   - Run smoke tests on critical endpoints
   - Validate data integrity

4. **Backup production database** before deployment:
   ```bash
   pg_dump -h prod-host -U postgres -d talentx_prod > backup_$(date +%Y%m%d_%H%M%S).sql
   ```

### Migration Rollback Strategy

**If migration fails**:

1. **Automatic rollback** (application won't start)
2. **Manual rollback**:
   ```bash
   dotnet ef database update PreviousMigrationName --project app/Core/Infrastructure/Infrastructure.csproj --startup-project app/Host/API/Host.csproj
   ```

3. **Restore from backup**:
   ```bash
   psql -h prod-host -U postgres -d talentx_prod < backup_20260415_100000.sql
   ```

### Database Maintenance

**Regular Tasks**:

- **Vacuum** (weekly): `VACUUM ANALYZE;`
- **Reindex** (monthly): `REINDEX DATABASE talentx_prod;`
- **Backup verification** (daily): Restore backup to test environment
- **Monitor slow queries**: Use `pg_stat_statements` extension

## 4. Failure Handling and Observability

### Global Exception Middleware

All exceptions are caught by `ExceptionHandlingMiddleware` and converted to `application/problem+json` responses:

```json
{
  "type": "https://httpstatuses.com/500",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred.",
  "traceId": "00-abc123..."
}
```

### Error Classification

- **400 Bad Request**: Client/validation errors (log as Warning)
- **401 Unauthorized**: Auth failures (log as Warning)
- **409 Conflict**: Database constraint violations (log as Warning)
- **500 Internal Server Error**: Unhandled exceptions (log as Error)

### Logging Configuration

Configure in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

**Production Recommendations**:
- Use structured logging (Serilog, NLog)
- Send logs to centralized system (ELK, Seq, Application Insights)
- Include correlation IDs for request tracing
- Log at appropriate levels (avoid Debug in production)

### Structured Logging Example

```csharp
_logger.LogInformation("User {UserId} created job {JobId}", userId, jobId);
_logger.LogWarning("Failed login attempt for email: {Email}", email);
_logger.LogError(ex, "Database connection failed during {Operation}", "job creation");
```

### Health Checks

**Recommended endpoints**:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .AddDbContextCheck<AppDbContext>();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
```

### Monitoring Metrics

**Track**:
- Request duration and throughput
- Error rates by endpoint
- Database connection pool usage
- Token refresh rates
- Failed authentication attempts
- Job creation/update rates

## 5. Incident Response Scenarios

### Scenario: Token Theft Suspected

**Symptoms**: Unusual activity patterns, unexpected API calls

**Response**:
1. Identify affected user accounts
2. Force logout for affected users:
   ```sql
   UPDATE "Users" 
   SET "TokenVersion" = "TokenVersion" + 1, 
       "RefreshToken" = NULL,
       "PreviousRefreshToken" = NULL,
       "RefreshTokenExpiryTime" = NULL,
       "PreviousRefreshTokenExpiryTime" = NULL
   WHERE "Id" = '<user-id>';
   ```
3. Notify affected users
4. If broad compromise suspected, rotate JWT signing key
5. Review logs for access patterns
6. Implement rate limiting if not present

### Scenario: Database Unique Conflicts Spike

**Symptoms**: High rate of `409 Conflict` responses

**Actions**:
1. Check monitoring dashboard for spike in registration/login traffic
2. Inspect logs for duplicate email patterns:
   ```sql
   SELECT "Email", COUNT(*) 
   FROM "Users" 
   WHERE "CreatedOn" > NOW() - INTERVAL '1 hour'
   GROUP BY "Email" 
   HAVING COUNT(*) > 1;
   ```
3. Verify client retry behavior (exponential backoff implemented?)
4. Check for automation/bot activity
5. Consider implementing request deduplication
6. Implement rate limiting by IP/user

### Scenario: OAuth Provider Issue

**Symptoms**: Google OAuth login failures, `401 Unauthorized` responses

**Actions**:
1. Verify OAuth provider status page
2. Check backend OAuth credentials:
   - Client ID and secret
   - Redirect URIs configuration
3. Test OAuth flow manually
4. Review auth logs for error patterns:
   ```bash
   grep "GoogleAuthCommand" logs/ | grep "Error"
   ```
5. Implement fallback to email/password login
6. Display user-friendly error message

### Scenario: High Database Connection Count

**Symptoms**: Connection pool exhausted, slow response times

**Actions**:
1. Check active connections:
   ```sql
   SELECT COUNT(*) FROM pg_stat_activity WHERE datname = 'talentx_prod';
   ```
2. Identify long-running queries:
   ```sql
   SELECT pid, query, state, query_start 
   FROM pg_stat_activity 
   WHERE state != 'idle' AND query_start < NOW() - INTERVAL '30 seconds';
   ```
3. Review connection pool configuration
4. Check for connection leaks in code
5. Terminate problematic queries if necessary:
   ```sql
   SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE pid = <pid>;
   ```
6. Scale database if consistently high load

### Scenario: Automatic Job Expiration Not Working

**Symptoms**: Jobs still showing as Active after deadline passed

**Actions**:
1. Verify dashboard endpoint is being called (triggers expiration)
2. Check for database time zone issues:
   ```sql
   SELECT NOW(), "ApplicationDeadline", "Status" 
   FROM "Jobs" 
   WHERE "ApplicationDeadline" < NOW() AND "Status" = 1;
   ```
3. Review expiration logic in `GetEmployerDashboardQueryHandler`
4. Implement background job for expiration if needed
5. Consider database trigger for automatic status updates

## 6. Deployment Safety Checklist

### Pre-Deployment

- [ ] All tests pass (unit + integration)
- [ ] Code review completed and approved
- [ ] Migration script reviewed and tested in staging
- [ ] Backup of production database taken
- [ ] Secrets/configuration verified for target environment
- [ ] Rollback plan documented and understood
- [ ] Monitoring dashboards prepared
- [ ] Incident response team notified

### Deployment

- [ ] Deploy to staging first
- [ ] Run smoke tests in staging
- [ ] Verify database migrations applied successfully
- [ ] Check application logs for errors
- [ ] Test critical user flows

### Post-Deployment

- [ ] Verify all auth endpoints healthy:
  - `POST /api/v1/auth/register`
  - `POST /api/v1/auth/login`
  - `POST /api/v1/auth/refresh`
  - `POST /api/v1/auth/logout`
  - `POST /api/v1/auth/oauth/google`
  - `POST /api/v1/auth/role`

- [ ] Verify all jobs endpoints healthy:
  - `GET /api/v1/jobs/public`
  - `GET /api/v1/jobs/dashboard`
  - `GET /api/v1/jobs/{jobId}`
  - `POST /api/v1/jobs/generate`
  - `PATCH /api/v1/jobs/{jobId}/status`
  - `DELETE /api/v1/jobs/{jobId}`

- [ ] Confirm CORS settings match frontend URLs
- [ ] Verify JWT settings (issuer, audience, expiry)
- [ ] Monitor error rates for first 30 minutes
- [ ] Check database connection pool health
- [ ] Validate automatic migration applied correctly
- [ ] Test end-to-end user registration and login flow

### Rollback Criteria

**Trigger immediate rollback if**:
- Error rate exceeds 5% of requests
- Database migration fails
- Critical endpoint returns 500 errors
- Authentication completely broken
- Data corruption detected

## 7. Performance Optimization

### Database Query Optimization

**Indexes to maintain**:
- `Users.Email` (unique, for login lookups)
- `Jobs.EmployerId` (for employer queries)
- `Jobs.Status` (for public job listings)
- `Jobs.ApplicationDeadline` (for expiration queries)

**N+1 Query Prevention**:
- Use `.Include()` for eager loading related entities
- Consider projections (`.Select()`) to fetch only needed fields
- Monitor query execution in development mode

### Caching Strategy (Future)

**Candidates for caching**:
- Public jobs list (cache for 5 minutes)
- User profile data (cache for 15 minutes)
- Available enum values (cache indefinitely)

### API Rate Limiting (Recommended)

Implement per-user and per-IP rate limits:
- **Registration**: 3 attempts per hour per IP
- **Login**: 5 attempts per 15 minutes per email
- **Refresh**: 10 attempts per minute per user
- **Job creation**: 20 per hour per employer

## 8. Security Hardening

### HTTPS Enforcement

```csharp
// Program.cs
app.UseHttpsRedirection();
```

Ensure all production traffic uses HTTPS.

### CORS Configuration

Restrict allowed origins to known frontend domains:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.talentx.com"
    ]
  }
}
```

Never use `AllowAnyOrigin()` in production.

### Security Headers

Add security headers middleware:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    await next();
});
```

### Input Validation

- All requests validated via FluentValidation
- Enum parsing with strict error handling
- SQL injection prevented via parameterized queries (EF Core)
- XSS prevented via output encoding

### Password Security

- BCrypt hashing with work factor 12
- Password complexity requirements enforced
- No password storage in logs or error messages
