# RIGX — Roadmap

Each step = one focused session. A step is **done** when: code + tests for it pass, the matching doc is updated, and it's committed.
Order matters: we restructure before adding features, so new features land in clean modules.

## Phase 0 — Safety & cleanup ✅ (13 Nov 2026)
- [x] Git repository + legacy snapshot commit
- [x] Remove zip snapshots, `.backups`, `archive`, old review screenshots, old frontend, preview scripts
- [x] New docs (`docs/01–06`), old reports removed
- [x] Backend builds (0 errors)
- [x] Baseline: full backend suite **194/194 passed** (3 min, real PostgreSQL) — 13 Nov 2026
- [x] Private GitHub repository (`Abdalmlek56M/rigx`) + push — 13 Nov 2026

## Phase 1 — Backend restructure (behavior unchanged)
1. **Common building blocks**
   - [x] `AppException` base + one global ProblemDetails mapping; all 14 module exception types derive from it
   - [ ] Remove the now-redundant endpoint try/catch blocks (done per area as each module is restructured; Retail, Catalog, Content, Identity ✅)
   - [ ] Soft delete + trash, audit interceptor (before/after), paging (built with the first feature that needs them: 2.10/2.11)
2. [x] **Migration runner**: `--migrate` command; `DatabaseMigrator` is the single ordered context list (preview init reuses it; it previously skipped Retail and PcBuilder)
3. [x] **Endpoints**: decided to keep all endpoints and the admin dashboard in Host for now (architecture decision 12); one shared `HttpContextUserExtensions` replaced 13 copied user-id/permission helpers.
4. [x] **Split `RetailService`** (characterization tests first) into: Inventory units · Assemblies · Sales (orders, reservation, payments, returns) · Partners · Financials (expenses, reports). Done inside the Retail module (`Domain/ Contracts/ Features/ Persistence/`), endpoints split per area in Host, Retail try/catch blocks removed; EF model unchanged.
5. [x] **Split `CatalogApplication`** (products · taxonomy · media · queries) and `ContentServices` (homepage · articles · FAQ · policies). Catalog and Content endpoints no longer catch business errors (standard ProblemDetails now).
6. [x] Remove dead code/legacy paths: empty Sales/Financials/Invoicing projects and unused `ModuleMarker`s removed, unused members/parameters removed, warnings-as-errors + unused-code analyzers enforced in the build. Still over ~400 lines, split in the Phase 2 step that touches them: `IdentityAccountLifecycleService` (796, removed with passwords in 2.1), `PreviewInitializationService` (612), `PurchaseDraftService` (561), `SupplierDirectoryService` (549), `IdentityPersistence` (537), `CatalogMediaStorage` (524), `Reviews` (497), `Offers` (453), `AccountGovernance` (453).

