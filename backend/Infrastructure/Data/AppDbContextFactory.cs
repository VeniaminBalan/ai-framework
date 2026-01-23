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
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=PatientSyncHealth;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new AppDbContext(optionsBuilder.Options);
    }
}
