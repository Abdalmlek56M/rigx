using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

public sealed class StorefrontException(int statusCode, string message) : AppException(statusCode, message);
