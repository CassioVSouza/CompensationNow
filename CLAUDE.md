# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

Started as a default **Blazor Web App** (`dotnet new blazor`) targeting **.NET 10**, using Interactive Server render mode. This is a deliberately **monolithic** project — a single `CompenseAgora` project, not a multi-project Clean Architecture split — with internal layering by folder instead of by assembly (see below). The UI is still just the default template pages (Home/Counter/Weather, layout, nav menu); the real work so far is the data model: EF Core is fully wired up against SQL Server, with entities, Fluent API configurations, `DbContext`, and an initial migration in place. `Application`-style CQRS handlers have not been added yet.

Solution file: `CompenseAgora.slnx` at the repo root (new XML solution format), referencing the single `CompenseAgora/CompenseAgora.csproj`.

## Commands

Run from `CompenseAgora/` (where `CompenseAgora.csproj` lives), or from the repo root using `--project CompenseAgora`.

- Build: `dotnet build`
- Run (dev server with hot reload): `dotnet watch run`
- Run without watch: `dotnet run`
- Restore packages: `dotnet restore`
- Add a migration: `dotnet ef migrations add <Name> -o Data/Migrations`
- Apply migrations to the configured database: `dotnet ef database update`

There are no test projects, linters, or CI configuration in the repo yet. The `dotnet-ef` global tool installed on this machine is v9.0.2, older than the project's EF Core v10.0.11 — it still works (with a warning) but consider `dotnet tool update -g dotnet-ef` if migration generation ever behaves unexpectedly.

Default dev URLs (from `Properties/launchSettings.json`): `https://localhost:7070` and `http://localhost:5091`.

## Architecture

- **Entry point**: `Program.cs` configures the standard ASP.NET Core minimal-hosting pipeline for a Blazor Web App:
  - `AddRazorComponents().AddInteractiveServerComponents()` — registers Razor Components with Interactive Server render mode (SignalR-based, server-side interactivity; no WASM).
  - `AddDbContext<CompenseAgoraDbContext>(...)` — registers EF Core against SQL Server using the `CompenseAgoraDb` connection string.
  - `MapRazorComponents<App>().AddInteractiveServerRenderMode()` — the root component is `Components/App.razor`, rendered with server interactivity applied app-wide.
  - Standard middleware: exception handler + HSTS in non-Development, status code re-execution to `/not-found`, HTTPS redirection, antiforgery, static assets.
- **Component structure** (`Components/`):
  - `App.razor` — the root HTML document component (head, root render mode).
  - `Routes.razor` — the `<Router>` that maps routes to page components.
  - `Layout/MainLayout.razor` + `NavMenu.razor` — shared page chrome/sidebar nav.
  - `Layout/ReconnectModal.razor(.css/.js)` — UI shown when the SignalR circuit drops (relevant to Interactive Server mode).
  - `Pages/` — routable pages (`Home`, `Counter`, `Weather`, `Error`, `NotFound`), each a `.razor` file with `@page` route directives.
  - `_Imports.razor` — shared `@using` directives for all components.
- **Static assets**: `wwwroot/` (app.css, favicon, Bootstrap under `wwwroot/lib/bootstrap`). Served via `app.MapStaticAssets()`.
- **Config**: `appsettings.json` / `appsettings.Development.json` — standard ASP.NET Core configuration. The `CompenseAgoraDb` connection string lives in `appsettings.Development.json` pointing at LocalDB (`(localdb)\mssqllocaldb`); `appsettings.json` has an empty placeholder — a real deployment should supply it via environment variable or user secrets, not a hardcoded value.

Since this app uses Interactive Server render mode exclusively (no WebAssembly/Auto), all UI state and event handling execute server-side over a persistent SignalR circuit — keep this in mind when adding interactivity (no client-side-only components, and connection drops are handled by `ReconnectModal`).

## Architecture and standards

The project is committed to the following standards; apply them when adding new code.

### Backend: Clean Architecture (by folder) + CQRS

