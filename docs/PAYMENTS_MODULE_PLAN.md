# Payments (Billing) Module — Implementation Plan

Status: planned (2026-07-18). A new vertical-slice module that adds Stripe
subscriptions/trials to the DopamineKick API.

## Core architectural principle

Everything funnels through a single `SubscriptionSyncService.SyncAsync(stripeCustomerId)`
that **re-fetches the full subscription state from Stripe and overwrites the local
`SubscriptionState` row**. Webhooks only trigger a resync — their payloads are never
trusted or parsed for business logic. Because sync always reflects Stripe's current
truth and always full-overwrites, it is idempotent by construction, so there is **no
processed-event dedup table** and **no per-event business branching**.

## How this maps onto our architecture

The codebase is a modular monolith with **hard-enforced module boundaries**
(`Directory.Build.targets` fails the build on any cross-module `ProjectReference`).
Each module is a vertical slice — `Domain / Application / Infrastructure / Api`, its own
`DbContext`, its own Postgres connection string, its own migrations — registered via
`Add{X}Module` / `Migrate{X}Database` and wired in `WebApiExtensions`. Modules
communicate only via integration events over the **Outbox → RabbitMQ → consumer**
pipeline. Auth is Keycloak JWT; `UserId` comes from the `sub` claim via
`ClaimsPrincipalExtensions.GetUserId()`.

Two consequences that resolve the generic plan's ambiguities:

- **The `UserId ↔ StripeCustomerId` binding lives inside the Payments module** (its own
  `CustomerMapping` table), NOT as a column on `Users.User`. Payments cannot reference the
  Users module. Payments learns `UserId` from the JWT `sub` claim and the email from JWT
  claims — it never touches the Users table.
- **Cross-module feature-gating** (Habits/Quests checking subscription status) cannot read
  the Payments DB directly. Decision below.

### Decisions (2026-07-18)

- **Access control:** shared abstraction + Host filter. Declare `ISubscriptionAccessService`
  in `Common.Abstractions`, implement in `Payments.Infrastructure`, enforce via a
  `RequireActiveSubscription` endpoint filter registered in the Host (`WebApi`, which already
  references every `.Api` project). Keeps the module boundary intact — a shared *abstraction*,
  not a module-to-module reference — mirroring how `IMessagePublisher` is shared.
- **Emails:** Stripe Dashboard emails (payment-failed, cancellation) + reuse the existing
  web-push pipeline for "trial ending". No new app email infrastructure in v1. The
  Notifications module is web-push only today; app-sent email is deferred to a follow-up.
- **Frontend** is a separate repo owned by a different agent; this repo delivers a
  `docs/PAYMENTS_API_CONTRACT.md`.

## Module layout

```
src/Modules/Payments/
  Payments.Domain/
    Entities/           CustomerMapping.cs, SubscriptionState.cs, OutboxMessage.cs
    Errors/             PaymentsErrors.cs
  Payments.Application/
    Abstractions/       IPaymentsRepository, IPaymentsUnitOfWork, IStripeService,
                        ISubscriptionSyncService
    Payments/Commands/  EnsureCustomer, CreateCheckoutSession, CreatePortalSession
    Payments/Queries/   GetSubscriptionState
    Common/Models/      DTOs
  Payments.Infrastructure/
    DbContexts/         PaymentsContext (+ PaymentsContextFactory)
    Configuration/      *Configuration.cs (EF), StripeOptions.cs
    Migrations/
    Repositories/       PaymentsRepository
    Services/           StripeService, SubscriptionSyncService, SubscriptionAccessService
    BackgroundServices/ InboxProcessor (durable webhook inbox, SKIP LOCKED poller)
  Payments.Api/
    PaymentsModule.cs
    EndpointDefinitions/ BillingEndpointDefinitions.cs, StripeWebhookEndpointDefinitions.cs
```

`Stripe.net` is referenced from `Payments.Infrastructure` **only**, wrapped behind
`IStripeService` — keep the SDK out of Domain/Application.

## Phase 1 — Stripe dashboard setup (external, no code)

- Create Product + recurring Prices (monthly/annual).
- Trial via `trial_period_days` on the **Checkout Session** (not the Price) for per-user control.
- Configure Customer Portal: enable cancellation, cancel-at-period-end, decide plan-switch/payment-update.
- **Enable "Limit customers to one subscription"** — the only reliable double-checkout guard.
- **Disable Cash App Pay** (fraud/chargeback correlation).
- Register the production webhook endpoint; capture the signing secret.
- `stripe listen --forward-to <local>/api/billing/webhook` for local dev.

