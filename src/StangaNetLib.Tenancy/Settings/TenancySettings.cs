using System.ComponentModel.DataAnnotations;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Settings;

/// <summary>Configuration for StangaNetLib.Tenancy. Bind from <c>TenancySettings</c> in appsettings.</summary>
public sealed class TenancySettings
{
    /// <summary>The configuration section key used to bind this class from <c>appsettings.json</c>.</summary>
    public const string SectionName = "TenancySettings";

    /// <summary>
    /// Ordered list of resolver strategies the middleware will attempt for each request.
    /// The first strategy that returns a non-null key wins. Default: <c>[Header]</c>.
    /// Must contain at least one entry.
    /// </summary>
    [Required, MinLength(1)]
    public IList<TenantResolverType> ResolverOrder { get; set; } = [TenantResolverType.Header];

    /// <summary>Name of the HTTP request header read by <c>HeaderTenantResolver</c>. Default: <c>X-Tenant-Id</c>.</summary>
    [Required, MinLength(1)]
    public string HeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>Whether the header value is a tenant ID (<c>ById</c>) or slug (<c>BySlug</c>). Default: <c>ById</c>.</summary>
    public TenantLookupType HeaderLookupType { get; set; } = TenantLookupType.ById;

    /// <summary>JWT claim type read by <c>ClaimTenantResolver</c>. Default: <c>tenant_id</c>.</summary>
    [Required, MinLength(1)]
    public string ClaimType { get; set; } = "tenant_id";

    /// <summary>Whether the claim value is a tenant ID (<c>ById</c>) or slug (<c>BySlug</c>). Default: <c>ById</c>.</summary>
    public TenantLookupType ClaimLookupType { get; set; } = TenantLookupType.ById;

    /// <summary>
    /// Zero-based index of the path segment extracted by <c>PathTenantResolver</c>.
    /// For a path of <c>/acme/orders</c>, index 0 yields <c>acme</c>. Default: <c>0</c>.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int PathSegmentIndex { get; set; } = 0;

    /// <summary>Query-string parameter name read by <c>QueryStringTenantResolver</c>. Default: <c>tenantId</c>.</summary>
    [Required, MinLength(1)]
    public string QueryStringParameterName { get; set; } = "tenantId";

    /// <summary>Whether the query-string value is a tenant ID (<c>ById</c>) or slug (<c>BySlug</c>). Default: <c>ById</c>.</summary>
    public TenantLookupType QueryStringLookupType { get; set; } = TenantLookupType.ById;

    /// <summary>Whether the path segment extracted by <c>PathTenantResolver</c> is treated as a tenant ID (<c>ById</c>) or slug (<c>BySlug</c>). Default: <c>BySlug</c>.</summary>
    public TenantLookupType PathLookupType { get; set; } = TenantLookupType.BySlug;

    /// <summary>Whether the subdomain extracted by <c>HostTenantResolver</c> is a tenant ID (<c>ById</c>) or slug (<c>BySlug</c>). Default: <c>BySlug</c>.</summary>
    public TenantLookupType HostLookupType { get; set; } = TenantLookupType.BySlug;

    /// <summary>
    /// Minimum number of dot-separated parts required in the host for subdomain extraction to proceed.
    /// Default: <c>3</c> (e.g. <c>acme.example.com</c>).
    /// Set to <c>2</c> to support two-part hosts such as <c>acme.localdev</c>.
    /// Must be at least <c>2</c>.
    /// </summary>
    [Range(2, int.MaxValue)]
    public int HostMinParts { get; set; } = 3;

    /// <summary>
    /// When <c>true</c>, requests that cannot be associated with an active tenant receive a response with <see cref="RequireTenantStatusCode"/>.
    /// When <c>false</c> (default), unresolved-tenant requests pass through to the next middleware.
    /// </summary>
    public bool RequireTenant { get; set; } = false;

    /// <summary>
    /// HTTP status code written when <see cref="RequireTenant"/> is <c>true</c> and no active tenant is resolved.
    /// Common values: <c>401</c> Unauthorized (default) or <c>403</c> Forbidden.
    /// </summary>
    [Range(400, 599)]
    public int RequireTenantStatusCode { get; set; } = 401;

    /// <summary>
    /// When <c>true</c>, tenants with <see cref="TenantInfo.IsActive"/> set to <c>false</c> are still resolved.
    /// Useful for admin or suspension-management endpoints. Default: <c>false</c>.
    /// </summary>
    public bool AllowInactiveTenants { get; set; } = false;

    /// <summary>
    /// Request path prefixes that bypass tenant resolution entirely.
    /// Useful for health checks, metrics, and Swagger endpoints.
    /// Each entry must start with <c>/</c>. Matching is case-insensitive. Default: empty.
    /// </summary>
    public IList<string> ExcludedPaths { get; set; } = [];

    /// <summary>
    /// Cache TTL used by <c>AddStangaNetLibTenancyCache</c> when no explicit TTL is supplied to the extension method.
    /// When <c>null</c>, the decorator falls back to its built-in default of 5 minutes.
    /// When set, the value must be positive (validated at startup).
    /// </summary>
    public TimeSpan? CacheTtl { get; set; } = null;
}
