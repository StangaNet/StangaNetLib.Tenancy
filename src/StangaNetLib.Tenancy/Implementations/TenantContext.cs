using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Implementations;

/// <summary>
/// Scoped, mutable holder for the current tenant. Populated by <see cref="StangaNetLib.Tenancy.Middleware.TenantResolutionMiddleware"/>.
/// Consume as <see cref="ITenantContext"/> via DI.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    /// <inheritdoc/>
    public TenantInfo? CurrentTenant { get; private set; }

    /// <inheritdoc/>
    public bool IsResolved => CurrentTenant is not null;

    /// <summary>Populates <see cref="CurrentTenant"/>. Internal so only <see cref="StangaNetLib.Tenancy.Middleware.TenantResolutionMiddleware"/> can mutate the context.</summary>
    internal void SetTenant(TenantInfo tenant) => CurrentTenant = tenant;
}
