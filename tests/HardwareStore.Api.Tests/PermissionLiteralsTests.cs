using System.Text.RegularExpressions;
using HardwareStore.Modules.Identity.Authorization;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>A permission written in the code must exist in <see cref="PermissionCatalog"/>; a typo would otherwise answer 403 to everybody.</summary>
public sealed class PermissionLiteralsTests
{
    [Fact]
    public void Every_permission_written_in_the_API_code_exists_in_the_catalogue()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "HARDWARE_STORE.slnx"))) root = Path.GetDirectoryName(root);
        Assert.NotNull(root);

        var areas = string.Join('|', PermissionCatalog.All.Select(permission => permission[..permission.IndexOf('.')]).Distinct());
        // A permission is the argument of RequirePermission / HasPermission, or a string constant in a file that calls them;
        // audit event names ("System.BackupCreated") look alike and are not permissions, so other literals are not scanned.
        var quoted = "\"((?:" + areas + ")\\.[A-Z][A-Za-z]+)\"";
        var call = new Regex("(?:RequirePermission|HasPermission)\\(" + quoted);
        var constant = new Regex("const string \\w+ = " + quoted);
        var catalogFile = Path.Combine("Modules", "Identity", "PermissionCatalog.cs");

        var unknown = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        var found = 0;
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}") && !path.EndsWith(catalogFile, StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(file);
            var usesPermissions = text.Contains("RequirePermission", StringComparison.Ordinal) || text.Contains("HasPermission", StringComparison.Ordinal);
            var matches = call.Matches(text).Concat(usesPermissions ? constant.Matches(text) : []);
            foreach (var match in matches)
            {
                found++;
                var permission = match.Groups[1].Value;
                if (!PermissionCatalog.All.Contains(permission))
                    (unknown.TryGetValue(permission, out var files) ? files : unknown[permission] = []).Add(Path.GetFileName(file));
            }
        }

        Assert.True(found > 100, $"The scan found only {found} permission names; the pattern no longer matches the code.");
        Assert.True(unknown.Count == 0, $"Permissions missing from PermissionCatalog:{Environment.NewLine}{string.Join(Environment.NewLine, unknown.Select(pair => $"{pair.Key} ({string.Join(", ", pair.Value.Distinct())})"))}");
    }
}
