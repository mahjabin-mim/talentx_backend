namespace Shared.Exceptions;

public sealed class ForbiddenException(string message)
    : ApplicationExceptionBase(
        message,
        403,
        "Forbidden",
        "https://httpstatuses.com/403");
