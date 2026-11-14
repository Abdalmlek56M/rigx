using System.Globalization;

namespace HardwareStore.Common.Domain;

/// <summary>One typed cell of an export: text, a number (kept numeric in Excel) or a calendar date.</summary>
public abstract record ExportCell
{
    public static readonly ExportCell Empty = new TextCell(null);

    public static ExportCell Text(string? value) => new TextCell(value);

    public static ExportCell Number(decimal value) => new NumberCell(value);

    public static ExportCell Number(decimal? value) => value is { } number ? new NumberCell(number) : Empty;

    public static ExportCell Date(DateOnly value) => new DateCell(value);

    public static ExportCell Date(DateOnly? value) => value is { } date ? new DateCell(date) : Empty;

    /// <summary>An instant as local business time ("2026-11-14 09:30"), the way staff read it.</summary>
    public static ExportCell Instant(DateTimeOffset? value, TimeZoneInfo zone) => value is { } instant
        ? new TextCell(TimeZoneInfo.ConvertTime(instant, zone).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
        : Empty;

    public static ExportCell Flag(bool value) => new TextCell(value ? "yes" : "no");

    /// <summary>The cell as plain text (numbers invariant, dates ISO); empty text is the empty string.</summary>
    public string AsText() => this switch
    {
        NumberCell number => number.Value.ToString(CultureInfo.InvariantCulture),
        DateCell date => date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TextCell text => text.Value ?? string.Empty,
        _ => string.Empty
    };

    public sealed record TextCell(string? Value) : ExportCell;

    public sealed record NumberCell(decimal Value) : ExportCell;

    public sealed record DateCell(DateOnly Value) : ExportCell;
}