## Phase 2 — Data model (`PaymentsContext`, `PaymentsDBConnectionString`)

Entities implement `IAuditable` (free `CreatedAt/UpdatedAt` via `AuditableEntityInterceptor`).

```csharp
public class CustomerMapping : IAuditable          // eager binding
{
    public Guid UserId { get; set; }               // PK, = Keycloak sub
    public string StripeCustomerId { get; set; }   // cus_xxx, unique index
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class SubscriptionState : IAuditable         // keyed on StripeCustomerId, full-overwrite
{
    public string StripeCustomerId { get; set; }    // PK
    public string? SubscriptionId { get; set; }
    public string Status { get; set; }              // none|trialing|active|past_due|canceled|...
    public string? PriceId { get; set; }
    public DateTimeOffset? CurrentPeriodStart { get; set; }
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string? PaymentMethodBrand { get; set; }
    public string? PaymentMethodLast4 { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

Copy the Habits `OutboxMessage` entity + its EF configuration verbatim so the notification
pipeline works. Add `PaymentsDBConnectionString` to `appsettings.json` + helm/env +
docker-compose. One `InitialPaymentsMigration`. Only `SubscriptionSyncService` writes
`SubscriptionState`, never field-patched.

## Phase 3 — Sign-up flow endpoints (`BillingEndpointDefinitions`, all `.RequireAuthorization()`)

- `POST /api/billing/customer` — `EnsureCustomer`: `UserId` from claims, email from JWT
  claims, `stripe.customers.create(metadata: { userId })` **only if no mapping exists**,
  persist `CustomerMapping`. Idempotent.
- `POST /api/billing/checkout` — `CreateCheckoutSession`: resolve caller's
  `StripeCustomerId` server-side (never client-supplied), pass it explicitly, set
  `trial_period_days`, `success_url`, `cancel_url`. Returns Stripe URL. Internally runs
  `EnsureCustomer` first.
- `POST /api/billing/portal` — `CreatePortalSession`: server-side customer resolution,
  returns portal URL.
- `POST /api/billing/sync` — runs `SyncAsync` for the caller's customer (used by `/success`).
- `GET /api/billing/subscription` — `GetSubscriptionState`: read model for the frontend;
  `{ status, currentPeriodEnd, cancelAtPeriodEnd, brand, last4 }` or a `none` shape.

**Pattern B funnel** (sign-up = start trial): FE sign-up page calls `CreateUser` (Keycloak) →
`POST /api/billing/checkout` → redirect to Stripe → `/success`.

## Phase 3.5 — `SubscriptionSyncService` (single source-of-truth writer)

`ISubscriptionSyncService.SyncAsync(string stripeCustomerId)`:
1. `stripe.Subscriptions.ListAsync(customer, limit:1, status:"all")`, expand `default_payment_method`.
2. Map current Stripe truth to a full `SubscriptionState` (`none` shape if no subscription).
3. Upsert (full overwrite) via `IPaymentsRepository`.
4. On a notify-worthy transition (trial-ending, payment-failed), write an `OutboxMessage` in
   the **same `SaveChanges`** transaction.

Called by both `/success` (`/api/billing/sync`) and the webhook.

## Phase 4 — `/success` handler

FE `/success` route calls `POST /api/billing/sync` (authenticated; resolves customer
server-side, runs `SyncAsync`) **before** showing "subscribed", then routes into the app.
Closes the browser-beats-webhook race; no `CHECKOUT_SESSION_ID` parsing.

## Phase 5 — Webhook handler (`StripeWebhookEndpointDefinitions`)

- `POST /api/billing/webhook` — **no `.RequireAuthorization()`**; the Stripe signature is the auth.
- Read raw body: `using var reader = new StreamReader(request.Body); var json = await reader.ReadToEndAsync();`
  then `EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"], webhookSecret)`.
  Verify no upstream middleware consumes the body first (add `Request.EnableBuffering()` only if needed).
- Filter to the allowed event-type list → extract `customerId` + Stripe `evt_` id → **durably record**
  an `InboxMessage` (deduped on the event id) via `RecordWebhookEvent` → return `200` immediately.
- `InboxProcessor : BackgroundService` drains the inbox with `FOR UPDATE SKIP LOCKED` and calls
  `SyncAsync(customerId)` ("ack fast, process async" without a job framework).
