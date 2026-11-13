using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Retail;

/// <summary>Expected Retail business failure; mapped to ProblemDetails by the global handler.</summary>
public sealed class RetailException(int statusCode, string message) : AppException(statusCode, message);
