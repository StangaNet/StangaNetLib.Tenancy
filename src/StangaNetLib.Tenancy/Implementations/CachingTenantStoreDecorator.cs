using Microsoft.Extensions.Caching.Memory;
using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Implementations;

/// <summary>
/// Decorates an <see cref="ITenantStore"/> with an in-memory cache layer.
/// Successful lookups are cached for <see cref="CacheTtl"/> so that repeated requests for the
/// same tenant within the TTL window do not hit the underlying store.
/// Failed lookups (not-found, errors) are never cached.
/// Both by-ID and by-slug cache entries are primed on every successful lookup to minimise store hits
/// when the pipeline uses mixed resolution strategies.
/// </summary>
/// <remarks>
/// Implements <see cref="ICachingTenantStore"/> for explicit cache-invalidation when tenant data changes.
/// </remarks>
public sealed class CachingTenantStoreDecorator : ICachingTenantStore
{
    private readonly ITenantStore _inner;
    private readonly IMemoryCache _cache;

    /// <summary>How long a successful tenant lookup is retained in memory. Default: 5 minutes.</summary>
    public TimeSpan CacheTtl { get; }

    /// <summary>Initialises the decorator.</summary>
    /// <param name="inner">The wrapped store used on cache misses.</param>
    /// <param name="cache">Memory cache instance shared with the application.</param>
    /// <param name="cacheTtl">Retention duration for cached entries. Defaults to 5 minutes when <c>null</c>. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="cacheTtl"/> is zero or negative.</exception>
    public CachingTenantStoreDecorator(ITenantStore inner, IMemoryCache cache, TimeSpan? cacheTtl = null)
    {
        _inner = Guard.Against.Null(inner, nameof(inner));
        _cache = Guard.Against.Null(cache, nameof(cache));

        if (cacheTtl.HasValue && cacheTtl.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(cacheTtl), cacheTtl.Value, "Cache TTL must be a positive duration.");

        CacheTtl = cacheTtl ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc/>
    public async Task<Result<TenantInfo>> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeyById(tenantId);
        if (_cache.TryGetValue(key, out TenantInfo? cached))
            return Result<TenantInfo>.Success(cached!);

        var result = await _inner.GetByIdAsync(tenantId, cancellationToken);
        if (result.IsSuccess)
        {
            _cache.Set(key, result.Value, CacheTtl);
            _cache.Set(CacheKeyBySlug(result.Value!.Slug), result.Value, CacheTtl);
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<TenantInfo>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(slug, nameof(slug));

        var key = CacheKeyBySlug(slug);
        if (_cache.TryGetValue(key, out TenantInfo? cached))
            return Result<TenantInfo>.Success(cached!);

        var result = await _inner.GetBySlugAsync(slug, cancellationToken);
        if (result.IsSuccess)
        {
            _cache.Set(key, result.Value, CacheTtl);
            _cache.Set(CacheKeyById(result.Value!.TenantId), result.Value, CacheTtl);
        }
        return result;
    }

    /// <inheritdoc/>
    public void InvalidateById(Guid tenantId) => _cache.Remove(CacheKeyById(tenantId));

    /// <inheritdoc/>
    public void InvalidateBySlug(string slug)
    {
        Guard.Against.NullOrWhiteSpace(slug, nameof(slug));
        _cache.Remove(CacheKeyBySlug(slug));
    }

    private static string CacheKeyById(Guid tenantId) => $"StangaNetLib.Tenancy:id:{tenantId}";
    private static string CacheKeyBySlug(string slug) => $"StangaNetLib.Tenancy:slug:{slug.ToLowerInvariant()}";
}
