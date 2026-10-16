namespace Shared.Exceptions;

public sealed class JobDescriptionGenerationUnavailableException(string message)
    : ApplicationExceptionBase(
        message,
        503,
        "Service Unavailable",
        "https://httpstatuses.com/503");
