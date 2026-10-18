using FluentAssertions;
using Microsoft.AspNetCore.Http;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Tests;

public sealed class HostTenantResolverTests
{
    private static FakeOptionsMonitor Monitor(TenantLookupType lookupType = TenantLookupType.BySlug, int hostMinParts = 3)
        => new(new TenancySettings { HostLookupType = lookupType, HostMinParts = hostMinParts });

    private static HostTenantResolver CreateResolver(TenantLookupType lookupType = TenantLookupType.BySlug, int hostMinParts = 3)
        => new(Monitor(lookupType, hostMinParts));

    [Fact]
    public async Task ResolveAsync_ReturnsSuccess_WhenThreePartHost()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.example.com");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("acme");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenHostHasNoDots()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenOnlyTwoPartHost()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("example.com");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ResolverType_IsHost()
    {
        CreateResolver().ResolverType.Should().Be(TenantResolverType.Host);
    }

    [Fact]
    public void LookupType_DefaultsBySlug()
    {
        CreateResolver().LookupType.Should().Be(TenantLookupType.BySlug);
    }

    [Fact]
    public void LookupType_ReflectsSettings()
    {
        CreateResolver(TenantLookupType.ById).LookupType.Should().Be(TenantLookupType.ById);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsSuccess_WhenTwoPartHostAndMinPartsIsTwo()
    {
        var resolver = CreateResolver(hostMinParts: 2);
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.localdev");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("acme");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenTwoPartHostAndDefaultMinParts()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.localdev");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }
}
