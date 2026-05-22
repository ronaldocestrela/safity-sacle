namespace SafetyScale.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; set; }

    /// <summary>Human-readable tenant name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Stable code used for routing or configuration (ASCII, unique).</summary>
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
