# RIGX — Architecture

Status: **current state** (what exists today) + **target** (what we are moving to). The roadmap (`05-roadmap.md`) lists the steps between them.

## 1. Shape of the system

```
apps/web            Next.js (App Router, TypeScript strict)   ← rebuilt from the design canvas (phase 4)
src/Host            ASP.NET Core minimal API host (composition root only)
src/Modules/*       Feature modules — each owns its domain, use cases, persistence, endpoints
src/Common/*        Cross-cutting building blocks used by every module
tests/*             xUnit + Testcontainers (real PostgreSQL); Playwright e2e lives in apps/web
PostgreSQL 17       One database, one schema per module
```

**Style: modular monolith.** One deployable, strong module boundaries. Chosen because the team is one owner + AI engineers: microservices would add operations cost with no benefit at this scale. Any module can be extracted later because modules never share tables.

## 2. Module map

| Module | Owns | Status |
|---|---|---|
| Identity | users, sessions, roles/permissions, email code sign-in, Google sign-in (ID token), step-up codes, new-device alerts, profile, addresses | exists — done in 2.1; passwords removed |
| Catalog | product models, category tree (code, menu/builder flags, inherited spec schema), brands, media | exists — `Features/Products`, `Features/Taxonomy` (+ `CategorySpecService`), rules in `Domain/CategorySpecSchema`; variants removed (D7) |
| Inventory | stock items (`RX-xxxx`) and their units, warehouses, stock states, hidden/visible | stock items + units live in Retail (`Domain/StockEntities`, `Features/StockItems`, `Features/Inventory`); move here later |
| Purchasing | sellers, purchases (`PO-xxxx`, lines, seller payment) | rebuilt in 2.2c (`Domain/ Contracts/ Features/Sellers, Purchases/ Persistence/`); receiving is orchestrated in Host (`PurchaseReceiptService`) |
| Assemblies (new) | builds: card (usage, sell mode, warranty, copies, "suitable for", photos), parts (price, warranty override), swaps, allocation | in Retail: rules in `Domain/AssemblyRules`, `Features/Assemblies` (`AssemblyService` card/publish/list, `AssemblyPartService` add/new/edit/remove/replace) → extract |
| Sales | orders, reservation, payments, POS, returns, blacklist, tracking | in Retail: rules in `Domain/OrderRules`, `Features/Sales` (checkout, POS, status/stock claim, payments, fulfillment + reservation expiry, tracking, blacklist, sold feed); warranty in `Domain/WarrantyRules`, `Features/Warranty` → extract |
| Partners (new) | agreements, per-unit overrides, earnings, settlements | logic in `Retail/Features/Partners` → extract |
| Financials | expenses, business reports | no project yet; logic in `Retail/Features/Financials` → extract |
| Invoicing | online invoices, quote images/PDF | in Retail: rules in `Domain/InvoiceRules`, `Features/Invoicing` (`InvoiceRegistry`, `InvoiceService`, `QuoteService`) → extract |
| Pricing (new) | offers, coupons, context pricing, customer tiers | offers partly in Catalog — consolidate |
| PcBuilder | saved builds (`saved_builds`, link token, optional owner) + builder rules in `Domain/BuilderRules` (slots from categories, compatibility, power); PC-builder templates removed (M32). Orchestration (catalog loader, options, check, quote, saved builds, from-build) in Host `Builder/` (decision 11); stock lookup in Retail `Features/StockItems/BuilderItemService` | exists — 2.13 |
| Storefront | settings, content, articles, FAQ, videos, requests inbox | exists — content split into `Features/Content` (articles, FAQ, policies); settings v2 in `Domain/Settings` + `Features/Settings` (2.17); requests inbox in `Domain/Requests` + `Features/Requests` + `Contracts/RequestContracts.cs` (2.12, replaces contact messages); public endpoint and admin routes in Host (`RequestEndpoints.cs`, `Requests/`) |
| Messaging (new) | email outbox, subscriptions, campaigns, WhatsApp queue | to build |
| Auditing | audit events (before/after), trash | exists (security events) — extend to full audit log |
| System (new) | backups, export, data reset | to build |

### Retail module today (step 1.4)
Retail still owns one schema and one `RetailDbContext` for units, builds, orders, partners and expenses, so it was split **inside** the module rather than into the empty Sales/Financials modules (moving code there would mean sharing tables across modules). Layout:

