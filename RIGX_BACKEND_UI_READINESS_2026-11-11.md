# RIGX — Backend UI Readiness & API Contract Finalization (11 November 2026)

Backend-only review. No Frontend, React, Next.js page, component, CSS, navigation, shared UI or translation file was modified.

## 1. Verified baseline
- Workspace: `DEV - Copy\HARDWARE STORE`.
- API routes were enumerated from `src/Host/HardwareStore.Api`; DTOs, permissions, CSRF and rate limits were checked against the actual handlers.
- No new database migration was required in this slice. Existing QA migrations from the prior backend slice already applied: Retail `M5_PartnerAllocationAudit`, Retail `M6_ExpensesAndReports`, Storefront `M9_ContactMessages`, Storefront `M11_ArticleVideoReferences`.

## 2. Backend gaps found and closed
### 2.1 Search suggestions
- New public endpoint `GET /api/v1/catalog/search/suggestions?q=&limit=`.
- Matches product name, SKU, brand name and category name.
- Bounded: term ≤80 chars, limit 1–10, 120 requests/min/IP.
- Returns only public fields: `id, name, slug, sku, basePrice, condition, categorySlug, brandName, thumbnailStorageKey`.
- No acquisition cost, supplier, serial, partner or internal data.
- Catalog PostgreSQL test added and passing.

### 2.2 Customer order detail
- New `GET /api/v1/orders/mine/{id}`.
- Returns `OrderView` only when the authenticated customer owns the order; another customer gets `404`.
- Test covers owner access and foreign-account denial.

### 2.3 Admin order detail with components
- New `GET /api/v1/admin/retail/orders/{id}` (`Sales.ViewOrders`).
- Returns `AdminOrderDetailView { order, units }` where units include physical unit name, SKU, condition, inspection condition, status and unit/product ids.
- `serialNumber` is returned only with `Inventory.ViewSerials`; otherwise null.
- Test covers serial visibility and masking.

### 2.4 Search scope correction
- Catalog product search now also matches brand and category names in addition to the previous name/SKU matching.

## 3. Contract and traceability documents
- Corrected `docs/api/BACKEND_UI_CONTRACTS.md`:
  - Added missing endpoint families: identity/account, admin catalog/taxonomy/media, content CMS, reviews/offers/PC Builder, purchasing/suppliers, warehouses, analytics/audit, identity administration.
  - Added exact new routes for search suggestions and order detail.
  - Documented permissions, rate limits, CSRF, concurrency/RowVersion, role-gated fields and error/status behavior.
  - Corrected prior omissions; no invented routes.
- Added `docs/api/BACKEND_UI_TRACEABILITY.md`:
  - Workflow-by-workflow backend status: implemented, partial, missing, blocked.
  - Explicit references to backend routes/tests and frontend-owned areas.

## 4. Verified financial/security invariants
Existing tests plus targeted review confirm:
- Stock is claimed atomically only on `Confirmed`; insufficient stock fails without partial deduction.
- Assembly/component transitions are atomic; a unit cannot belong to two assemblies or be sold twice.
- Server resolves customer/manual order prices; client prices are not trusted.
- Partner active allocation percentages per effective scope cannot exceed 100%; precedence and snapshots are tested.
- Returns reverse unpaid earnings or create per-partner recoverable balances; history is append-only.
- General overhead is separate from item profit and partner share in the business report.
- Cost/source/serial/customer contact fields are permission-gated; public search/builds/catalog DTOs exclude them.
- Allocation and order mutations use RowVersion; allocation/deactivation writes append audit events.
- Customer order detail is IDOR-safe.

## 5. Test evidence
| Command / suite | Result |
|---|---|
| `dotnet build HARDWARE_STORE.slnx` | 0 errors |
| Full backend xUnit/Testcontainers | **189/189 passed, 0 skipped** |
| Retail PostgreSQL | **15/15** |
| Catalog PostgreSQL | **8/8** |
| Authorization matrix | **2/2** |
| Contact | **1/1** |
| Article video | **1/1** |
| Email OTP / YouTube parser / prior slices | still green |

No frontend build/test was run because no frontend file changed. The added APIs are additive; existing UI contracts remain valid.

## 6. Implemented / partial / missing / blocked
### Implemented
- Catalog/category/brand/product/filter/detail, search and suggestions.
- PC build templates with practical Socket/DDR checks and Unconfirmed fallback.
- Cart handoff (backend checkout with server pricing), customer/manual orders, builds purchase.
- Password login, Email OTP login/reset, registration, sessions, addresses, wishlist, notifications, profile/avatar.
- Customer order list/detail; admin order list/detail/status/complete/payment/waive/cancel.
- Individual units, assemblies, atomic buy/swap/disassemble, returns/inspection.
- Partner allocations, earnings, ledger, settlements, per-partner recovery.
- Expenses/reports, contact inbox, reviews/moderation, YouTube/article video refs.
- Roles/permissions, owner protections, audit query, analytics.

### Partial
- Password change has current-password verification but no fresh Email OTP step-up yet.
- Assembly component-level sales are done by disassemble/remove then order; no one-call subset sale.
- Offers are display-only; no quantity caps or checkout price engine.
- Audit has structured events but no generic before/after diff or undo contract.
- Contact messages persist, but admin email notification awaits SMTP/recipient configuration.
- Unit archive/retire without a return/scrap flow is absent.

### Blocked / owner-gated
- Google OIDC: client ID/secret + approved redirect domain.
- Admin TOTP MFA and recovery policy.
- Sales invoice/quotation PDF and legal/tax seller identity/ETA.
- Domain/hosting/TLS/SMTP/off-host backup, final brand.

## 7. Remaining risks
- Admin protection is still password/session (plus OTP login option) without MFA.
- Offers cannot be enforced at checkout until owner pricing rules exist.
- Invoice legal identity and ETA applicability remain unresolved.
- No external penetration test or dependency CVE audit in this round.
- Frontend integration must use `docs/api/BACKEND_UI_CONTRACTS.md` and not assume unavailable Google/MFA/invoice endpoints.
