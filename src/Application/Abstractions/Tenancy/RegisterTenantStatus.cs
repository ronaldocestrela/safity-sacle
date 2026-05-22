namespace SafetyScale.Application.Abstractions.Tenancy;

public enum RegisterTenantStatus
{
    Success,
    TenantSlugConflict,
    AdminEmailAlreadyExists,
    InvalidPassword,
    ValidationFailed,
}
