using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.PcBuilder;

public static class PcBuilderRegistration
{
    public static IServiceCollection AddPcBuilderModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddAuditedDbContextFactory<PcBuilderDbContext>(connectionString);
        services.AddScoped<SavedBuildStore>();
        services.AddScoped<BuildReactionStore>();
        services.AddSingleton<BuildViewCounter>();
        services.AddScoped<BuildViewStore>();
        services.AddScoped<CommunityBuildStore>();
        services.AddScoped<IDataResetSource, PcBuilderDataReset>();
        return services;
    }
}

public sealed class PcBuilderDbContextFactory : IDesignTimeDbContextFactory<PcBuilderDbContext>
{
    public PcBuilderDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PcBuilderDbContext>()
            .UseNpgsql("Host=localhost;Database=pcbuilder_design;Username=unused;Password=unused")
            .Options);
}
