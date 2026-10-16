# Testing

## Unit Tests

Unit tests are located under `tests/UnitTests`.

### Coverage

#### Auth Module Tests

Located in `tests/UnitTests/Auth/AuthValidatorsTests.cs`:

- **RegisterUserCommandValidator**
  - Password strength validation (minimum 8 chars, uppercase, lowercase, digit, special character)
  - Email format validation
  - Role validation (Admin, Employer, Talent)
  - CompanyName validation when role is Employer

- **LoginUserCommandValidator**
  - Email and password presence validation
  - Email format validation

- **RefreshTokenCommandValidator**
  - UserId and RefreshToken presence validation
  - GUID format validation

- **LogoutUserCommandValidator**
  - UserId presence and format validation

- **GoogleAuthCommandValidator**
  - Email presence and format validation

- **UpdateUserRoleCommandValidator**
  - UserRole validation (valid enum values)
  - CompanyName validation when role is Employer

#### Jobs Module Tests

Located in `tests/UnitTests/Jobs/JobsValidatorsTests.cs`:

- **CreateJobCommandValidator**
  - Title validation (required, max 160 chars)
  - Content fields validation (description, responsibilities, skillRequirements)
    - Max 4000 chars each
    - Required when `publishJob = true`
  - Enum validation (EmploymentType, WorkArrangement, ExperienceLevel)
  - Experience years range validation (0-80, min ≤ max)
  - Salary validation (min ≤ max, both ≥ 0)
  - NumberOfPositions validation (1-1000 range)
  - ApplicationDeadline validation (must be future date)
  - Location validation based on WorkArrangement:
    - Remote: timezone required
    - Non-remote: countryCode and city required

- **GetPublicJobsQueryValidator**
  - No validation required (public endpoint)

- **GetEmployerDashboardQueryValidator**
  - UserId validation (not empty GUID)

- **GetJobByIdQueryValidator**
  - UserId and JobId validation (not empty GUIDs)

- **UpdateJobStatusCommandValidator**
  - UserId and JobId validation
  - Action validation (activate, deactivate, close)

- **DeleteJobCommandValidator**
  - UserId and JobId validation (not empty GUIDs)

### Test Framework and Dependencies

- **xUnit 2.9.3** - Test framework
- **FluentValidation** - Validator testing
- **Moq 4.20.72** - Mocking framework
- **Microsoft.EntityFrameworkCore.InMemory 9.0.4** - In-memory database for testing

### Running Unit Tests

From repository root:

```bash
# Run all unit tests
dotnet test tests/UnitTests/UnitTests.csproj

# Run with detailed output
dotnet test tests/UnitTests/UnitTests.csproj --verbosity detailed

# Run specific test class
dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~AuthValidatorsTests"

# Run with code coverage
dotnet test tests/UnitTests/UnitTests.csproj /p:CollectCoverage=true
```

## Integration Tests

Integration tests are located under `tests/IntegrationTests`.

### Test Infrastructure

- **CustomWebApplicationFactory**: Custom test server factory
  - Uses in-memory SQLite database for fast, isolated tests
  - Unique database per test run (prevents test interference)
  - Configures test environment automatically
  - Skips EF Core migrations for in-memory provider

### Coverage

Located in `tests/IntegrationTests/AuthEndpointsIntegrationTests.cs`:

#### Register Endpoint Test

**Test**: `Register_ShouldReturnCreated_WithVerificationPayload`

- Creates new user account
- Validates `201 Created` status code
- Validates response requests email verification
- Validates response includes the `verify_email` next action
- Validates response includes a future verification code expiry time

#### Complete Auth Lifecycle Test

**Test**: `Login_Refresh_Logout_RefreshAgain_ShouldFailAfterLogout`

Tests complete authentication flow:

1. **Register**: Create new user account
   - Validates `201 Created` response
2. **Login**: Authenticate with email/password
   - Validates `200 OK` response
   - Extracts access and refresh tokens
3. **Refresh**: Rotate tokens using refresh token
   - Validates `200 OK` response
   - Validates new tokens are returned
4. **Logout**: Invalidate session with access token
   - Validates `204 No Content` response
