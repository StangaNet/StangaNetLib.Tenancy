# StangaNetLib.Tenancy

[![.NET CI](https://github.com/StangaNet/StangaNetLib.Tenancy/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.Tenancy/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4)

ASP.NET Core multi-tenancy: resolve the current tenant from HTTP header, host, path, query string, or claims; populate scoped context and ambient accessor; optional memory-cache store decorator. Uses `Result` / `Guard` from [`StangaNetLib.Core`](https://github.com/StangaNet/StangaNetLib.Core).

## Installation

The package is hosted on **GitHub Packages** (pulls Core from the same feed).

```xml
<!-- NuGet.config — source key must match your packageSourceCredentials -->
<configuration>
  <packageSources>
    <add key="StangaNet" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- .csproj -->
<PackageReference Include="StangaNetLib.Tenancy" Version="1.0.0" />
```

See [CHANGELOG.md](./CHANGELOG.md) for release history.

## Quick Start

```csharp
builder.Services.AddStangaNetLibTenancy(builder.Configuration);
builder.Services.AddScoped<ITenantStore, MyEfCoreTenantStore>();
// or: builder.Services.AddStangaNetLibTenancyCache<MyEfCoreTenantStore>();

app.UseAuthentication(); // before tenancy if Claim resolver is used
app.UseStangaNetLibTenancy();
```

Optional `appsettings.json`:

```json
{
  "TenancySettings": {
    "ResolverOrder": ["Header"],
    "HeaderName": "X-Tenant-Id",
    "RequireTenant": false
  }
}
```

## Core Components

| Namespace | Components | Purpose |
|---|---|---|
| `StangaNetLib.Tenancy.Abstractions` | `ITenantContext`, `ITenantStore`, `ITenantResolver`, `ITenantAccessor` | Contracts |
| `StangaNetLib.Tenancy.Middleware` | `TenantResolutionMiddleware` | Pipeline |
| `StangaNetLib.Tenancy.Settings` | `TenancySettings` | Configuration |
| `StangaNetLib.Tenancy.Extensions` | `AddStangaNetLibTenancy`, `UseStangaNetLibTenancy` | DI / pipeline |

## Project Structure

```
StangaNetLib.Tenancy/
├── assets/
│   └── package-icon.png
├── build/
│   └── StangaNetLib.Tenancy.props
├── src/
│   └── StangaNetLib.Tenancy/
└── tests/
    └── StangaNetLib.Tenancy.Tests/
```

## License

Apache-2.0 — see [LICENSE](./LICENSE).
