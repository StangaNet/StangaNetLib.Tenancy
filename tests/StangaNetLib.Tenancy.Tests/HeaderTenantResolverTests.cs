using FluentAssertions;
using Microsoft.AspNetCore.Http;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Tests;

public sealed class HeaderTenantResolverTests
{
    private static FakeOptionsMonitor Monitor(TenancySettings settings) => new(settings);

    private static HeaderTenantResolver CreateResolver(string headerName = "X-Tenant-Id", TenantLookupType lookupType = TenantLookupType.ById)
    {
        var settings = new TenancySettings { HeaderName = headerName, HeaderLookupType = lookupType };
        return new HeaderTenantResolver(Monitor(settings));
    }

    [Fact]
    public async Task ResolveAsync_ReturnsSuccess_WhenHeaderPresent()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "abc-123";

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abc-123");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenHeaderAbsent()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenHeaderIsWhitespace()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "   ";

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_UsesCustomHeaderName()
    {
        var resolver = CreateResolver(headerName: "X-Custom-Tenant");
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Custom-Tenant"] = "tenant-42";

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("tenant-42");
    }

    [Fact]
    public void ResolverType_IsHeader()
    {
        CreateResolver().ResolverType.Should().Be(TenantResolverType.Header);
    }

    [Fact]
    public void LookupType_ReflectsSettings()
    {
        CreateResolver(lookupType: TenantLookupType.BySlug).LookupType.Should().Be(TenantLookupType.BySlug);
    }
}
