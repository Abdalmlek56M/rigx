using System.Text;
using HardwareStore.Modules.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Explicit local command used once to create the first Owner.
///
/// It is not an HTTP endpoint and is never invoked automatically. The operator
/// must pass <c>--bootstrap-owner</c>, enable <c>OwnerBootstrap:Enabled</c>, and
/// type the password interactively. Passwords are never read from command-line
/// arguments, environment variables, logs, or URLs.
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
        var password = ReadPassword("Password: ");
        var confirmation = ReadPassword("Confirm password: ");
        if (password is null || confirmation is null)
        {
            Console.Error.WriteLine("Owner bootstrap requires an interactive terminal; redirected stdin is refused.");
            return 1;
        }
        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Passwords do not match.");
            return 1;
        }

        try
        {
            var service = scope.ServiceProvider.GetRequiredService<FirstOwnerBootstrapService>();
            await service.BootstrapAsync(
                new FirstOwnerBootstrapRequest(username, email, fullName, password),
                cancellationToken);
            Console.Out.WriteLine("Owner bootstrap completed.");
            return 0;
        }
        catch (FirstOwnerBootstrapException exception)
        {
            Console.Error.WriteLine($"Owner bootstrap rejected: {exception.Failure}.");
            return 1;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Owner bootstrap failed.");
            return 1;
        }
    }

    private static string Prompt(string label)
    {
        Console.Out.Write(label);
        return (Console.In.ReadLine() ?? string.Empty).Trim();
    }

    private static string? ReadPassword(string label)
    {
        Console.Out.Write(label);
        if (Console.IsInputRedirected)
            return null;

        var buffer = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.Out.WriteLine();
                return buffer.ToString();
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length -= 1;
                    Console.Out.Write("\b \b");
                }
                continue;
            }
            if (!char.IsControl(key.KeyChar))
                buffer.Append(key.KeyChar);
        }
    }
}
