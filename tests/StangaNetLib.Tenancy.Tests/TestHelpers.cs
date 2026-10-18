using Microsoft.Extensions.Options;
using StangaNetLib.Tenancy.Settings;

namespace StangaNetLib.Tenancy.Tests;

internal sealed class FakeOptionsMonitor(TenancySettings settings) : IOptionsMonitor<TenancySettings>
{
    public TenancySettings CurrentValue => settings;
    public TenancySettings Get(string? name) => settings;
    public IDisposable? OnChange(Action<TenancySettings, string?> listener) => null;
}
