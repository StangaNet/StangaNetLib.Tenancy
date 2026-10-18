using StangaNetLib.Tenancy.Abstractions;

namespace StangaNetLib.Tenancy.Models;

/// <summary>
/// A disposable scope that sets <see cref="ITenantAccessor.CurrentTenant"/> on construction and restores
/// the previous value on <see cref="Dispose"/>. Create instances via <c>accessor.BeginScope(tenant)</c>.
/// </summary>
/// <remarks>
/// Designed for background jobs and hosted services that process data for a specific tenant.
/// Using a scope ensures tenant state is always cleaned up, even if the work throws:
/// <code>
/// using (_tenantAccessor.BeginScope(tenant))
/// {
///     await _someService.ProcessAsync();
/// }
/// // CurrentTenant is restored here
/// </code>
/// Scopes are nestable: the outer tenant is restored when the inner scope is disposed.
/// </remarks>
public sealed class TenantScope : IDisposable
{
    private readonly ITenantAccessor _accessor;
    private readonly TenantInfo? _previous;
    private bool _disposed;

    /// <summary>Initialises the scope, sets the current tenant, and captures the previous value.</summary>
    /// <param name="accessor">The ambient accessor to update.</param>
    /// <param name="tenant">The tenant to set for the duration of the scope.</param>
    internal TenantScope(ITenantAccessor accessor, TenantInfo tenant)
    {
        _accessor = accessor;
        _previous = accessor.CurrentTenant;
        accessor.CurrentTenant = tenant;
    }

    /// <summary>Restores <see cref="ITenantAccessor.CurrentTenant"/> to the value it held before the scope was created.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _accessor.CurrentTenant = _previous;
    }
}
