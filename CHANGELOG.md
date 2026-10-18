# Changelog

All notable changes to **StangaNetLib.Tenancy** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-10-18

### Added
- Tenant resolution middleware with ordered strategies: Header, Host, Path, QueryString, Claim.
- `ITenantStore` / `ICachingTenantStore` abstractions and memory-cache decorator.
- Scoped `ITenantContext` and ambient `ITenantAccessor` (AsyncLocal).
- `TenancySettings` with data-annotation validation and custom options validator.
- DI (`AddStangaNetLibTenancy`, `AddStangaNetLibTenancyCache`) and pipeline (`UseStangaNetLibTenancy`).
- Dual targeting `net8.0` / `net9.0`, XML docs, symbols (`snupkg`), package icon, and MSBuild consumer metadata stamp.
- Depends on `StangaNetLib.Core` 1.0.0; Microsoft.Extensions.* pinned to 8.x.
- CI via shared StangaNet reusable workflow (push / PR / manual; publish outside PRs).

[Unreleased]: https://github.com/StangaNet/StangaNetLib.Tenancy/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/StangaNet/StangaNetLib.Tenancy/releases/tag/v1.0.0
