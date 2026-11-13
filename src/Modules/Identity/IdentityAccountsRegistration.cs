using HardwareStore.Modules.Identity.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HardwareStore.Modules.Identity;

public static class IdentityAccountsRegistration
{
    /// <summary>Sign-in (email code, Google), step-up, sessions and the Identity email outbox + provider.</summary>
    public static IServiceCollection AddIdentityAccounts(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        services.AddSingleton<IdentityEmailOutboxWriter>();
        services.AddScoped<IIdentityEmailDispatcher, IdentityEmailOutboxDispatcher>();
        services.AddHostedService<IdentityEmailOutboxProcessor>();
        services.AddSingleton(IdentityLinks.FromConfiguration(configuration));
        services.AddScoped<IdentitySessionService>();
        services.AddScoped<EmailCodeService>();
        services.AddScoped<IdentityAccountProvisioner>();
        services.AddScoped<EmailCodeSignInService>();
        services.AddScoped<StepUpService>();
        services.AddScoped<NewDeviceSignInAlerts>();
        services.AddSingleton(GoogleSignInOptions.FromConfiguration(configuration));
        services.AddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
        services.AddScoped<GoogleSignInService>();

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<InMemoryTestEmailProvider>();
            services.AddSingleton<IEmailProvider>(provider =>
                provider.GetRequiredService<InMemoryTestEmailProvider>());
        }
        else
        {
            var options = configuration
                .GetSection(SmtpEmailOptions.SectionName)
                .Get<SmtpEmailOptions>() ?? new SmtpEmailOptions();
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<IEmailProvider, SmtpEmailProvider>();
        }

        return services;
    }
}

/// <summary>Frontend pages that Identity emails link to (configuration, validated once at startup).</summary>
public sealed record IdentityLinks(Uri SignIn, Uri EmailChange)
{
    /// <summary>Local frontend defaults, used when nothing is configured (development and tests).</summary>
    public static readonly IdentityLinks Local = new(
        new Uri("http://localhost:3000/login"), new Uri("http://localhost:3000/change-email"));

    public static IdentityLinks FromConfiguration(IConfiguration configuration) => new(
        Read(configuration, "Identity:SignInUrl", Local.SignIn),
        Read(configuration, "Identity:EmailChangeBaseUrl", Local.EmailChange));

    private static Uri Read(IConfiguration configuration, string key, Uri fallback)
    {
        var value = configuration[key];
        if (value is null)
            return fallback;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException($"{key} must be an absolute HTTP or HTTPS URL.");
        return uri;
    }
}
