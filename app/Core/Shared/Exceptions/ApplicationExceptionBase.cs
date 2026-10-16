namespace Shared.Exceptions;

public abstract class ApplicationExceptionBase(
    string message,
    int statusCode,
    string title,
    string type) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string Type { get; } = type;
}
