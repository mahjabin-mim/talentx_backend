namespace Shared.Exceptions;

public sealed class RegisterConflictException(string message)
    : ApplicationExceptionBase(
        message,
        409,
        "Conflict",
        "https://httpstatuses.com/409");
