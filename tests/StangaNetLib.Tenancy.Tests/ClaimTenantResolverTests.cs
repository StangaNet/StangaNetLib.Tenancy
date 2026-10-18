using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Tests;

public sealed class ClaimTenantResolverTests
{
    private static FakeOptionsMonitor Monitor(TenancySettings settings) => new(settings);

    private static ClaimTenantResolver CreateResolver(string claimType = "tenant_id", TenantLookupType lookupType = TenantLookupType.ById)
    {
        var settings = new TenancySettings { ClaimType = claimType, ClaimLookupType = lookupType };
        return new ClaimTenantResolver(Monitor(settings));
    }

    private static DefaultHttpContext ContextWithClaim(string claimType, string claimValue)
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity([new Claim(claimType, claimValue)], "test");
        context.User = new ClaimsPrincipal(identity);
        return context;
    }

    [Fact]
    public async Task ResolveAsync_ReturnsSuccess_WhenClaimPresent()
    {
        var resolver = CreateResolver();
        var context = ContextWithClaim("tenant_id", "guid-value-here");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("guid-value-here");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsFailure_WhenClaimAbsent()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_UsesCustomClaimType()
    {
        var resolver = CreateResolver(claimType: "tid");
        var context = ContextWithClaim("tid", "tenant-slug");

        var result = await resolver.ResolveAsync(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("tenant-slug");
    }

    [Fact]
    public void ResolverType_IsClaim()
    {
        CreateResolver().ResolverType.Should().Be(TenantResolverType.Claim);
    }

    [Fact]
    public void LookupType_ReflectsSettings()
    {
        CreateResolver(lookupType: TenantLookupType.BySlug).LookupType.Should().Be(TenantLookupType.BySlug);
    }
}
