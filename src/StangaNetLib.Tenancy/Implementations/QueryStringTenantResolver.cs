using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Errors;
using StangaNetLib.Tenancy.Models;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Implementations;

/// <summary>
/// Extracts the tenant key from a configurable query-string parameter.
/// Useful for webhook callbacks and Swagger exploration where custom headers are not available.
/// </summary>
internal sealed class QueryStringTenantResolver : ITenantResolver
{
    private readonly IOptionsMonitor<TenancySettings> _options;

    public QueryStringTenantResolver(IOptionsMonitor<TenancySettings> options)
    {
        Guard.Against.Null(options, nameof(options));
        _options = options;
    }

    /// <inheritdoc/>
    public TenantResolverType ResolverType => TenantResolverType.QueryString;

    /// <inheritdoc/>
    public TenantLookupType LookupType => _options.CurrentValue.QueryStringLookupType;

    /// <inheritdoc/>
    public Task<Result<string>> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(context, nameof(context));

        var settings = _options.CurrentValue;
        var value = context.Request.Query[settings.QueryStringParameterName].FirstOrDefault();
        return string.IsNullOrWhiteSpace(value)
            ? Task.FromResult<Result<string>>(TenancyErrors.KeyNotFound(ResolverType))
            : Task.FromResult<Result<string>>(Result<string>.Success(value));
    }
}
