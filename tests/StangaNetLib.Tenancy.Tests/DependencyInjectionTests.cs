using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StangaNetLib.Core.Common;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Extensions;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddStangaNetLibTenancy_registers_core_services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStangaNetLibTenancy(opt =>
        {
            opt.ResolverOrder = [TenantResolverType.Header];
        });
        services.AddScoped<ITenantStore, StubTenantStore>();

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITenantContext>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ITenantStore>().Should().NotBeNull();
        sp.GetRequiredService<ITenantAccessor>().Should().NotBeNull();
        sp.GetServices<ITenantResolver>().Should().NotBeEmpty();
    }

    private sealed class StubTenantStore : ITenantStore
    {
        public Task<Result<TenantInfo>> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<TenantInfo>.Failure(Error.NotFound("Tenant", tenantId)));

        public Task<Result<TenantInfo>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<TenantInfo>.Failure(Error.NotFound("Tenant", slug)));
    }
}
