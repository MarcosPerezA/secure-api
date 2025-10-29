namespace SecureApi.Security;

public interface IPasswordService
{
    string Hash(string plain);
    bool Verify(string plain, string hash);
}

public class PasswordService : IPasswordService
{
    public string Hash(string plain) =>
        BCrypt.Net.BCrypt.HashPassword(plain, workFactor: 10);

    public bool Verify(string plain, string hash) =>
        BCrypt.Net.BCrypt.Verify(plain, hash);
}
