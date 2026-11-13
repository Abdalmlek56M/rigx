using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HardwareStore.Modules.Identity;

public sealed record CustomerRegistrationRequest(
    string? FullName,
    string? PhoneNumber,
    string? Username,
    string? Email);

public sealed record EmailVerificationMessage(string Email, string FullName, Uri VerificationUrl);
public sealed record PasswordResetMessage(string Email, string FullName, Uri ResetUrl);
public sealed record EmailCodeMessage(string Email, string FullName, string Code, DateTimeOffset ExpiresAtUtc, string Purpose);

public enum IdentitySecurityNotificationKind
{
    PasswordResetCompleted,
    PasswordChanged
}

public sealed record IdentitySecurityNotificationMessage(
    string Email,
    string FullName,
    IdentitySecurityNotificationKind Kind,
    DateTimeOffset OccurredAtUtc);

public sealed record GeneratedIdentityToken(string RawToken, IdentityToken PersistenceRecord);

public static class IdentityTokenFactory
{
    public static GeneratedIdentityToken Create(
        Guid userAccountId,
        DateTimeOffset nowUtc,
        string purpose,
        TimeSpan lifetime)
    {
        if (userAccountId == Guid.Empty)
            throw new ArgumentException("A user account id is required.", nameof(userAccountId));
        if (purpose is not ("EmailVerification" or "PasswordReset"))
            throw new ArgumentOutOfRangeException(nameof(purpose));
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return new GeneratedIdentityToken(
            WebEncoders.Base64UrlEncode(tokenBytes),
            new IdentityToken
            {
                Id = Guid.CreateVersion7(),
                UserAccountId = userAccountId,
                Purpose = purpose,
                TokenHash = SHA256.HashData(tokenBytes),
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.Add(lifetime)
            });
    }

