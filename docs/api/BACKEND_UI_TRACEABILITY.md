# RIGX — Backend ↔ UI Traceability (10–11 November 2026)

Purpose: map approved customer/Admin workflows to the actual backend implementation. “Implemented” means routes + DTOs + authorization + tests exist. “Partial” means a usable backend exists but the full approved capability is not complete. “Blocked” means implementation depends on an owner/legal/external decision.

## 1. Storefront and customer workflows

| Workflow | Status | Actual backend | Evidence / gap |
|---|---|---|---|
| Product catalog, categories, brands, filters, details | Implemented | `GET /api/v1/catalog/categories`, `/brands`, `/specification-filters`, `/products`, `/products/{slug}` | Catalog PostgreSQL tests; public DTOs exclude cost/source/serial |
| Product search by name/SKU/brand/category | Implemented | `GET /api/v1/catalog/products?search=` | Name/SKU already existed; brand/category matching added this round |
| Search autocomplete/suggestions | Implemented | `GET /api/v1/catalog/search/suggestions?q=&limit=` | Bounded 1–10, 80-char term, 120/min/IP, public fields only; test added |
| PC builds/templates | Implemented (practical V1) | `GET /api/v1/pc-builder/templates`, `/{id}`, `POST /{id}/evaluate`; admin CRUD/publish | Simple Socket/DDR rules and Unconfirmed when data incomplete; no BIOS/slot-count engine |
| Cart | Frontend localStorage | Backend does not own cart; checkout carries lines | No server cart/reservation before order |
| Checkout | Implemented | `POST /api/v1/orders` authenticated; server resolves prices | Real order, stock claim on `Confirmed`; no guest checkout |
| Customer registration | Implemented | `POST /api/v1/identity/registrations` | Email verification link; rate limited |
| Password login | Implemented | `POST /api/v1/identity/sessions` | 5/IP/min preserved |
| Email OTP login | Implemented | `/api/v1/identity/email-otp/login/request|verify` | 6 digits, 5 min, 5 attempts, encrypted, generic responses |
| Password reset (link + OTP) | Implemented | `/password-reset-requests`, `/password-resets`, `/password-reset/otp/*` | OTP reset revokes sessions |
| Password change while signed in | Partial | `POST /api/v1/identity/password-changes` | Current password flow exists; fresh Email OTP step-up is not enforced/document-linked yet |
| Google OIDC | Blocked | None | Requires client ID/secret and approved redirect domain; no fake endpoint/button |
| Customer account/profile/avatar/addresses/wishlist/notifications | Implemented | `/identity/me`, `/profile`, `/profile/avatar`, `/identity/addresses`, `/wishlist`, `/notifications` | Authorization required; IDOR-safe |
| Customer order history | Implemented | `GET /api/v1/orders/mine` | Own orders only |
| Customer order detail | Implemented (new) | `GET /api/v1/orders/mine/{id}` | 404 for another customer; test added |
| Customer-visible status information | Implemented | Order `status`, `paymentStatus`, `events` | No internal notes in public/customer fields |

## 2. Admin and business workflows