```
Modules/Retail/
  Domain/        entities per area, status constants, RetailException, AllocationMath (split/rounding rules)
  Contracts/     request/view records per area (public API shapes)
  Features/      Inventory · Assemblies · Sales (orders, status, payments, stock claim, returns) · Partners (agreements, allocations, ledger, earnings posting) · Financials (expenses, report)
  Persistence/   RetailDbContext, RetailQueries (shared includes, order lock, rollback), Migrations
  RetailModule.cs  DI: one scoped service per feature area
```

All files keep the `HardwareStore.Modules.Retail` namespace until each area moves to its own module. Retail HTTP handlers stay in Host (`Host/HardwareStore.Api/Retail/RetailEndpoints.*.cs`, one partial file per area): they need Identity's `RequirePermission`, and several orchestrate Catalog/Purchasing/Storefront (decision 11).

## 3. Inside a module (target)

```
Modules/Sales/
  Domain/          entities, value objects, status rules (no EF, no HTTP)
  Features/        one folder per use case: ConfirmOrder/ {Request, Handler, Validator, Endpoint}
  Persistence/     DbContext, EF configurations, migrations (schema "sales")
  Contracts/       public DTOs + interfaces other modules may call
  SalesModule.cs   DI registration + endpoint mapping
```

Rules:
- **Vertical slices** (feature folders). A use case is found in one folder, not spread across a 1,400-line service.
- Other modules talk to a module **only through its `Contracts`** (interfaces/DTOs). No cross-module DbContext or table access.
- Endpoints live **in the module**, not in Host. Host only composes (`AddSalesModule()`, `MapSalesEndpoints()`).
- Domain rules (status transitions, price floors, allocation = 100%) live in Domain, so they're testable without a database.

## 4. Cross-cutting (written once in `Common`)

| Concern | Mechanism |
|---|---|
| Errors | Every expected business failure derives from `Common.Domain.AppException` (status code + message + optional field errors). `ProblemDetailsExceptionHandler` maps it once, globally. **Endpoints don't try/catch business errors.** |
| Validation | FluentValidation per request, run by one endpoint filter |
| Authorization | permission constants + `RequirePermission(...)` endpoint filter; groups per area |
| Audit log | EF `SaveChanges` interceptor writes actor, time, entity, before/after JSON |
| Trash | `ISoftDeletable` + global query filter; purge job after 30 days |
| Concurrency | `row_version` token; conflict → `409` |
| Time | `TimeProvider` everywhere (no `DateTime.UtcNow`) |
| Paging/filtering | shared `PageRequest`/`PagedResult<T>` |
| Background work | hosted job runner (backups, reminders, reservation expiry, outbox) |
| Email | transactional outbox table → sender job (retries, no lost email) |
| Media | storage abstraction (local volume now, S3-compatible later) + auto-watermark on upload |
| Logging | Serilog structured logs with correlation id |

## 5. Data
- PostgreSQL 17, EF Core, **one schema per module**, migrations per module.
- Money: `numeric(12,2)` in EGP. Never `float`.
- Statuses: enums/smart constants in Domain, stored as strings.
- Inventory integrity: stock claimed at `Confirmed` inside a transaction with `FOR UPDATE SKIP LOCKED` (already implemented and tested).
- Migrations: `DatabaseMigrator` is the single ordered list of module contexts; run with `dotnet run --project src/Host/HardwareStore.Api -- --migrate`.

## 6. API
- REST under `/api/v1`, admin under `/api/v1/admin`.
- OpenAPI document generated from code; the frontend TypeScript client is **generated** from it (no hand-written duplicate types).
- Cookie auth (HttpOnly, `__Host-`, `SameSite=Lax`) + CSRF token for unsafe methods.
- Public DTOs never contain cost, supplier, serial, partner, or private user data. Exception: the invoice link `/i/{token}` is a customer document (name, address, serials), reachable only by its unguessable token (decision 22).

## 7. Frontend (target)
- Next.js App Router, TypeScript `strict`, server components for catalog pages (SEO), client components only where interaction needs it.
- Design tokens from the canvas (carbon/paper/sand/orange, IBM Plex Sans Arabic + Plex Mono); one component library; RTL first, English via `next-intl`.
- Generated API client + one fetch wrapper (CSRF, errors).

