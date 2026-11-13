using HardwareStore.Modules.Identity.Persistence;

namespace HardwareStore.Modules.Identity;

/// <summary>
/// Person classification is additive: a person can be a customer, an employee,
/// and a supplier at the same time. Roles never erase an existing lawful flag.
/// </summary>
public static class PersonClassificationPolicy
{
    public static void ApplyForRoles(Person person, IEnumerable<string> roleCodes)
    {
        var roles = roleCodes.ToHashSet(StringComparer.Ordinal);
        if (roles.Contains("Customer"))
            person.IsCustomer = true;
        if (roles.Any(code => !string.Equals(code, "Customer", StringComparison.Ordinal)))
            person.IsEmployee = true;
    }
}
