using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Implementations;

/// <summary>
/// <see cref="AsyncLocal{T}"/>-backed implementation of <see cref="ITenantAccessor"/>.
/// The tenant value propagates automatically through async/await continuations within the same execution context.
/// Each independent execution context (new thread, new task without captured context) starts with a clean <c>null</c> value.
/// </summary>
public sealed class AsyncLocalTenantAccessor : ITenantAccessor
{
    private static readonly AsyncLocal<TenantInfo?> _current = new();

    /// <inheritdoc/>
    public TenantInfo? CurrentTenant
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
