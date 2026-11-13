using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Identity;

/// <summary>Expected Identity business failure (validation, not found, conflict, forbidden); mapped globally to ProblemDetails.</summary>
public sealed class IdentityException(int statusCode, string message) : AppException(statusCode, message);
