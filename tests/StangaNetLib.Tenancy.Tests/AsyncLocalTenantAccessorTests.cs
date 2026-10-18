using FluentAssertions;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Tests;

public sealed class AsyncLocalTenantAccessorTests
{
    private static readonly TenantInfo SampleTenant = new(Guid.NewGuid(), "Acme", "acme", IsActive: true);

    [Fact]
    public void CurrentTenant_IsNull_Initially()
    {
        var accessor = new AsyncLocalTenantAccessor();

        accessor.CurrentTenant.Should().BeNull();
    }

    [Fact]
    public void CurrentTenant_ReturnsTenant_AfterSet()
    {
        var accessor = new AsyncLocalTenantAccessor();

        accessor.CurrentTenant = SampleTenant;

        accessor.CurrentTenant.Should().Be(SampleTenant);
    }

    [Fact]
    public void CurrentTenant_CanBeReset_ToNull()
    {
        var accessor = new AsyncLocalTenantAccessor();
        accessor.CurrentTenant = SampleTenant;

        accessor.CurrentTenant = null;

        accessor.CurrentTenant.Should().BeNull();
    }

    [Fact]
    public async Task CurrentTenant_PropagatesIntoChildTask()
    {
        var accessor = new AsyncLocalTenantAccessor();
        accessor.CurrentTenant = SampleTenant;

        TenantInfo? captured = null;
        await Task.Run(() => { captured = accessor.CurrentTenant; }, TestContext.Current.CancellationToken);

        captured.Should().Be(SampleTenant);
    }

    [Fact]
    public async Task CurrentTenant_DoesNotLeakBetweenIndependentTasks()
    {
        var accessor = new AsyncLocalTenantAccessor();
        var tenant1 = new TenantInfo(Guid.NewGuid(), "T1", "t1", IsActive: true);

        // Task 1 sets the tenant inside its own context.
        var task1 = Task.Run(async () =>
        {
            accessor.CurrentTenant = tenant1;
            await Task.Delay(10, TestContext.Current.CancellationToken);
            return accessor.CurrentTenant;
        }, TestContext.Current.CancellationToken);

        // Task 2 starts with no tenant (independent execution context via Task.Run).
        var task2 = Task.Run(() => accessor.CurrentTenant, TestContext.Current.CancellationToken);

        await Task.WhenAll((Task)task1, (Task)task2);

        var result1 = await task1;
        var result2 = await task2;

        result1.Should().Be(tenant1);
        result2.Should().BeNull();
    }
}
