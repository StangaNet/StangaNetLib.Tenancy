namespace StangaNetLib.Tenancy.Abstractions;

/// <summary>
/// Extends <see cref="ITenantStore"/> with explicit cache-invalidation operations.
/// Cast <see cref="ITenantStore"/> to this interface — or inject it directly — when you need to evict
/// a specific tenant entry after a data change (status update, rename, deletion).
/// </summary>
/// <remarks>
/// <see cref="StangaNetLib.Tenancy.Implementations.CachingTenantStoreDecorator"/> implements this interface.
/// You can inject <see cref="ICachingTenantStore"/> directly when caching is registered via
/// <c>AddStangaNetLibTenancyCache&lt;TStore&gt;</c>, for example from an admin endpoint:
/// <code>
/// public IActionResult DeactivateTenant(Guid tenantId)
/// {
///     // ... update DB ...
///     if (_tenantStore is ICachingTenantStore cachingStore)
///         cachingStore.InvalidateById(tenantId);
///     return NoContent();
/// }
/// </code>
/// </remarks>
public interface ICachingTenantStore : ITenantStore
{
    /// <summary>
    /// Removes the in-memory cache entry for the tenant identified by <paramref name="tenantId"/>.
    /// The next lookup for this tenant will hit the underlying store.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant whose cache entry should be evicted.</param>
    void InvalidateById(Guid tenantId);

    /// <summary>
    /// Removes the in-memory cache entry for the tenant identified by <paramref name="slug"/>.
    /// The comparison is case-insensitive, matching the caching behaviour of
    /// <see cref="StangaNetLib.Tenancy.Implementations.CachingTenantStoreDecorator"/>.
    /// The next lookup for this slug will hit the underlying store.
    /// </summary>
    /// <param name="slug">The URL-safe slug of the tenant whose cache entry should be evicted.</param>
    void InvalidateBySlug(string slug);
}
