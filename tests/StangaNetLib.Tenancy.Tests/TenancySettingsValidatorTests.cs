using FluentAssertions;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Tests;

public sealed class TenancySettingsValidatorTests
{
    private static readonly TenancySettingsValidator Validator = new();

    [Fact]
    public void Validate_ReturnsSuccess_WhenSettingsAreValid()
    {
        var settings = new TenancySettings
        {
            ResolverOrder = [TenantResolverType.Header, TenantResolverType.Claim],
            ExcludedPaths = ["/health", "/metrics"]
        };

        var result = Validator.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReturnsFail_WhenResolverOrderHasDuplicates()
    {
        var settings = new TenancySettings
        {
            ResolverOrder = [TenantResolverType.Header, TenantResolverType.Header]
        };

        var result = Validator.Validate(null, settings);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Header");
    }

    [Fact]
    public void Validate_ReturnsFail_WhenExcludedPathMissingLeadingSlash()
    {
        var settings = new TenancySettings
        {
            ExcludedPaths = ["health"]
        };

        var result = Validator.Validate(null, settings);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("health");
    }

    [Fact]
    public void Validate_ReturnsFail_WhenBothDuplicatesAndInvalidPaths()
    {
        var settings = new TenancySettings
        {
            ResolverOrder = [TenantResolverType.Path, TenantResolverType.Path],
            ExcludedPaths = ["metrics"]
        };

        var result = Validator.Validate(null, settings);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Path");
        result.FailureMessage.Should().Contain("metrics");
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenCacheTtlIsPositive()
    {
        var settings = new TenancySettings
        {
            CacheTtl = TimeSpan.FromMinutes(10)
        };

        var result = Validator.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReturnsFail_WhenCacheTtlIsZero()
    {
        var settings = new TenancySettings
        {
            CacheTtl = TimeSpan.Zero
        };

        var result = Validator.Validate(null, settings);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("CacheTtl");
    }

    [Fact]
    public void Validate_ReturnsFail_WhenCacheTtlIsNegative()
    {
        var settings = new TenancySettings
        {
            CacheTtl = TimeSpan.FromSeconds(-30)
        };

        var result = Validator.Validate(null, settings);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("CacheTtl");
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenCacheTtlIsNull()
    {
        var settings = new TenancySettings { CacheTtl = null };

        var result = Validator.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }
}
