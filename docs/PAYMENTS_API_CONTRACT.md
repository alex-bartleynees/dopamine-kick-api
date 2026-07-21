# Payments / Billing API Contract

Hand-off contract for the frontend. Describes the billing endpoints, request/response shapes, and the subscribe → checkout → success flow. Status: **implemented** (backend on branch `feature/payments`; not yet exercised against live Stripe test keys). Ping the backend agent if you need a change.

## Concept

Subscriptions are managed by **Stripe**; the app keeps a local mirror of each customer's current subscription and exposes it as a simple read model. The frontend never talks to Stripe's API directly — it calls these endpoints, which return **hosted Stripe URLs** (Checkout, Billing Portal) to redirect the browser to.

Two identities are involved and are **not** the same value:
- **`userId`** — your app user (Keycloak `sub`), taken from the JWT. Never sent in a request body.
- **`customerReference`** — an opaque provider customer id (e.g. `cus_…`), bound to the user server-side. It stays server-side; the frontend never sees or sends it, and it is **not** part of the read model.

## Conventions

- Base path: `/api/billing`
- **Auth:** all endpoints below require a Bearer JWT (same as Habits/Quests). The user is resolved from the token. (The Stripe webhook endpoint is server-to-server and **not** part of this contract — the frontend never calls it.)
- **Content type:** `application/json`. Property names are **camelCase**.
- **Timestamps:** ISO-8601 `DateTimeOffset` with offset, e.g. `2026-08-01T00:00:00+00:00`. Nullable fields are `null` when absent.
- **POST endpoints take no request body** — every input is derived from the authenticated user server-side. Send an empty body.
- **Error body** (returned on 4xx), identical shape to the rest of the API:
  ```json
  {
    "code": "Payments.NoCustomer",
    "detail": "Human readable message",
    "type": "NotFound",
    "status": 404,
    "title": "Not Found"
  }
  ```
  `code` is a stable machine-readable identifier (`Payments.*`); `type` is the semantic category (`Failure`, `Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Gone`). Both are additive and safe to ignore. Note: business failures on the billing endpoints use RFC-7807 problem responses, so 4xx bodies may instead carry the standard `{ "detail", "status", "title" }` problem shape — read `detail` for the message either way.

## Models

### SubscriptionState
The read model for the account/settings page. Returned by `GET /api/billing/subscription`.
```json
{
  "status": "active",
  "priceId": "price_1Q…",
  "currentPeriodEnd": "2026-08-01T00:00:00+00:00",
  "cancelAtPeriodEnd": false,
  "paymentMethodBrand": "visa",
  "paymentMethodLast4": "4242"
}
```
| Field | Type | Notes |
|---|---|---|
| `status` | string | Stripe status, lower-case string (see table below). `"none"` when the user has no subscription yet. |
| `priceId` | string \| null | The subscribed Price. `null` when `status` is `none`. |
| `currentPeriodEnd` | DateTimeOffset \| null | When the current paid/trial period ends. For a canceling sub, this is when access ends. |
| `cancelAtPeriodEnd` | bool | `true` when the user has scheduled a cancellation but still has access until `currentPeriodEnd`. |
| `paymentMethodBrand` | string \| null | e.g. `visa`, `mastercard`. `null` if no card on file. |
| `paymentMethodLast4` | string \| null | Last 4 digits, e.g. `4242`. |

### `status` values
| Value | Meaning | Has access? |
|---|---|---|
| `none` | No subscription ever created for this user | No → show Subscribe CTA |
| `trialing` | In free trial | Yes |
| `active` | Paid and current | Yes |
| `past_due` | A renewal payment failed; Stripe is retrying | Yes (soft-grace) — show a "update payment method" banner |
| `canceled` | Ended | No → show Subscribe CTA |
| `incomplete` / `incomplete_expired` / `unpaid` / `paused` | Edge states | No — treat as no access |

> Access rule (mirror of the backend): grant premium access when `status` is `trialing`, `active`, or `past_due`. A `cancelAtPeriodEnd: true` sub still reports `active` until the period ends, so no special-casing is needed — access follows `status`.

