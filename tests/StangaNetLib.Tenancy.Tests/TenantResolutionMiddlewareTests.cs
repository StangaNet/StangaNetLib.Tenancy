using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Middleware;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Tests;

public sealed class TenantResolutionMiddlewareTests
{
    private static readonly TenantInfo ActiveTenant = new(Guid.NewGuid(), "Acme", "acme", IsActive: true);
    private static readonly TenantInfo InactiveTenant = new(Guid.NewGuid(), "Old Corp", "oldcorp", IsActive: false);

    private static TenantResolutionMiddleware CreateMiddleware(RequestDelegate? next = null, ITenantAccessor? accessor = null)
        => new(next ?? (_ => Task.CompletedTask), NullLogger<TenantResolutionMiddleware>.Instance, accessor ?? new AsyncLocalTenantAccessor());

    private static IOptionsSnapshot<TenancySettings> Settings(
        bool requireTenant = false,
        TenantResolverType resolverType = TenantResolverType.Header,
        int requireTenantStatusCode = 401,
        bool allowInactiveTenants = false,
        IList<string>? excludedPaths = null)
        => new FakeOptionsSnapshot(new TenancySettings
        {
            ResolverOrder = [resolverType],
            RequireTenant = requireTenant,
            RequireTenantStatusCode = requireTenantStatusCode,
            AllowInactiveTenants = allowInactiveTenants,
            ExcludedPaths = excludedPaths ?? []
        });

    [Fact]
    public async Task InvokeAsync_SetsTenant_WhenResolverAndStoreSucceed()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header, LookupType = TenantLookupType.BySlug };

        resolver.SetupResolve(Result<string>.Success("acme"));
        store.SetupBySlug("acme", Result<TenantInfo>.Success(ActiveTenant));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings());

        tenantCtx.IsResolved.Should().BeTrue();
        tenantCtx.CurrentTenant.Should().Be(ActiveTenant);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_DoesNotSetTenant_WhenResolverReturnsNull()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header, LookupType = TenantLookupType.BySlug };

        resolver.SetupResolve(Result<string>.Failure(Error.NotFound("TenantKey", "Header")));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings());

        tenantCtx.IsResolved.Should().BeFalse();
        store.GetBySlugCallCount.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotSetTenant_WhenTenantIsInactive()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header, LookupType = TenantLookupType.BySlug };

        resolver.SetupResolve(Result<string>.Success("oldcorp"));
        store.SetupBySlug("oldcorp", Result<TenantInfo>.Success(InactiveTenant));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings());

        tenantCtx.IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_Returns401WithJsonBody_WhenRequireTenantAndNotResolved()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header };

        resolver.SetupResolve(Result<string>.Failure(Error.NotFound("TenantKey", "Header")));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings(requireTenant: true));

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.ContentType.Should().StartWith("application/json");
        nextCalled.Should().BeFalse();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("error");
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenRequireTenantFalseAndNotResolved()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header };

        resolver.SetupResolve(Result<string>.Failure(Error.NotFound("TenantKey", "Header")));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings(requireTenant: false));

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_LooksUpById_WhenLookupTypeIsById()
    {
        var middleware = CreateMiddleware();
        var tenantId = Guid.NewGuid();
        var tenant = ActiveTenant with { TenantId = tenantId };
        var context = new DefaultHttpContext();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header, LookupType = TenantLookupType.ById };

        resolver.SetupResolve(Result<string>.Success(tenantId.ToString()));
        store.SetupById(tenantId, Result<TenantInfo>.Success(tenant));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings());

        tenantCtx.IsResolved.Should().BeTrue();
        store.GetByIdCallCount.Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_SkipsResolver_WhenKeyIsInvalidGuid()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header, LookupType = TenantLookupType.ById };

        resolver.SetupResolve(Result<string>.Success("not-a-guid"));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings());

        tenantCtx.IsResolved.Should().BeFalse();
        store.GetByIdCallCount.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_SkipsResolver_WhenStoreThrows()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header, LookupType = TenantLookupType.BySlug };

        resolver.SetupResolve(Result<string>.Success("acme"));
        store.SetupBySlugThrows("acme", new InvalidOperationException("DB error"));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings());

        tenantCtx.IsResolved.Should().BeFalse();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ReturnsConfiguredStatusCode_WhenRequireTenantAndCustomCode()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header };

        resolver.SetupResolve(Result<string>.Failure(Error.NotFound("TenantKey", "Header")));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver],
            Settings(requireTenant: true, requireTenantStatusCode: 403));

        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InvokeAsync_SetsTenant_WhenAllowInactiveTenantsAndTenantIsInactive()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header, LookupType = TenantLookupType.BySlug };

        resolver.SetupResolve(Result<string>.Success("oldcorp"));
        store.SetupBySlug("oldcorp", Result<TenantInfo>.Success(InactiveTenant));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver],
            Settings(allowInactiveTenants: true));

        tenantCtx.IsResolved.Should().BeTrue();
        tenantCtx.CurrentTenant.Should().Be(InactiveTenant);
    }

    [Fact]
    public async Task InvokeAsync_BypassesResolution_WhenPathIsExcluded()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header };

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver],
            Settings(requireTenant: true, excludedPaths: ["/health"]));

        tenantCtx.IsResolved.Should().BeFalse();
        nextCalled.Should().BeTrue();
        resolver.ResolveCallCount.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_BypassesResolution_WhenPathStartsWithExcludedPrefix()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header };

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver],
            Settings(requireTenant: true, excludedPaths: ["/health"]));

        nextCalled.Should().BeTrue();
        tenantCtx.IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_PopulatesTenantAccessor_WhenTenantResolved()
    {
        // AsyncLocal values propagate downward into child continuations (not back to the caller).
        // The accessor must be sampled from inside the `next` delegate, which runs within the middleware's execution context.
        var accessor = new AsyncLocalTenantAccessor();
        TenantInfo? capturedInNext = null;
        var middleware = CreateMiddleware(
            next: _ => { capturedInNext = accessor.CurrentTenant; return Task.CompletedTask; },
            accessor: accessor);
        var context = new DefaultHttpContext();
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header, LookupType = TenantLookupType.BySlug };

        resolver.SetupResolve(Result<string>.Success("acme"));
        store.SetupBySlug("acme", Result<TenantInfo>.Success(ActiveTenant));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings());

        capturedInNext.Should().Be(ActiveTenant);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotWriteBody_WhenResponseAlreadyStarted()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await context.Response.StartAsync(TestContext.Current.CancellationToken);
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header };

        resolver.SetupResolve(Result<string>.Failure(Error.NotFound("TenantKey", "Header")));

        var act = async () => await middleware.InvokeAsync(context, tenantCtx, store, [resolver],
            Settings(requireTenant: true));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_IncludesTraceId_InRequireTenantErrorBody()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace-42";
        var tenantCtx = new TenantContext();
        var store = new FakeTenantStore();
        var resolver = new FakeTenantResolver { ResolverType = TenantResolverType.Header };

        resolver.SetupResolve(Result<string>.Failure(Error.NotFound("TenantKey", "Header")));

        await middleware.InvokeAsync(context, tenantCtx, store, [resolver], Settings(requireTenant: true));

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("test-trace-42");
    }
}

