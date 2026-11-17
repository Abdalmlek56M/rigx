using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.EntityFrameworkCore;

internal sealed partial class PreviewDemoSeeder
{
    /// <summary>Ten demo customers with their saved addresses; an existing demo email is left untouched and counts as 0.</summary>
    private async Task<int> SeedCustomersAsync(CancellationToken cancellationToken)
    {
        clock.UtcNow = seedNow;
        var created = 0;
        var provisioner = new IdentityAccountProvisioner(clock, businessZone);
        foreach (var customer in PreviewDemoDefinitions.Customers())
        {
            await using var db = await identityFactory.CreateDbContextAsync(cancellationToken);
            var provisioned = await provisioner.FindOrCreateVerifiedAsync(db, customer.Email, cancellationToken);
            customerIdsByEmail[customer.Email] = provisioned.AccountId;
            if (!provisioned.IsNew) continue;
            await Profiles.UpdateAsync(provisioned.AccountId,
                new UpdateIdentityProfileRequest(customer.Name, customer.Phone), cancellationToken);
            foreach (var address in customer.Addresses)
                await Addresses.CreateAsync(provisioned.AccountId, new CustomerAddressCreateRequest(
                    address.Label, customer.Name, customer.Phone, "EG", "+20", address.GovernorateName, address.City,
                    null, address.Street, address.Building, null, null, null, null, null, address.IsDefault), cancellationToken);
            created++;
        }
        return created;
    }
}
