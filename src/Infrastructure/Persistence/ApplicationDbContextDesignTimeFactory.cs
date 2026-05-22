using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Infrastructure.Persistence;

public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseSqlite("Data Source=safetyscale.db");

        ITenantExecutionContext bypass = new BypassTenantExecutionContext();
        return new ApplicationDbContext(builder.Options, bypass);
    }
}
