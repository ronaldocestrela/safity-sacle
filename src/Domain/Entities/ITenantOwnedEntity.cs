namespace SafetyScale.Domain.Entities;

/// <summary>
/// Rows that physically belong to a tenant (logical isolation in shared database).
/// </summary>
public interface ITenantOwnedEntity
{
    Guid TenantId { get; set; }
}
