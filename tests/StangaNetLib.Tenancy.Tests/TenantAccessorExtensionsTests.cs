using FluentAssertions;
using StangaNetLib.Tenancy.Extensions;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Tests;

public sealed class TenantAccessorExtensionsTests
{
    private static readonly TenantInfo TenantA = new(Guid.NewGuid(), "Acme", "acme", IsActive: true);
    private static readonly TenantInfo TenantB = new(Guid.NewGuid(), "Beta", "beta", IsActive: true);

    [Fact]
    public void BeginScope_SetsTenant_ForDurationOfScope()
    {
        var accessor = new AsyncLocalTenantAccessor();

        using (accessor.BeginScope(TenantA))
        {
            accessor.CurrentTenant.Should().Be(TenantA);
        }
    }

    [Fact]
    public void BeginScope_RestoresPreviousValue_AfterDispose()
    {
        var accessor = new AsyncLocalTenantAccessor();
        accessor.CurrentTenant = TenantA;

        using (accessor.BeginScope(TenantB))
        {
            accessor.CurrentTenant.Should().Be(TenantB);
        }

        accessor.CurrentTenant.Should().Be(TenantA);
    }

    [Fact]
    public void BeginScope_RestoresNull_WhenNoPreviousTenant()
    {
        var accessor = new AsyncLocalTenantAccessor();

        using (accessor.BeginScope(TenantA))
        {
        }

        accessor.CurrentTenant.Should().BeNull();
    }

    [Fact]
    public void BeginScope_SupportsNesting_RestoringEachLevel()
    {
        var accessor = new AsyncLocalTenantAccessor();

        using (accessor.BeginScope(TenantA))
        {
            accessor.CurrentTenant.Should().Be(TenantA);

            using (accessor.BeginScope(TenantB))
            {
                accessor.CurrentTenant.Should().Be(TenantB);
            }

            accessor.CurrentTenant.Should().Be(TenantA);
        }

        accessor.CurrentTenant.Should().BeNull();
    }

    [Fact]
    public void BeginScope_IsIdempotentOnDoubleDispose()
    {
        var accessor = new AsyncLocalTenantAccessor();
        accessor.CurrentTenant = TenantA;

        var scope = accessor.BeginScope(TenantB);
        scope.Dispose();
        scope.Dispose();

        accessor.CurrentTenant.Should().Be(TenantA);
    }

    [Fact]
    public void BeginScope_ThrowsArgumentNull_WhenAccessorIsNull()
    {
        var act = () => ((AsyncLocalTenantAccessor)null!).BeginScope(TenantA);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BeginScope_ThrowsArgumentNull_WhenTenantIsNull()
    {
        var accessor = new AsyncLocalTenantAccessor();

        var act = () => accessor.BeginScope(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
