using StangaNetLib.Core.Common;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Abstractions;

/// <summary>
/// Retrieves tenant data by ID or slug. Consuming projects provide the concrete implementation (e.g. EF Core).
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// Looks up a tenant by its unique identifier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the <see cref="TenantInfo"/> when found;
    /// a failed <see cref="Result{T}"/> with <see cref="Error.NotFound(string,object)"/> when the ID does not match any tenant.
    /// </returns>
    Task<Result<TenantInfo>> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a tenant by its URL-safe slug.
    /// </summary>
    /// <param name="slug">The slug to search for (case-insensitive match is recommended).</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the <see cref="TenantInfo"/> when found;
    /// a failed <see cref="Result{T}"/> with <see cref="Error.NotFound(string,object)"/> when the slug does not match any tenant.
    /// </returns>
    Task<Result<TenantInfo>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
