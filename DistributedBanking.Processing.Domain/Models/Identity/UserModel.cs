using MongoDB.Bson;

namespace DistributedBanking.Processing.Domain.Models.Identity;

public class UserModel
{
    public ObjectId Id { get; set; }
    public required string Email { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string PasswordHash { get; set; }
    public required string PhoneNumber { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required IEnumerable<string> Roles { get; set; }
    public string EndUserId { get; set; }
}