// ── Test helpers ─────────────────────────────────────────────────────────────

file sealed class FakeOptionsSnapshot(TenancySettings settings) : IOptionsSnapshot<TenancySettings>
{
    public TenancySettings Value => settings;
    public TenancySettings Get(string? name) => settings;
}

file sealed class FakeTenantStore : ITenantStore
{
    private readonly Dictionary<Guid, Result<TenantInfo>> _byId = new();
    private readonly Dictionary<string, Result<TenantInfo>> _bySlug = new(StringComparer.OrdinalIgnoreCase);
    private string? _slugThrowKey;
    private Exception? _slugThrowException;

    public int GetByIdCallCount { get; private set; }
    public int GetBySlugCallCount { get; private set; }

    public void SetupById(Guid id, Result<TenantInfo> result) => _byId[id] = result;
    public void SetupBySlug(string slug, Result<TenantInfo> result) => _bySlug[slug] = result;

    public void SetupBySlugThrows(string slug, Exception ex)
    {
        _slugThrowKey = slug;
        _slugThrowException = ex;
    }

    public Task<Result<TenantInfo>> GetByIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        GetByIdCallCount++;
        return Task.FromResult(_byId.TryGetValue(tenantId, out var r) ? r : Result<TenantInfo>.Failure(Error.NotFound("Tenant", tenantId)));
    }

    public Task<Result<TenantInfo>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        GetBySlugCallCount++;
        if (_slugThrowException is not null && string.Equals(slug, _slugThrowKey, StringComparison.OrdinalIgnoreCase))
            throw _slugThrowException;
        return Task.FromResult(_bySlug.TryGetValue(slug, out var r) ? r : Result<TenantInfo>.Failure(Error.NotFound("Tenant", slug)));
    }
}

file sealed class FakeTenantResolver : ITenantResolver
{
    private Result<string>? _resolveResult;

    public int ResolveCallCount { get; private set; }
    public TenantResolverType ResolverType { get; init; } = TenantResolverType.Header;
    public TenantLookupType LookupType { get; init; } = TenantLookupType.BySlug;

    public void SetupResolve(Result<string> result) => _resolveResult = result;

    public Task<Result<string>> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        ResolveCallCount++;
        return Task.FromResult(_resolveResult ?? Result<string>.Failure(Error.NotFound("TenantKey", "Resolver")));
    }
}
