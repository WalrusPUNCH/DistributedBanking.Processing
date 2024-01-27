namespace DistributedBanking.Processing.Domain.Services.Implementation;

public static class Generator
{
    private static readonly Random Random = new();
    
    public static DateTime GenerateExpirationDate()
    {
        return DateTime.UtcNow.AddDays(Random.Next(500, 1000));
    }

    public static string GenerateSecurityCode()
    {
        return Random.Next(100, 1000).ToString();
    }
}