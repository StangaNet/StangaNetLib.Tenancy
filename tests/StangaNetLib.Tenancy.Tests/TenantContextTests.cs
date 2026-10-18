using FluentAssertions;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Tests;

public sealed class TenantContextTests
{
    [Fact]
    public void IsResolved_ReturnsFalse_WhenNoTenantSet()
    {
        var ctx = new TenantContext();

        ctx.IsResolved.Should().BeFalse();
        ctx.CurrentTenant.Should().BeNull();
    }

    [Fact]
    public void SetTenant_PopulatesCurrentTenant()
    {
        var ctx = new TenantContext();
        var tenant = new TenantInfo(Guid.NewGuid(), "Acme", "acme", true);

        ctx.SetTenant(tenant);

        ctx.IsResolved.Should().BeTrue();
        ctx.CurrentTenant.Should().Be(tenant);
    }

    [Fact]
    public void SetTenant_OverwritesPreviousTenant()
    {
        var ctx = new TenantContext();
        var first = new TenantInfo(Guid.NewGuid(), "Acme", "acme", true);
        var second = new TenantInfo(Guid.NewGuid(), "Beta", "beta", true);

        ctx.SetTenant(first);
        ctx.SetTenant(second);

        ctx.CurrentTenant.Should().Be(second);
    }
}