## 8. Decisions log
| # | Decision | Why |
|---|---|---|
| 1 | Modular monolith on .NET 10 | single owner, low ops cost, clean extraction path |
| 2 | PostgreSQL + EF Core, schema per module | relational money/stock data, real transactions and row locks |
| 3 | Email OTP + Google only, no passwords, no SMS | no paid SMS, no password resets/leaks |
| 4 | Cookie sessions, not JWT in browser storage | HttpOnly cookies can't be stolen by XSS; server-side revocation |
| 5 | Stock reserved only at `Confirmed` | avoid fake reservations from abandoned carts |
| 6 | Online invoices only | owner has no printer; legal invoice profile not required now |
| 7 | WhatsApp via prepared `wa.me` links | official bulk API is paid; can be swapped later behind `Messaging` |
| 8 | Frontend rebuilt from scratch | old UI did not match the approved design |
| 9 | Vertical-slice features inside modules | small files, one place per use case, easy to fix |
| 10 | Business errors = `AppException` subclasses mapped globally (not `Result<T>`) | the existing services already throw typed errors; one base type removes ~130 duplicated try/catch blocks without rewriting every service |
| 11 | Endpoints that orchestrate several modules stay in Host; single-module endpoints move into their module when it is restructured | avoids cross-module references just to relocate files |
| 12 | Endpoints stay in Host for now, even single-module ones; the admin dashboard (`AdminAnalyticsService`) stays in Host | every endpoint needs Identity's `RequirePermission`, so moving them would make each module reference Identity; the dashboard reads six modules (catalog, purchasing, identity, content, inventory, audit) and holds no money data, so it is composition, not Financials. Revisit when modules expose `Contracts` (Phase 2) |
| 13 | Module projects are created when their first feature lands (no empty placeholder projects) | an empty project is dead code; creating one takes a minute |
| 14 | Gap-audit decisions D1–D8 approved by the owner (13 Nov 2026), see `07-gap-audit.md` §6: RX code per used piece / per new lot; customer sees received→confirmed→preparing→shipped→delivered (+cancelled), "completed" is internal; money accounts (cash/InstaPay/wallet) with opening balances; partner "whole shop" basis = % of monthly net profit after expenses; editable lead-source and warranty-option lists; remove warehouses, supplier bank data, PC-builder templates, variants, video file hosting, in-app notifications (reviews stay); product Q&A is owner-written | needed to model Phase 2 data correctly |
| 15 | Google uses the ID-token flow (client id only, no secret) | the frontend gets the ID token from Google's button; the server checks signature, issuer, audience and lifetime with `Identity:Google:ClientId` only, so no client secret is stored or rotated |
| 16 | Store settings = one row, one `jsonb` column per section (12 sections), each section a record with its defaults and its own `Normalize()` rules; admin replaces one section at a time (`PUT /api/v1/admin/storefront/settings/{section}`) guarded by one row version; shipping fee and upfront/deposit rules live in the section records and are served by `GET /api/v1/storefront/checkout-quote` and `GET /api/v1/storefront/shipping-rates` | one place per rule, defaults in code (`{}` = defaults), later steps (2.4, 2.6, 2.7, 2.9, 2.12) read typed values; one owner editing makes a shared row version enough |
| 17 | Storefront sells **stock items**, not catalog products: `StockItem` (RX code, listing fields, prices, photos) owns 1 unit (used) or N units (new lot); units keep cost/serial/partner/status. Public codes RX/BLD/PO come from one PostgreSQL sequence each, formatted by `Common.Domain.PublicCode`; free-text model names match through `ModelName.Key` | owner decisions S1/S2/D1; one listing per page with real photos; codes are short, readable and never reused |
| 18 | Category spec schema = jsonb definitions per category, inherited root→leaf (child overrides by key); item specs stay optional and are checked against the effective schema in Host before reaching Retail; storefront spec filters use jsonb containment | one place for filter + compatibility data without a cross-module reference |
| 19 | Build warranty = one label from the settings list applied to every part; a part override wins; no build warranty ("per part") falls back to the stock item's own. Each part keeps a `PartPrice` (default = item in-build price) for split sales; adding a part resets the revenue split to the default (alone prices), removing rebalances the rest, a replacement takes the old part's share. "Add a part not in stock" creates a single-piece RX item straight into the build. `Copies` is a declared count; how extra copies are sold is settled in 2.4 | one place per rule (`AssemblyRules`, `AssemblyEditing`); the owner's design (ADM-Build) drives the fields |
| 20 | Checkout and POS share one pricing path (`OrderDrafting`) over stock items and published builds; catalog-product order lines removed. Reservation starts at `Confirmed` with settings defaults (24h) and a per-order expiry action (notify only, or release). A build with copies > 1 refills a sold part from an available unit of the same RX item. Order emails go through the existing email outbox (new kind) until Messaging exists. Tracking by an unguessable token. Blacklist flags orders and gates confirmation on full prepayment | owner decisions (13 Nov 2026) + design boards ADM-POS, ADM-Orders, D-Checkout, X-Track, X-Sold |
| 21 | Warranty is snapshotted per sold unit at claim (part override → build → item) and registered at delivery (start = delivery, which the 2.6 invoice date will equal); status is computed, never stored; the public lookup is one box parsed by prefix (`RX-`, `RIGX-`, digits, else serial), masked, rate-limited | one record per unit with the terms it was sold on; no stale status flags; one search box matches how customers ask |
| 22 | Invoice issued at delivery in the same transaction as the warranties (invoice date = warranty start); number `{prefix}-{YYYY}-{NNNN}` from a per-year counter table (gap-free, Africa/Cairo year); one invoice per order; kind (Build / Parts) and status (Void when cancelled, else Issued) computed; content read live from the order; seller = staff who marked it delivered (snapshot); public link `/i/{token}` with a 32-byte unguessable token; backend serves data only, the frontend renders the page, watermark, warranty QR and PDF/PNG (no PDF/QR libraries on the server) | owner has no printer; the invoice always matches the order; the unguessable link is the access control; rendering belongs to the frontend |
| 23 | Upfront (shipping fee + deposit) is computed from settings at checkout and stored on the order, with a snapshot of the fee and delivery time (from the governorate group or the default); a delivery order cannot move to `Preparing` until its upfront is collected (409); `UpfrontDue` is 0 when paid or waived; POS and pickup orders have no upfront. Host bridges Storefront shipping settings into Retail through `ShippingRate` in `CheckoutPolicy` and delegates; Retail never references Storefront | owner decision (shipping paid upfront, deposit rule); a later settings change never alters an order already sold; one rule in `OrderRules`, one bridge in Host |
| 24 | One requests inbox (`customer_requests`, Storefront) for sell, trade-in, upgrade, notify-me, support, contact and service bookings; `contact_messages` migrated into it as type Contact, then dropped. Each request has a public code `REQ-xxxx` (`PublicCode` prefix REQ, own sequence). Statuses New / Following / Offered / Accepted / Rejected / Converted / Closed, rules in `CustomerRequestRules` (offer and decision only for sell / trade-in / upgrade; convert only accepted sell or trade-in). Conversion to a draft purchase is orchestrated in Host (`RequestPurchaseConversion`) and marks the request Converted with a conditional update. Egyptian mobile normalized by the shared `Common.Domain.EgyptMobile` (also used by Purchasing) | one table and one status flow for every customer request; replaces contact messages without a second inbox; the REQ code is what the customer quotes; conversion crosses Storefront and Purchasing, so it stays in Host (decision 11); one phone rule for all modules |
| 25 | Free builder over **published stock items**. Slots are categories with "show in builder" + code (CPU, BOARD, …); a part's slot is its item's category. Compatibility comes from category specs: a shared key must share a value (lists split by `,` `/` `|`), `max_x` on one part limits `x` on others, missing values never conflict. Power = sum of `power_w` × quantity excluding PSU, against PSU `power_w` (over 100% conflict, over 80% warning). Every part is priced at its in-build price when the build has a processor and a board, else at its alone price (`OrderPriceOption.InBuild`; online needs CPU + BOARD, enforced by `OrderRules.EnsureBuildPrice`). Saved builds are reached by a random link token: anyone can save, only a signed-in saver can update or delete. "Edit build" re-picks the same models from free stock. PC-builder templates removed (M32) | the owner's category specs drive compatibility, so new parts need no code; one price rule for builds (owner decision, 13 Nov 2026); links make builds shareable without accounts; the fixed-option templates duplicated what categories now define |
