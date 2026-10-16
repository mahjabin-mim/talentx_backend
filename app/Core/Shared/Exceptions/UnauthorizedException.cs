namespace Shared.Exceptions;

public sealed class UnauthorizedException(string message)
    : ApplicationExceptionBase(
        message,
        401,
        "Unauthorized",
        "https://httpstatuses.com/401");
