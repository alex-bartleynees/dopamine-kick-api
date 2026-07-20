# Plan: Extract shared `Common.*` into a standalone NuGet-published repo

Status: **in progress**. Moves the shared kernel projects out of the DopamineKick monolith into their own
repository that builds, versions and publishes them as NuGet packages. The monolith and the Payments.Gateway
then consume the packages instead of project references / vendored copies. Companion to
`PAYMENT_GATEWAY_EXTRACTION_PLAN.md`, whose "later: publish `Common.*` as internal NuGet packages" step this
realises.

**Progress (2026-07-20):**
- ✅ **Stage 1 — prereq messaging refactor** landed in the monolith (config-driven exchanges + **multi-exchange
  support**). Build + 16 integration tests green. See "Prerequisite refactor" below.
- ✅ **Stage 2 — `shared-kernel` stood up** and split into **6 focused packages** (not the original 2). Builds
  + packs locally (`0.0.0-alpha.0`, MinVer, uncommitted; no remote/tag yet). Namespaces were renamed to
  match the packages (`SharedKernel.*`).
- ✅ **Stage 3 — monolith consumes packages** (done). ⬜ **Stage 4 — gateway consumes packages.**

## Why now

The gateway extraction **vendored a copy** of `Common.Abstractions`/`Common.Infrastructure` into
`Payments.Gateway/src/Shared/`. There are now two divergent copies of the same code (the gateway's is a
subset and already edited `MessagingConstants`). Packaging gives a single versioned source of truth and
removes the copy-drift before a third consumer appears.

Current state:
- Shared projects live at `src/Shared/{Common.Abstractions,Common.Infrastructure,Common.IntegrationEvents}`,
  net10.0, referenced by every monolith module + Host via `<ProjectReference>`.
- Dependency graph: `Common.Infrastructure` → `Common.Abstractions`; `Common.IntegrationEvents` →
  `Common.Abstractions`. No central package management (`Directory.Packages.props` absent).
- CI/CD already exists (`.github/workflows/ci.yml`, `cd.yml`, GitHub Actions, `setup-dotnet 10.0.x`).
- GitHub org: `alex-bartleynees`.

## Recommended decisions (confirm before starting)

- **New repo:** `alex-bartleynees/shared-kernel` (the DDD term for code deliberately shared across bounded
  contexts — serves DopamineKick, the gateway, and future products).
- **Feed:** **GitHub Packages** (private, free at this scale, same auth as the repos already use). Alternative
  considered: nuget.org private / Azure Artifacts — heavier, not worth it here.
- **Versioning:** **MinVer** (git-tag driven). Tag `v1.2.3` → package `1.2.3`; commits after a tag get an
  automatic `-preview.N` height. No hand-edited version numbers.
- **Package IDs:** six `SharedKernel.*` packages (see "What gets packaged"), each PackageId = project =
  assembly name.
- **Namespaces:** **renamed to match the packages** (`SharedKernel.Results`, `SharedKernel.Messaging.RabbitMq`,
  …). *(This reverses the earlier "namespaces stay `Common.*`" idea — the six-way split made namespace =
  package the clearer choice. Cost: stage-3/4 consumers get a one-time `using` rewrite, see Consumer changes.)*
- **Collision guard:** add **package source mapping** in consumers so `SharedKernel.*` only ever resolves
  from GitHub Packages and everything else from nuget.org.

## What gets packaged — and what deliberately does not

**Packaged** — the original two projects were split into **six focused packages** along their real dependency
boundaries, so a consumer only pulls what it uses (namespace = package for each):

