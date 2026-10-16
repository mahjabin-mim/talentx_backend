using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

public sealed class SecretHashService : ISecretHashService
{
    public string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
