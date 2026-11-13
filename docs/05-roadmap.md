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
   - [ ] Remove the now-redundant endpoint try/catch blocks (done per area as each module is restructured; Retail, Catalog, Content ✅)
   - [ ] Soft delete + trash, audit interceptor (before/after), paging (built with the first feature that needs them: 2.10/2.11)
2. [x] **Migration runner**: `--migrate` command; `DatabaseMigrator` is the single ordered context list (preview init reuses it; it previously skipped Retail and PcBuilder)
3. [x] **Endpoints**: decided to keep all endpoints and the admin dashboard in Host for now (architecture decision 12); one shared `HttpContextUserExtensions` replaced 13 copied user-id/permission helpers.
4. [x] **Split `RetailService`** (characterization tests first) into: Inventory units · Assemblies · Sales (orders, reservation, payments, returns) · Partners · Financials (expenses, reports). Done inside the Retail module (`Domain/ Contracts/ Features/ Persistence/`), endpoints split per area in Host, Retail try/catch blocks removed; EF model unchanged.
5. [x] **Split `CatalogApplication`** (products · taxonomy · media · queries) and `ContentServices` (homepage · articles · FAQ · policies). Catalog and Content endpoints no longer catch business errors (standard ProblemDetails now).
6. [x] Remove dead code/legacy paths: empty Sales/Financials/Invoicing projects and unused `ModuleMarker`s removed, unused members/parameters removed, warnings-as-errors + unused-code analyzers enforced in the build. Still over ~400 lines, split in the Phase 2 step that touches them: `IdentityAccountLifecycleService` (796, removed with passwords in 2.1), `PreviewInitializationService` (612), `PurchaseDraftService` (561), `SupplierDirectoryService` (549), `IdentityPersistence` (537), `CatalogMediaStorage` (524), `Reviews` (497), `Offers` (453), `AccountGovernance` (453).

## Phase 2 — Align with owner decisions (backend)
0. [x] **Gap audit**: `docs/07-gap-audit.md` (13 Nov 2026). 7 structural gaps (S1–S7), per-screen gaps mapped to the steps below, extra backend parts to remove, owner decisions D1–D8. Proposed order of work: §7 of that file.
1. **Auth**: remove password login/registration/reset; email OTP only + Google OIDC; step-up code for destructive admin actions; session list + "logout all" + new-device email.
2. **Catalog/intake** (split 2.2a/b/c): public codes `RX-`/`BLD-`/`PO-`/`INV-`; storefront sells stock items (used = one page per piece with its own photos, new = lot with quantity); category spec schema (types, filter/compatibility flags, inherited) + category code + menu/builder flags; free-text model + alias suggestions; warranty per item; hidden items; box flag; internal note; min 3 photos to publish; context pricing (in-build / with board / alone); purchase intake (seller record, full-PC intake, trade-in payment, request → purchase).
3. **Assemblies**: BLD code, usage + budget, description/"مناسبة لـ" + photos, warranty default + per-part override, copies count, sell mode (full / full-or-split), add a new part inline, remove → stock or another build (swap/remove already exist).
4. **Sales**: guest checkout + structured address; statuses incl. Shipped; reservation timer + expiry policy; courier, waybill, public tracking link; blacklist; POS free price + discount limit, split build sale, split payment, InstaPay; source & usage questions; order emails (received, confirmed); satisfaction score; public "sold before" feed.
5. **Warranty registry** + public lookup (phone / invoice / code), rate-limited and masked.
6. **Invoicing**: online invoice (part / build), shareable quote image/PDF, watermark, QR to warranty lookup.
7. **Shipping**: governorate groups + default price; deposit rule from settings.
8. **Notify-me** (phone required, email optional) + demand report.
9. **Media**: auto-watermark on upload (position/logo/opacity from settings), originals kept.
10. **System**: full backups (DB + media + settings) on schedule, two destinations, restore drill; trash with 30-day purge; data reset (owner + step-up); export (Excel/PDF/CSV/XML).
11. **Audit log UI data**: filters, before/after, "restore previous values".
12. **Requests inbox**: sell / trade-in, upgrade, notify-me, support, contact, service bookings; statuses, offer + decision, WhatsApp reply template, convert to purchase.
13. **Builder v2**: free builder over stock items, compatibility from category specs, power estimate, saved builds by link, quote PDF/PNG; remove PC-builder templates.
14. **Partners v2**: partner record (phone, capital, join date, default %, basis), capital deposits/withdrawals, payouts, statement PDF.
15. **Money & dashboard**: money accounts (cash / InstaPay / wallet) if D3 = yes; dashboard KPIs from the design; expenses v2 (types list, build link, paid-from, recurring, export).
16. **Content v2**: scheduled articles, view counts, structured article blocks incl. store product card; FAQ scope (general / category / product, exclusions, YouTube).
17. **Settings v2**: store info, social links table, shipping groups, payment + deposit, watermark, store controls, WhatsApp templates, reviews, language, invoice settings (do right after 2.1).
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
- D1–D8 in `docs/07-gap-audit.md` §6 (RX codes per lot/piece, customer-facing statuses, money accounts, partner "whole shop" basis, lead sources, warranty options, removing unused backend parts, product Q&A).
- Reservation length default (24h?) and expiry action.
- Tier conditions and perk values (when tiers are enabled).
- Hosting provider and domain mail.
