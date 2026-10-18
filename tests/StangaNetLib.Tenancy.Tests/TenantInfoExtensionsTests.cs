using FluentAssertions;
using StangaNetLib.Tenancy.Extensions;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Tests;

public sealed class TenantInfoExtensionsTests
{
    private static TenantInfo TenantWith(IReadOnlyDictionary<string, string>? metadata) =>
        new(Guid.NewGuid(), "Acme", "acme", IsActive: true, Metadata: metadata);

    // --- GetMetadata(string) ---

    [Fact]
    public void GetMetadata_ReturnsValue_WhenKeyExists()
    {
        var tenant = TenantWith(new Dictionary<string, string> { ["plan"] = "pro" });

        tenant.GetMetadata("plan").Should().Be("pro");
    }

    [Fact]
    public void GetMetadata_ReturnsNull_WhenKeyAbsent()
    {
        var tenant = TenantWith(new Dictionary<string, string> { ["plan"] = "pro" });

        tenant.GetMetadata("missing").Should().BeNull();
    }

    [Fact]
    public void GetMetadata_ReturnsNull_WhenMetadataIsNull()
    {
        var tenant = TenantWith(null);

        tenant.GetMetadata("plan").Should().BeNull();
    }

    // --- GetMetadata<T> ---

    [Fact]
    public void GetMetadataT_ReturnsString_WithoutJsonParsing()
    {
        var tenant = TenantWith(new Dictionary<string, string> { ["plan"] = "enterprise" });

        tenant.GetMetadata<string>("plan").Should().Be("enterprise");
    }

    [Fact]
    public void GetMetadataT_DeserializesInt()
    {
        var tenant = TenantWith(new Dictionary<string, string> { ["maxUsers"] = "50" });

        tenant.GetMetadata<int>("maxUsers").Should().Be(50);
    }

    [Fact]
    public void GetMetadataT_DeserializesBool()
    {
        var tenant = TenantWith(new Dictionary<string, string> { ["beta"] = "true" });

        tenant.GetMetadata<bool>("beta").Should().BeTrue();
    }

    [Fact]
    public void GetMetadataT_ReturnsDefaultValue_WhenKeyAbsent()
    {
        var tenant = TenantWith(new Dictionary<string, string>());

        tenant.GetMetadata<int>("maxUsers", defaultValue: 10).Should().Be(10);
    }

    [Fact]
    public void GetMetadataT_ReturnsDefaultValue_WhenMetadataIsNull()
    {
        var tenant = TenantWith(null);

        tenant.GetMetadata<string>("plan", defaultValue: "free").Should().Be("free");
    }

    [Fact]
    public void GetMetadataT_ThrowsArgumentNull_WhenTenantIsNull()
    {
        var act = () => ((TenantInfo)null!).GetMetadata("plan");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetMetadataT_ThrowsArgumentNull_WhenKeyIsNull()
    {
        var tenant = TenantWith(null);

        var act = () => tenant.GetMetadata(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetMetadataT_ReturnsDefault_WhenStoredValueIsEmptyString()
    {
        var tenant = TenantWith(new Dictionary<string, string> { ["count"] = "" });

        tenant.GetMetadata<int>("count", defaultValue: 7).Should().Be(7);
    }

    [Fact]
    public void GetMetadata_ReturnsEmptyString_WhenStoredValueIsEmpty()
    {
        // The raw-string overload returns the stored value as-is (including empty string).
        var tenant = TenantWith(new Dictionary<string, string> { ["tag"] = "" });

        tenant.GetMetadata("tag").Should().Be("");
    }
}