## Endpoints

### Get my subscription state
`GET /api/billing/subscription`

Always returns **200** with a `SubscriptionState` — a brand-new user (no Stripe customer yet) gets `status: "none"` rather than a 404, so you can render the Subscribe CTA off a single call.

**200** → `SubscriptionState`
**401** → unauthenticated

---

### Start checkout (subscribe)
`POST /api/billing/checkout`

No request body. Ensures the Stripe customer exists (creating + binding it on first call), then creates a Checkout Session and returns its hosted URL. Redirect the browser to `url`.

**200** →
```json
{ "url": "https://checkout.stripe.com/c/pay/cs_test_…" }
```
**401** → unauthenticated

> The trial length, price, and success/cancel URLs are configured server-side — you don't pass them.

---

### Open the billing portal (manage / cancel)
`POST /api/billing/portal`

No request body. Creates a Stripe Billing Portal session for the caller's own customer and returns its URL. Redirect the browser to `url`. Use this for the "Manage subscription" / cancel / update-card button.

**200** →
```json
{ "url": "https://billing.stripe.com/p/session/…" }
```
**404** → `Payments.NoCustomer` — the user has never started checkout (no Stripe customer). Show the Subscribe CTA instead.
**401** → unauthenticated

---

### Sync subscription state (called by `/success`)
`POST /api/billing/sync`

No request body. Forces an immediate re-fetch from Stripe and overwrites the local state. Call this from the `/success` redirect route **before** you render "you're subscribed" — it closes the race where the browser gets back before Stripe's webhook does.

**200** → empty body (state is now fresh; follow with `GET /api/billing/subscription` to read it)
**404** → `Payments.NoCustomer` if no Stripe customer exists for the user
**401** → unauthenticated

---

### Ensure customer (optional, advanced)
`POST /api/billing/customer`

No request body. Idempotently creates the Stripe customer + binding for the user without starting checkout. Usually you don't need this — `checkout` does it for you. Provided in case you want to create the customer eagerly at sign-up.

**200** →
```json
{ "customerReference": "cus_…" }
```
**401** → unauthenticated

## The subscribe flow (Pattern B: sign-up = start trial)

```
Sign-up form submitted
  → Keycloak user created (existing flow), session established
  → FE calls  POST /api/billing/checkout      (Bearer token)
  → FE redirects browser to the returned Stripe `url`

[user pays / starts trial on Stripe's hosted page]

  → Stripe redirects to your success_url  (e.g. /success)
  → /success route:
       is the Keycloak session still valid?
         no  → send to login, then back to /success
         yes → POST /api/billing/sync          (Bearer token)
             → GET  /api/billing/subscription   → render state → into the app
  → if the user abandons checkout, Stripe redirects to cancel_url
    (e.g. /pricing) — show a "no worries, try again" state
```

