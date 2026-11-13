# RIGX — Overnight Master Execution (10 November 2026)

## Scope delivered in this pass
This pass focused on the P0 security/authorization and data-integrity items, then completed the highest-value P1 retail and SEO slices. It did not rebuild completed Retail functionality.

### P0 — Finance permission boundary
- Added `Finance.ManagePartnerAgreements` and `Finance.PostPartnerSettlements`.
- `POST /partner/agreements` and `POST /partner/settlements` no longer use `Finance.ViewReports`.
- Read endpoints (earnings, adjustments, ledger, agreements) remain on `Finance.ViewReports`.
- Identity migration `M10_RetailLeastPrivilege` grants the new permissions to the `Owner` role only; other roles must be granted explicitly.
- This closes the defect where a report-viewer could change an agreement or post a settlement.

### P0 — Sensitive inventory / assembly / customer projections
- Added `Inventory.ViewCosts`, `Inventory.ViewSerials`, `Sales.ViewCustomerContact`.
- `/admin/retail/units` now nulls acquisition cost, standalone price, partner override, source name/type and purchase metadata for users without `Inventory.ViewCosts`, and serials for users without `Inventory.ViewSerials`.
- `/admin/retail/assemblies` nulls component cost / allocated amounts without `Inventory.ViewCosts`.
- `/admin/retail/orders` masks phone and hides address without `Sales.ViewCustomerContact`.
- All sensitive responses keep `Cache-Control: no-store`.
- Owner retains full capability through the migration grant. UI renders “Restricted” when a cost field is withheld.

### P0 — Purchase unit snapshot integrity
`InventoryUnit` now preserves:
purchase kind/source type/source name/date, brand, model, category, description, external code, specifications JSON, image storage-key references, condition, serial, warranty note, acquisition cost, standalone price, partner override, receipt reference and timestamps.
`ReceiveFromDraftAsync` maps these fields from `PurchaseDraftItem`/draft, and snapshots the primary catalog image keys without copying files.
Tests cover all supported fields, multi-quantity receipt, idempotent retry, and restricted-role masking.

### P0 — Backend failure reconciliation
The 11 previously reported failures were reproduced and fixed:
- stale endpoint/table-count expectations updated to current architecture;
- legacy migration-preflight fixtures now insert legacy rows with raw SQL rather than today’s entity shape;
- registration tests assert the durable encrypted outbox rather than a synchronous in-memory provider message;
- account-lifecycle outbox timing tolerates worker scheduling;
- cascade exceptions for account-dependent and media-usage rows are explicit instead of blanket “all Restrict”.
Result: **full backend suite 164/164 passed**.

### P1 — Retail completeness
- Admin manual/phone order form in `/admin/orders`: customer, phone, address, source, payment-required, published build or product, quantity.
- Manual orders resolve product/assembly price on the server; client price/discount is not trusted.
- Public `/builds` page lists published assemblies with real components and prices.
- Builds can be added to the cart and complete through the real checkout; server resolves the assembly price and stock claim still happens only at `Confirmed`.
- Builds E2E: published build → cart → checkout line (1/1).

### P1 — SEO
- `NEXT_PUBLIC_SITE_URL` is now required in production builds and rejects localhost. Missing/invalid configuration fails the build rather than emitting localhost canonicals.
- Sitemap paginates all products and articles (up to a bounded 50 pages / 5,000 records per type).
- Build verification used `NEXT_PUBLIC_SITE_URL=https://rigxhardware.com` (preferred domain, not purchased/verified). It must be replaced with the owner-approved production URL before launch.

## Verification executed
| Check | Result |
|---|---|
| `dotnet build HARDWARE_STORE.slnx` | 0 errors |
| Full xUnit/Testcontainers suite | **164/164 passed** |
| Retail PostgreSQL tests | **10/10** |
| Playwright — retail admin/returns/partner | **1/1** |
| Playwright — cart/checkout | **4/4** |
| Playwright — public builds/cart/checkout | **1/1** |
| Manual order API (client sent price 1, server resolved 2,500; then cancelled) | **200 / priceMatches=true** |
| `npx tsc --noEmit` | 0 |
| `npx vitest run` | 37/37 |
| `npm run build` with `NEXT_PUBLIC_SITE_URL` | success |
| Media backup/restore verification (previous slice) | `BACKUP_OK` + `RESTORE_VERIFIED` |

Migrations applied to QA only: Retail `M3_UnitSnapshotFields`, Identity `M10_RetailLeastPrivilege`.
Backup before migration: `.backups/rigx_qa_before_overnight_20261110.dump`.
Screenshots: `docs/review/overnight-2026-11-10/` (home EN/AR, products, builds, admin stock/orders/partners, customer orders, mobile orders).

## Security findings — actual status
Closed/high-value:
- Finance report-viewer write access.
- Cost/serial/supplier/PII leakage to unauthorized admin roles.
- Purchase-unit metadata loss.
- Stale test suite masking regressions.
- Production SEO fallback to localhost.

Residual (not claimed fixed):
- Admin MFA/step-up and re-authentication for high-risk actions are **not implemented**; needs provider/policy decision.
- Google OIDC and Email OTP are **not implemented** in this pass.
- Contact Us page/form and layered anti-spam are **not implemented**.
- Sales invoice / price quotation PDFs are **not implemented**; legal seller/tax identity is owner/accountant-gated.
- Expenses, monthly/yearly reports, installments and full financial posting remain incomplete.
- Unit image keys are snapshotted, but Catalog media cleanup does not yet query Retail unit references; this is a **Medium** residual risk. Do not delete catalog media referenced by historical units until a cross-module reference guard is added.
- No independent penetration test; dependency CVE audit and production host hardening are still required.
- Admin UI buttons are not permission-aware in every page; the API enforces authorization, but UI polish to hide unavailable actions remains.

## Owner decisions still required
1. Production domain/URL, hosting, SMTP, off-host backup and TLS/reverse-proxy plan.
2. Legal seller/tax/registered correspondence address for invoices, plus ETA applicability via accountant.
3. Approval of final logo/brand direction and any public claims.
4. Google OAuth credentials and whether Email OTP supplements or replaces password login.
5. MFA/step-up policy for admin accounts.
6. Installment/payment policy if required.
7. Multiple partners on one order (current resolver uses the latest active agreement).
