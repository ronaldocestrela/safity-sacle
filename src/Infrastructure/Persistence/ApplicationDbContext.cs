using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Identity;

namespace SafetyScale.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole, string>(options)
{
    public DbSet<SecurityGuard> SecurityGuards => Set<SecurityGuard>();
    public DbSet<UnavailableDay> UnavailableDays => Set<UnavailableDay>();
    public DbSet<MonthlySchedule> MonthlySchedules => Set<MonthlySchedule>();
    public DbSet<ScheduleItem> ScheduleItems => Set<ScheduleItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SecurityGuard>(entity =>
        {
            entity.ToTable("SecurityGuards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
        });

        builder.Entity<UnavailableDay>(entity =>
        {
            entity.ToTable("UnavailableDays");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(250);
            entity.HasIndex(x => new { x.SecurityGuardId, x.Date }).IsUnique();
            entity.HasOne(x => x.SecurityGuard)
                .WithMany(x => x.UnavailableDays)
                .HasForeignKey(x => x.SecurityGuardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MonthlySchedule>(entity =>
        {
            entity.ToTable("MonthlySchedules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Month).IsRequired();
            entity.Property(x => x.Year).IsRequired();
            entity.Property(x => x.GeneratedAt).IsRequired();
            entity.HasIndex(x => new { x.Month, x.Year }).IsUnique();
        });

        builder.Entity<ScheduleItem>(entity =>
        {
            entity.ToTable("ScheduleItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.IsWeekend).IsRequired();
            entity.HasIndex(x => new { x.SecurityGuardId, x.Date }).IsUnique();
            entity.HasOne(x => x.SecurityGuard)
                .WithMany(x => x.ScheduleItems)
                .HasForeignKey(x => x.SecurityGuardId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MonthlySchedule)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.MonthlyScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
