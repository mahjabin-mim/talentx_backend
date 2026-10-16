namespace Infrastructure.Services;

public interface IPasswordService
{
    string HashPassword(string plainTextPassword);
    bool VerifyPassword(string plainTextPassword, string hashedPassword);
}
