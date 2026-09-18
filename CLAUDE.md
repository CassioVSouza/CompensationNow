# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

Started as a default **Blazor Web App** (`dotnet new blazor`) targeting **.NET 10**, using Interactive Server render mode. This is a deliberately **monolithic** project — a single `CompenseAgora` project, not a multi-project Clean Architecture split — with internal layering by folder instead of by assembly (see below). The UI is still mostly the default template pages (Home/Counter/Weather, layout, nav menu), plus a real Register/Login/Confirm-email flow. Backend plumbing in place: EF Core against SQL Server (entities, Fluent API configurations, `DbContext`, migrations), the CQRS/MediatR pipeline with a full CRUD example for `Pessoa` (the other 9 entities follow the same pattern but don't have handlers yet), and cookie-session authentication backed by Amazon Cognito (see the Authentication section below).

Solution file: `CompenseAgora.slnx` at the repo root (new XML solution format), referencing the single `CompenseAgora/CompenseAgora.csproj`.

## Commands

Run from `CompenseAgora/` (where `CompenseAgora.csproj` lives), or from the repo root using `--project CompenseAgora`.

- Build: `dotnet build`
- Run (dev server with hot reload): `dotnet watch run`
- Run without watch: `dotnet run`
- Restore packages: `dotnet restore`
- Add a migration: `dotnet ef migrations add <Name> -o Data/Migrations`
- Apply migrations to the configured database: `dotnet ef database update`
- Run tests: `dotnet test` (from the repo root, or `dotnet test CompenseAgora.slnx`) — runs the `CompenseAgora.Tests` xUnit project.

There are no linters or CI configuration in the repo yet. The `dotnet-ef` global tool installed on this machine is v9.0.2, older than the project's EF Core v10.0.11 — it still works (with a warning) but consider `dotnet tool update -g dotnet-ef` if migration generation ever behaves unexpectedly.

Default dev URLs (from `Properties/launchSettings.json`): `https://localhost:7070` and `http://localhost:5091`.

## Architecture

- **Entry point**: `Program.cs` configures the standard ASP.NET Core minimal-hosting pipeline for a Blazor Web App:
  - `AddRazorComponents().AddInteractiveServerComponents()` — registers Razor Components with Interactive Server render mode (SignalR-based, server-side interactivity; no WASM).
  - `AddDbContext<CompenseAgoraDbContext>(...)` — registers EF Core against SQL Server using the `CompenseAgoraDb` connection string.
  - `AddMediatR(...)` / `AddValidatorsFromAssembly(...)` / the `ValidationBehavior` pipeline registration — wire up MediatR and FluentValidation for the CQRS layer (see below).
  - `AddAWSService<IAmazonCognitoIdentityProvider>()`, `Configure<CognitoOptions>(...)`, `AddScoped<CognitoAuthService>()`, `AddAuthentication(...).AddCookie(...)`, `AddAuthorization()`, `AddCascadingAuthenticationState()` — the Cognito + cookie-session authentication stack (see the Authentication section below).
  - `MapRazorComponents<App>().AddInteractiveServerRenderMode()` — the root component is `Components/App.razor`, rendered with server interactivity applied app-wide.
  - Standard middleware: exception handler + HSTS in non-Development, status code re-execution to `/not-found`, HTTPS redirection, `UseAuthentication()`/`UseAuthorization()` (before antiforgery), antiforgery, static assets.
- **Component structure** (`Components/`):
  - `App.razor` — the root HTML document component (head, root render mode).
  - `Routes.razor` — the `<Router>` that maps routes to page components.
  - `Layout/MainLayout.razor` + `NavMenu.razor` — shared page chrome/sidebar nav.
  - `Layout/ReconnectModal.razor(.css/.js)` — UI shown when the SignalR circuit drops (relevant to Interactive Server mode).
  - `Pages/` — routable pages (`Home`, `Counter`, `Weather`, `Error`, `NotFound`), each a `.razor` file with `@page` route directives.
  - `_Imports.razor` — shared `@using` directives for all components.
- **Static assets**: `wwwroot/` (app.css, favicon, Bootstrap under `wwwroot/lib/bootstrap`). Served via `app.MapStaticAssets()`.
- **Config**: `appsettings.json` / `appsettings.Development.json` — standard ASP.NET Core configuration. `CompenseAgoraDb` (the connection string) currently points at a local SQL Server instance (`Server=localhost`) directly in `appsettings.json` — fine for solo local dev, but move it to user-secrets/environment variables before this is ever shared or deployed. `appsettings.json` also has empty `AWS:Region` and `Cognito:UserPoolId`/`ClientId`/`ClientSecret` placeholders — see Authentication below.

Since this app uses Interactive Server render mode exclusively (no WebAssembly/Auto), all UI state and event handling execute server-side over a persistent SignalR circuit — keep this in mind when adding interactivity (no client-side-only components, and connection drops are handled by `ReconnectModal`).

## Architecture and standards

The project is committed to the following standards; apply them when adding new code.

### Backend: Clean Architecture (by folder) + CQRS

Because the project is monolithic, Clean Architecture is expressed as folders within the one project rather than as separate assemblies. Keep the same dependency direction rules a multi-project split would enforce, just without the compiler enforcing them for you:

- `Entities/` — domain entities, plain POCOs, **no EF Core attributes**. All persistence mapping (keys, relationships, delete behavior, precision, max length) lives in `Data/Configurations/`, not on the entities themselves. Primary keys are named `Codigo` (not `Id`), matching the domain's own vocabulary.
- `Data/` — the persistence layer: `CompenseAgoraDbContext`, `Data/Configurations/*Configuration.cs` (one `IEntityTypeConfiguration<T>` per entity, applied via `ApplyConfigurationsFromAssembly`), and `Data/Migrations/`.
- `Features/<EntityName-plural>/` — CQRS use cases, powered by **MediatR**. Each command/query, its FluentValidation validator, and its handler live together in one file, `Features/<Entity>/Commands/<UseCase>/<UseCase>Command.cs` or `Features/<Entity>/Queries/<UseCase>/<UseCase>Query.cs` — this is the Jason Taylor "Clean Architecture" template convention (vertical-slice-per-file rather than one-class-per-file), chosen so a use case's request shape, validation, and behavior read as one unit. A shared `<Entity>Dto` record sits at the feature root and is reused by that entity's queries.
  - Commands mutate through `CompenseAgoraDbContext` directly (no repository abstraction — EF Core's `DbContext` already *is* the unit-of-work/repository for this project) and return either nothing (`IRequest`) or the new key (`IRequest<int>` for creates).
  - Queries use `AsNoTracking()` and project straight to the DTO — never return entities out of a query handler.
  - `Common/Behaviors/ValidationBehavior.cs` is a `MediatR` `IPipelineBehavior` that runs all registered `IValidator<TRequest>` instances before a handler executes and throws `FluentValidation.ValidationException` on failure — this is automatic per-request; individual handlers do not call validators themselves.
  - `Common/Exceptions/NotFoundException.cs` — thrown by update/delete handlers when the target `Codigo` doesn't exist.
  - **Reference implementation**: `Features/Pessoas/` has the full pattern (Create/Update/Delete commands, GetById/GetAll queries) — replicate this structure for the other 9 entities as their use cases are needed, rather than reinventing the shape each time.
- Razor components should dispatch commands/queries through `IMediator` rather than querying `CompenseAgoraDbContext` directly from the UI layer.
- Keep reads (queries) and writes (commands) as distinct models/handlers even where an EF entity is reused — don't collapse them back into a single CRUD service.

**MediatR licensing note**: MediatR versions 13+ moved to the Reciprocal Public License 1.5 (copyleft — requires releasing your own source, or purchasing a commercial license from Lucky Penny Software). This project deliberately pins `MediatR` to **12.5.0**, the last Apache-2.0-licensed release, to avoid that obligation. Do not bump MediatR past 12.x without the user explicitly deciding to accept the new license terms or pay for it.

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

### Authentication: Amazon Cognito + cookie session

Cognito is the credential authority (passwords never touch our database); the app issues its own session via ASP.NET Core cookie authentication once Cognito confirms a login. `Pessoa` is the local profile, linked to its Cognito identity by a `CognitoSub` column (the Cognito "sub").

- `Auth/CognitoOptions.cs` — bound from the `Cognito` config section (`UserPoolId`, `ClientId`, `ClientSecret`). `ClientSecret` is optional — only set it if the Cognito App Client is confidential (was created with a secret); when present, `CognitoAuthService` computes the `SECRET_HASH` Cognito requires on every call.
- `Auth/CognitoAuthService.cs` — the only place that touches the AWS SDK (`IAmazonCognitoIdentityProvider`). Wraps `SignUp`, `ConfirmSignUp`, `ResendConfirmationCode`, and `InitiateAuth` (using the `USER_PASSWORD_AUTH` flow), translating AWS SDK exceptions into `Common/Exceptions/AuthException` with a user-facing message. Nothing outside this file should reference `Amazon.CognitoIdentityProvider` types.
- `Features/Auth/Commands/{Register,ConfirmEmail,ResendConfirmationCode,Login}/` — the same one-file-per-use-case CQRS pattern as `Features/Pessoas/`. `RegisterCommandHandler` calls Cognito `SignUp` *then* inserts the `Pessoa` row with the returned `CognitoSub` — there's no distributed rollback, so a DB failure after a successful Cognito sign-up leaves an orphaned Cognito user (acceptable for now; revisit if it becomes a real problem). `LoginCommandHandler` calls Cognito `InitiateAuth` to validate credentials, then looks up `Pessoa` **by Email** (not by decoding the returned JWT) to build the session.
- `Components/Pages/Account/{Register,Login,ConfirmEmail}.razor` — deliberately **static server-rendered** (no `@rendermode`), unlike `Counter.razor`. This is required, not incidental: `HttpContext.SignInAsync`/`SignOutAsync` only work reliably during a real HTTP request/response, which static SSR form posts are and an established Interactive Server SignalR circuit is not. They use `[SupplyParameterFromForm]` input models + `EditForm` + `[CascadingParameter] HttpContext` — the same pattern the official `dotnet new blazor -au Individual` Identity template uses, for the same reason. Follow this pattern for any future page that needs to sign in/out or otherwise mutate the auth cookie; don't try to do it from an interactive component.
- Session claims set on login: `ClaimTypes.NameIdentifier` (`Pessoa.Codigo`), `Email`, `GivenName`, `Surname`, and a custom `cognito_sub` claim. `NavMenu.razor` (also static SSR) reads `AuthorizeView`/`context.User` to show Login/Register vs. a name + logout form, and is where the `HttpContext.SignOutAsync` logout call lives.
- Password rules in `RegisterCommandValidator` (8+ chars, upper/lower/digit/special) mirror **Cognito's default** password policy — if the User Pool's actual policy is configured differently, update the validator to match, or validation will pass locally and then fail against Cognito with a less friendly error.
- **Setup required before this works**: a Cognito User Pool + App Client must exist, with `ALLOW_USER_PASSWORD_AUTH` and `ALLOW_REFRESH_TOKEN_AUTH` enabled on the App Client, email as a required/verified user attribute, and the pool's standard AWS credentials resolvable to whatever's running the app (env vars, shared credentials file, IAM role, etc. — this project doesn't hardcode AWS credentials, only `Region` and the pool/client IDs). Fill in `AWS:Region` and `Cognito:UserPoolId`/`ClientId`/`ClientSecret` in configuration (put `ClientSecret` in user-secrets, not `appsettings.json`).
- The `PESSOA.Email` and `PESSOA.CognitoSub` columns both got unique indexes in the `AddCognitoAuthToPessoa` migration — safe on an empty table (the case so far), but if `PESSOA` ever has pre-existing rows without a `CognitoSub`, the migration's `NOT NULL DEFAULT ''` backfill will collide on the new unique index and needs a manual data fix first.
- No password reset / MFA / hosted-UI flow yet — only register, confirm, login, logout.

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

### Localization: user-facing text in Portuguese

All text a user actually sees or hears must be in **Portuguese (pt-BR)**: Razor component markup (headings, labels, button text, placeholders, empty/loading/error states, `PageTitle`), FluentValidation error messages, and exception messages that surface to the UI (e.g. `AuthException`, `NotFoundException`). Development stays in English: C# identifiers, entity/property names, comments, commit messages, route/URL segments (`@page` directives), log messages, and anything in this file. When adding or editing anything user-facing, write it in Portuguese from the start rather than translating later.
