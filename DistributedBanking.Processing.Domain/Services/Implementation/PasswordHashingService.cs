using System.Security.Cryptography;
using System.Text;

namespace DistributedBanking.Processing.Domain.Services.Implementation;

public class PasswordHashingService : IPasswordHashingService
{
    private const int KeySize = 64;
    private const int Iterations = 350000;
    private readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA512;
    
    public string HashPassword(string password, out string salt)
    {
        var bytesOfSalt = RandomNumberGenerator.GetBytes(KeySize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            bytesOfSalt,
            Iterations,
            _hashAlgorithm,
            KeySize);
        
        salt = Convert.ToHexString(bytesOfSalt);
        
        return Convert.ToHexString(hash);
    }
    
    public bool VerifyPassword(string password, string hash, string salt)
    {
        var bytesOfSalt  = Convert.FromHexString(salt);
        var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(password, bytesOfSalt, Iterations, _hashAlgorithm, KeySize);
        
        return CryptographicOperations.FixedTimeEquals(hashToCompare, Convert.FromHexString(hash));
    }
}