using StangaNetLib.Core.Guards;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Extensions;

/// <summary>Extension methods for <see cref="ITenantAccessor"/>.</summary>
public static class TenantAccessorExtensions
{
    /// <summary>
    /// Creates a <see cref="TenantScope"/> that sets <see cref="ITenantAccessor.CurrentTenant"/> to <paramref name="tenant"/>
    /// and restores the previous value when disposed.
    /// </summary>
    /// <param name="accessor">The ambient tenant accessor to update.</param>
    /// <param name="tenant">The tenant to activate for the duration of the scope.</param>
    /// <returns>A <see cref="TenantScope"/> that restores the previous tenant on <see cref="IDisposable.Dispose"/>.</returns>
    /// <example>
    /// <code>
    /// foreach (var tenant in tenants)
    /// {
    ///     using (_tenantAccessor.BeginScope(tenant))
    ///     {
    ///         await _processor.RunAsync();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static TenantScope BeginScope(this ITenantAccessor accessor, TenantInfo tenant)
    {
        Guard.Against.Null(accessor, nameof(accessor));
        Guard.Against.Null(tenant, nameof(tenant));
        return new TenantScope(accessor, tenant);
    }
}
