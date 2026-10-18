using FluentAssertions;
using Microsoft.AspNetCore.Http;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Tests;

public sealed class QueryStringTenantResolverTests
{
    private static FakeOptionsMonitor Monitor(TenancySettings settings) => new(settings);

    private static QueryStringTenantResolver CreateResolver(string paramName = "tenantId", TenantLookupType lookupType = TenantLookupType.ById)
    {
        var settings = new TenancySettings { QueryStringParameterName = paramName, QueryStringLookupType = lookupType };
        return new QueryStringTenantResolver(Monitor(settings));
    }

    [Fact]
    public async Task ResolveAsync_ReturnsSuccess_WhenParamPresent()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?tenantId=abc-123");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abc-123");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenParamAbsent()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenParamIsWhitespace()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?tenantId=   ");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_UsesCustomParamName()
    {
        var resolver = CreateResolver(paramName: "tid");
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?tid=acme");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("acme");
    }

    [Fact]
    public void ResolverType_IsQueryString()
    {
        CreateResolver().ResolverType.Should().Be(TenantResolverType.QueryString);
    }

    [Fact]
    public void LookupType_ReflectsSettings()
    {
        CreateResolver(lookupType: TenantLookupType.BySlug).LookupType.Should().Be(TenantLookupType.BySlug);
    }
}
