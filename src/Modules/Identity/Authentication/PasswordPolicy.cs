using System.Collections.Frozen;
using System.Text;

namespace HardwareStore.Modules.Identity.Authentication;

public enum PasswordPolicyFailure
{
    None,
    TooShort,
    TooLong,
    Common
}

public interface IPasswordPolicyValidator
{
    PasswordPolicyFailure Validate(string? password);
}

public sealed class PasswordPolicyValidator : IPasswordPolicyValidator
{
    public const int MinimumUnicodeScalars = 15;
    public const int MaximumUnicodeScalars = 128;

    // This baseline deliberately remains local. A larger versioned deny-list can replace
    // this set without changing the password-setting flows or calling an external service.
    private static readonly FrozenSet<string> CommonPasswords = new[]
    {
        "123456789012345",
        "1234567890123456",
        "passwordpassword",
        "password123456",
        "qwertyuiopasdfgh",
        "qwerty123456789",
        "letmeinletmein12",
        "adminadminadmin",
        "iloveyouiloveyou",
        "welcomewelcome1",
        "changemechangeme",
        "correcthorsebattery",
        "thisisapassword",
        "thisismypassword"
    }.ToFrozenSet(StringComparer.Ordinal);

    public PasswordPolicyFailure Validate(string? password)
    {
        if (password is null)
            return PasswordPolicyFailure.TooShort;

        // A valid 128-scalar password cannot exceed 256 UTF-16 code units.
        if (password.Length > MaximumUnicodeScalars * 2)
            return PasswordPolicyFailure.TooLong;

        var scalarCount = password.EnumerateRunes().Count();
        if (scalarCount < MinimumUnicodeScalars)
            return PasswordPolicyFailure.TooShort;
        if (scalarCount > MaximumUnicodeScalars)
            return PasswordPolicyFailure.TooLong;
        if (CommonPasswords.Contains(password))
            return PasswordPolicyFailure.Common;

        return PasswordPolicyFailure.None;
    }
}
