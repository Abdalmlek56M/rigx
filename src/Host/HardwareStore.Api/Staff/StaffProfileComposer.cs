using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;

/// <summary>The staff profile page: the user, their account facts and note, their own purchases and their customer record, if any.</summary>
public sealed record StaffProfile(AdminIdentityUser User, StaffAccountView Account, CustomerSalesView Sales,
    IReadOnlyList<LinkedAccount> LinkedCustomers);

/// <summary>Composes the staff profile across Identity and Retail; each module only supplies its own data.</summary>
public sealed class StaffProfileComposer(IdentityAdministrationService users, StaffCustomerLinkService links, CustomerSalesService sales)
{
    public async Task<StaffProfile> GetAsync(Guid id, bool includeSerial, CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(id, cancellationToken) ?? throw new IdentityException(404, "The user was not found.");
        var account = await users.GetStaffAccountAsync(id, cancellationToken) ?? throw new IdentityException(404, "The user was not found.");
        return new StaffProfile(user, account, await sales.GetAsync(id, user.PhoneNumber, user.Email, includeSerial, cancellationToken),
            await links.CustomersOfStaffAsync(id, cancellationToken));
    }
}
