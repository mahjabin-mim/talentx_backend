namespace Shared.Exceptions;

public sealed class ResourceConflictException(string message)
    : ApplicationExceptionBase(
        message,
        409,
        "Conflict",
        "https://httpstatuses.com/409");
