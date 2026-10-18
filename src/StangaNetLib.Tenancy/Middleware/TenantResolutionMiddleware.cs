using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Middleware;

/// <summary>
/// Runs the configured resolver chain on each request and populates <see cref="ITenantContext"/> and
/// <see cref="ITenantAccessor"/> with the resolved tenant.
/// Register via <c>app.UseStangaNetLibTenancy()</c>.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly ITenantAccessor _tenantAccessor;

    /// <summary>Initialises the middleware.</summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger used to record resolution outcomes.</param>
    /// <param name="tenantAccessor">Ambient accessor also set on successful resolution, enabling non-HTTP access.</param>
    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        ITenantAccessor tenantAccessor)
    {
        _next = Guard.Against.Null(next, nameof(next));
        _logger = Guard.Against.Null(logger, nameof(logger));
        _tenantAccessor = Guard.Against.Null(tenantAccessor, nameof(tenantAccessor));
    }

    /// <summary>
    /// Runs the configured resolver chain; populates <see cref="ITenantContext"/> and <see cref="ITenantAccessor"/> on success
    /// and short-circuits with <see cref="TenancySettings.RequireTenantStatusCode"/> when
    /// <see cref="TenancySettings.RequireTenant"/> is <c>true</c> and no active tenant was found.
    /// Paths listed in <see cref="TenancySettings.ExcludedPaths"/> bypass resolution entirely.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="tenantContext">Scoped context that receives the resolved tenant.</param>
    /// <param name="tenantStore">Store used to look up tenants by ID or slug.</param>
    /// <param name="resolvers">All registered resolver implementations; filtered and ordered by <see cref="TenancySettings.ResolverOrder"/>.</param>
    /// <param name="settingsOptions">Bound <see cref="TenancySettings"/>.</param>
    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        ITenantStore tenantStore,
        IEnumerable<ITenantResolver> resolvers,
        IOptionsSnapshot<TenancySettings> settingsOptions)
    {
        var settings = settingsOptions.Value;

        if (settings.ExcludedPaths.Count > 0 &&
            settings.ExcludedPaths.Any(p => context.Request.Path.StartsWithSegments(new PathString(p), StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // O(1) lookup per resolver type; logs a warning for types configured but not registered.
        var resolverMap = new Dictionary<TenantResolverType, ITenantResolver>();
        foreach (var resolver in resolvers)
            resolverMap.TryAdd(resolver.ResolverType, resolver);

        foreach (var resolverType in settings.ResolverOrder)
        {
            if (!resolverMap.TryGetValue(resolverType, out var resolver))
            {
                _logger.LogWarning("ResolverOrder includes {ResolverType} but no ITenantResolver for that type is registered. Skipping.", resolverType);
                continue;
            }

            var resolveResult = await resolver.ResolveAsync(context, context.RequestAborted);
            if (resolveResult.IsFailure)
                continue;
            var key = resolveResult.Value!;

            Result<TenantInfo> result;
            try
            {
                if (resolver.LookupType == TenantLookupType.ById)
                {
                    if (!Guid.TryParse(key, out var tenantId))
                    {
                        _logger.LogWarning("Resolver {ResolverType} returned '{Key}' which is not a valid tenant ID GUID.", resolverType, key);
                        continue;
                    }
                    result = await tenantStore.GetByIdAsync(tenantId, context.RequestAborted);
                }
                else
                {
                    result = await tenantStore.GetBySlugAsync(key, context.RequestAborted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ITenantStore threw an exception during lookup via {ResolverType} for key '{Key}'. Skipping resolver.", resolverType, key);
                continue;
            }

            if (result.IsFailure)
            {
                _logger.LogDebug("Tenant lookup via {ResolverType} for key '{Key}' returned: {Error}.", resolverType, key, result.Error);
                continue;
            }

            if (!result.Value!.IsActive && !settings.AllowInactiveTenants)
            {
                _logger.LogWarning("Tenant '{TenantId}' (slug: {Slug}) is not active.", result.Value.TenantId, result.Value.Slug);
                continue;
            }

            tenantContext.SetTenant(result.Value);
            _tenantAccessor.CurrentTenant = result.Value;
            _logger.LogDebug(
                "Tenant '{TenantId}' (slug: {Slug}, name: {Name}) resolved via {ResolverType}.",
                result.Value.TenantId, result.Value.Slug, result.Value.Name, resolverType);
            break;
        }

        if (!tenantContext.IsResolved && settings.RequireTenant)
        {
            _logger.LogWarning("No active tenant resolved and RequireTenant is true. Returning {StatusCode}.", settings.RequireTenantStatusCode);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = settings.RequireTenantStatusCode;
                await context.Response.WriteAsJsonAsync(
                    new { error = "Tenant could not be resolved.", traceId = context.TraceIdentifier },
                    context.RequestAborted);
            }
            return;
        }

        await _next(context);
    }
}
