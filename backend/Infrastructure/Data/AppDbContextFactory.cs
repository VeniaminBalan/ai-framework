using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PatientSyncHealth.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating AppDbContext during migrations.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Use a default connection string for design-time operations
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=PatientSyncHealth;Username=postgres;Password=postgres");

        return new AppDbContext(optionsBuilder.Options);
    }
}
