namespace Shared.Exceptions;

public sealed class EmailVerificationRequiredException(string message)
    : ApplicationExceptionBase(
        message,
        403,
        "Email Verification Required",
        "https://httpstatuses.com/403");
