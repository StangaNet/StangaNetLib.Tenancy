using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Abstractions;

/// <summary>
/// Provides read-only access to the current tenant for the duration of an HTTP request.
/// Resolved by <see cref="StangaNetLib.Tenancy.Middleware.TenantResolutionMiddleware"/> and available via DI as a scoped service.
/// </summary>
public interface ITenantContext
{
    /// <summary>The resolved tenant, or <c>null</c> if no tenant was identified for the current request.</summary>
    TenantInfo? CurrentTenant { get; }

    /// <summary><c>true</c> when a tenant was successfully resolved and is active; <c>false</c> otherwise.</summary>
    bool IsResolved { get; }
}
