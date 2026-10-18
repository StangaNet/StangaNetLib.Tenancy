using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Guards;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Implementations;
using StangaNetLib.Tenancy.Middleware;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Extensions;

/// <summary>Extension methods for registering StangaNetLib.Tenancy services into an <see cref="IServiceCollection"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the tenant resolution pipeline: settings, all built-in <see cref="ITenantResolver"/> implementations,
    /// the scoped <see cref="ITenantContext"/>, and the singleton <see cref="ITenantAccessor"/>.
    /// </summary>
    /// <remarks>
    /// The consuming project must register its own <see cref="ITenantStore"/> implementation.
    /// Call <c>app.UseStangaNetLibTenancy()</c> to activate <see cref="TenantResolutionMiddleware"/> in the pipeline.
    /// <para>
    /// Example <c>appsettings.json</c>:
    /// <code>
    /// {
    ///   "TenancySettings": {
    ///     "ResolverOrder": ["Header"],
    ///     "HeaderName": "X-Tenant-Id",
    ///     "HeaderLookupType": "ById",
    ///     "RequireTenant": false
    ///   }
    /// }
    /// </code>
    /// Example <c>Program.cs</c>:
    /// <code>
    /// builder.Services.AddStangaNetLibTenancy(builder.Configuration);
    /// builder.Services.AddScoped&lt;ITenantStore, MyEfCoreTenantStore&gt;();
    ///
    /// app.UseStangaNetLibTenancy();
    /// </code>
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration used to bind <see cref="TenancySettings"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibTenancy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Guard.Against.Null(services, nameof(services));
        Guard.Against.Null(configuration, nameof(configuration));

        services.AddOptions<TenancySettings>()
            .Bind(configuration.GetSection(TenancySettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return AddTenancyCore(services);
    }

    /// <summary>
    /// Registers the tenant resolution pipeline using an inline configuration delegate.
    /// Registers the same services as <see cref="AddStangaNetLibTenancy(IServiceCollection,IConfiguration)"/>;
    /// useful in tests and minimal setups where <c>appsettings.json</c> is not available.
    /// </summary>
    /// <remarks>
    /// The consuming project must still register its own <see cref="ITenantStore"/> implementation.
    /// Call <c>app.UseStangaNetLibTenancy()</c> to activate <see cref="TenantResolutionMiddleware"/> in the pipeline.
    /// <para>
    /// Example:
    /// <code>
    /// builder.Services.AddStangaNetLibTenancy(opt =>
    /// {
    ///     opt.ResolverOrder = [TenantResolverType.Header];
    ///     opt.RequireTenant = true;
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Delegate that configures <see cref="TenancySettings"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibTenancy(
        this IServiceCollection services,
        Action<TenancySettings> configure)
    {
        Guard.Against.Null(services, nameof(services));
        Guard.Against.Null(configure, nameof(configure));

        services.AddOptions<TenancySettings>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return AddTenancyCore(services);
    }

    /// <summary>
    /// Adds <see cref="TenantResolutionMiddleware"/> to the request pipeline.
    /// Must be called after <c>UseAuthentication()</c> when the <c>Claim</c> resolver is in use.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseStangaNetLibTenancy(this IApplicationBuilder app)
    {
        Guard.Against.Null(app, nameof(app));
        return app.UseMiddleware<TenantResolutionMiddleware>();
    }

    /// <summary>
    /// Registers a <see cref="CachingTenantStoreDecorator"/> as both <see cref="ITenantStore"/> and <see cref="ICachingTenantStore"/>,
    /// wrapping <typeparamref name="TStore"/> with an in-memory cache layer.
    /// Call this instead of manually registering <c>ITenantStore</c> when caching is desired.
    /// Injecting <see cref="ICachingTenantStore"/> gives access to explicit cache-invalidation methods.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="AddStangaNetLibTenancy(IServiceCollection,IConfiguration)"/> or the <c>Action</c> overload to be called first.
    /// <para>
    /// Example:
    /// <code>
    /// builder.Services.AddStangaNetLibTenancy(builder.Configuration);
    /// builder.Services.AddStangaNetLibTenancyCache&lt;MyEfCoreTenantStore&gt;(TimeSpan.FromMinutes(10));
    /// </code>
    /// </para>
    /// </remarks>
    /// <typeparam name="TStore">The concrete <see cref="ITenantStore"/> implementation to wrap.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="cacheTtl">
    /// How long successful lookups are cached. Priority order: this parameter → <see cref="TenancySettings.CacheTtl"/> → 5-minute built-in default.
    /// Must be positive when specified.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibTenancyCache<TStore>(
        this IServiceCollection services,
        TimeSpan? cacheTtl = null)
        where TStore : class, ITenantStore
    {
        Guard.Against.Null(services, nameof(services));

        services.AddMemoryCache();
        services.AddScoped<TStore>();
        services.AddScoped<CachingTenantStoreDecorator>(sp =>
        {
            var effectiveTtl = cacheTtl
                ?? sp.GetRequiredService<IOptions<TenancySettings>>().Value.CacheTtl;
            return new CachingTenantStoreDecorator(
                sp.GetRequiredService<TStore>(),
                sp.GetRequiredService<IMemoryCache>(),
                effectiveTtl);
        });
        services.AddScoped<ITenantStore>(sp => sp.GetRequiredService<CachingTenantStoreDecorator>());
        services.AddScoped<ICachingTenantStore>(sp => sp.GetRequiredService<CachingTenantStoreDecorator>());

        return services;
    }

    private static IServiceCollection AddTenancyCore(IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<TenancySettings>, TenancySettingsValidator>();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        services.AddSingleton<ITenantAccessor, AsyncLocalTenantAccessor>();

        services.AddSingleton<ITenantResolver, HeaderTenantResolver>();
        services.AddSingleton<ITenantResolver, HostTenantResolver>();
        services.AddSingleton<ITenantResolver, PathTenantResolver>();
        services.AddSingleton<ITenantResolver, ClaimTenantResolver>();
        services.AddSingleton<ITenantResolver, QueryStringTenantResolver>();

        return services;
    }
}