- No per-event business branches.

**Why a durable inbox, not an in-process queue (multi-instance correctness).** The webhook handler
and its processor run on the same instance, so routing isn't the issue — but an in-memory
`Channel<string>` loses queued work if the instance crashes/rolls *after* the 200-ack (Stripe won't
retry a 200'd event). The **inbox pattern** (idempotent receiver) fixes this: persist "resync
customer X" before acking, then a poller drains it with `SELECT … FOR UPDATE SKIP LOCKED` so multiple
instances share the backlog without double-processing. Dedup key = Stripe `evt_` id (unique index);
dead-letters after `MaxAttempts`. The same `SKIP LOCKED` guard was also added to the Habits/Quests
**Outbox** publishers, which previously could double-publish across instances.

> EF gotcha: these contexts use `EnableRetryOnFailure()`, so any user-initiated
> `BeginTransactionAsync` must run inside
> `context.Database.CreateExecutionStrategy().ExecuteAsync(...)` or EF throws. Applies to the inbox
> poller and both outbox publishers.

Allowed event types (all just trigger a resync):
`checkout.session.completed`; `customer.subscription.created|updated|deleted|paused|resumed`;
`customer.subscription.pending_update_applied|pending_update_expired`;
`customer.subscription.trial_will_end`;
`invoice.paid|payment_failed|payment_action_required|upcoming|marked_uncollectible|payment_succeeded`;
`payment_intent.succeeded|payment_failed|canceled`.

## Phase 6 — Access control

- `ISubscriptionAccessService` in `Common.Abstractions`, implemented by
  `Payments.Infrastructure.SubscriptionAccessService` (reads `SubscriptionState`).
- `RequireActiveSubscription` endpoint filter in the Host (`WebApi`), applied to premium
  write endpoints in Habits/Quests.
- Gate: `Status ∈ {active, trialing}` → allow. `CancelAtPeriodEnd && active && now < CurrentPeriodEnd`
  → allow (grace period). `past_due` → soft-warn (allow + surface a banner; revisit).
- Never derive access from a webhook event type — always read `SubscriptionState`.

## Phase 7 — Frontend

Separate repo. This repo delivers `docs/PAYMENTS_API_CONTRACT.md`: the billing endpoints,
the `subscription` read shape, and the sign-up → checkout → success sequence. Mirrors
`docs/QUESTS_API_CONTRACT.md`.

## Phase 8 — Emails/notifications

- Payments raises `Common.IntegrationEvents/Payments/` events (e.g. `SubscriptionTrialEnding`)
  from `SyncAsync` via the outbox. Notifications adds a consumer + handler and sends web-push.
- Payment-failed / cancellation confirmation: rely on **Stripe Dashboard emails** for v1.
- App-sent email (`IEmailSender` + provider) is out of scope for v1 — deferred follow-up.

## Phase 9 — Testing

Reuse `WebApi.IntegrationTests` (`CustomWebApplicationFactory`, `TestAuthHandler`, Testcontainers).
Mock `IStripeService`. Add: webhook signature verification (valid/invalid), out-of-order &
duplicate event delivery → assert `SubscriptionState` always matches the mocked Stripe list
result, and the `/success` sync path. Manual: `stripe trigger`, test cards incl. trial-then-fail,
cancel → reactivate via Portal.

## Phase 10 — Config / legal

- Secrets via `StripeOptions` (`Configure<StripeOptions>`, same pattern as `KeycloakSettings`/
  `WebPushOptions`): `Stripe:SecretKey`, `Stripe:PublishableKey`, `Stripe:WebhookSecret`,
  `Stripe:PriceId` per tier per env. Empty in committed `appsettings.json`, real values via env/helm.
- ToS/Privacy cover trial terms + cancellation. GST/tax via Stripe Tax or manual (external).

## Wiring checklist

1. 4 new `.csproj` under `src/Modules/Payments/`, added to `.slnx`.
2. `Payments` added to `Directory.Build.targets` (`ModuleNames` + 4 `Contains('Modules/Payments')` guards).
3. `AddPaymentsModule` + `MigratePaymentsDatabase` called in `WebApiExtensions`.
4. `PaymentsDBConnectionString` in appsettings + helm + docker-compose.
5. `Payments.Api` assembly ends in `.Api` → endpoint auto-discovery + Mediator
   `Namespace = "Payments.Api.Mediator"` picks it up automatically.
6. `Stripe.net` package ref in `Payments.Infrastructure`.
