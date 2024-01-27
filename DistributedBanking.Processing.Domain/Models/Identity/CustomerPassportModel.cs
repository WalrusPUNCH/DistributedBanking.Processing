namespace DistributedBanking.Processing.Domain.Models.Identity;

public class CustomerPassportModel
{
    public required string DocumentNumber { get; set; }
    public required string Issuer { get; set; }
    public required DateTime IssueDateTime { get; set; }
    public required DateTime ExpirationDateTime { get; set; }
}