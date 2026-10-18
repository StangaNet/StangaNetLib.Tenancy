using StangaNetLib.Core.Validators;

namespace StangaNetLib.Tenancy.Settings;

/// <summary>
/// Startup validator for <see cref="TenancySettings"/> that enforces cross-field constraints not expressible with data annotations:
/// no duplicate entries in <see cref="TenancySettings.ResolverOrder"/>, all <see cref="TenancySettings.ExcludedPaths"/> starting with <c>/</c>,
/// and <see cref="TenancySettings.CacheTtl"/> being positive when specified.
/// </summary>
internal sealed class TenancySettingsValidator : SettingsValidatorBase<TenancySettings>
{
    /// <inheritdoc/>
    protected override void ValidateCore(TenancySettings options, List<string> errors)
    {
        var duplicates = options.ResolverOrder
            .GroupBy(r => r)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.ToString())
            .ToList();

        if (duplicates.Count > 0)
            errors.Add($"ResolverOrder contains duplicate resolver types: {string.Join(", ", duplicates)}.");

        foreach (var path in options.ExcludedPaths)
        {
            if (!path.StartsWith('/'))
                errors.Add($"ExcludedPaths entry '{path}' does not start with '/'. All excluded paths must start with '/'.");
        }

        if (options.CacheTtl.HasValue && options.CacheTtl.Value <= TimeSpan.Zero)
            errors.Add($"CacheTtl must be a positive duration, but was '{options.CacheTtl.Value}'.");
    }
}
