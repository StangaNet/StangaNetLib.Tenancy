# StangaNetLib.Tenancy — Copilot instructions

- ASP.NET Core multi-tenancy: resolvers (header/host/path/query/claim), store abstraction, middleware, ambient accessor.
- Prefer `Result` / `Result<T>` and `Guard` from `StangaNetLib.Core` — do not reintroduce local Result/Guard types.
- Keep public APIs documented (`///`) and options-driven via `TenancySettings`.
- DI: `AddStangaNetLibTenancy` / `AddStangaNetLibTenancyCache`; pipeline: `UseStangaNetLibTenancy`.
- Match existing naming, file-scoped namespaces, and EditorConfig / Roslyn conventions.
- Tests use xUnit v3 + FluentAssertions; cover both `net8.0` and `net9.0` when changing shared behavior.
