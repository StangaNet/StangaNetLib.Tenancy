namespace StangaNetLib.Tenancy.Models;

/// <summary>Identifies which strategy is used to extract the tenant key from an HTTP request.</summary>
public enum TenantResolverType
{
    /// <summary>Reads a custom HTTP request header (e.g. <c>X-Tenant-Id</c>).</summary>
    Header,

    /// <summary>Extracts the subdomain from the <c>Host</c> header (e.g. <c>acme.example.com</c> → <c>acme</c>).</summary>
    Host,

    /// <summary>Reads a path segment at a configurable index (e.g. <c>/acme/orders</c> → <c>acme</c>).</summary>
    Path,

    /// <summary>Reads a claim from the authenticated user's identity.</summary>
    Claim,

    /// <summary>Reads a query-string parameter (e.g. <c>?tenantId=acme</c>). Useful for webhooks and Swagger.</summary>
    QueryString,
}
