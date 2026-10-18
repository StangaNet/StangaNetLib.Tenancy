using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Tenancy.Abstractions;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Extensions;

/// <summary>Extension methods for <see cref="ITenantContext"/>.</summary>
public static class TenantContextExtensions
{
    /// <summary>
    /// Returns the resolved tenant or a failure if the current request has no active tenant.
    /// Use this in place of the <c>if (!IsResolved) return Unauthorized()</c> pattern.
    /// </summary>
    /// <param name="context">The tenant context for the current request.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the <see cref="TenantInfo"/> when a tenant is resolved;
    /// a failed <see cref="Result{T}"/> with <see cref="ErrorType.Unauthorized"/> otherwise.
    /// </returns>
    /// <example>
    /// <code>
    /// public IActionResult Get()
    /// {
    ///     var tenantResult = _tenantContext.GetRequiredTenant();
    ///     if (tenantResult.IsFailure)
    ///         return Unauthorized(tenantResult.Error.Description);
    ///
    ///     var tenantId = tenantResult.Value!.TenantId;
    ///     // ...
    /// }
    /// </code>
    /// </example>
    public static Result<TenantInfo> GetRequiredTenant(this ITenantContext context)
    {
        Guard.Against.Null(context, nameof(context));

        return context.IsResolved
            ? Result<TenantInfo>.Success(context.CurrentTenant!)
            : Result<TenantInfo>.Failure(Error.Unauthorized("No tenant has been resolved for the current request."));
    }

    /// <summary>
    /// Returns the resolved tenant's ID, or <c>null</c> when no tenant is resolved.
    /// Use <see cref="RequireTenantId"/> when a missing tenant should be treated as an error.
    /// </summary>
    /// <param name="context">The tenant context for the current request.</param>
    /// <returns>The tenant ID when resolved; <c>null</c> otherwise.</returns>
    public static Guid? GetTenantId(this ITenantContext context)
    {
        Guard.Against.Null(context, nameof(context));
        return context.CurrentTenant?.TenantId;
    }

    /// <summary>
    /// Returns the resolved tenant's ID or a failure when no tenant is resolved.
    /// Equivalent to calling <see cref="GetRequiredTenant"/> and extracting <see cref="TenantInfo.TenantId"/>.
    /// </summary>
    /// <param name="context">The tenant context for the current request.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the tenant <see cref="Guid"/> when resolved;
    /// a failed <see cref="Result{T}"/> with <see cref="ErrorType.Unauthorized"/> otherwise.
    /// </returns>
    /// <example>
    /// <code>
    /// public IActionResult Get()
    /// {
    ///     var idResult = _tenantContext.RequireTenantId();
    ///     if (idResult.IsFailure)
    ///         return Unauthorized(idResult.Error.Description);
    ///
    ///     var tenantId = idResult.Value;
    ///     // query data scoped to tenantId ...
    /// }
    /// </code>
    /// </example>
    public static Result<Guid> RequireTenantId(this ITenantContext context)
    {
        Guard.Against.Null(context, nameof(context));

        return context.IsResolved
            ? Result<Guid>.Success(context.CurrentTenant!.TenantId)
            : Result<Guid>.Failure(Error.Unauthorized("No tenant has been resolved for the current request."));
    }

    /// <summary>
    /// Returns the resolved tenant's per-tenant connection string, or <c>null</c> when no tenant is resolved
    /// or the tenant has no connection string configured.
    /// Use <see cref="RequireConnectionString"/> when a missing connection string should be treated as an error.
    /// </summary>
    /// <param name="context">The tenant context for the current request.</param>
    /// <returns>The connection string when present; <c>null</c> otherwise.</returns>
    public static string? GetConnectionString(this ITenantContext context)
    {
        Guard.Against.Null(context, nameof(context));
        return context.CurrentTenant?.ConnectionString;
    }

    /// <summary>
    /// Returns the resolved tenant's per-tenant connection string, or a failure when not available.
    /// Returns <see cref="ErrorType.Unauthorized"/> when no tenant is resolved and
    /// <see cref="ErrorType.NotFound"/> when the tenant has no connection string configured.
    /// </summary>
    /// <param name="context">The tenant context for the current request.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the non-empty connection string;
    /// a failed <see cref="Result{T}"/> with <see cref="ErrorType.Unauthorized"/> or <see cref="ErrorType.NotFound"/> otherwise.
    /// </returns>
    /// <example>
    /// <code>
    /// public async Task&lt;IActionResult&gt; GetAsync()
    /// {
    ///     var csResult = _tenantContext.RequireConnectionString();
    ///     if (csResult.IsFailure)
    ///         return StatusCode(csResult.Error.Type == ErrorType.Unauthorized ? 401 : 404);
    ///
    ///     using var conn = new SqlConnection(csResult.Value);
    ///     // ...
    /// }
    /// </code>
    /// </example>
    public static Result<string> RequireConnectionString(this ITenantContext context)
    {
        Guard.Against.Null(context, nameof(context));

        if (!context.IsResolved)
            return Result<string>.Failure(Error.Unauthorized("No tenant has been resolved for the current request."));

        if (string.IsNullOrEmpty(context.CurrentTenant!.ConnectionString))
            return Result<string>.Failure(Error.NotFound("Tenant.ConnectionString", context.CurrentTenant.TenantId));

        return Result<string>.Success(context.CurrentTenant.ConnectionString!);
    }

    /// <summary>
    /// Returns the raw string value stored in the resolved tenant's <see cref="TenantInfo.Metadata"/> for <paramref name="key"/>,
    /// or <c>null</c> when no tenant is resolved, the key is absent, or the metadata bag is <c>null</c>.
    /// </summary>
    /// <param name="context">The tenant context for the current request.</param>
    /// <param name="key">The metadata key to look up (case-sensitive).</param>
    /// <returns>The raw string value, or <c>null</c> when not found.</returns>
    public static string? GetMetadata(this ITenantContext context, string key)
    {
        Guard.Against.Null(context, nameof(context));
        Guard.Against.Null(key, nameof(key));

        return context.CurrentTenant?.GetMetadata(key);
    }

    /// <summary>
    /// Deserializes the metadata value for <paramref name="key"/> from the resolved tenant as <typeparamref name="T"/>.
    /// Returns <paramref name="defaultValue"/> when no tenant is resolved, the key is absent, or the metadata bag is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize into.</typeparam>
    /// <param name="context">The tenant context for the current request.</param>
    /// <param name="key">The metadata key to look up (case-sensitive).</param>
    /// <param name="defaultValue">Value returned when the key is absent or no tenant is resolved. Defaults to <c>default(T)</c>.</param>
    /// <returns>The deserialized value, or <paramref name="defaultValue"/> when absent.</returns>
    /// <exception cref="System.Text.Json.JsonException">
    /// Thrown when the stored value is non-empty and cannot be deserialized as <typeparamref name="T"/>.
    /// </exception>
    public static T? GetMetadata<T>(this ITenantContext context, string key, T? defaultValue = default)
    {
        Guard.Against.Null(context, nameof(context));
        Guard.Against.Null(key, nameof(key));

        return context.CurrentTenant is null
            ? defaultValue
            : context.CurrentTenant.GetMetadata(key, defaultValue);
    }
}
