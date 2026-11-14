using HardwareStore.Common.Domain;

/// <summary>A Host-level request problem (missing upload, oversized body, unsupported filter value) mapped to ProblemDetails like any <see cref="AppException"/>.</summary>
public sealed class ApiProblemException(int statusCode, string message, IReadOnlyDictionary<string, string[]>? errors = null)
    : AppException(statusCode, message, errors);
