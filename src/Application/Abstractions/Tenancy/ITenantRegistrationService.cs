namespace SafetyScale.Application.Abstractions.Tenancy;

public interface ITenantRegistrationService
{
    Task<RegisterTenantResult> RegisterAsync(RegisterTenantInput input, CancellationToken cancellationToken = default);
}
