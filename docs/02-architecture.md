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
| Identity | users, sessions, roles/permissions, email OTP, profile, addresses | exists — password login to be removed, Google to be added |
| Catalog | products, variants, category tree, brands, specs, media | exists — `CatalogApplication.cs` too large, split planned |
| Inventory | units (`RX-xxxx`), warehouses, stock states, hidden/visible | exists — units currently live in Retail; move here |
| Purchasing | suppliers/sellers, purchase drafts, receipts | exists |
| Assemblies (new) | builds: components, swaps, warranty, allocation | logic in `RetailService` → extract |
| Sales | orders, reservation, payments, POS, returns, blacklist, tracking | module is empty; logic in `RetailService` → extract |
| Partners (new) | agreements, per-unit overrides, earnings, settlements | logic in `RetailService` → extract |
| Financials | expenses, business reports | empty module; logic in `RetailService` → extract |
| Invoicing | online invoices, quote images/PDF | empty — to build |
| Pricing (new) | offers, coupons, context pricing, customer tiers | offers partly in Catalog — consolidate |
| PcBuilder | templates, slots, compatibility | exists |
| Storefront | settings, content, articles, FAQ, videos, contact | exists — `ContentServices.cs` large, split planned |
| Messaging (new) | email outbox, subscriptions, campaigns, WhatsApp queue | to build |
| Auditing | audit events (before/after), trash | exists (security events) — extend to full audit log |
| System (new) | backups, export, data reset | to build |

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
| Errors | `Result<T>` + typed `Error` (Validation, NotFound, Conflict, Forbidden) → one mapper to `ProblemDetails`. No exceptions for expected business outcomes. |
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
- Migrations are applied by a dedicated startup step/CLI command (today they only run in the removed "preview" mode — to fix in phase 0/1).

## 6. API
- REST under `/api/v1`, admin under `/api/v1/admin`.
- OpenAPI document generated from code; the frontend TypeScript client is **generated** from it (no hand-written duplicate types).
- Cookie auth (HttpOnly, `__Host-`, `SameSite=Lax`) + CSRF token for unsafe methods.
- Public DTOs never contain cost, supplier, serial, partner, or private user data.

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
