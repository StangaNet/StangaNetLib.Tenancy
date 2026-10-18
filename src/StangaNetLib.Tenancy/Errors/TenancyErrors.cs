using StangaNetLib.Core.Common;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Errors;

/// <summary>Centralised factory for <see cref="StangaNetLib.Core.Common.Error"/> instances produced by the Tenancy library.</summary>
internal static class TenancyErrors
{
    /// <summary>
    /// Returns a not-found error indicating that the tenant key was absent from the request source handled by <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The resolver strategy that failed to locate a tenant key.</param>
    /// <returns>
    /// An <see cref="StangaNetLib.Core.Common.Error"/> with code <c>TenantKey.NotFound.{source}</c>
    /// and type <see cref="StangaNetLib.Core.Common.ErrorType.NotFound"/>.
    /// </returns>
    internal static Error KeyNotFound(TenantResolverType source)
        => new($"TenantKey.NotFound.{source}", $"Tenant key not found in {source} source.", ErrorType.NotFound);
}
