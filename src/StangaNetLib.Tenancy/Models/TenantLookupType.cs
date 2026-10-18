namespace StangaNetLib.Tenancy.Models;

/// <summary>Indicates whether the key extracted by an <see cref="Abstractions.ITenantResolver"/> is a tenant ID or a slug.</summary>
public enum TenantLookupType
{
    /// <summary>The extracted key is a <see cref="System.Guid"/> string and must be looked up via <see cref="Abstractions.ITenantStore.GetByIdAsync"/>.</summary>
    ById,

    /// <summary>The extracted key is a slug string and must be looked up via <see cref="Abstractions.ITenantStore.GetBySlugAsync"/>.</summary>
    BySlug,
}
