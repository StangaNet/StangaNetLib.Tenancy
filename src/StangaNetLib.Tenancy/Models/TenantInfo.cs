namespace StangaNetLib.Tenancy.Models;

/// <summary>Immutable snapshot of a resolved tenant's data.</summary>
/// <param name="TenantId">Unique identifier of the tenant.</param>
/// <param name="Name">Display name of the tenant.</param>
/// <param name="Slug">URL-safe identifier used for host- and path-based resolution.</param>
/// <param name="IsActive">Whether the tenant is currently active and allowed to access the system.</param>
/// <param name="ConnectionString">Optional per-tenant database connection string for data isolation.</param>
/// <param name="Metadata">Optional bag of arbitrary key/value pairs for application-specific tenant data.</param>
public sealed record TenantInfo(
    Guid TenantId,
    string Name,
    string Slug,
    bool IsActive,
    string? ConnectionString = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
