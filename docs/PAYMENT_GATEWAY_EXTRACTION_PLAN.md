# Plan: Extract Payments into a standalone multi-product Payment Gateway

Status: **proposed** (not started). Extracts the in-monolith Payments module into its own deployable
service that can bill multiple products. Companion to `PAYMENTS_MODULE_PLAN.md` (the original in-monolith
build) and `PAYMENTS_API_CONTRACT.md` (frontend contract).

## Context

The Payments module currently lives inside the DopamineKick modular monolith (`src/Modules/Payments/`,
own `PaymentsContext`/`paymentsdb`, Stripe subscriptions + trials, durable webhook inbox, and an
in-process `ISubscriptionAccessService` consumed by the Host's `RequireActiveSubscriptionFilter`). The
goal is to lift it out into its own deployable **payment gateway** service that can serve **multiple
products** (DopamineKick being the first), so billing/Stripe logic is built and operated once.

Confirmed decisions (2026-07-18):
- **Entitlement propagation:** event-driven. The gateway publishes subscription-change events; each
  product keeps a **local entitlement read-model** and gates requests locally (no per-request call to
  the gateway). Matches the existing outbox → RabbitMQ → consumer pattern.
- **Stripe:** one Stripe account; each product has its own Products/Prices with a `productId` in
  metadata; a single webhook endpoint resolves the product from event metadata.
- **Identity:** shared Keycloak realm (SSO). Same `sub` across products; all records scoped by `productId`.
- **Client topology:** product frontends call the gateway **directly** (e.g. `payments.alexbartleynees.com`)
  with the user's JWT. Product backends only consume entitlement events.

## Target architecture

```
Product SPA ──JWT──► Payment Gateway (own service, own DB, own ingress)
                        │  checkout / portal / subscription  (Stripe hosted URLs)
                        │  webhook (Stripe → inbox → sync)
                        └─ publishes SubscriptionEntitlementChanged ─► RabbitMQ
                                                                         │
DopamineKick monolith ◄── consumes ──────────────────────────────────────┘
   Entitlements read-model (local table)  ◄─ RequireActiveSubscriptionFilter reads this
```

The gateway owns all Stripe state. Products never read Stripe or the gateway DB — they react to events
and gate off their own copy.

## What changes

### 1. Multi-product data model (in the Payments projects, before/at extraction)
- Add `ProductId` (string slug, e.g. `"dopamine-kick"`) to `CustomerMapping`, `SubscriptionState`,
  `InboxMessage` — files `src/Modules/Payments/Payments.Domain/Entities/*.cs` + their configs in
  `Payments.Infrastructure/Configuration/*.cs`.
- `CustomerMapping` key → composite `(ProductId, UserId)` (same person can subscribe to multiple
  products = distinct Stripe customers). Keep the unique index on `StripeCustomerId`.
- `SubscriptionState` stays keyed on `StripeCustomerId`; add `ProductId` column + index for scoping.
- New EF migration + **backfill** existing rows with `productId = "dopamine-kick"`.

### 2. Product registry + per-product config
- Replace single `StripeOptions` (`Payments.Infrastructure/Configuration/StripeOptions.cs`) with a
  `ProductBillingOptions` map keyed by `productId`: `{ PriceId(s), TrialPeriodDays, SuccessUrl,
  CancelUrl, PortalReturnUrl, allowed JWT audience/client }`. Single Stripe secret + webhook secret
  stay global (one account).
- `StripeService` (`Payments.Infrastructure/Services/StripeService.cs`) takes `productId` on each call;
  sets `metadata.productId` + `metadata.userId` on customer/subscription creation so webhooks can
  resolve the product.
- **Product resolution:** user-facing endpoints derive `productId` from the request (subdomain, a
  validated `product` param, or a client/audience claim). Webhooks resolve `productId` from the
  subscription/price/customer metadata (no trust in payload beyond routing — same as today).

### 3. Entitlement events (the decoupling contract)
- New integration event `SubscriptionEntitlementChanged { ProductId, UserId, Status, HasAccess,
  CurrentPeriodEnd, CancelAtPeriodEnd }` in `Common.IntegrationEvents/Payments/` (shared contract).
- `SubscriptionSyncService` (`Payments.Infrastructure/Services/SubscriptionSyncService.cs`), after the
  full-overwrite upsert, writes an **outbox** row for this event in the same transaction. Reuse the
  existing `OutboxPublisher` + SKIP-LOCKED pattern (copy from Habits/Quests) — add `OutboxMessages` to
  `PaymentsContext`. `HasAccess` is computed canonically in the gateway (active/trialing/past-due grace,
  the logic currently in `SubscriptionAccessService`).

### 4. DopamineKick monolith side (strangler-friendly)
- New lightweight `Entitlements` consumer (mirror `Notifications.Infrastructure/BackgroundServices/*ConsumerService.cs`):
  subscribe to `SubscriptionEntitlementChanged` for `productId = "dopamine-kick"`, upsert a local
  `Entitlements` table (`userId → hasAccess/status/currentPeriodEnd`).
- **Re-implement `ISubscriptionAccessService`** (`Common.Abstractions/Billing/ISubscriptionAccessService.cs`)
  to read the local `Entitlements` table instead of `PaymentsContext`. **The Host filter
  `src/Host/WebApi/Filters/RequireActiveSubscriptionFilter.cs` stays byte-for-byte identical** — only
  the implementation behind the interface changes. This is the clean seam that makes the split low-risk.
- Remove the Payments module projects + billing endpoints from the monolith (`src/Modules/Payments/**`,
  `AddPaymentsModule`/`MigratePaymentsDatabase` in `WebApiExtensions.cs`, `.slnx`,
  `Directory.Build.targets`, `PaymentsDBConnectionString`, Stripe config).

### 5. The gateway service itself
- New deployable Host wrapping the existing `Payments.Domain/Application/Infrastructure/Api` projects
  (they already have no cross-module refs). Near-term: keep in this monorepo as a second Host +
  solution, referencing the existing `Shared/Common.*` projects (Result, `IEndpointDefinition`,
  `IAuditable`, `AuditableEntityInterceptor`, RabbitMQ publisher/consumer, `IntegrationEvent`). Later:
  split to its own repo and publish `Common.*` + `Payments.Contracts` as internal NuGet packages.
- Auth: validate the shared Keycloak JWT (same issuer as today, `WebApiExtensions.cs` JWT setup); webhook
  endpoint stays anonymous (signature is auth). No service-to-service auth needed since propagation is
  event-based.

### 6. Deployment
- New Helm chart `deployment/helm-charts/payment-gateway/` (copy `dopamine-kick-api`): own Deployment,
  ClusterIP service, ingress host `payments.alexbartleynees.com`, own image, own DB secret/connection,
  Stripe secrets (secret key + webhook secret), per-product config via ConfigMap.
- RabbitMQ: dedicated exchange (e.g. `payments-direct`) on the shared broker; each product binds its own
  entitlement queue. Register the gateway's public webhook URL in Stripe → new signing secret.
- Data: point the gateway at the existing `paymentsdb` (or dump/restore into a new DB), then run the
  `ProductId` migration + backfill.

## Rollout (strangler, low-risk)
1. **In the monolith:** add `ProductId` + product registry + `SubscriptionEntitlementChanged` outbox +
   the local `Entitlements` consumer, and swap `ISubscriptionAccessService` to read the local table.
   Now entitlement flows over events even while Payments still lives in the monolith (dogfood the seam).
2. **Stand up the gateway** as a separate deployable against the same `paymentsdb`, register its Stripe
   webhook + ingress, run in parallel, and cut FE billing traffic + the Stripe webhook over to it.
3. **Remove** the Payments module from the monolith; it now only consumes entitlement events.

## Verification
- **Gateway:** build; `ProductId` migration applies + backfills; reuse the Testcontainers harness
  (`tests/WebApi.IntegrationTests/Infrastructure/*`) for multi-product tests — two `productId`s,
  checkout/portal/subscription scoped per product, webhook resolves product from metadata, and an
  entitlement event is published on sync. Port the existing `Payments/*Tests.cs`.
- **Monolith:** entitlement consumer builds the local read-model; `RequireActiveSubscriptionFilter`
  still returns **402** without access / **200** with — drive it by publishing a seeded
  `SubscriptionEntitlementChanged` and asserting the gated endpoint flips.
- **End-to-end (Stripe test mode):** checkout on product A → gateway sync → event → monolith local table
  → gated endpoint allows; repeat for product B and assert **isolation** (A's subscription does not grant
  B). Confirm the durable inbox still dedups redelivered webhooks per product.

## Notes / non-goals
- Keep the inbox + SKIP-LOCKED poller and the `EnableRetryOnFailure` → `CreateExecutionStrategy` wrapping
  already in place; they carry over unchanged.
- FE contract update: `docs/PAYMENTS_API_CONTRACT.md` base URL becomes the gateway host and endpoints gain
  product scoping; the access model (gate on `status`/entitlement, 402 on gated endpoints) is unchanged —
  except products now read entitlement from their own backend, fed by events.
- Not doing per-product Stripe accounts, per-product IdPs, or backend-proxied billing (all rejected above).
