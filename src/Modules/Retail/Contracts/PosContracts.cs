namespace HardwareStore.Modules.Retail;

/// <summary>
/// A store sale (ADM-POS): lines with a free sale price, an order-level discount, pickup or shipping (fee from the shipping settings
/// unless staff type one), one or more payments, and either confirm now (stock claimed) or save as a pending order.
/// </summary>
public sealed record PosSaleRequest(OrderCustomer? Customer, string? FulfillmentMethod, OrderAddress? Address,
    IReadOnlyList<OrderLineRequest>? Items, decimal Discount = 0m, decimal? DeliveryFee = null,
    IReadOnlyList<PosPaymentRequest>? Payments = null, string? LeadSource = null, string? Usage = null, string? UsageNote = null,
    string? Notes = null, bool Confirm = true);
public sealed record PosPaymentRequest(string? Method, decimal Amount);

/// <summary>Discount limit (settings) unless the seller has the price-override permission; reservation defaults; shipping rate per governorate code.</summary>
public sealed record PosPolicy(decimal MaxDiscountPercent, bool UnlimitedDiscount, ReservationPolicy Reservation, Func<string, ShippingRate> ShippingFor);

public sealed record PosPreview(IReadOnlyList<PosPreviewLine> Lines, decimal LinesTotal, decimal Discount, decimal Subtotal,
    decimal DeliveryFee, decimal Total, decimal? CostTotal, decimal? Profit, IReadOnlyList<string> Warnings,
    IReadOnlyList<PosReturningPart> ReturningToStock);
public sealed record PosPreviewLine(string Name, string? Code, int Quantity, decimal ShownPrice, decimal SalePrice, decimal? Cost, bool BelowCost);
/// <summary>Parts of a split build that go back to stock when the sale is confirmed.</summary>
public sealed record PosReturningPart(Guid UnitId, string Name, string Code);
public sealed record PosCustomerView(string Name, string Phone, bool PhoneHasWhatsApp, string? Email, OrderAddress? Address, int OrderCount);
