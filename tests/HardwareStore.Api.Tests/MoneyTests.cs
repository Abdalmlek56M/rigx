using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Money accounts (roadmap 2.15): seeded accounts, sales in, refunds/payouts/capital/transfers out, never below zero.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class MoneyPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();

    private async Task<RetailTestServices> StartAsync() => (await fixture.CreateDatabaseAsync()).CreateRetailServices();

    private static ReceiptLine Line(string model, decimal cost, decimal price, Guid? partnerId = null) =>
        new(model, "New", 1, cost, price, PartnerId: partnerId);

    private static async Task<MoneyAccountView> AccountAsync(RetailTestServices retail, Guid id) =>
        (await retail.Money.ListAsync()).Single(account => account.Id == id);

    [Fact]
    public async Task The_four_default_accounts_are_seeded_empty_and_in_order()
    {
        var retail = await StartAsync();

        var accounts = await retail.Money.ListAsync();

        Assert.Equal([MoneyAccountKind.Cash, MoneyAccountKind.InstaPay, MoneyAccountKind.Wallet, MoneyAccountKind.BankTransfer],
            accounts.Select(account => account.Kind).ToList());
        Assert.Equal("الخزنة", accounts[0].Name);
        Assert.Equal("حساب البنك", accounts[3].Name);
        Assert.All(accounts, account => Assert.Equal((0m, 0m, true), (account.OpeningBalance, account.Balance, account.IsActive)));
    }

    [Fact]
    public async Task Sale_payments_raise_the_account_of_their_method_or_the_chosen_account()
    {
        var retail = await StartAsync();
        var drawer = await retail.Money.CreateAsync(new MoneyAccountRequest("درج الفرع", MoneyAccountKind.Cash, 0m, SortOrder: 9));
        var item = await retail.ReceiveItemAsync(Line("GPU", 100m, 600m));
        var other = await retail.ReceiveItemAsync(Line("CPU", 100m, 200m));

        var order = await retail.PosAsync(RetailSales.Sale([RetailSales.StockLine(item.Id, 1, 600m)],
            payments: [new PosPaymentRequest("Cash", 100m), new PosPaymentRequest("InstaPay", 500m)]));
        await retail.PosAsync(RetailMoney.PaidSale(other, 200m, "Cash", drawer.Id));

        Assert.Equal(100m, await retail.BalanceAsync(MoneyAccountKind.Cash));
        Assert.Equal(500m, await retail.BalanceAsync(MoneyAccountKind.InstaPay));
        Assert.Equal(0m, await retail.BalanceAsync(MoneyAccountKind.Wallet));
        Assert.Equal(200m, (await AccountAsync(retail, drawer.Id)).Balance);
        var movement = Assert.Single(await retail.Money.ListMovementsAsync((await retail.AccountAsync(MoneyAccountKind.InstaPay)).Id, null, null));
        Assert.Equal((MoneySource.Sale, 500m, order.Reference), (movement.Source, movement.Amount, movement.Reference));
    }

    [Fact]
    public async Task A_payment_added_later_lands_in_its_method_account_and_an_unknown_account_is_refused()
    {
        var retail = await StartAsync();
        var item = await retail.ReceiveItemAsync(Line("GPU", 100m, 300m));
        var order = await retail.PosAsync(RetailSales.Sale([RetailSales.StockLine(item.Id, 1, 300m)]));

        order = await retail.Payments.AddPaymentAsync(order.Id, new OrderPaymentRequest("Wallet", 100m, null, order.RowVersion), Actor);

        Assert.Equal(100m, await retail.BalanceAsync(MoneyAccountKind.Wallet));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest("Cash", 50m, null, order.RowVersion, Guid.NewGuid()), Actor)));
    }

    [Fact]
    public async Task A_fully_paid_order_cannot_be_marked_unpaid_by_hand()
    {
        var retail = await StartAsync();
        var item = await retail.ReceiveItemAsync(Line("GPU", 100m, 300m));
        var order = await retail.PosAsync(RetailSales.Sale([RetailSales.StockLine(item.Id, 1, 300m)]));
        order = await retail.Payments.AddPaymentAsync(order.Id, new OrderPaymentRequest("Cash", 300m, null, order.RowVersion), Actor);

        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Payments.UpdatePaymentAsync(order.Id,
            new PaymentStatusRequest(PaymentStatus.Unpaid, null, order.RowVersion), Actor)));
    }

    [Fact]
    public async Task A_refund_lowers_the_account_and_is_refused_when_it_cannot_cover_it()
    {
        var retail = await StartAsync();
        var item = await retail.ReceiveItemAsync(Line("GPU", 100m, 300m));
        var order = await retail.CompletedSaleAsync(RetailMoney.PaidSale(item, 300m));
        var cash = await retail.AccountAsync();
        var insta = await retail.AccountAsync(MoneyAccountKind.InstaPay);
        await retail.Money.TransferAsync(new MoneyTransferRequest(cash.Id, insta.Id, 200m), Actor);
        var line = order.Items.Single();
        var pending = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(order.Id, line.Id, line.Components.Single().UnitId, 300m, "Defect"), Actor);

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() =>
            retail.Returns.InspectReturnAsync(pending.Id, new ReturnInspectRequest("New", null, pending.RowVersion), Actor)));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() =>
            retail.Returns.InspectReturnAsync(pending.Id, new ReturnInspectRequest("New", null, pending.RowVersion, insta.Id), Actor)));
        Assert.Equal(100m, await retail.BalanceAsync());
        await retail.FundAsync(100m, MoneyAccountKind.InstaPay);
        var inspected = await retail.Returns.InspectReturnAsync(pending.Id, new ReturnInspectRequest("New", null, pending.RowVersion, insta.Id), Actor);

        Assert.Equal(ReturnStatus.Inspected, inspected.Status);
        Assert.Equal((100m, 0m), (await retail.BalanceAsync(), await retail.BalanceAsync(MoneyAccountKind.InstaPay)));
    }

    [Fact]
    public async Task A_refund_without_an_account_comes_out_of_the_cash_account()
    {
        var retail = await StartAsync();
        var item = await retail.ReceiveItemAsync(Line("GPU", 100m, 300m));
        var order = await retail.CompletedSaleAsync(RetailMoney.PaidSale(item, 300m));
        var line = order.Items.Single();
        var pending = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(order.Id, line.Id, line.Components.Single().UnitId, 120m, "Defect"), Actor);

        await retail.Returns.InspectReturnAsync(pending.Id, new ReturnInspectRequest("New", null, pending.RowVersion), Actor);

        Assert.Equal(180m, await retail.BalanceAsync());
        Assert.Contains(await retail.Money.ListMovementsAsync((await retail.AccountAsync()).Id, null, null),
            movement => (movement.Source, movement.Amount) == (MoneySource.Refund, -120m));
    }

    [Fact]
    public async Task A_payout_is_refused_when_its_account_is_short_and_leaves_the_dues_untouched()
    {
        var retail = await StartAsync();
        var partner = await retail.CreateAsync("Partner", 10m);
        var item = await retail.ReceiveItemAsync(Line("GPU", 100m, 300m, partner.Id));
        await retail.CompletedSaleAsync(RetailMoney.PaidSale(item, 300m));
        var cash = await retail.AccountAsync();
        var wallet = await retail.AccountAsync(MoneyAccountKind.Wallet);
        await retail.Money.TransferAsync(new MoneyTransferRequest(cash.Id, wallet.Id, 300m), Actor);

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.PartnerLedger.PayoutAsync(partner.Id, new PartnerPayoutRequest(20m, "Cash", null, null), Actor)));
        Assert.Equal(20m, (await retail.Partners.ListAsync()).Single().Due);
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PartnerLedger.PayoutAsync(partner.Id, new PartnerPayoutRequest(5m, "Visa", null, null), Actor)));

        var payout = await retail.PartnerLedger.PayoutAsync(partner.Id, new PartnerPayoutRequest(15m, "Wallet", "R1", null, wallet.Id), Actor);

        Assert.Equal("Wallet", payout.Method);
        Assert.Equal(285m, await retail.BalanceAsync(MoneyAccountKind.Wallet));
        Assert.Equal(5m, (await retail.Partners.ListAsync()).Single().Due);
    }

    [Fact]
    public async Task A_capital_deposit_raises_an_account_and_a_withdrawal_needs_the_balance()
    {
        var retail = await StartAsync();
        var partner = await retail.CreateAsync("Partner", 10m);
        var wallet = await retail.AccountAsync(MoneyAccountKind.Wallet);
        await retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Deposit", 500m, null, "InstaPay"), Actor);
        await retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Deposit", 200m, null, "Cash", null, wallet.Id), Actor);
        var insta = await retail.AccountAsync(MoneyAccountKind.InstaPay);
        await retail.Money.TransferAsync(new MoneyTransferRequest(insta.Id, (await retail.AccountAsync()).Id, 450m), Actor);

        Assert.Equal((50m, 200m, 450m), (await retail.BalanceAsync(MoneyAccountKind.InstaPay), await retail.BalanceAsync(MoneyAccountKind.Wallet),
            await retail.BalanceAsync()));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() =>
            retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Withdrawal", 100m, null, "InstaPay"), Actor)));
        Assert.Equal(2, (await retail.Capital.ListAsync(partner.Id)).Count);

        await retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Withdrawal", 50m, null, "InstaPay"), Actor);

        Assert.Equal(0m, await retail.BalanceAsync(MoneyAccountKind.InstaPay));
    }

    [Fact]
    public async Task A_transfer_moves_money_in_one_step_and_refuses_overdrafts_and_the_same_account()
    {
        var retail = await StartAsync();
        await retail.FundAsync(300m);
        var cash = await retail.AccountAsync();
        var insta = await retail.AccountAsync(MoneyAccountKind.InstaPay);

        await retail.Money.TransferAsync(new MoneyTransferRequest(cash.Id, insta.Id, 100m, null, "إيداع"), Actor);

        Assert.Equal((200m, 100m), (await retail.BalanceAsync(), await retail.BalanceAsync(MoneyAccountKind.InstaPay)));
        Assert.Equal([(MoneySource.TransferOut, -100m)],
            (await retail.Money.ListMovementsAsync(cash.Id, null, null)).Select(item => (item.Source, item.Amount)).ToList());
        Assert.Equal([(MoneySource.TransferIn, 100m)],
            (await retail.Money.ListMovementsAsync(insta.Id, null, null)).Select(item => (item.Source, item.Amount)).ToList());
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Money.TransferAsync(new MoneyTransferRequest(cash.Id, insta.Id, 200.01m), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Money.TransferAsync(new MoneyTransferRequest(cash.Id, cash.Id, 1m), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Money.TransferAsync(new MoneyTransferRequest(cash.Id, insta.Id, 0m), Actor)));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Money.TransferAsync(new MoneyTransferRequest(cash.Id, Guid.NewGuid(), 1m), Actor)));
        Assert.Equal((200m, 100m), (await retail.BalanceAsync(), await retail.BalanceAsync(MoneyAccountKind.InstaPay)));
    }

    [Fact]
    public async Task Editing_an_account_needs_its_current_row_version_and_a_unique_name()
    {
        var retail = await StartAsync();
        var cash = await retail.AccountAsync();

        var funded = await retail.Money.UpdateAsync(cash.Id, new MoneyAccountRequest(cash.Name, cash.Kind, 500m, cash.OpeningDate, true, 1, cash.RowVersion));

        Assert.Equal((500m, 500m, cash.RowVersion + 1), (funded.OpeningBalance, funded.Balance, funded.RowVersion));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Money.UpdateAsync(cash.Id,
            new MoneyAccountRequest(cash.Name, cash.Kind, 900m, cash.OpeningDate, true, 1, cash.RowVersion))));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Money.CreateAsync(new MoneyAccountRequest("الخزنة", MoneyAccountKind.Cash, 0m))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Money.CreateAsync(new MoneyAccountRequest("حساب", "Cheque", 0m))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Money.CreateAsync(new MoneyAccountRequest("حساب", MoneyAccountKind.Cash, -1m))));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Money.UpdateAsync(Guid.NewGuid(), new MoneyAccountRequest("x", "Cash", 0m, RowVersion: 1))));
        Assert.Equal(500m, await retail.BalanceAsync());
    }

    [Fact]
    public async Task An_inactive_account_takes_no_postings()
    {
        var retail = await StartAsync();
        var cash = await retail.AccountAsync();
        await retail.Money.UpdateAsync(cash.Id, new MoneyAccountRequest(cash.Name, cash.Kind, 0m, cash.OpeningDate, false, 1, cash.RowVersion));
        var item = await retail.ReceiveItemAsync(Line("GPU", 100m, 300m));

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.PosAsync(RetailMoney.PaidSale(item, 300m))));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.PosAsync(RetailMoney.PaidSale(item, 300m, "Cash", cash.Id))));
    }
}