| Workflow | Status | Actual backend | Evidence / gap |
|---|---|---|---|
| Order list | Implemented | `GET /admin/retail/orders` | Contact masked without `Sales.ViewCustomerContact` |
| Order detail incl. components | Implemented (new) | `GET /admin/retail/orders/{id}` | Returns order + unit components; serial only with `Inventory.ViewSerials`; test added |
| Status transitions | Implemented | `POST /orders/{id}/status` | New→Confirmed→Preparing→Ready→Delivered; cancel before completion |
| Completion | Implemented | `POST /orders/{id}/complete` `Sales.CompleteOrder` | Delivered + Paid/valid Waived only |
| Payment confirmation/waiver | Implemented | `POST /orders/{id}/payment`, `/payment/waive` | Waived stores actor/time/reason; never Paid |
| Manual/phone orders | Implemented | `POST /admin/retail/orders` `Sales.CreatePOS` | Server resolves published product/build prices |
| Stock reservation | Implemented | Claim on `Confirmed` only | Insufficient stock → 409 without partial deduction |
| Individual unit inventory | Implemented | Units + receipt from purchase draft | Snapshot fields + idempotent receipt |
| Assembly create/update/publish/swap/remove/disassemble | Implemented | `/admin/retail/assemblies*` | Atomic unit links; no duplicate stock |
| Sell individual unit / complete assembly | Implemented | Orders by product or assembly | Assembly consumes components once |
| Sell selected assembly components | Partial | Must disassemble or remove units first | No single endpoint sells a subset atomically; can be done safely via disassemble then order |
| Unit archive/retire | Missing | Statuses include Scrapped/PendingInspection only | No admin API to archive an available unit without a return/scrap workflow |
| Returns workflow | Implemented | `/admin/retail/returns*` | PendingInspection → Inspected/Rejected; legal return fee/window intentionally absent |
| Partner allocations | Implemented | `/partner/allocations*` | Unit→Product→Category→Default; audit/concurrency/100% cap |
| Partner earnings/ledger/settlements | Implemented | `/partner/earnings`, `/adjustments`, `/ledger`, `/settlements` | Per-unit snapshots; post-payout recovery offsets |
| Direct expense vs overhead | Implemented | `/admin/retail/expenses` | Classification required; report keeps overhead separate |
| Monthly/yearly report | Implemented (backend) | `GET /admin/retail/reports/business?from=&to=` | Period based on completed order last-update; tested reconciliation |
| Contact inbox | Implemented (backend) | `POST /api/v1/contact`, admin `/admin/contact-messages` | Honeypot/dedupe/rate limit; SMTP notification pending |
| Reviews + moderation | Implemented | Public product/store reviews; `/reviews/mine`; `/admin/reviews` | Catalog.ManageProducts moderation |
| YouTube videos + article refs | Implemented | `/admin/media/videos/youtube`, `/content/videos`; article video APIs | Strict URL validation; unpublish guard; uploaded video preserved |
| Offers | Partial | Public `/content/offers`; admin CRUD/publish/cancel | No quantity caps or checkout price application; owner pricing rule missing |
| Permissions/roles/users | Implemented | `/admin/identity/users|roles|permissions` | Owner/last-owner protections exist |
| Audit history | Partial | `GET /admin/audit/events` | Structured metadata exists; no before/after diff UI contract or generic undo |
| Invoice/quotation PDF | Blocked | None | Legal seller identity/ETA applicability missing; no fake PDF |
| Admin MFA/step-up | Blocked | None | Requires enforcement/recovery policy |
| Sitemap/SEO HTML | Frontend-owned | Backend exposes public data | `NEXT_PUBLIC_SITE_URL` required in production; sitemap component frontend-owned |

## 3. Security and invariants verified
- Inventory/assembly/order/partner/return tests cover no double sale, atomic confirmation, per-unit costs, multi-partner percentages ≤100%, post-payout recovery and append-only ledger behavior.
- Authorization matrix proves `Finance.ViewReports` cannot write partner data and `Inventory.View` masks costs/serials.
- Customer order detail is ownership-scoped (`404` for another account).
- Admin order component serials are permission-gated.
- Contact is bounded/deduplicated; catalog suggestions are bounded/rate-limited and public-only.
- No acquisition cost, supplier, serial, partner amount or internal note is exposed in public catalog/builds/search/video DTOs.

## 4. Owner/external gates
1. Google OAuth client credentials + redirect domain.
2. Admin MFA enforcement and Owner recovery policy.
3. Legal seller/tax identity and ETA applicability for invoices.
4. Offers quantity/discount policy and checkout application rules.
5. Domain/hosting/TLS/SMTP/off-host backup.
6. Return window/voluntary fee policy (deliberately not implemented).
