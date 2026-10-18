using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Tests;

public sealed class CachingTenantStoreDecoratorTests
{
    private static readonly TenantInfo SampleTenant = new(Guid.NewGuid(), "Acme", "acme", IsActive: true);

    private static IMemoryCache CreateCache() => new MemoryCache(Options.Create(new MemoryCacheOptions()));

    private static CachingTenantStoreDecorator CreateDecorator(ITenantStore inner, IMemoryCache? cache = null, TimeSpan? ttl = null)
        => new(inner, cache ?? CreateCache(), ttl);

    [Fact]
    public async Task GetByIdAsync_ReturnsTenantFromInner_OnCacheMiss()
    {
        var inner = new FakeTenantStore();
        inner.SetupById(SampleTenant.TenantId, Result<TenantInfo>.Success(SampleTenant));
        var decorator = CreateDecorator(inner);

        var result = await decorator.GetByIdAsync(SampleTenant.TenantId, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(SampleTenant);
        inner.GetByIdCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTenantFromCache_OnCacheHit()
    {
        var inner = new FakeTenantStore();
        inner.SetupById(SampleTenant.TenantId, Result<TenantInfo>.Success(SampleTenant));
        var decorator = CreateDecorator(inner);

        await decorator.GetByIdAsync(SampleTenant.TenantId, TestContext.Current.CancellationToken);
        var result = await decorator.GetByIdAsync(SampleTenant.TenantId, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(SampleTenant);
        inner.GetByIdCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotCache_OnFailure()
    {
        var inner = new FakeTenantStore();
        inner.SetupById(SampleTenant.TenantId, Result<TenantInfo>.Failure(Error.NotFound("Tenant", SampleTenant.TenantId)));
        var decorator = CreateDecorator(inner);

        await decorator.GetByIdAsync(SampleTenant.TenantId, TestContext.Current.CancellationToken);
        await decorator.GetByIdAsync(SampleTenant.TenantId, TestContext.Current.CancellationToken);

        inner.GetByIdCallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsTenantFromInner_OnCacheMiss()
    {
        var inner = new FakeTenantStore();
        inner.SetupBySlug("acme", Result<TenantInfo>.Success(SampleTenant));
        var decorator = CreateDecorator(inner);

        var result = await decorator.GetBySlugAsync("acme", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(SampleTenant);
        inner.GetBySlugCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsTenantFromCache_OnCacheHit()
    {
        var inner = new FakeTenantStore();
        inner.SetupBySlug("acme", Result<TenantInfo>.Success(SampleTenant));
        var decorator = CreateDecorator(inner);

        await decorator.GetBySlugAsync("acme", TestContext.Current.CancellationToken);
        var result = await decorator.GetBySlugAsync("ACME", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        inner.GetBySlugCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBySlugAsync_DoesNotCache_OnFailure()
    {
        var inner = new FakeTenantStore();
        inner.SetupBySlug("unknown", Result<TenantInfo>.Failure(Error.NotFound("Tenant", "unknown")));
        var decorator = CreateDecorator(inner);

        await decorator.GetBySlugAsync("unknown", TestContext.Current.CancellationToken);
        await decorator.GetBySlugAsync("unknown", TestContext.Current.CancellationToken);

        inner.GetBySlugCallCount.Should().Be(2);
    }

    [Fact]
    public void CacheTtl_DefaultsFiveMinutes_WhenNotProvided()
    {
        var decorator = CreateDecorator(new FakeTenantStore());

        decorator.CacheTtl.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void CacheTtl_UsesProvidedValue()
    {
        var decorator = CreateDecorator(new FakeTenantStore(), ttl: TimeSpan.FromMinutes(10));

        decorator.CacheTtl.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Constructor_Throws_WhenCacheTtlIsZero()
    {
        var cache = CreateCache();

        var act = () => new CachingTenantStoreDecorator(new FakeTenantStore(), cache, TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("cacheTtl");
    }

    [Fact]
    public void Constructor_Throws_WhenCacheTtlIsNegative()
    {
        var cache = CreateCache();

        var act = () => new CachingTenantStoreDecorator(new FakeTenantStore(), cache, TimeSpan.FromSeconds(-1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("cacheTtl");
    }

    [Fact]
    public async Task InvalidateById_ClearsCache_SoNextCallHitsInner()
    {
        var inner = new FakeTenantStore();
        inner.SetupById(SampleTenant.TenantId, Result<TenantInfo>.Success(SampleTenant));
        var decorator = CreateDecorator(inner);

        await decorator.GetByIdAsync(SampleTenant.TenantId, TestContext.Current.CancellationToken);
        decorator.InvalidateById(SampleTenant.TenantId);
        await decorator.GetByIdAsync(SampleTenant.TenantId, TestContext.Current.CancellationToken);

        inner.GetByIdCallCount.Should().Be(2);
    }

    [Fact]
    public async Task InvalidateBySlug_ClearsCache_SoNextCallHitsInner()
    {
        var inner = new FakeTenantStore();
        inner.SetupBySlug("acme", Result<TenantInfo>.Success(SampleTenant));
        var decorator = CreateDecorator(inner);

        await decorator.GetBySlugAsync("acme", TestContext.Current.CancellationToken);
        decorator.InvalidateBySlug("ACME");
        await decorator.GetBySlugAsync("acme", TestContext.Current.CancellationToken);

        inner.GetBySlugCallCount.Should().Be(2);
    }

    [Fact]
    public void GetBySlugAsync_Throws_WhenSlugIsWhitespace()
    {
        var decorator = CreateDecorator(new FakeTenantStore());

        var act = async () => await decorator.GetBySlugAsync("   ", TestContext.Current.CancellationToken);

        act.Should().ThrowAsync<ArgumentException>().WithParameterName("slug");
    }

    [Fact]
    public void InvalidateBySlug_Throws_WhenSlugIsWhitespace()
    {
        var decorator = CreateDecorator(new FakeTenantStore());

        var act = () => decorator.InvalidateBySlug("   ");

        act.Should().Throw<ArgumentException>().WithParameterName("slug");
    }

    [Fact]
    public async Task GetByIdAsync_AlsoPrimesSlugCache_OnCacheMiss()
    {
        var inner = new FakeTenantStore();
        inner.SetupById(SampleTenant.TenantId, Result<TenantInfo>.Success(SampleTenant));
        var decorator = CreateDecorator(inner);

        await decorator.GetByIdAsync(SampleTenant.TenantId, TestContext.Current.CancellationToken);
        var result = await decorator.GetBySlugAsync(SampleTenant.Slug, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        inner.GetBySlugCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBySlugAsync_AlsoPrimesIdCache_OnCacheMiss()
    {
        var inner = new FakeTenantStore();
        inner.SetupBySlug(SampleTenant.Slug, Result<TenantInfo>.Success(SampleTenant));
        var decorator = CreateDecorator(inner);

        await decorator.GetBySlugAsync(SampleTenant.Slug, TestContext.Current.CancellationToken);
        var result = await decorator.GetByIdAsync(SampleTenant.TenantId, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        inner.GetByIdCallCount.Should().Be(0);
    }
}

// ── Test helpers ─────────────────────────────────────────────────────────────

file sealed class FakeTenantStore : ITenantStore
{
    private readonly Dictionary<Guid, Result<TenantInfo>> _byId = new();
    private readonly Dictionary<string, Result<TenantInfo>> _bySlug = new(StringComparer.OrdinalIgnoreCase);

    public int GetByIdCallCount { get; private set; }
    public int GetBySlugCallCount { get; private set; }

    public void SetupById(Guid id, Result<TenantInfo> result) => _byId[id] = result;
    public void SetupBySlug(string slug, Result<TenantInfo> result) => _bySlug[slug] = result;

    public Task<Result<TenantInfo>> GetByIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        GetByIdCallCount++;
        return Task.FromResult(_byId.TryGetValue(tenantId, out var r) ? r : Result<TenantInfo>.Failure(Error.NotFound("Tenant", tenantId)));
    }

    public Task<Result<TenantInfo>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        GetBySlugCallCount++;
        return Task.FromResult(_bySlug.TryGetValue(slug, out var r) ? r : Result<TenantInfo>.Failure(Error.NotFound("Tenant", slug)));
    }
}
