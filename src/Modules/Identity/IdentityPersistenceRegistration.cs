using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Identity;

public static class IdentityPersistenceRegistration
{
    public static IServiceCollection AddIdentityPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        var timeZoneId = configuration["Business:TimeZone"]
            ?? throw new InvalidOperationException("Business:TimeZone is not configured.");

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException($"Business:TimeZone '{timeZoneId}' is not a valid time zone.", exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new InvalidOperationException($"Business:TimeZone '{timeZoneId}' is invalid.", exception);
        }

        services.AddSingleton(timeZone);
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddDbContextFactory<IdentityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IdentityLoginService>();
        services.AddScoped<IdentityCurrentUserService>();
        services.AddScoped<IdentityProfileService>();
        services.AddScoped<CustomerAddressService>();
        services.AddScoped<IdentityEmailChangeService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<AccountRestrictionService>();
        services.AddScoped<PersonCodeGenerator>();
        services.AddScoped<FirstOwnerBootstrapService>();
        services.AddScoped<IdentityAdministrationService>();
        return services;
    }
}
