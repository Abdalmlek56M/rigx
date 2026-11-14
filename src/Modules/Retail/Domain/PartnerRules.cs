using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Retail;

/// <summary>How a partner earns: on the units it finances, or a monthly percent of the whole shop's net profit (D4).</summary>
public static class PartnerBasis
{
    public const string FinancedParts = "FinancedParts";
    public const string WholeShop = "WholeShop";
    public static bool IsKnown(string? value) => value is FinancedParts or WholeShop;
}

public static class PartnerCapitalKind
{
    public const string Deposit = "Deposit";
    public const string Withdrawal = "Withdrawal";
    public static bool IsKnown(string? value) => value is Deposit or Withdrawal;
}

/// <summary>Partner rules (business §7, owner decisions 14 Nov 2026). One place, no database.</summary>
public static class PartnerRules
{
    public const int NameMaxLength = 160;
    public const int NotesMaxLength = 1000;

    /// <summary>Cleans and checks a partner card; returns the trimmed name, normalized phone and notes.</summary>
    public static (string Name, string? Phone, string? Notes) CleanCard(string? name, string? phone, decimal defaultPercent, string? basis, string? notes)
    {
        var cleanName = name?.Trim();
        if (string.IsNullOrEmpty(cleanName) || cleanName.Length > NameMaxLength)
            throw new RetailException(400, $"Partner name is required (at most {NameMaxLength} characters).");
        EnsurePercent(defaultPercent);
        if (!PartnerBasis.IsKnown(basis))
            throw new RetailException(400, "Partner basis must be FinancedParts or WholeShop.");
        string? cleanPhone = null;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            if (!EgyptMobile.TryNormalize(phone, out var mobile)) throw new RetailException(400, "Partner phone must be an Egyptian mobile number.");
            cleanPhone = mobile;
        }
        var cleanNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (cleanNotes?.Length > NotesMaxLength) throw new RetailException(400, $"Partner notes are limited to {NotesMaxLength} characters.");
        return (cleanName, cleanPhone, cleanNotes);
    }

    public static void EnsurePercent(decimal percent)
    {
        if (percent is < 0 or > 100) throw new RetailException(400, "Partner percentage must be between 0 and 100.");
    }

    /// <summary>Active whole-shop partners share one month base, so their percents together cannot pass 100%.</summary>
    public static void EnsureWholeShopTotal(decimal otherActiveWholeShopPercents, decimal percent)
    {
        if (otherActiveWholeShopPercents + percent > 100m)
            throw new RetailException(400, "Whole-shop partner percentages together cannot exceed 100%.");
    }

    /// <summary>One basis per partner: a whole-shop partner never finances units, so it cannot switch while it has unsold financed units.</summary>
    public static void EnsureBasisChange(string currentBasis, string newBasis, int unsoldFinancedUnits)
    {
        if (currentBasis != newBasis && newBasis == PartnerBasis.WholeShop && unsoldFinancedUnits > 0)
            throw new RetailException(409, "Move this partner's unsold financed parts to another partner before switching to whole-shop basis.");
    }

    /// <summary>A unit can only be financed by an active partner on the financed-parts basis.</summary>
    public static void EnsureCanFinance(Partner partner)
    {
        if (!partner.IsActive) throw new RetailException(400, "This partner is not active.");
        if (partner.Basis != PartnerBasis.FinancedParts)
            throw new RetailException(400, "A whole-shop partner earns from the monthly profit and cannot finance single parts.");
    }

    /// <summary>The unit's own percent wins over the partner's default.</summary>
    public static decimal EffectivePercent(decimal? unitPercent, decimal defaultPercent) => unitPercent ?? defaultPercent;

    /// <summary>Partner share of a profit; a loss is never a partner debt unless loss sharing is on (§7).</summary>
    public static decimal Share(decimal profit, decimal percent, bool lossSharing)
    {
        var amount = decimal.Round(decimal.Round(profit, 2) * percent / 100m, 2);
        return amount < 0 && !lossSharing ? 0m : amount;
    }

    /// <summary>Whole-shop base = store net profit after expenses minus what financed partners earned in the month (owner decision 14 Nov 2026).</summary>
    public static decimal WholeShopBase(decimal storeNetResult, decimal financedShares) =>
        decimal.Round(storeNetResult - financedShares, 2);

    /// <summary>First day of the month; the month must be valid.</summary>
    public static DateOnly MonthStart(int year, int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12) throw new RetailException(400, "Choose a valid month.");
        return new DateOnly(year, month, 1);
    }

    /// <summary>A month is closed by hand only after it has ended in the business time zone (owner decision 14 Nov 2026).</summary>
    public static void EnsureMonthEnded(DateOnly monthStart, DateOnly today)
    {
        if (monthStart.AddMonths(1) > today) throw new RetailException(409, "The month has not ended yet.");
    }

    /// <summary>Checks a capital entry; a withdrawal cannot take out more than the partner's capital balance.</summary>
    public static void EnsureCapitalEntry(string? kind, decimal amount, decimal balance)
    {
        if (!PartnerCapitalKind.IsKnown(kind)) throw new RetailException(400, "Capital entry kind must be Deposit or Withdrawal.");
        if (amount <= 0) throw new RetailException(400, "Capital amount must be greater than zero.");
        if (kind == PartnerCapitalKind.Withdrawal && amount > balance)
            throw new RetailException(409, "The withdrawal is larger than the partner's capital balance.");
    }

    public static decimal CapitalBalance(IEnumerable<PartnerCapitalEntry> entries) =>
        decimal.Round(entries.Sum(entry => entry.Kind == PartnerCapitalKind.Deposit ? entry.Amount : -entry.Amount), 2);

    /// <summary>
    /// Splits a POS line profit over the units it would take, by each unit's cost (equal split when all cost 0),
    /// for the partner share preview. Shares are estimates; the real ones are posted when the order completes.
    /// </summary>
    public static IReadOnlyList<decimal> SplitProfitByCost(decimal lineProfit, IReadOnlyList<decimal> unitCosts)
    {
        if (unitCosts.Count == 0) return [];
        var totalCost = unitCosts.Sum();
        var result = new decimal[unitCosts.Count];
        var assigned = 0m;
        for (var index = 0; index < unitCosts.Count; index++)
        {
            result[index] = index == unitCosts.Count - 1
                ? decimal.Round(lineProfit - assigned, 2)
                : decimal.Round(totalCost == 0 ? lineProfit / unitCosts.Count : lineProfit * unitCosts[index] / totalCost, 2);
            assigned += result[index];
        }
        return result;
    }
}
