namespace DistributedBanking.Processing.Domain.Models.Identity;

public class WorkerRegistrationModel : EndUserRegistrationModel
{
    public required string Position { get; set; }
    
    public required AddressModel Address { get; set; }
}