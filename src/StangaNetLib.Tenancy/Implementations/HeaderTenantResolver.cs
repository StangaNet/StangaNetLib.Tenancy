using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Errors;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Implementations;

/// <summary>Extracts the tenant key from a configurable HTTP request header.</summary>
internal sealed class HeaderTenantResolver : ITenantResolver
{
    private readonly IOptionsMonitor<TenancySettings> _options;

    public HeaderTenantResolver(IOptionsMonitor<TenancySettings> options)
    {
        Guard.Against.Null(options, nameof(options));
        _options = options;
    }

    /// <inheritdoc/>
    public TenantResolverType ResolverType => TenantResolverType.Header;

    /// <inheritdoc/>
    public TenantLookupType LookupType => _options.CurrentValue.HeaderLookupType;

    /// <inheritdoc/>
    public Task<Result<string>> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(context, nameof(context));

        var settings = _options.CurrentValue;
        var value = context.Request.Headers[settings.HeaderName].FirstOrDefault();
        return string.IsNullOrWhiteSpace(value)
            ? Task.FromResult<Result<string>>(TenancyErrors.KeyNotFound(ResolverType))
            : Task.FromResult<Result<string>>(Result<string>.Success(value));
    }
}
