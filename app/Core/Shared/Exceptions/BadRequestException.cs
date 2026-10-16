namespace Shared.Exceptions;

public sealed class BadRequestException(string message)
    : ApplicationExceptionBase(
        message,
        400,
        "Bad Request",
        "https://httpstatuses.com/400");
