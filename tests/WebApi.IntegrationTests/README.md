# WebApi.IntegrationTests

End-to-end integration tests for the DopamineKick API. Each test boots the **real** WebApi host with
`WebApplicationFactory<Program>` and drives it over HTTP against **real infrastructure** spun up in
Docker via [Testcontainers](https://dotnet.testcontainers.org/): Postgres, RabbitMQ and Redis. The only
faked dependency is the external Keycloak identity server (see [Authentication](#authentication)).

Because the host runs for real, these tests exercise the full stack — endpoint → validation → Mediator
handler → EF Core → Postgres — plus the asynchronous outbox → RabbitMQ publishing loop.

## Prerequisites

- **.NET 10 SDK** — provided by the repo's Nix flake (`nix develop`).
- **Docker** — must be running. Testcontainers pulls and starts the images below on first run and
  removes them afterwards (via the Ryuk resource-reaper container).

No local Postgres/RabbitMQ/Redis is required; everything runs in throwaway containers.

## Running the tests

```bash
# from the repo root, inside the dev shell
nix develop --command dotnet test tests/WebApi.IntegrationTests
```

The first run is slower while Docker pulls the images. Subsequent runs reuse the cached images.

To run a single test or class:

```bash
nix develop --command dotnet test tests/WebApi.IntegrationTests \
  --filter "FullyQualifiedName~HabitsEndpointsTests"
```

## Containers

| Service   | Image                 | Purpose                                                             |
|-----------|-----------------------|--------------------------------------------------------------------|
| Postgres  | `postgres:17`         | The four per-module databases (`usersdb`, `habitsdb`, `questsdb`, `notificationsdb`) |
| RabbitMQ  | `rabbitmq:3-management` | Integration-event broker for the outbox publisher / consumers    |
| Redis     | `redis:7-alpine`      | Distributed cache                                                  |

Images mirror the versions in the repo's `docker-compose.yml`. The app uses **one Postgres server with
four databases**; the fixture creates the extra three after the container starts, then hands a
per-module connection string to the host.

## How it works

The harness lives in `Infrastructure/`:

- **`ContainerFixture`** — starts the three containers (in parallel), creates the four module databases,
  and exposes the connection settings.
- **`CustomWebApplicationFactory`** — boots the real WebApi host pointed at those containers. The app's
  start-up code runs EF Core **migrations automatically**, so all schemas (including Quartz tables) exist
  by the time the first test runs. Only the auth layer is swapped for a test scheme.
- **`ApiTestFixture`** + **`IntegrationCollection`** — an xUnit **collection fixture** that owns the
  containers and the factory and shares them across every test class (containers are expensive, so they
  start once per test run). It also provides helpers for authenticated `HttpClient`s and for running work
  in a scoped service provider (e.g. reading a `DbContext`).

Test classes opt in with `[Collection(IntegrationCollection.Name)]`.

### Configuration injection

The app reads connection strings and JWT settings inside `RegisterServices` **during `Program.Main`** —
that is, *before* `WebApplicationFactory`'s `ConfigureAppConfiguration` callbacks are layered in. So the
factory injects config via **environment variables** (using the `__` separator, e.g.
`ConnectionStrings__HabitsDBConnectionString`), which `WebApplication.CreateBuilder` reads up front. The
original environment values are restored on dispose.

### Authentication

Keycloak is external and is not containerised. Instead, `TestAuthHandler` replaces the real JWT bearer
scheme and is registered as the default auth scheme in the factory. Requests are authenticated using a
`X-Test-UserId` header (a GUID), which the handler turns into a `NameIdentifier` claim — exactly what the
modules' `UserIdEndpointFilter` expects. Two extra headers drive the negative-auth paths:

| Header             | Effect                                                            |
|--------------------|------------------------------------------------------------------|
| `X-Test-UserId`    | Authenticate as this GUID user (default: a fixed test GUID)       |
| `X-Test-NoAuth`    | Treat the request as anonymous → `401` on secured endpoints       |
| (non-GUID `X-Test-UserId`) | Authenticated but no valid user id → `400` from the filter |

Use the fixture helpers rather than setting headers by hand:

```csharp
var client = fixture.CreateClientAs(userId);          // authenticated as userId
var anon   = fixture.CreateAnonymousClient();          // expect 401
var bad    = fixture.CreateClientWithInvalidUserId();  // expect 400
```

## Test coverage

- **`Habits/HabitsEndpointsTests`** — habit CRUD, structured `404`/`400` error bodies, the
  completion/streak logic (fresh streak, consecutive-day increment, same-day duplicate handling), and the
  `401`/`400` auth paths.
- **`Habits/OutboxPublishingTests`** — creating an enabled reminder writes an outbox message; the test
  waits for the real `OutboxPublisher` background service to publish it to RabbitMQ (polls the outbox row
  until `Published == true`).

## Writing a new test

1. Add a class in a module folder and annotate it with `[Collection(IntegrationCollection.Name)]`.
2. Take `ApiTestFixture` via the constructor.
3. Use `fixture.CreateClientAs(...)` for HTTP calls and `fixture.WithScopeAsync(sp => ...)` for direct
   DbContext access when seeding data or asserting persisted state.
4. Give each test a **unique user id** (`Guid.NewGuid()`) so rows never collide — the containers and their
   databases are shared across the whole run.

## Data isolation

There is no per-test database reset; the databases persist for the lifetime of the run. Isolate tests by
scoping their data to a fresh `Guid` user id instead of relying on a clean database.

## Notes

- Keep this project **outside** `src/Modules/` — `Directory.Build.targets` fails the build if a project
  under `Modules/*` references another module, and this project references all of them.
- Package versions (EF Core, Npgsql, RabbitMQ.Client) track the app's versions to avoid conflicts, since
  the repo does not use central package management.
