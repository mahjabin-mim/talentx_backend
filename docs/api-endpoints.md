# API Endpoints

## Auth

### POST /api/v1/auth/register

Register a new employer account and start email verification.

#### Request

```json
{
    "email": "jane.doe@example.com",
    "password": "StrongP@ssw0rd!",
    "fullname": "Jane Doe",
    "companyName": "Company Name",
    "phoneNumber": "+8801234567890"
}
```

#### Validation Rules

- `email`: required, valid email format, max 254 chars.
- `password`: required, 8-128 chars, must include uppercase, lowercase, number, special character.
- `fullname`: required, max 120 chars.
- `companyName`: required, max 160 chars.
- `phoneNumber`: required, max 20 chars.

#### Success Response (201)

The OTP code is sent to the registered email address using the auth email template.

```json
{
    "message": "Registration completed. Please verify your email with the OTP code.",
    "requiresVerification": true,
    "nextAction": "verify_email",
    "verificationCodeExpiresAtUtc": "2026-04-28T12:34:56+00:00"
}
```

#### Error Responses

- `400 Bad Request`: validation failure.
- `409 Conflict`: email already exists.
- `500 Internal Server Error`: unexpected server-side issue.

Validation failures return `application/problem+json` with this shape:

```json
{
    "type": "https://httpstatuses.com/400",
    "title": "Bad Request",
    "status": 400,
    "detail": "One or more validation errors occurred.",
    "instance": "/api/v1/auth/register",
    "errors": {
        "Email": [
            "'Email' must not be empty."
        ]
    }
}
```

Application and database errors return `application/problem+json` with this shape:

```json
{
    "type": "https://httpstatuses.com/409",
    "title": "Conflict",
    "status": 409,
    "detail": "Email is already registered.",
    "traceId": "<trace-id>"
}
```

#### Database Exception Mapping

The API maps known PostgreSQL errors to consistent HTTP responses:

- `23505` (`unique_violation`) -> `409 Conflict`
- `23503` (`foreign_key_violation`) -> `409 Conflict`
- `23502` (`not_null_violation`) -> `400 Bad Request`
- `22001` (`string_data_right_truncation`) -> `400 Bad Request`
- other/unmapped DB errors -> `500 Internal Server Error`

#### Security Notes

- `password` is stored as a BCrypt hash.
- Email verification codes are stored as SHA-256 hashes in the database.
- The plain OTP is used only for the outbound verification email.
- Register does not return access or refresh tokens. The user must complete email verification next.

### POST /api/v1/auth/verify-email

Verify a pending account email with the OTP code sent during registration or resend.

#### Request

```json
{
    "email": "user@example.com",
    "otpCode": "123456"
}
```

#### Validation Rules

- `email`: required, valid email format, max 254 chars.
- `otpCode`: required, exactly 6 digits.

#### Success Response (200)

```json
{
    "message": "Email verified successfully.",
    "verified": true,
    "nextAction": "login"
}
```

If the email is already verified, the endpoint returns `200 OK` with:

```json
{
    "message": "Email is already verified.",
    "verified": true,
    "nextAction": "login"
}
```

#### Error Responses

- `400 Bad Request`: validation failure, invalid OTP, or expired OTP.
- `404 Not Found`: account was not found.
- `500 Internal Server Error`: unexpected server-side issue.

### POST /api/v1/auth/resend-verification-otp

Generate a new email verification OTP for a pending account and send it by email.

#### Request

```json
{
    "email": "user@example.com"
}
```

#### Validation Rules

- `email`: required, valid email format, max 254 chars.

#### Success Response (200)

```json
{
    "message": "Verification OTP sent. Please verify your email with the OTP code.",
    "requiresVerification": true,
    "nextAction": "verify_email",
    "verificationCodeExpiresAtUtc": "2026-04-28T12:34:56+00:00"
}
```

#### Error Responses

- `400 Bad Request`: validation failure or email is already verified.
- `404 Not Found`: account was not found.
- `500 Internal Server Error`: unexpected server-side issue.

### POST /api/v1/auth/login

Sign in with existing credentials.

#### Request

```json
{
    "email": "jane.doe@example.com",
    "password": "StrongP@ssw0rd!"
}
```

#### Success Response (200)

```json
{
    "id": "0ef8dc17-d1ea-4f56-bce2-06dbbd6f4f4f",
    "fullname": "Jane Doe",
    "email": "jane.doe@example.com",
    "phoneNumber": "+8801234567890",
    "userRole": "Talent",
    "accessToken": "<jwt>",
    "refreshToken": "<token>",
    "refreshTokenExpiryTime": "2026-05-11T12:00:00+00:00",
    "message": "Sign in successful."
}
```

