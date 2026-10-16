namespace Infrastructure.Services;

public sealed class PasswordService : IPasswordService
{
    public string HashPassword(string plainTextPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainTextPassword);

    public bool VerifyPassword(string plainTextPassword, string hashedPassword)
        => BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
}
