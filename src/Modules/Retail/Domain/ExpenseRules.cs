namespace HardwareStore.Modules.Retail;

public static class ExpenseRules
{
    public const int MaxTypeNameLength = 80;
    public const int LastRecurringDay = 28;

    public static decimal Amount(decimal value)
    {
        var rounded = decimal.Round(value, 2);
        return rounded > 0 ? rounded : throw new RetailException(400, "Expense amount must be greater than zero.");
    }

    /// <summary>No date means today; an expense cannot be dated after today (Cairo business day). Owner decision 18 Nov 2026.</summary>
    public static DateOnly Date(DateOnly? value, DateOnly today) => value is DateOnly date
        ? date <= today ? date : throw new RetailException(400, "An expense cannot be dated in the future.")
        : today;

    /// <summary>The category must be the name of an active expense type.</summary>
    public static string Category(string? value, IReadOnlySet<string> activeTypes)
    {
        var name = value?.Trim();
        if (string.IsNullOrEmpty(name)) throw new RetailException(400, "Expense category is required.");
        return activeTypes.Contains(name) ? name : throw new RetailException(400, "The expense category must be an active expense type.");
    }

    /// <summary>No classification means general overhead.</summary>
    public static string Classification(string? value) => value?.Trim() switch
    {
        null or "" or ExpenseClassification.GeneralOverhead => ExpenseClassification.GeneralOverhead,
        ExpenseClassification.Direct => ExpenseClassification.Direct,
        _ => throw new RetailException(400, "The classification must be Direct or GeneralOverhead.")
    };

    public static void EnsureDirectLink(string classification, Guid? orderId, Guid? unitId, Guid? assemblyId)
    {
        if (classification == ExpenseClassification.Direct && orderId is null && unitId is null && assemblyId is null)
            throw new RetailException(400, "A direct expense must be linked to an order, a unit or a build.");
    }

    public static void EnsureSinglePayer(Guid? moneyAccountId, Guid? paidByPartnerId)
    {
        if (moneyAccountId is not null && paidByPartnerId is not null)
            throw new RetailException(400, "An expense is paid from an account or from a partner, not both.");
    }

    public static string TypeName(string? value) => OrderRules.Required(value, MaxTypeNameLength, "Expense type name");

    public static int DayOfMonth(int value) => value is >= 1 and <= LastRecurringDay
        ? value
        : throw new RetailException(400, $"The day of the month must be between 1 and {LastRecurringDay}.");

    public static DateOnly MonthStart(DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>
    /// The dates a monthly template is still owed on: every month from the one after the last posted (or its start month) up to today's
    /// month, whose day has already been reached.
    /// </summary>
    public static IReadOnlyList<DateOnly> DueDates(DateOnly startMonth, DateOnly? lastPostedMonth, int dayOfMonth, DateOnly today)
    {
        var first = lastPostedMonth is DateOnly last ? MonthStart(last).AddMonths(1) : MonthStart(startMonth);
        if (first < MonthStart(startMonth)) first = MonthStart(startMonth);
        var due = new List<DateOnly>();
        for (var month = first; month <= today; month = month.AddMonths(1))
        {
            var date = new DateOnly(month.Year, month.Month, dayOfMonth);
            if (date > today) break;
            due.Add(date);
        }
        return due;
    }
}
