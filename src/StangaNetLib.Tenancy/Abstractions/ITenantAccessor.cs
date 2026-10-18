using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Abstractions;

/// <summary>
/// Provides ambient read/write access to the current tenant outside the HTTP pipeline.
/// Backed by <see cref="System.Threading.AsyncLocal{T}"/> so the value flows automatically through async continuations.
/// </summary>
/// <remarks>
/// <see cref="StangaNetLib.Tenancy.Middleware.TenantResolutionMiddleware"/> sets this automatically for HTTP requests.
/// In background jobs, hosted services, and batch processors, set <see cref="CurrentTenant"/> manually before starting tenant-scoped work:
/// <code>
/// public async Task ProcessTenantAsync(TenantInfo tenant)
/// {
///     _tenantAccessor.CurrentTenant = tenant;
///     await _someService.DoWorkAsync();
/// }
/// </code>
/// </remarks>
public interface ITenantAccessor
{
    /// <summary>
    /// The current tenant in scope for the executing async flow, or <c>null</c> when no tenant is set.
    /// Setting this to <c>null</c> clears the tenant for the current async context.
    /// </summary>
    TenantInfo? CurrentTenant { get; set; }
}
