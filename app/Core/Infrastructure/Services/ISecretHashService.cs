namespace Infrastructure.Services;

public interface ISecretHashService
{
    string ComputeSha256(string input);
}
