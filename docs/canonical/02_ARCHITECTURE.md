# RIGX — Architecture, Modules, API and Database Map

## Runtime architecture
- **Host**: `src/Host/HardwareStore.Api` — minimal APIs, ProblemDetails, Serilog, health checks, rate limiting, CSRF, permission authorization.
- **Modules** (`src/Modules`): Auditing, Catalog, Identity, Inventory, PcBuilder, Purchasing, Retail, Storefront. `Financials`, `Invoicing`, `Sales` exist only as assembly markers.
- **Common**: `Common.Domain`, `Common.Application`, `Common.Infrastructure` (shared persistence primitives).
- **Frontend**: `apps/web` Next.js App Router, proxying `/api` to the API.

## Module responsibilities
| Module | Responsibility | Main storage |
|---|---|---|
| Identity | Accounts, sessions, roles/permissions, OTP, addresses, profile avatars, admin users | `identity.*` tables via `IdentityDbContext` |
| Catalog | Products, variants, categories, brands, media metadata, reviews/offers adjacency | `CatalogDbContext` |
| Inventory | Warehouses; inventory units are owned by Retail in the current schema | `inventory_*` |
| Retail | Units, receipts, assemblies, orders, payments, returns, partners, allocations, expenses, reports | `RetailDbContext` |
| Purchasing | Suppliers, supplier products, purchase drafts, receipts | `PurchasingDbContext` |
| PcBuilder | Templates, slots, options, compatibility evaluation | `PcBuilderDbContext` |
| Storefront | Settings, homepage layout, articles, FAQs, policies, videos, media, contact | `StorefrontDbContext` |
| Auditing | Security/audit events | `AuditingDbContext` |

## API surface
- 210 `MapGet/MapPost/MapPut/MapDelete/MapPatch` calls across 30 endpoint files.
- Public: catalog, storefront content, contact, builds, PC Builder evaluation, auth entry points.
- Customer: `/api/v1/orders`, `/orders/mine`, addresses, wishlist, reviews, sessions, notifications.
- Admin: `/api/v1/admin/*`, permission-gated (`RequirePermission`) per area.
- Contract detail: `docs/api/BACKEND_UI_CONTRACTS.md`; traceability: `docs/api/BACKEND_UI_TRACEABILITY.md`.

## Database
- EF Core + Npgsql. Separate DbContexts per module; no cross-context navigation.
- Migrations directories: `src/Modules/{Auditing,Catalog,Identity,Inventory,PcBuilder,Purchasing,Retail,Storefront}/**/Migrations`.
- Concurrency: `row_version` concurrency tokens on aggregate tables; order confirmation uses explicit PostgreSQL row locks.
- Inventory integrity: order creation does **not** reserve stock; confirmation claims stock under transaction (`FOR UPDATE SKIP LOCKED` for product units, assembly row lock for builds).
- ERD and relationships: `docs/database/DATABASE_DESIGN_ERD.md`.

## Key architectural decisions
See `docs/adr/ARCHITECTURE_DECISION_RECORDS.md` and `08_ADR_INDEX.md`.