    public static bool TryHashRawToken(string? rawToken, out byte[] hash)
    {
        hash = [];
        if (string.IsNullOrWhiteSpace(rawToken))
            return false;

        try
        {
            var tokenBytes = WebEncoders.Base64UrlDecode(rawToken);
            if (tokenBytes.Length != 32)
                return false;

            hash = SHA256.HashData(tokenBytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public interface IEmailProvider
{
    Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken = default);
    Task SendEmailCodeAsync(EmailCodeMessage message, CancellationToken cancellationToken = default);
    Task SendSecurityNotificationAsync(
        IdentitySecurityNotificationMessage message,
        CancellationToken cancellationToken = default);

    Task SendEmailChangeVerificationAsync(
        EmailVerificationMessage message,
        CancellationToken cancellationToken = default) =>
        SendEmailVerificationAsync(message, cancellationToken);
}

/// <summary>Development-only provider. Messages remain in process memory and are never logged or sent externally.</summary>
public sealed class InMemoryTestEmailProvider : IEmailProvider
{
    private readonly ConcurrentQueue<EmailVerificationMessage> _verificationMessages = new();
    private readonly ConcurrentQueue<PasswordResetMessage> _passwordResetMessages = new();
    private readonly ConcurrentQueue<EmailCodeMessage> _emailCodes = new();
    private readonly ConcurrentQueue<IdentitySecurityNotificationMessage> _securityNotifications = new();

    public IReadOnlyCollection<EmailVerificationMessage> Messages => _verificationMessages.ToArray();
    public IReadOnlyCollection<PasswordResetMessage> PasswordResetMessages => _passwordResetMessages.ToArray();
    public IReadOnlyCollection<EmailCodeMessage> EmailCodes => _emailCodes.ToArray();
    public IReadOnlyCollection<IdentitySecurityNotificationMessage> SecurityNotifications =>
        _securityNotifications.ToArray();

    public Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _verificationMessages.Enqueue(message);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _passwordResetMessages.Enqueue(message);
        return Task.CompletedTask;
    }

    public Task SendEmailCodeAsync(EmailCodeMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _emailCodes.Enqueue(message);
        return Task.CompletedTask;
    }

    public Task SendSecurityNotificationAsync(
        IdentitySecurityNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _securityNotifications.Enqueue(message);
        return Task.CompletedTask;
    }
}

public enum CustomerRegistrationStatus
{
    Created,
    IdentifierConflict,
    InvalidInput
}

public sealed record CustomerRegistrationResult(
    CustomerRegistrationStatus Status,
    Guid? UserAccountId = null,
    bool EmailDispatchQueued = false,
    string? InvalidField = null);

public sealed class CustomerRegistrationService(
    IdentityDbContext db,
    PersonCodeGenerator personCodeGenerator,
    UserAccountLoginIdentifierSynchronizer identifierSynchronizer,
    IIdentityEmailDispatcher emailDispatcher,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<CustomerRegistrationService> logger)
{
    public const string EmailVerificationPurpose = "EmailVerification";
    private static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromHours(24);
    private const string CustomerRoleCode = "Customer";

    public async Task<CustomerRegistrationResult> RegisterAsync(
        CustomerRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAndValidate(request);
        if (normalized.ErrorField is not null)
            return new CustomerRegistrationResult(CustomerRegistrationStatus.InvalidInput,
                InvalidField: normalized.ErrorField);

        var now = timeProvider.GetUtcNow();
        var generatedToken = IdentityTokenFactory.Create(Guid.CreateVersion7(), now,
            EmailVerificationPurpose, EmailVerificationLifetime);
        var person = new Person
        {
            FullName = normalized.FullName!,
            PhoneNumber = normalized.PhoneNumber,
            Email = normalized.Email,
            IsCustomer = true,
            CreatedAtUtc = now
        };
        var account = new UserAccount
        {
            Id = generatedToken.PersistenceRecord.UserAccountId,
            Username = normalized.Username!,
            NormalizedUsername = normalized.NormalizedUsername!,
            Email = normalized.Email!,
            NormalizedEmail = normalized.NormalizedEmail!,
            PasswordHash = null,
            IsEmailVerified = false,
            IsActive = true,
            CreatedAtUtc = now
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await personCodeGenerator.CreatePersonInCurrentTransactionAsync(person, cancellationToken);
            account.PersonId = person.Id;

            var customerRole = await db.Roles.SingleOrDefaultAsync(role => role.Code == CustomerRoleCode, cancellationToken)
                ?? throw new InvalidOperationException("The seeded Customer role is missing.");

            db.UserAccounts.Add(account);
            await identifierSynchronizer.SetAsync(account, normalized.Username!, normalized.Email!, cancellationToken);
            db.UserRoles.Add(new UserRole { UserAccountId = account.Id, RoleId = customerRole.Id });
            db.IdentityTokens.Add(generatedToken.PersistenceRecord);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsLoginIdentifierConflict(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new CustomerRegistrationResult(CustomerRegistrationStatus.IdentifierConflict);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        try
        {
            var baseUrl = configuration["Identity:EmailVerificationBaseUrl"]
                ?? "http://localhost:3000/verify-email";
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var verificationBase))
                throw new InvalidOperationException("Identity:EmailVerificationBaseUrl must be an absolute URL.");
            var verificationUrl = new UriBuilder(verificationBase)
            {
                Fragment = $"token={Uri.EscapeDataString(generatedToken.RawToken)}"
            }.Uri;
            await emailDispatcher.EnqueueVerificationAsync(
                account.Id,
                new EmailVerificationMessage(account.Email, person.FullName, verificationUrl),
                cancellationToken);
            return new CustomerRegistrationResult(CustomerRegistrationStatus.Created, account.Id, true);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Email verification dispatch failed after registration commit; the pending account and token were retained.");
            return new CustomerRegistrationResult(CustomerRegistrationStatus.Created, account.Id, false);
        }
    }

    private static NormalizedRegistration NormalizeAndValidate(CustomerRegistrationRequest request)
    {
        var fullName = NormalizeOriginal(request.FullName);
        var phoneNumber = NormalizeOriginal(request.PhoneNumber);
        var username = NormalizeOriginal(request.Username);
        var email = NormalizeOriginal(request.Email);

        if (fullName is null || fullName.Length > 255)
            return NormalizedRegistration.Invalid("fullName");
        if (phoneNumber?.Length > 32)
            return NormalizedRegistration.Invalid("phoneNumber");
        if (username is null || username.Length > 128)
            return NormalizedRegistration.Invalid("username");
        if (email is null || email.Length > 255 || !new EmailAddressAttribute().IsValid(email))
            return NormalizedRegistration.Invalid("email");

        var normalizedUsername = IdentityLoginService.NormalizeIdentifier(username);
        var normalizedEmail = IdentityLoginService.NormalizeIdentifier(email);
        if (normalizedUsername.Length > 128 || normalizedEmail.Length > 255)
            return NormalizedRegistration.Invalid("username");

        return new NormalizedRegistration(fullName, phoneNumber, username, email,
            normalizedUsername, normalizedEmail, null);
    }

    private static string? NormalizeOriginal(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Normalize(NormalizationForm.FormC);

    private static bool IsLoginIdentifierConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "PK_user_account_login_identifiers"
                or "ux_user_accounts_username"
                or "ux_user_accounts_normalized_username"
                or "ux_user_accounts_email"
                or "ux_user_accounts_normalized_email"
        };

    private sealed record NormalizedRegistration(
        string? FullName,
        string? PhoneNumber,
        string? Username,
        string? Email,
        string? NormalizedUsername,
        string? NormalizedEmail,
        string? ErrorField)
    {
        public static NormalizedRegistration Invalid(string field) =>
            new(null, null, null, null, null, null, field);
    }
}

public sealed class UserAccountLoginIdentifierSynchronizer(IdentityDbContext db)
{
    public async Task SetAsync(
        UserAccount account,
        string username,
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Login identifier changes require an active caller-owned transaction.");

        if (db.Entry(account).State != EntityState.Added)
        {
            await db.UserAccountLoginIdentifiers
                .Where(identifier => identifier.UserAccountId == account.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        account.Username = username;
        account.NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username);
        account.Email = email;
        account.NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email);

        foreach (var normalizedIdentifier in new[] { account.NormalizedUsername, account.NormalizedEmail }
                     .Distinct(StringComparer.Ordinal))
        {
            db.UserAccountLoginIdentifiers.Add(new UserAccountLoginIdentifier
            {
                NormalizedIdentifier = normalizedIdentifier,
                UserAccountId = account.Id
            });
        }
    }
}

public static class CustomerRegistrationServices
{
    public static IServiceCollection AddCustomerRegistration(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        services.AddScoped<CustomerRegistrationService>();
        services.AddScoped<UserAccountLoginIdentifierSynchronizer>();
        services.AddSingleton<IdentityEmailOutboxWriter>();
        services.AddScoped<IIdentityEmailDispatcher, IdentityEmailOutboxDispatcher>();
        services.AddHostedService<IdentityEmailOutboxProcessor>();
        services.AddScoped<IPasswordPolicyValidator, PasswordPolicyValidator>();
        services.AddScoped<IdentityAccountLifecycleService>();
        services.AddScoped<IdentitySessionService>();
        services.AddScoped<EmailCodeService>();
        services.AddScoped<IdentityAccountProvisioner>();
        services.AddScoped<EmailCodeSignInService>();

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<InMemoryTestEmailProvider>();
            services.AddSingleton<IEmailProvider>(provider =>
                provider.GetRequiredService<InMemoryTestEmailProvider>());
        }
        else
        {
            var options = configuration
                .GetSection(SmtpEmailOptions.SectionName)
                .Get<SmtpEmailOptions>() ?? new SmtpEmailOptions();
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<IEmailProvider, SmtpEmailProvider>();
        }

        return services;
    }
}
