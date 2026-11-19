using HardwareStore.Modules.Identity.Persistence;

namespace HardwareStore.Modules.Identity;

/// <summary>What makes an account a customer: it holds no role except <c>Customer</c> (staff accounts hold other roles).</summary>
public static class CustomerAccounts
{
    public const string RoleCode = "Customer";
    public const int MaxNoteLength = PersonNotes.MaxLength;

    public static bool IsCustomer(IEnumerable<string> roleCodes) => roleCodes.All(code => code == RoleCode);

    public static IQueryable<UserAccount> Customers(this IQueryable<UserAccount> accounts) =>
        accounts.Where(account => account.UserRoles.All(userRole => userRole.Role.Code == RoleCode));
}
