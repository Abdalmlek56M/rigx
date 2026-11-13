namespace HardwareStore.Common.Domain;

/// <summary>Egyptian mobile numbers: one stored form (01xxxxxxxxx) and the international form WhatsApp links need.</summary>
public static class EgyptMobile
{
    /// <summary>Reads a number typed in any common spelling (spaces, dashes, +20 / 0020 prefix); false when it is not a mobile number.</summary>
    public static bool TryNormalize(string? text, out string mobile)
    {
        var digits = string.Concat((text ?? string.Empty).Where(char.IsAsciiDigit));
        if (digits.StartsWith("0020", StringComparison.Ordinal)) digits = digits[4..];
        else if (digits.StartsWith("20", StringComparison.Ordinal) && digits.Length == 12) digits = digits[2..];
        if (digits.Length == 10 && digits[0] == '1') digits = "0" + digits;
        var valid = digits.Length == 11 && digits.StartsWith("01", StringComparison.Ordinal);
        mobile = valid ? digits : string.Empty;
        return valid;
    }

    /// <summary>The number with the country code and no leading zero (201xxxxxxxxx), as wa.me expects.</summary>
    public static string International(string mobile) => "20" + mobile[1..];
}