5. **Refresh After Logout**: Attempt to refresh with invalidated token
   - Validates `401 Unauthorized` response
   - Confirms token rotation is invalidated after logout

### Test Framework and Dependencies

- **xUnit 2.9.3** - Test framework
- **Microsoft.AspNetCore.Mvc.Testing 10.0.3** - In-memory test server
- **Microsoft.EntityFrameworkCore.InMemory 9.0.4** - In-memory database provider

### Running Integration Tests

From repository root:

```bash
# Run all integration tests
dotnet test tests/IntegrationTests/IntegrationTests.csproj

# Run with detailed output
dotnet test tests/IntegrationTests/IntegrationTests.csproj --verbosity detailed

# Run specific test
dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~Register_ShouldReturnCreated_WithVerificationPayload"
```

## Running All Tests

```bash
# Run all unit and integration tests
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run with code coverage (requires coverlet)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run tests in parallel
dotnet test --parallel
```

## Test Data

Sample test data is available in `tests/Users.json` for manual testing and seeding.

## Error Handling Test Coverage

### Validation Error Tests

- Invalid password strength
- Invalid email format
- Invalid enum values
- Out-of-range numeric values
- Missing required fields
- Field length violations

### Authorization Error Tests

- Invalid user context
- Expired tokens
- Mismatched token versions
- Unauthorized access attempts

### Database Constraint Tests

For integration/manual API testing, verify mapped DB behaviors:

- **Unique Constraint Violation** (`23505`)
  - Duplicate email registration → `409 Conflict`
  
- **Foreign Key Violation** (`23503`)
  - Invalid employer ID → `409 Conflict`

- **Not Null Violation** (`23502`)
  - Missing required field → `400 Bad Request`

- **String Truncation** (`22001`)
  - Value exceeding max length → `400 Bad Request`

## Test Organization

```
tests/
├── UnitTests/
│   ├── Auth/
│   │   └── AuthValidatorsTests.cs      # Auth validator tests
│   ├── Jobs/
│   │   └── JobsValidatorsTests.cs      # Jobs validator tests
│   └── UnitTests.csproj
└── IntegrationTests/
    ├── AuthEndpointsIntegrationTests.cs  # End-to-end auth tests
    ├── CustomWebApplicationFactory.cs     # Test server setup
    └── IntegrationTests.csproj
```

## Testing Best Practices

### Unit Tests

- Focus on validator logic and business rules
- Mock external dependencies (repositories, services)
- Test both valid and invalid cases
- Keep tests fast and isolated
- Use descriptive test names (method_scenario_expectedResult)

### Integration Tests

- Test complete request/response cycles
- Verify HTTP status codes and response structures
- Test authentication and authorization flows
- Validate error responses
- Use in-memory database for speed and isolation

## Future Test Coverage Goals

### Unit Tests (Planned)

- Handler business logic tests (currently validators only)
- Auth handler tests:
  - Token generation and validation
  - Password hashing and verification
  - Role assignment logic
  - Employer profile creation
- Jobs handler tests:
  - Job creation and update logic
  - Status transition validation
  - Automatic expiration logic
  - Ownership verification

### Integration Tests (Planned)

- Jobs endpoints:
  - Public jobs listing
  - Dashboard with pagination/filtering
  - Job creation and updates
  - Status updates
  - Job deletion
- Error scenario tests:
  - Database constraint violations
  - Concurrent update conflicts
  - Token expiration handling
- Performance tests:
  - Response time benchmarks
  - Database query optimization

## Continuous Integration

### Recommended CI Pipeline

```yaml
# Example GitHub Actions workflow
name: Build and Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Run unit tests
        run: dotnet test tests/UnitTests --no-build --verbosity normal
      
      - name: Run integration tests
        run: dotnet test tests/IntegrationTests --no-build --verbosity normal
```

## Notes

- Validation is centralized through MediatR + FluentValidation pipeline
- All request validation happens before handler execution
- Controller actions are thin; business logic resides in handler classes
- Tests follow AAA pattern (Arrange, Act, Assert)
- Integration tests use unique in-memory databases to prevent interference
- Test database is automatically reset between test runs
