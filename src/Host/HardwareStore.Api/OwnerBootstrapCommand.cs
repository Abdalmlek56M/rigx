using HardwareStore.Modules.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Explicit local command used once to create the first Owner.
///
/// It is not an HTTP endpoint and is never invoked automatically. The operator
/// must pass <c>--bootstrap-owner</c> and enable <c>OwnerBootstrap:Enabled</c>.
/// There is no password: the Owner signs in with a code sent to the given email.
/// </summary>
public static class OwnerBootstrapCommand
{
    public static async Task<int> RunAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("OwnerBootstrap:Enabled", false))
        {
            Console.Error.WriteLine(
                "Owner bootstrap is disabled. Set OwnerBootstrap:Enabled=true for this one-time command only.");
            return 2;
        }

        Console.Out.WriteLine("RIGX first Owner bootstrap");
        Console.Out.WriteLine("This command creates the Owner role and one Owner account once.");
        var username = Prompt("Username: ");
        var email = Prompt("Email: ");
        var fullName = Prompt("Full name: ");

        try
        {
            var service = scope.ServiceProvider.GetRequiredService<FirstOwnerBootstrapService>();
            await service.BootstrapAsync(new FirstOwnerBootstrapRequest(username, email, fullName), cancellationToken);
            Console.Out.WriteLine("Owner bootstrap completed. Sign in with a code sent to this email.");
            return 0;
        }
        catch (FirstOwnerBootstrapException exception)
        {
            Console.Error.WriteLine($"Owner bootstrap rejected: {exception.Failure}.");
            return 1;
        }
    }

    private static string Prompt(string label)
    {
        Console.Out.Write(label);
        return (Console.In.ReadLine() ?? string.Empty).Trim();
    }
}
