using CodingJamAPI.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CodingJamAPI.Infrastructure.Persistence;

/// <summary>Lets dotnet ef create migrations without starting the web host.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Either an explicit connection string, or the postgres password from the repository .env file.
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DotEnv.HostConnectionString(DotEnv.Read(Directory.GetCurrentDirectory()))
            ?? throw new InvalidOperationException(
                "No database connection: set ConnectionStrings__DefaultConnection or POSTGRES_PASSWORD in .env.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .ConfigureWarnings(w =>
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        return new AppDbContext(options);
    }
}
