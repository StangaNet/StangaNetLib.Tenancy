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
/// Extracts the tenant key from a path segment at a configurable zero-based index.
/// For a path of <c>/acme/orders</c> and index 0, returns <c>acme</c>.
/// Whether the segment is treated as an ID or slug is controlled by <see cref="TenancySettings.PathLookupType"/>.
/// </summary>
internal sealed class PathTenantResolver : ITenantResolver
{
    private readonly IOptionsMonitor<TenancySettings> _options;

    public PathTenantResolver(IOptionsMonitor<TenancySettings> options)
    {
        Guard.Against.Null(options, nameof(options));
        _options = options;
    }

    /// <inheritdoc/>
    public TenantResolverType ResolverType => TenantResolverType.Path;

    /// <inheritdoc/>
    public TenantLookupType LookupType => _options.CurrentValue.PathLookupType;

    /// <inheritdoc/>
    public Task<Result<string>> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(context, nameof(context));

        var settings = _options.CurrentValue;
        // Path.Value produces "/seg0/seg1/..."; splitting and skipping the leading empty entry
        var segments = context.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments is null || segments.Length <= settings.PathSegmentIndex)
            return Task.FromResult<Result<string>>(TenancyErrors.KeyNotFound(ResolverType));

        var segment = segments[settings.PathSegmentIndex];
        return string.IsNullOrWhiteSpace(segment)
            ? Task.FromResult<Result<string>>(TenancyErrors.KeyNotFound(ResolverType))
            : Task.FromResult<Result<string>>(Result<string>.Success(segment));
    }
}
