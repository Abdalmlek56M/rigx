using System.Security.Cryptography.X509Certificates;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HardwareStore.Modules.Identity.Authentication;

public static class IdentityAuthenticationRegistration
{
    public static IServiceCollection AddIdentityAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        var dataProtection = services.AddDataProtection();
        var isDevelopment = environment.IsDevelopment();
        var applicationName = $"HardwareStore.{environment.EnvironmentName}";
        dataProtection.SetApplicationName(applicationName);

        if (!isDevelopment)
        {
            var keyRingPath = Required(configuration, "DataProtection:KeyRingPath");
            var certificatePath = Required(configuration, "DataProtection:CertificatePath");
            var certificatePassword = Required(configuration, "DataProtection:CertificatePassword");
            Directory.CreateDirectory(keyRingPath);
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                certificatePassword,
                X509KeyStorageFlags.EphemeralKeySet);
            if (!certificate.HasPrivateKey)
                throw new InvalidOperationException("The Data Protection certificate must include its private key.");
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
                .ProtectKeysWithCertificate(certificate);
        }
        else
        {
            var keyRingPath = configuration["DataProtection:KeyRingPath"];
            if (string.IsNullOrWhiteSpace(keyRingPath))
            {
                var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(localData))
                    throw new InvalidOperationException("LocalApplicationData is unavailable; configure DataProtection:KeyRingPath.");
                keyRingPath = Path.Combine(localData, "HardwareStore", "DataProtection", "Development");
            }
            Directory.CreateDirectory(keyRingPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        }

        services.AddSingleton<DatabaseTicketStore>();
        services.AddSingleton<Microsoft.AspNetCore.Authentication.Cookies.ITicketStore>(sp =>
            sp.GetRequiredService<DatabaseTicketStore>());

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = !isDevelopment
                    ? "__Host-HardwareStore.Session"
                    : "HardwareStore.Dev.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = !isDevelopment
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<Microsoft.AspNetCore.Authentication.Cookies.ITicketStore>((options, ticketStore) =>
                options.SessionStore = ticketStore);

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = !isDevelopment ? "__Host-HardwareStore.CSRF" : "HardwareStore.Dev.CSRF";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = !isDevelopment
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
            options.Cookie.IsEssential = true;
        });

        return services;
    }

    private static string Required(IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key])
            ? configuration[key]!
            : throw new InvalidOperationException($"{key} must be configured in Production.");
}