Because the project is monolithic, Clean Architecture is expressed as folders within the one project rather than as separate assemblies. Keep the same dependency direction rules a multi-project split would enforce, just without the compiler enforcing them for you:

- `Entities/` — domain entities, plain POCOs, **no EF Core attributes**. All persistence mapping (keys, relationships, delete behavior, precision, max length) lives in `Data/Configurations/`, not on the entities themselves. Primary keys are named `Codigo` (not `Id`), matching the domain's own vocabulary.
- `Data/` — the persistence layer: `CompenseAgoraDbContext`, `Data/Configurations/*Configuration.cs` (one `IEntityTypeConfiguration<T>` per entity, applied via `ApplyConfigurationsFromAssembly`), and `Data/Migrations/`.
- Use cases (not yet added) should live under something like `Features/` or `Application/`, structured as CQRS **commands** (writes) and **queries** (reads), each with its own handler — one responsibility per handler, no shared "service" god-classes. These depend on `Entities`/`Data`, never the reverse.
- Razor components should dispatch commands/queries rather than querying `CompenseAgoraDbContext` directly from the UI layer once that handler layer exists.
- Keep reads (queries) and writes (commands) as distinct models/handlers even where an EF entity is reused — don't collapse them back into a single CRUD service.

#### Domain entities (`CompenseAgora/Entities/`)

Modeled from the project's ER diagram (carbon-emissions domain: people, fuels, fleets, trips, energy consumption). Table names in SQL Server match the diagram's `ALL_CAPS_SNAKE` names exactly (e.g. `PESSOA`, `FATORES_DO_COMBUSTIVEL`) via `ToTable(...)` in each configuration class, rather than EF's pluralized-English convention.

- `Pessoa` → `Viagem` (1:N), `Energia` (1:N)
- `Combustivel` self-references itself twice (`CombustivelBiogenico`, `CombustivelFossil`, both nullable) and is referenced by `FatorCombustivel` (1:N, yearly fuel factors), `Viagem` (1:N), and three times from `Frota` (`CombustivelPrimario` required, `CombustivelBiogenico`/`CombustivelFossil` nullable).
- `Frota` → `ConsumoMedioFrota` (1:N), `Viagem` (1:N)
- `Viagem` and `Energia` carry the computed `EmissaoCO2` result and reference `Pessoa`/`Frota`/`Combustivel` as applicable.
- `GasEfeitoEstufa`, `FatorComposicaoCombustivel`, and `FatorEnergia` are standalone reference/factor tables with no FK relationships in the diagram.
- Date-only diagram fields (`CriadoEm`, `DataReferencia`) are modeled as `DateOnly`, not `DateTime`.
- **All FK relationships use `DeleteBehavior.Restrict`**, deliberately — `Frota` alone has three separate FKs into `Combustivel` (Primário/Biogênico/Fóssil), and SQL Server rejects cascade deletes that create multiple cascade paths to the same table. Restrict avoids that class of migration error entirely; revisit per-relationship only if a specific cascade is actually needed.
- All `decimal` properties are configured with `HasPrecision(18, 6)` as a uniform default (factors, emissions, percentages) — tune per-field if a specific business precision is later specified.
- Note: the source diagram has `EmissaoCO2` on `VIAGEM` but `EmissaCO2` (missing "o") on `ENERGIA` — treated as a typo and normalized to `EmissaoCO2` on both entities. Flag if the real schema actually needs the misspelled column name.

### Frontend: Nielsen's 10 usability heuristics

UI/UX work (new pages, components, and reviews of existing ones) should be evaluated against Nielsen's 10 heuristics:

1. Visibility of system status
2. Match between system and the real world
3. User control and freedom
4. Consistency and standards
5. Error prevention
6. Recognition rather than recall
7. Flexibility and efficiency of use
8. Aesthetic and minimalist design
9. Help users recognize, diagnose, and recover from errors
10. Help and documentation

When building or reviewing Razor components, check against these explicitly (e.g., loading/saving states for #1, confirmation before destructive actions for #3/#5, clear validation messages for #9) rather than treating them as implicit.
