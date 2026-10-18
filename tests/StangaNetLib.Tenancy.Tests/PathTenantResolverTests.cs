using FluentAssertions;
using Microsoft.AspNetCore.Http;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Tests;

public sealed class PathTenantResolverTests
{
    private static FakeOptionsMonitor Monitor(TenancySettings settings) => new(settings);

    private static PathTenantResolver CreateResolver(int segmentIndex = 0, TenantLookupType lookupType = TenantLookupType.BySlug)
    {
        var settings = new TenancySettings { PathSegmentIndex = segmentIndex, PathLookupType = lookupType };
        return new PathTenantResolver(Monitor(settings));
    }

    [Fact]
    public async Task ResolveAsync_ReturnsSuccess_WhenIndexIsZero()
    {
        var resolver = CreateResolver(0);
        var context = new DefaultHttpContext();
        context.Request.Path = "/acme/orders/42";

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("acme");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsSecondSegment_WhenIndexIsOne()
    {
        var resolver = CreateResolver(1);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/acme/orders";

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("acme");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenPathTooShort()
    {
        var resolver = CreateResolver(2);
        var context = new DefaultHttpContext();
        context.Request.Path = "/acme";

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenPathIsRoot()
    {
        var resolver = CreateResolver(0);
        var context = new DefaultHttpContext();
        context.Request.Path = "/";

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ResolverType_IsPath()
    {
        CreateResolver().ResolverType.Should().Be(TenantResolverType.Path);
    }

    [Fact]
    public void LookupType_DefaultsBySlug()
    {
        CreateResolver().LookupType.Should().Be(TenantLookupType.BySlug);
    }

    [Fact]
    public void LookupType_ReflectsSettings()
    {
        CreateResolver(lookupType: TenantLookupType.ById).LookupType.Should().Be(TenantLookupType.ById);
    }
}
