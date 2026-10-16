namespace Shared.Exceptions;

public sealed class ResourceNotFoundException(string message)
    : ApplicationExceptionBase(
        message,
        404,
        "Not Found",
        "https://httpstatuses.com/404");