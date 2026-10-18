using FluentAssertions;
using StangaNetLib.Core.Common;
using StangaNetLib.Tenancy.Extensions;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Tests;

public sealed class TenantContextExtensionsTests
{
    private static readonly TenantInfo SampleTenant = new(Guid.NewGuid(), "Acme", "acme", IsActive: true);
    private static readonly TenantInfo TenantWithConn = new(Guid.NewGuid(), "Acme", "acme", IsActive: true, ConnectionString: "Server=.;Database=acme");
    private static readonly TenantInfo TenantWithMeta = new(Guid.NewGuid(), "Acme", "acme", IsActive: true,
        Metadata: new Dictionary<string, string> { ["plan"] = "pro", ["maxUsers"] = "50" });

    // --- GetRequiredTenant ---

    [Fact]
    public void GetRequiredTenant_ReturnsSuccess_WhenTenantResolved()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(SampleTenant);

        var result = ctx.GetRequiredTenant();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(SampleTenant);
    }

    [Fact]
    public void GetRequiredTenant_ReturnsUnauthorized_WhenNoTenantResolved()
    {
        var ctx = new TenantContext();

        var result = ctx.GetRequiredTenant();

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void GetRequiredTenant_ThrowsArgumentNull_WhenContextIsNull()
    {
        var act = () => ((TenantContext)null!).GetRequiredTenant();

        act.Should().Throw<ArgumentNullException>();
    }

    // --- GetTenantId / RequireTenantId ---

    [Fact]
    public void GetTenantId_ReturnsTenantId_WhenTenantResolved()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(SampleTenant);

        ctx.GetTenantId().Should().Be(SampleTenant.TenantId);
    }

    [Fact]
    public void GetTenantId_ReturnsNull_WhenNoTenantResolved()
    {
        var ctx = new TenantContext();

        ctx.GetTenantId().Should().BeNull();
    }

    [Fact]
    public void RequireTenantId_ReturnsSuccess_WhenTenantResolved()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(SampleTenant);

        var result = ctx.RequireTenantId();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(SampleTenant.TenantId);
    }

    [Fact]
    public void RequireTenantId_ReturnsUnauthorized_WhenNoTenantResolved()
    {
        var ctx = new TenantContext();

        var result = ctx.RequireTenantId();

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    // --- GetConnectionString / RequireConnectionString ---

    [Fact]
    public void GetConnectionString_ReturnsConnectionString_WhenTenantResolved()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(TenantWithConn);

        ctx.GetConnectionString().Should().Be(TenantWithConn.ConnectionString);
    }

    [Fact]
    public void GetConnectionString_ReturnsNull_WhenNoTenantResolved()
    {
        var ctx = new TenantContext();

        ctx.GetConnectionString().Should().BeNull();
    }

    [Fact]
    public void GetConnectionString_ReturnsNull_WhenTenantHasNoConnectionString()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(SampleTenant);

        ctx.GetConnectionString().Should().BeNull();
    }

    [Fact]
    public void RequireConnectionString_ReturnsSuccess_WhenConnectionStringPresent()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(TenantWithConn);

        var result = ctx.RequireConnectionString();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(TenantWithConn.ConnectionString);
    }

    [Fact]
    public void RequireConnectionString_ReturnsUnauthorized_WhenNoTenantResolved()
    {
        var ctx = new TenantContext();

        var result = ctx.RequireConnectionString();

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void RequireConnectionString_ReturnsNotFound_WhenConnectionStringMissing()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(SampleTenant);

        var result = ctx.RequireConnectionString();

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    // --- GetMetadata on ITenantContext ---

    [Fact]
    public void GetMetadata_ReturnsValue_WhenTenantResolvedAndKeyExists()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(TenantWithMeta);

        ctx.GetMetadata("plan").Should().Be("pro");
    }

    [Fact]
    public void GetMetadata_ReturnsNull_WhenNoTenantResolved()
    {
        var ctx = new TenantContext();

        ctx.GetMetadata("plan").Should().BeNull();
    }

    [Fact]
    public void GetMetadata_ReturnsNull_WhenKeyAbsent()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(TenantWithMeta);

        ctx.GetMetadata("missing").Should().BeNull();
    }

    [Fact]
    public void GetMetadataT_ReturnsDeserializedValue_WhenKeyExists()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(TenantWithMeta);

        ctx.GetMetadata<int>("maxUsers").Should().Be(50);
    }

    [Fact]
    public void GetMetadataT_ReturnsDefault_WhenNoTenantResolved()
    {
        var ctx = new TenantContext();

        ctx.GetMetadata<int>("maxUsers", defaultValue: 5).Should().Be(5);
    }

    [Fact]
    public void GetMetadataT_ReturnsDefault_WhenKeyAbsent()
    {
        var ctx = new TenantContext();
        ctx.SetTenant(TenantWithMeta);

        ctx.GetMetadata<int>("unknown", defaultValue: 99).Should().Be(99);
    }
}