| Package | Deps | Contents |
|---|---|---|
| `SharedKernel.Results` | none (BCL) | `Result`/`Result<T>`, `Error`, `ErrorType`, `ResultExtensions` |
| `SharedKernel.Messaging.Abstractions` | none (BCL) | `IMessagePublisher`, `IMessageConsumer`, `IIntegrationEventHandler`, `IntegrationEvent`, `IntegrationEventRoutingKeyAttribute` |
| `SharedKernel.Abstractions` | none (BCL) | `IAuditable`, `ClaimsPrincipalExtensions` |
| `SharedKernel.AspNetCore` | FrameworkRef `Microsoft.AspNetCore.App` | `IEndpointDefinition` |
| `SharedKernel.Messaging.RabbitMq` | → `Messaging.Abstractions`; `RabbitMQ.Client`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions` | RabbitMQ publisher/consumer/connection-factory/options, `AsyncLazy` |
| `SharedKernel.EntityFrameworkCore` | → `Abstractions`; `Microsoft.EntityFrameworkCore` | `AuditableEntityInterceptor` |

Why six, not two: the old `Abstractions` forced **all of ASP.NET Core** onto every consumer (via
`IEndpointDefinition`), and the old `Infrastructure` forced **both EF Core and RabbitMQ** together. The split
isolates AspNetCore, and fully decouples RabbitMQ from EF Core. Three packages are zero-dependency BCL. The
unused `Mediator.Abstractions` reference was dropped.

**NOT packaged (stays per-product):**
- **`Common.IntegrationEvents`** — this holds *concrete event DTOs* (`HabitReminderCreated`,
  `SubscriptionEntitlementChanged`, …). Event contracts are bounded-context-specific; a single shared
  package would couple every product to every other product's events. Each producing service keeps (or
  publishes) its own contract; subscribers own a matching copy or reference a per-context contract package
  (e.g. a future `DopamineKick.Payments.Contracts`) — **out of scope here**.
- **App-specific messaging constants** (exchange name, routing keys, queue names) — see the prerequisite
  refactor below; routing keys now live in the monolith-local `Common.IntegrationEvents/RoutingKeys.cs`.
- `Common.Abstractions/Billing/ISubscriptionAccessService` — the monolith's cross-module seam, not generic
  infra. Stays in the monolith (move under a Host-owned folder, not the package).
- **`AppTelemetry`** — its `SourceName = "DopamineKick.API"` is app-specific and nothing in the kernel emits
  through it (only the monolith Host's `ObservabilityExtensions` uses it). Stays in the monolith; **stage 3
  must relocate it to a Host-owned folder** before deleting `src/Shared`. The gateway defines its own.

## Prerequisite refactor (✅ DONE, in the monolith)

`MessagingConstants` hardcoded app-specific values — `ExchangeName = "habits-direct"` plus habit/quest routing
keys and queue names — which **a shared package cannot own**. Completed:

1. ✅ Moved exchange/DLX names onto **`RabbitMqOptions`** (`ExchangeName`, `ExchangeType`,
   `DeadLetterExchangeName`, `DeadLetterQueueSuffix`; exchange names default empty = product-neutral, type
   `"direct"` + `.dlq` suffix = generic conventions). `RabbitMqPublisher`/`RabbitMqConsumer` read them from
   `IOptions<RabbitMqOptions>`.
2. ✅ Deleted `MessagingConstants`. Routing keys (bounded-context-specific) moved to monolith-local
   `Common.IntegrationEvents/RoutingKeys.cs`; the unused queue-name constants were dropped.
3. ✅ Each app sets its exchange in config: monolith `appsettings.json` → `ExchangeName: habits-direct`,
   `DeadLetterExchangeName: habits-dlx` (base file, so tests + prod inherit; Helm only overrides host/vhost).
   Gateway will set `payments-direct`.
4. ✅ **Multi-exchange support added** (deliberately before v1.0.0 froze the API — later would be a binary
   break): `PublishAsync`/`Subscribe` gained an optional `exchange` param (+ `deadLetterExchange` on
   Subscribe), null ⇒ configured default; exchanges are now declared on demand, so one consumer can bind
   across `habits-direct` *and* the gateway's `payments-direct`. Needed for the monolith's future
   entitlement consumer.

Result: the RabbitMQ infra is genuinely product-neutral. Build + 16 integration tests green.

## Target repo layout (`shared-kernel`)

```
shared-kernel/
  Directory.Build.props         net10.0, Nullable, ImplicitUsings, deterministic, MinVer, SourceLink,
                                shared package metadata, NoWarn CS1591;CS1573
  SharedKernel.slnx             folder names MUST be "/src/" (slnx serializer requirement)
  nuget.config                  (restore transitive deps from nuget.org)
  src/
    SharedKernel.Results/
    SharedKernel.Messaging.Abstractions/
    SharedKernel.Abstractions/
    SharedKernel.AspNetCore/
    SharedKernel.Messaging.RabbitMq/        (→ Messaging.Abstractions)
    SharedKernel.EntityFrameworkCore/       (→ Abstractions)
  .github/workflows/
    ci.yml                      PR: restore/build/pack (validate)
    release.yml                 tag push: pack + push to GitHub Packages
