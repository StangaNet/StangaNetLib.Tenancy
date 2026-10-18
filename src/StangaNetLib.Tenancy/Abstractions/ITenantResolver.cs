using Microsoft.AspNetCore.Http;
using StangaNetLib.Core.Common;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Abstractions;

/// <summary>
/// Extracts a raw tenant key (ID or slug) from an incoming HTTP request.
/// Multiple implementations can be registered; the middleware runs them in the order configured in <see cref="StangaNetLib.Tenancy.Settings.TenancySettings.ResolverOrder"/>.
/// </summary>
public interface ITenantResolver
{
    /// <summary>The resolver strategy this implementation handles.</summary>
    TenantResolverType ResolverType { get; }

    /// <summary>Indicates whether the returned key should be interpreted as a tenant ID or a slug.</summary>
    TenantLookupType LookupType { get; }

    /// <summary>
    /// Attempts to extract the tenant key from <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the raw non-empty key string when the resolver
    /// finds tenant information in the request; a failed <see cref="Result{T}"/> when this resolver
    /// cannot determine the tenant from the current request.
    /// </returns>
    Task<Result<string>> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default);
}
