using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Retail;

/// <summary>Expected Retail business failure; mapped to ProblemDetails by the global handler (with field-level errors when given).</summary>
public sealed class RetailException(int statusCode, string message, IReadOnlyDictionary<string, string[]>? errors = null)
    : AppException(statusCode, message, errors);