## Phase 2 — Align with owner decisions (backend)
0. [x] **Gap audit**: `docs/07-gap-audit.md` (13 Nov 2026). 7 structural gaps (S1–S7), per-screen gaps mapped to the steps below, extra backend parts to remove, owner decisions D1–D8. Proposed order of work: §7 of that file.
1. [x] **Auth**: remove password login/registration/reset; email OTP only + Google OIDC; step-up code for destructive admin actions; session list + "logout all" + new-device email. Done 13 Nov 2026 (commits 403b106, b553796, 992f3b5, 1becc17, 2d7a2dd, 57a0624; migrations M12–M15). Checkpoint: `docs/checkpoints/2.1-auth.md`.
2. [x] **Catalog/intake**: stock items `RX-xxxx` (used = one piece, new = lot), context prices (in build / with board / alone), warranty per item, hidden, box flag, internal note, optional specs, photos (min 3 to publish), public `/api/v1/store/items`; builds `BLD-xx`; category spec schema (types, filter/compatibility flags, inherited) + category code + menu/builder flags; free-text model + suggestions; purchase intake `PO-xxxx` (seller record, full-PC intake kept as build, seller payment incl. trade-in); supplier profiles/catalog and product variants removed (D7). Done 13 Nov 2026 (migrations M17–M22). `INV-` codes land with 2.6; request → purchase with 2.12. Checkpoint: `docs/checkpoints/2.2-catalog-intake.md`.
3. [x] **Assemblies**: BLD code, usage + budget, description/"مناسبة لـ" + photos, warranty default + per-part override, copies count, sell mode (full / full-or-split), per-part price, add from stock or a new part inline, remove → stock or another build, replace from stock or swap with another build's part. Done 13 Nov 2026 (migration M23). Selling a build split or by copies lands with 2.4 (POS). Checkpoint: `docs/checkpoints/2.3-assemblies.md`.
4. [x] **Sales**: guest checkout + structured address; statuses incl. Shipped; reservation timer + expiry policy; courier, waybill, public tracking link; blacklist; POS free price + discount limit, split build sale, split payment, InstaPay; source & usage questions; order emails (received, confirmed); satisfaction score; public "sold before" feed. Done 13 Nov 2026 (migrations M24–M25). Partner share preview in POS → 2.14; invoice/print → 2.6. Checkpoint: `docs/checkpoints/2.4-sales.md`.
5. [x] **Warranty registry** + public lookup (phone / invoice / code), rate-limited and masked. Done 13 Nov 2026 (migration M26): warranty snapshot per sold unit at claim, one record per unit registered at delivery; lookup by `RX-` code, `RIGX-` order number, phone digits or serial (`warranty-lookup`, 20 / 10 min per IP, phone masked to last 3 digits); auto-void when a delivered order is cancelled or a return is accepted; manual void (`Sales.ProcessReturn`); customer account list. Invoice number search → 2.6; extend warranty → 2.18. Checkpoint: `docs/checkpoints/2.5-warranty.md`.
6. [x] **Invoicing**: online invoice (part / build), shareable quote image/PDF, watermark, QR to warranty lookup. Done 13 Nov 2026 (migrations M27–M28): invoice issued at delivery in the warranty transaction, `{prefix}-{YYYY}-{NNNN}` with a gap-free per-year counter; public `/i/{token}` link; customer and admin lists; invoice email on delivery (repeated status requests no longer resend order emails); quote data endpoint (no invoice number); warranty lookup by invoice number; warranty BuildCode fix for split builds; dead invoice permissions removed. Page, watermark, QR and PDF/PNG rendering → Phase 4; part type and Cairo-time year added 13 Nov 2026 (2.7 commit). Checkpoint: `docs/checkpoints/2.6-invoicing.md`
7. [x] **Shipping**: governorate groups + default price; deposit rule from settings. Done 13 Nov 2026 (migration M29): order keeps a fee + delivery time snapshot; upfront (shipping + deposit) must be collected before Preparing; POS fee from the governorate when omitted; public `GET /api/v1/storefront/shipping-rates`. Checkpoint: `docs/checkpoints/2.7-shipping.md`.
8. **Notify-me** (phone required, email optional) + demand report.
9. **Media**: auto-watermark on upload (position/logo/opacity from settings), originals kept.
10. **System**: full backups (DB + media + settings) on schedule, two destinations, restore drill; trash with 30-day purge; data reset (owner + step-up); export (Excel/PDF/CSV/XML).
11. **Audit log UI data**: filters, before/after, "restore previous values".
12. [x] **Requests inbox**: sell / trade-in, upgrade, notify-me, support, contact, service bookings; statuses, offer + decision, WhatsApp reply template, convert to purchase. Done 13 Nov 2026 (migrations M30–M31): one `customer_requests` inbox replaces contact messages (`REQ-xxxx` codes); public `POST /api/v1/requests` (anonymous, rate-limited, honeypot, 10-min duplicate guard); statuses New, Following, Offered, Accepted, Rejected, Converted, Closed; offer + decision for sell / trade-in / upgrade; convert an accepted sell or trade-in to a draft purchase; permissions `Requests.View` / `Requests.Manage`. Photos → 2.9; back-in-stock + demand report → 2.8; customer "my requests" → 2.18. Checkpoint: `docs/checkpoints/2.12-requests.md`.
13. [x] **Builder v2**: free builder over stock items, compatibility from category specs, power estimate, saved builds by link, quote PDF/PNG; remove PC-builder templates. Done 13 Nov 2026 (migration M32): free builder over published stock items; slots CPU/BOARD/RAM/GPU/SSD/PSU/CASE/COOL come from categories with "show in builder" + code (sub-categories included); compatibility and power rules from category specs; in-build price rule (`InBuild` order price option, online needs CPU + BOARD); check, from-build and quote (`POST /api/v1/builder/quote`); saved builds by link (`saved_builds`, signed-in saver only edits or deletes, max 50 per account); "عدّل فيها" from free stock; PC-builder templates removed (`/api/v1/pc-builder`, `pc_build_*` tables dropped). Builder page, quote PDF/PNG and WhatsApp send → Phase 4. Checkpoint: `docs/checkpoints/2.13-builder.md`.
14. [x] **Partners v2**: one partner record (phone, join date, default %, basis FinancedParts or WholeShop) replaces agreements, product/category allocations and several partners per unit; unit partner + % set on purchase lines, receipt, build part and `PUT /units/{id}/partner`; monthly whole-shop share posted by hand after month end; capital deposits/withdrawals (append-only); payouts against oldest earnings; statement data endpoint. Done 14 Nov 2026 (migration M33: data migrated by `M33_PartnersV2`, `purchase_lines.partner_id` by `M33_PurchaseLinePartner`). POS partner-share preview ✓ (item 4). Statement PDF and partners admin page → Phase 4; money accounts / "paid from" for payouts → 2.15. Checkpoint: `docs/checkpoints/2.14-partners.md`.
15. [x] **Money & dashboard**: money accounts (cash / InstaPay / wallet / bank transfer) with opening balances, append-only movements, no negative balance, transfers; sales, returns, purchase seller payments, partner payouts and capital, and expenses move them. Expenses v2: editable types, build link, paid from account or partner dues, recurring monthly (hourly job), summary, CSV export. Dashboard `GET /api/v1/admin/dashboard`: period vs previous period, snapshot, 30-day series, to-do counts, breakdowns, stock aging, partner profits; the old catalog-count dashboard removed. Done 14 Nov 2026 (migration M34): Retail `Features/Money`, `Features/Financials`, `Features/Dashboard`; Host `DashboardComposer`, `RecurringExpenseJob`. Dashboard page and charts → Phase 4; Excel/PDF export → 2.10; receipt image → 2.9; notify-me demand → 2.8. Checkpoint: `docs/checkpoints/2.15-money.md`.
16. **Content v2**: scheduled articles, view counts, structured article blocks incl. store product card; FAQ scope (general / category / product, exclusions, YouTube).
17. [x] **Settings v2**: store info, social links table, shipping groups, payment + deposit, watermark, store controls, WhatsApp templates, reviews, language, invoice settings, lead-source and warranty-option lists (D5/D6); homepage layout JSON replaced by section order in store controls; public checkout quote (shipping fee + upfront/deposit). Done 13 Nov 2026 (migration M16). Logo uploads (store + watermark) land with 2.9. Checkpoint: `docs/checkpoints/2.17-settings.md`.
18. **Customers admin**: customer 360 view, merge accounts, extend warranty, read-only "view as customer".

