using System.Text.Json;
using StangaNetLib.Core.Guards;
using StangaNetLib.Tenancy.Models;

namespace StangaNetLib.Tenancy.Extensions;

/// <summary>Extension methods for <see cref="TenantInfo"/> metadata access.</summary>
public static class TenantInfoExtensions
{
    /// <summary>
    /// Returns the raw string value stored in <see cref="TenantInfo.Metadata"/> for <paramref name="key"/>,
    /// or <c>null</c> when the key is absent or <see cref="TenantInfo.Metadata"/> is <c>null</c>.
    /// </summary>
    /// <param name="tenant">The tenant whose metadata to read.</param>
    /// <param name="key">The metadata key to look up (case-sensitive).</param>
    /// <returns>The raw string value, or <c>null</c> when not found.</returns>
    /// <example>
    /// <code>
    /// var plan = tenant.GetMetadata("plan");          // "starter" | null
    /// </code>
    /// </example>
    public static string? GetMetadata(this TenantInfo tenant, string key)
    {
        Guard.Against.Null(tenant, nameof(tenant));
        Guard.Against.Null(key, nameof(key));

        return tenant.Metadata?.TryGetValue(key, out var value) == true ? value : null;
    }

    /// <summary>
    /// Deserializes the metadata value for <paramref name="key"/> as <typeparamref name="T"/> using
    /// <see cref="JsonSerializer"/>. Returns <paramref name="defaultValue"/> when the key is absent,
    /// the stored value is <c>null</c> or empty, or <see cref="TenantInfo.Metadata"/> is <c>null</c>.
    /// When <typeparamref name="T"/> is <c>string</c>, the raw value is returned without JSON parsing.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize into.</typeparam>
    /// <param name="tenant">The tenant whose metadata to read.</param>
    /// <param name="key">The metadata key to look up (case-sensitive).</param>
    /// <param name="defaultValue">Value returned when <paramref name="key"/> is not present. Defaults to <c>default(T)</c>.</param>
    /// <returns>The deserialized value, or <paramref name="defaultValue"/> when absent.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the stored value exists but cannot be deserialized as <typeparamref name="T"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var maxUsers = tenant.GetMetadata&lt;int&gt;("maxUsers", defaultValue: 5);
    /// var plan     = tenant.GetMetadata&lt;string&gt;("plan", defaultValue: "free");
    /// var features = tenant.GetMetadata&lt;string[]&gt;("features");
    /// </code>
    /// </example>
    public static T? GetMetadata<T>(this TenantInfo tenant, string key, T? defaultValue = default)
    {
        Guard.Against.Null(tenant, nameof(tenant));
        Guard.Against.Null(key, nameof(key));

        if (tenant.Metadata?.TryGetValue(key, out var raw) != true || string.IsNullOrEmpty(raw))
            return defaultValue;

        if (typeof(T) == typeof(string))
            return (T)(object)raw;

        return JsonSerializer.Deserialize<T>(raw);
    }
}
