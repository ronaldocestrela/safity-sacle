namespace SafetyScale.Application.Abstractions.Authentication;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