## Phase 3 — API contract
- OpenAPI covering every endpoint, consistent error shape, paging/filter conventions.
- Generated TypeScript client package for the frontend.

## Phase 4 — Frontend (Next.js, from the approved canvas)
1. Foundation: tokens, fonts, RTL/EN, layout, header (search, EN, account, cart drawer), footer, floating contact, back-to-top.
2. Storefront: home, catalog (category tree, in/out of stock filter), product (context price, notify-me, WhatsApp prefilled), builds, PC builder (new/used per slot, out-of-stock struck), services, sold page.
3. Checkout → order success → tracking page; login (email/Google); account (orders, warranties, profile/security, subscriptions, tier); warranty lookup.
4. Admin: dashboard, POS, orders, purchase intake, inventory, build editor, partners, categories, customers, offers/coupons, expenses, requests inbox, campaigns, tiers, FAQ/articles, users/roles, audit log, trash/backup/export, settings.
5. Playwright: the critical flows listed in `04-engineering-standards.md`.

## Phase 5 — Growth features (after the core works)
- Offers engine + coupons, customer tiers + perks, reviews (off by default), campaigns & subscriptions, browse-based suggestions + "محتار؟ كلمنا" nudge (first-party, once per session).

## Phase 6 — Launch
- Hosting + TLS + reverse proxy, production SMTP (`rigxhardware@gmail.com` sender or domain mail), off-host backups, CSP, monitoring/logs, Google OIDC production client, external security review, real catalog data replaces seed data.

## Owner decisions still needed (asked when the step arrives)
- Tier conditions and perk values (when tiers are enabled).
- Hosting provider and domain mail.
