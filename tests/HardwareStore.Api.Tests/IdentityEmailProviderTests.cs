using HardwareStore.Modules.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class IdentityEmailProviderTests
{
    [Fact]
    public void Production_registration_fails_closed_without_smtp_settings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCustomerRegistration(new TestHostEnvironment("Production"), configuration));

        Assert.Contains(SmtpEmailOptions.SectionName, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_registration_accepts_complete_configuration_without_sending_email()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SmtpEmailOptions.SectionName}:Host"] = "smtp.example.invalid",
                [$"{SmtpEmailOptions.SectionName}:Port"] = "587",
                [$"{SmtpEmailOptions.SectionName}:EnableSsl"] = "true",
                [$"{SmtpEmailOptions.SectionName}:FromAddress"] = "accounts@example.invalid",
                [$"{SmtpEmailOptions.SectionName}:FromName"] = "HARDWARE STORE",
                [$"{SmtpEmailOptions.SectionName}:UserName"] = "test-user",
                [$"{SmtpEmailOptions.SectionName}:Password"] = "not-a-real-secret"
            })
            .Build();

        services.AddCustomerRegistration(new TestHostEnvironment("Production"), configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();
        Assert.IsType<SmtpEmailProvider>(scope.ServiceProvider.GetRequiredService<IEmailProvider>());
    }

    [Fact]
    public void Smtp_settings_reject_partial_credentials()
    {
        var options = new SmtpEmailOptions
        {
            Host = "smtp.example.invalid",
            FromAddress = "accounts@example.invalid",
            UserName = "test-user"
        };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("must both be configured", exception.Message, StringComparison.Ordinal);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "HardwareStore.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
