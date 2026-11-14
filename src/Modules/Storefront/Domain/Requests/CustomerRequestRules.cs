using System.Net.Mail;
using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

/// <summary>A submission after checking: trimmed, normalized, and with the fields that do not belong to its type removed.</summary>
public sealed record CustomerRequestDraft(string Type, string Name, string Phone, string? Email, string? Governorate, string? Source,
    string? Subject, string? Message, string? PartType, string? ModelName, string? OwnedFor, string? ItemCode, string? ServiceKind,
    DateOnly? PreferredDate);

public static class CustomerRequestRules
{
    public const decimal MaxAmount = 10_000_000m;
    public const int MaxNoteLength = 2000;

    public static CustomerRequestDraft CheckSubmission(CustomerRequestSubmission request, DateOnly today)
    {
        var type = CustomerRequestType.All.FirstOrDefault(value =>
                string.Equals(value, request.Type?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new StorefrontException(400, "Request type is not valid.");
        var common = new CustomerRequestDraft(type, Required(request.Name, 2, 120, "Name"), CheckPhone(request.Phone),
            CheckEmail(request.Email), CheckGovernorate(request.Governorate), Optional(request.Source, 64, "Source"),
            null, null, null, null, null, null, null, null);
        var message = Optional(request.Message, 4000, "Message");
        var model = Optional(request.ModelName, 200, "Model");
        var item = Optional(request.ItemCode, 40, "Item code");
        return type switch
        {
            CustomerRequestType.Sell or CustomerRequestType.TradeIn => common with
            {
                ModelName = model ?? throw new StorefrontException(400, "Model is required."), Message = message,
                PartType = Optional(request.PartType, 120, "Part type"), OwnedFor = Optional(request.OwnedFor, 60, "Owned for")
            },
            CustomerRequestType.Upgrade => common with { Message = LongMessage(message), ModelName = model },
            CustomerRequestType.NotifyMe => model is null && item is null
                ? throw new StorefrontException(400, "Enter the item code or the model.")
                : common with { ModelName = model, ItemCode = item },
            CustomerRequestType.Support => common with { Message = LongMessage(message), ItemCode = item },
            CustomerRequestType.Contact => common with { Message = LongMessage(message), Subject = Optional(request.Subject, 200, "Subject") },
            _ => common with
            {
                Message = message, ServiceKind = CheckServiceKind(request.ServiceKind), PreferredDate = CheckDate(request.PreferredDate, today)
            }
        };
    }

    public static bool OffersAllowed(string type) =>
        type is CustomerRequestType.Sell or CustomerRequestType.TradeIn or CustomerRequestType.Upgrade;

    public static bool ConvertAllowed(string type) => type is CustomerRequestType.Sell or CustomerRequestType.TradeIn;

    /// <summary>The manual status endpoint: start following (from New, or reopen a closed request) or close. Other steps have their own endpoints.</summary>
    public static void EnsureCanSetStatus(CustomerRequest request, string? newStatus)
    {
        var allowed = newStatus switch
        {
            CustomerRequestStatus.Following => request.Status is CustomerRequestStatus.New or CustomerRequestStatus.Closed,
            CustomerRequestStatus.Closed => request.Status is not (CustomerRequestStatus.Converted or CustomerRequestStatus.Closed),
            _ => throw new StorefrontException(400, "Status must be Following or Closed.")
        };
        if (!allowed) throw new StorefrontException(409, $"A {request.Status} request cannot be set to {newStatus}.");
    }

    public static void EnsureCanOffer(CustomerRequest request)
    {
        if (!OffersAllowed(request.Type)) throw new StorefrontException(400, "This request type does not take an offer.");
        if (request.Status is not (CustomerRequestStatus.New or CustomerRequestStatus.Following or CustomerRequestStatus.Offered
            or CustomerRequestStatus.Rejected))
            throw new StorefrontException(409, $"An offer cannot be made on a {request.Status} request.");
    }

    public static void CheckOfferAmounts(decimal offerAmount, decimal? expectedResale)
    {
        if (offerAmount <= 0) throw new StorefrontException(400, "The offer must be more than zero.");
        CheckMoney(offerAmount, "Offer");
        if (expectedResale is not { } resale) return;
        if (resale < 0) throw new StorefrontException(400, "Expected resale cannot be negative.");
        CheckMoney(resale, "Expected resale");
    }

    public static void EnsureCanDecide(CustomerRequest request, string? decision)
    {
        if (decision is not (CustomerRequestStatus.Accepted or CustomerRequestStatus.Rejected))
            throw new StorefrontException(400, "Decision must be Accepted or Rejected.");
        if (request.Status != CustomerRequestStatus.Offered)
            throw new StorefrontException(409, "Only a request with an offer can be accepted or rejected.");
    }

    public static void EnsureCanConvert(CustomerRequest request)
    {
        if (!ConvertAllowed(request.Type)) throw new StorefrontException(400, "Only a sell or trade-in request can become a purchase.");
        if (request.Status != CustomerRequestStatus.Accepted || request.OfferAmount is null)
            throw new StorefrontException(409, "Only an accepted offer can become a purchase.");
    }

    /// <summary>The customer sees the offered amount while it is on the table or accepted; staff estimates never reach the customer.</summary>
    public static bool OfferVisibleToCustomer(string status) => status is CustomerRequestStatus.Offered or CustomerRequestStatus.Accepted;

    public static string? CheckNote(string? note) => Optional(note, MaxNoteLength, "Note");

    /// <summary>Moves to <paramref name="status"/>: Closed and Converted stamp the close time, anything else (a reopen or revised offer) clears it.</summary>
    public static void SetStatus(CustomerRequest request, string status, DateTimeOffset now)
    {
        request.Status = status;
        request.ClosedAtUtc = status is CustomerRequestStatus.Closed or CustomerRequestStatus.Converted ? now : null;
    }

    private static void CheckMoney(decimal amount, string label)
    {
        if (amount > MaxAmount || decimal.Round(amount, 2) != amount)
            throw new StorefrontException(400, $"{label} must be at most {MaxAmount:0} with two decimals.");
    }

    private static string CheckPhone(string? phone) =>
        EgyptMobile.TryNormalize(phone, out var mobile)
            ? mobile
            : throw new StorefrontException(400, "Enter an Egyptian mobile number (01xxxxxxxxx).");

    private static string? CheckEmail(string? email)
    {
        var value = Optional(email, 255, "Email");
        return value is null || MailAddress.TryCreate(value, out _) ? value : throw new StorefrontException(400, "Email is not valid.");
    }

    private static string? CheckGovernorate(string? code)
    {
        var value = code?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(value)) return null;
        return EgyptGovernorates.IsKnown(value) ? value : throw new StorefrontException(400, "Unknown governorate.");
    }

    private static string CheckServiceKind(string? kind) =>
        ServiceKind.All.FirstOrDefault(value => string.Equals(value, kind?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new StorefrontException(400, "Choose a service.");

    private static DateOnly? CheckDate(DateOnly? date, DateOnly today) =>
        date is { } value && value < today ? throw new StorefrontException(400, "The preferred date cannot be in the past.") : date;

    private static string LongMessage(string? message) =>
        message is { Length: >= 10 } ? message : throw new StorefrontException(400, "Message must be between 10 and 4000 characters.");

    private static string Required(string? value, int min, int max, string field)
    {
        var text = value?.Trim();
        return text is { } known && known.Length >= min && known.Length <= max
            ? known
            : throw new StorefrontException(400, $"{field} must be between {min} and {max} characters.");
    }

    private static string? Optional(string? value, int max, string field)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return text.Length <= max ? text : throw new StorefrontException(400, $"{field} must be at most {max} characters.");
    }
}
