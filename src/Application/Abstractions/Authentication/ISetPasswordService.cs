namespace SafetyScale.Application.Abstractions.Authentication;

public interface ISetPasswordService
{
    Task<SetPasswordResult> SetInitialPasswordAsync(
        string userId,
        string token,
        string password,
        CancellationToken cancellationToken = default);
}