#### Error Responses

- `400 Bad Request`: validation failure.
- `401 Unauthorized`: invalid credentials or inactive account.
- `403 Forbidden`: email verification required.
- `500 Internal Server Error`: unexpected server-side issue.

When email verification is required, the response includes redirect hints:

```json
{
    "type": "https://httpstatuses.com/403",
    "title": "Email Verification Required",
    "status": 403,
    "detail": "Email verification is required before sign in.",
    "traceId": "<trace-id>",
    "requiresVerification": true,
    "nextAction": "verify_email"
}
```

The access token includes `phone_number` when the user has a phone number.

### POST /api/v1/auth/refresh

Rotate refresh token and issue a new access token.

#### Request

```json
{
    "userId": "0ef8dc17-d1ea-4f56-bce2-06dbbd6f4f4f",
    "refreshToken": "<existing-refresh-token>"
}
```

#### Success Response (200)

```json
{
    "id": "0ef8dc17-d1ea-4f56-bce2-06dbbd6f4f4f",
    "fullname": "Jane Doe",
    "email": "jane.doe@example.com",
    "phoneNumber": "+8801234567890",
    "userRole": "Talent",
    "accessToken": "<jwt>",
    "refreshToken": "<new-refresh-token>",
    "refreshTokenExpiryTime": "2026-05-11T12:00:00+00:00",
    "message": "Token refreshed successfully."
}
```

#### Error Responses

- `400 Bad Request`: validation failure.
- `401 Unauthorized`: refresh token invalid/expired or user inactive.
- `500 Internal Server Error`: unexpected server-side issue.

### POST /api/v1/auth/logout

Invalidate server-side auth state for the current user.

#### Auth

- Requires `Authorization: Bearer <access-token>`.

#### Success Response (204)

- No response body.

#### Behavior

- Clears stored refresh token.
- Clears refresh token expiry.
- Increments `TokenVersion`, immediately invalidating previously-issued access tokens.

#### Error Responses

- `401 Unauthorized`: invalid or expired access token.
- `500 Internal Server Error`: unexpected server-side issue.

### POST /api/v1/auth/oauth/google

Create or update a user from Google OAuth identity and issue backend auth tokens.

#### Request

```json
{
    "email": "jane.doe@gmail.com",
    "fullname": "Jane Doe",
    "profileImageURL": "https://lh3.googleusercontent.com/a/...."
}
```

#### Success Response (200)

```json
{
    "id": "0ef8dc17-d1ea-4f56-bce2-06dbbd6f4f4f",
    "fullname": "Jane Doe",
    "email": "jane.doe@gmail.com",
    "phoneNumber": null,
    "userRole": null,
    "accessToken": "<jwt>",
    "refreshToken": "<token>",
    "refreshTokenExpiryTime": "2026-05-11T12:00:00+00:00",
    "message": "Google sign-in successful."
}
```

For first-time Google users, the same response shape is returned and `message` is:

```json
"Google sign-up successful."
```

#### Error Responses

- `400 Bad Request`: validation failure.
- `401 Unauthorized`: inactive account.
- `500 Internal Server Error`: unexpected server-side issue.

### POST /api/v1/auth/role

Persist/update authenticated user's `UserRole` and re-issue backend tokens.

#### Auth

- Requires `Authorization: Bearer <access-token>`.

#### Request

```json
{
    "userRole": "Employer"
}
```

#### Success Response (200)

```json
{
    "id": "0ef8dc17-d1ea-4f56-bce2-06dbbd6f4f4f",
    "fullname": "Jane Doe",
    "email": "jane.doe@gmail.com",
    "phoneNumber": "+8801234567890",
    "userRole": "Employer",
    "accessToken": "<jwt>",
    "refreshToken": "<token>",
    "refreshTokenExpiryTime": "2026-05-11T12:00:00+00:00",
    "message": "Role updated successfully."
}
```

#### Error Responses

- `400 Bad Request`: validation failure or unsupported role.
- `401 Unauthorized`: invalid token/inactive account.
- `500 Internal Server Error`: unexpected server-side issue.

## Token Security Model

- Access tokens expire in 15 minutes.
- Refresh tokens expire in 30 days and are rotated on each refresh.
- Refresh tokens are stored hashed (SHA-256).
- Access tokens carry a `token_version` claim validated against the user row.
- Logout increments `TokenVersion`, revoking old access tokens even before expiry.
