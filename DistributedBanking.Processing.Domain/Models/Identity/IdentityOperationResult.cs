using System.Globalization;

namespace DistributedBanking.Processing.Domain.Models.Identity;

public class IdentityOperationResult
{
    public bool Succeeded { get; }
    public IEnumerable<string> Errors { get; }

    private IdentityOperationResult(bool success, IEnumerable<string>? errors = null)
    {
        Succeeded = success;
        Errors = errors ?? new List<string>();
    }

    public static IdentityOperationResult Success { get; } = new(true);
    public static IdentityOperationResult Failed(params string[] errors)
    {
        return new IdentityOperationResult(false, errors);
    }
    
    public override string ToString()
    {
        return Succeeded ? 
            "Succeeded" 
            : string.Format(CultureInfo.InvariantCulture, "{0} : {1}", "Failed", string.Join(",", Errors.ToList()));
    }
}