Configured server-side (you don't send these, but so you know where the browser lands):
- **success_url** → `/success` (dev default `http://localhost:3000/success`)
- **cancel_url** → `/pricing` (dev default `http://localhost:3000/pricing`)
- **portal return_url** → `/account` (dev default `http://localhost:3000/account`)

Confirm these targets with the backend agent and keep them in sync with the actual FE routes.

## Notes for the frontend

- **Never send `userId` or `customerReference`** in any request — the server resolves the customer from the token. A client-supplied id would be rejected/ignored by design.
- Gate premium UI off `GET /api/billing/subscription` (the access rule above). Backend endpoints that require a subscription independently return **402 Payment Required** if you call them without one — handle 402 by routing to the pricing/subscribe screen.
- After returning from the Billing Portal (cancel, resume, card update), re-fetch `GET /api/billing/subscription` to reflect the change — the portal itself doesn't call back into the FE.
- State is eventually consistent with Stripe via webhooks, but the `/success` `sync` call makes the post-checkout read immediately correct. Elsewhere, a brief lag after a portal action is possible; re-fetching resolves it.

## Failure modes & how the frontend detects them

There is **no push channel** to the frontend and **no separate "payment failed" signal**. Every outcome — success, decline, abandonment, cancellation — is folded into `SubscriptionState.status`, read from `GET /api/billing/subscription`. The Keycloak account and the subscription are independent: **a failed or absent payment never affects login** — the user can still authenticate; only `status` changes. "Does this user have a valid membership?" is always answered by the read model, never by the auth session.

The FE learns the current truth at three moments:
1. **Right after checkout** — the `/success` route calls `POST /api/billing/sync` (authoritative re-fetch from Stripe), then reads `GET /api/billing/subscription`. No webhook race.
2. **On every app entry / route guard** — read `GET /api/billing/subscription` and gate on `status`.
3. **When calling a premium endpoint without entitlement** — the backend returns **402 Payment Required**; treat as "route to pricing / fix payment".

### Pattern B scenarios (Keycloak user created *before* payment)

An account can exist with no valid membership — this is expected; handle it, don't try to prevent it.

| Scenario | In Stripe | `status` the FE sees | FE behaviour |
|---|---|---|---|
| Abandons checkout (closes tab / back) | No subscription created; redirect to `cancel_url` | `none` | Land on `cancel_url` (e.g. `/pricing`) with "no worries, try again"; on later login, route to pricing/resume — **not** the app |
| Card declined *during* checkout | Checkout can't complete | `none` (until they succeed) | Same as abandon |
| Completes trial signup | `trialing` | `trialing` | Full access; optionally show trial end (`currentPeriodEnd`) |
| Trial ends, first charge fails | `past_due` → dunning → `canceled` | `past_due` | **Soft-grace**: keep access + "update payment method" banner → `POST /api/billing/portal` |
| Payment needs SCA/3DS | `incomplete` / action required | `incomplete` (or `past_due`) | Prompt to complete payment via the portal |
| Cancels via portal (at period end) | `cancel_at_period_end: true`, still `active` | `active` + `cancelAtPeriodEnd: true` | "Canceling — access until `currentPeriodEnd`"; keep access |
| Cancels immediately / sub deleted | `canceled` | `canceled` | No access → Subscribe / reactivate CTA |
| Renewal permanently fails | `unpaid` / `canceled` / `paused` | that value | No access → Subscribe / reactivate CTA |

### The orphaned-account rule

Gate **app entry** on `status`, not on having a Keycloak session:
- `trialing` / `active` (incl. `cancelAtPeriodEnd: true`) → into the app.
- `past_due` → into the app **+ banner** (soft-grace; the backend also still grants access here).
- everything else (`none`, `incomplete`, `unpaid`, `canceled`, `paused`) → pricing / resume-checkout screen.

A logged-in user with `none` / `canceled` is normal (orphaned or lapsed) — send them to pricing and let them re-run `POST /api/billing/checkout`. It **reuses their existing Stripe customer**, so retrying never creates a duplicate.

### Timing / consistency caveats

- Away from `/success`, `status` is **eventually consistent** — after a portal action or a background payment failure there's a few-seconds lag until the inbox poller syncs. Re-fetch after returning from the portal; consider refetch-on-focus (or a light poll) for transitions that happen while the app is open (e.g. `trialing` → `past_due`).
- The `/success` `sync` is authoritative *at that instant*, but a payment may still be **processing** (async methods) or **pending SCA** — so `/success` can legitimately show `incomplete` / `past_due`, not just `active`. Handle a non-active landing gracefully ("payment processing / action needed"); don't assume success.

### Remediation path

For any payment problem (`past_due`, `incomplete`), the fix is **`POST /api/billing/portal`** → Stripe's hosted portal (update card, retry payment). The contract intentionally does not expose decline reasons. *(Optional enhancement for a one-click "complete payment": the backend could surface the latest open invoice's `hosted_invoice_url` — ask if you want it.)*

## Open items / may still change

- Account-page copy for `past_due` (soft-grace banner vs. hard lock) is a product decision — currently access is granted during `past_due`.
- Email notifications (trial ending, payment failed) are handled by **Stripe's own emails** for now; the app does not send billing emails yet.
- Plan switching / multiple tiers: a single Price is wired today. Say if you need a tier selector on the pricing page.
