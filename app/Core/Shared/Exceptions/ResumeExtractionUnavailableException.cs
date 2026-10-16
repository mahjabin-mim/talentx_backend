namespace Shared.Exceptions;

public sealed class ResumeExtractionUnavailableException(string message)
    : ApplicationExceptionBase(
        message,
        503,
        "Service Unavailable",
        "https://httpstatuses.com/503");
