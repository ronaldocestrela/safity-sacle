using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Identity;

namespace SafetyScale.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantExecutionContext tenantExecution)
    : IdentityDbContext<AppUser, IdentityRole, string>(options)
{
    private readonly ITenantExecutionContext _tenant = tenantExecution;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SecurityGuard> SecurityGuards => Set<SecurityGuard>();
    public DbSet<UnavailableDay> UnavailableDays => Set<UnavailableDay>();
    public DbSet<MonthlySchedule> MonthlySchedules => Set<MonthlySchedule>();
    public DbSet<ScheduleItem> ScheduleItems => Set<ScheduleItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();

            entity.HasQueryFilter(t =>
                !_tenant.IsTenantIsolationEnabled ||
                t.Id == (_tenant.TenantId ?? Guid.Empty));
        });

        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.TenantId).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
            entity.HasOne(u => u.Tenant)
                .WithMany()
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Intentionally no global query filter: Identity's UserStore queries must not be blocked
            // by tenant resolution during login/seed, and EF cannot reliably translate combined filters here.
        });

        builder.Entity<SecurityGuard>(entity =>
        {
            entity.ToTable("SecurityGuards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.TenantId).IsRequired();
            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(g =>
                !_tenant.IsTenantIsolationEnabled ||
                g.TenantId == (_tenant.TenantId ?? Guid.Empty));
        });

        builder.Entity<UnavailableDay>(entity =>
        {
            entity.ToTable("UnavailableDays");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(250);
            entity.Property(x => x.TenantId).IsRequired();

            entity.HasIndex(x => new { x.TenantId, x.SecurityGuardId, x.Date }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SecurityGuard)
                .WithMany(x => x.UnavailableDays)
                .HasForeignKey(x => x.SecurityGuardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(ud =>
                !_tenant.IsTenantIsolationEnabled ||
                ud.TenantId == (_tenant.TenantId ?? Guid.Empty));
        });

        builder.Entity<MonthlySchedule>(entity =>
        {
            entity.ToTable("MonthlySchedules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Month).IsRequired();
            entity.Property(x => x.Year).IsRequired();
            entity.Property(x => x.GeneratedAt).IsRequired();
            entity.Property(x => x.TenantId).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Month, x.Year }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(ms =>
                !_tenant.IsTenantIsolationEnabled ||
                ms.TenantId == (_tenant.TenantId ?? Guid.Empty));
        });

        builder.Entity<ScheduleItem>(entity =>
        {
            entity.ToTable("ScheduleItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.IsWeekend).IsRequired();
            entity.Property(x => x.TenantId).IsRequired();

            entity.HasIndex(x => new { x.TenantId, x.SecurityGuardId, x.Date }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SecurityGuard)
                .WithMany(x => x.ScheduleItems)
                .HasForeignKey(x => x.SecurityGuardId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MonthlySchedule)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.MonthlyScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(si =>
                !_tenant.IsTenantIsolationEnabled ||
                si.TenantId == (_tenant.TenantId ?? Guid.Empty));
        });
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
        => await SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceTenantOnPendingChanges();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceTenantOnPendingChanges()
    {
        if (!_tenant.IsTenantIsolationEnabled || _tenant.TenantId is null)
        {
            return;
        }

        var tid = _tenant.TenantId.Value;

        foreach (var entry in ChangeTracker.Entries<ITenantOwnedEntity>())
        {
            if (entry.Metadata.ClrType == typeof(Tenant))
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId = tid;
                    break;
                case EntityState.Modified:
                    if (entry.Entity.TenantId != tid)
                    {
                        throw new InvalidOperationException(
                            "Cannot modify a row that belongs to a different tenant.");
                    }

                    entry.Property(nameof(ITenantOwnedEntity.TenantId)).IsModified = false;
                    break;
                case EntityState.Deleted:
                    if (entry.Entity.TenantId != tid)
                    {
                        throw new InvalidOperationException(
                            "Cannot delete a row that belongs to a different tenant.");
                    }

                    break;
            }
        }
    }
}
