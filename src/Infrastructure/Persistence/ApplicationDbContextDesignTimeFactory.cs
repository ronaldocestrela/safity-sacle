using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Infrastructure.Persistence;

public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("SafetyScaleDesignTime_ConnectionStrings__DefaultConnection")
                 ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                 ?? "Server=localhost,1433;Database=safetyscale;User Id=sa;Password=Your_Strong_LocalDev_Pwd1;Encrypt=True;TrustServerCertificate=True;";

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseSqlServer(cs);

        ITenantExecutionContext bypass = new BypassTenantExecutionContext();
        return new ApplicationDbContext(builder.Options, bypass);
    }
}