```

Common package metadata (`<IsPackable>`, `<Authors>`, `<RepositoryUrl>`, `<PackageLicenseExpression>`,
`<GenerateDocumentationFile>`, MinVer, SourceLink) lives once in `Directory.Build.props`; each `.csproj` sets
only its `<PackageId>`/`<Description>`/`<PackageTags>` and its own deps. Inter-package `<ProjectReference>`s
(RabbitMq → Messaging.Abstractions, EntityFrameworkCore → Abstractions) become package-to-package
dependencies on pack. **Gotcha fixed:** `GenerateDocumentationFile=true` + `TreatWarningsAsErrors=true` turns
undocumented/partially-documented public members into errors (CS1591/CS1573) — suppressed via `NoWarn` so
the `.xml` still ships without forcing full docs.

## CI/CD pipeline (new repo)

- **`ci.yml`** (on PR): `dotnet restore` → `build -c Release` → `test` (if tests added) → `dotnet pack -c Release`
  to prove the packages build. No publish.
- **`release.yml`** (on `push: tags: ['v*']`): `dotnet pack -c Release` then
  `dotnet nuget push "**/*.nupkg" --source https://nuget.pkg.github.com/alex-bartleynees/index.json
  --api-key ${{ secrets.GITHUB_TOKEN }} --skip-duplicate`. MinVer derives the version from the tag, so the
  release workflow is: `git tag v1.0.0 && git push --tags`.
- `GITHUB_TOKEN` has `packages: write` in-repo — no PAT needed for publishing.
- Include `.snupkg` symbol packages + `<PublishRepositoryUrl>`/`<EmbedUntrackedSources>` for source link.

## Consumer changes

**Both repos** get a `nuget.config` adding the GitHub Packages source + **package source mapping**
(`SharedKernel.*` → github, `*` → nuget.org) so restore is unambiguous. Local dev + CI authenticate to
GitHub Packages with a PAT (`read:packages`) or `GITHUB_TOKEN`; document the token in each repo's README and
the nix dev-shell env.

Because the kernel is now **6 packages with `SharedKernel.*` namespaces**, each consumer project references
only the package(s) it uses, and its `using` directives are rewritten. Namespace → package map for the
rewrite (find/replace across each repo):

| Old `using` | New `using` (= package) |
|---|---|
| `Common.Abstractions.Results` | `SharedKernel.Results` |
| `Common.Abstractions.Messaging` (contracts) | `SharedKernel.Messaging.Abstractions` |
| `Common.Abstractions` / `Common.Abstractions.Extensions` (IAuditable, ClaimsPrincipalExtensions) | `SharedKernel.Abstractions` |
| `Common.Abstractions` (IEndpointDefinition) | `SharedKernel.AspNetCore` |
| `Common.Infrastructure.Messaging` / `Common.Infrastructure.Utils` | `SharedKernel.Messaging.RabbitMq` |
| `Common.Infrastructure.Interceptors` | `SharedKernel.EntityFrameworkCore` |

**DopamineKick.API (monolith) — stage 3:**
- Per project, add a `<PackageReference>` for each `SharedKernel.*` package it actually uses (map by the
  `Common.*` namespaces it imports — the ~26 old refs to `Common.Abstractions`/`Common.Infrastructure` fan
  out across the 6 packages) and rewrite that project's `using`s per the table.
- **Relocate `AppTelemetry`** to a Host-owned folder (namespace no longer `Common.Abstractions.Telemetry`);
  update `ObservabilityExtensions.cs`.
- Delete `src/Shared/Common.Abstractions` + `src/Shared/Common.Infrastructure` and drop them from
  `DopamineKick.API.slnx`. Keep `src/Shared/Common.IntegrationEvents` (product events stay local); its files
  reference the messaging contracts, so rewrite their `using Common.Abstractions.Messaging;` →
  `SharedKernel.Messaging.Abstractions`.
- Exchange config already added (prereq refactor). Verify integration tests still green.

**Payments.Gateway — stage 4:**
- Delete the vendored `src/Shared/Common.Abstractions` + `src/Shared/Common.Infrastructure`; add the
  `SharedKernel.*` `<PackageReference>`s each project needs; rewrite `using`s per the table; drop the vendored
  projects from the slnx.
- Keep `src/Shared/Common.IntegrationEvents` (the `SubscriptionEntitlementChanged` contract stays here).
- Set `RabbitMQ:ExchangeName = payments-direct` in config (replacing the vendored constant edit).

## Rollout (strangler, low-risk)

1. ✅ **Prereq refactor** in the monolith: `MessagingConstants` → `RabbitMqOptions` + multi-exchange. Shipped
   + verified (build + 16 integration tests).
2. ✅ **Stand up `shared-kernel`**: projects moved in and **split into 6 packages**; MinVer + metadata + CI in
   place; builds + packs locally. ⬜ *Remaining:* commit, add GitHub remote, `git tag v1.0.0 && git push
   --tags`, confirm packages land in GitHub Packages. *(Outward-facing — do explicitly.)*
3. ✅ **Monolith consumes packages**: per-project `PackageReference`s (6-way), `using` rewrites, relocated
   `AppTelemetry` → `WebApi.Telemetry`, moved `ISubscriptionAccessService` → `Payments.Domain.Billing`,
   deleted `src/Shared/Common.{Abstractions,Infrastructure}`, `nuget.config` already present.
   Restore fails only on 401 (no token in shell) — zero structural errors.
4. ⬜ **Gateway consumes packages**: delete vendored copies, swap to `PackageReference`s + `using` rewrites.
   Build green + boot.
5. *(Optional follow-ups)* introduce `Directory.Packages.props` (central package management) in each consumer
   to pin the shared versions in one place; and, if event sharing becomes painful, publish per-context
   contract packages (e.g. `DopamineKick.Payments.Contracts`) from the producing repos.

## Verification

- **shared-kernel:** ✅ `dotnet pack` produces all **six** `SharedKernel.*.<v>.nupkg` with the correct
  package-to-package dependencies (RabbitMq → Messaging.Abstractions, EntityFrameworkCore → Abstractions) —
  verified locally at `0.0.0-alpha.0`. ⬜ A tag publishes them to GitHub Packages, restorable from a clean
  machine (auth token present).
- **Monolith:** solution builds with **zero `src/Shared/Common.{Abstractions,Infrastructure}`** on disk; the
  full Testcontainers integration suite passes (RabbitMQ exchange now from config).
- **Gateway:** solution builds with the vendored copies deleted; host boots and migrates.
- **Version bump loop:** cut `v1.0.1` with a trivial change, bump both consumers, confirm restore picks it up.

## Risks / notes / non-goals

- **GitHub Packages restore auth** is the main friction: even *reading* a private GH Packages feed needs a
  token, so local dev + CI + the Dockerfiles all need `nuget.config` + a `read:packages` credential. Document
  it; wire the token through the nix dev shell and the Docker build (`--secret`), not committed.
- **Two-repo lockstep during a breaking change**: a breaking API change means bump → publish → update both
  consumers. SemVer major signals it; keep breaking changes rare. This is the accepted cost of packaging.
- Package IDs are prefixed (`SharedKernel.*`) + source-mapped specifically to avoid any nuget.org name
  collision resolving the wrong assembly.
- **Non-goals:** moving product event contracts into the shared repo (they stay per-product); central package
  management (optional Phase 5). *(Note: the prereq refactor was mostly config-driven, but did add
  backward-compatible **multi-exchange** support to the publisher/consumer — an additive API change made
  before v1.0.0, not a behaviour change to existing single-exchange flows.)*
```
