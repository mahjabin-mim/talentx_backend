namespace Infrastructure.Services;

public interface IApplicationAccountEmailService
{
    Task SendNewAccountPasswordAsync(string email, string? fullName, string password);
}
