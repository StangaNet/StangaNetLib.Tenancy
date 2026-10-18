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
/// Extracts the tenant key from the subdomain of the <c>Host</c> header.
/// For example, <c>acme.example.com</c> yields <c>acme</c>.
/// Returns a failed <see cref="StangaNetLib.Core.Common.Result{T}"/> when the host does not contain enough
/// dot-separated parts (controlled by <see cref="TenancySettings.HostMinParts"/>, default 3 — so
/// <c>localhost</c> and bare two-part domains are rejected unless <c>HostMinParts</c> is lowered).
/// Whether the subdomain is treated as an ID or slug is controlled by <see cref="TenancySettings.HostLookupType"/>.
/// </summary>
internal sealed class HostTenantResolver : ITenantResolver
{
    private readonly IOptionsMonitor<TenancySettings> _options;

    public HostTenantResolver(IOptionsMonitor<TenancySettings> options)
    {
        Guard.Against.Null(options, nameof(options));
        _options = options;
    }

    /// <inheritdoc/>
    public TenantResolverType ResolverType => TenantResolverType.Host;

    /// <inheritdoc/>
    public TenantLookupType LookupType => _options.CurrentValue.HostLookupType;

    /// <inheritdoc/>
    public Task<Result<string>> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(context, nameof(context));

        var settings = _options.CurrentValue;
        var host = context.Request.Host.Host;
        var parts = host.Split('.');

        if (parts.Length < settings.HostMinParts)
            return Task.FromResult<Result<string>>(TenancyErrors.KeyNotFound(ResolverType));

        var subdomain = parts[0];
        return string.IsNullOrWhiteSpace(subdomain)
            ? Task.FromResult<Result<string>>(TenancyErrors.KeyNotFound(ResolverType))
            : Task.FromResult<Result<string>>(Result<string>.Success(subdomain));
    }
}
