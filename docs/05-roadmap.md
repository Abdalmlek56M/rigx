# RIGX — Roadmap

Each step = one focused session. A step is **done** when: code + tests for it pass, the matching doc is updated, and it's committed.
Order matters: we restructure before adding features, so new features land in clean modules.

## Phase 0 — Safety & cleanup ✅ (13 Nov 2026)
- [x] Git repository + legacy snapshot commit
- [x] Remove zip snapshots, `.backups`, `archive`, old review screenshots, old frontend, preview scripts
- [x] New docs (`docs/01–06`), old reports removed
- [x] Backend builds (0 errors)
- [x] Baseline: full backend suite **194/194 passed** (3 min, real PostgreSQL) — 13 Nov 2026
- [ ] Private GitHub repository + push (owner creates the repo)

## Phase 1 — Backend restructure (behavior unchanged)
1. **Common building blocks**
   - [x] `AppException` base + one global ProblemDetails mapping; all 14 module exception types derive from it
   - [ ] Remove the now-redundant endpoint try/catch blocks (done per area as each module is restructured; Retail ✅)
   - [ ] Soft delete + trash, audit interceptor (before/after), paging (built with the first feature that needs them: 2.10/2.11)
2. [x] **Migration runner**: `--migrate` command; `DatabaseMigrator` is the single ordered context list (preview init reuses it; it previously skipped Retail and PcBuilder)
3. **Endpoints**: single-module endpoint files move into their module during that module's split (4–5); cross-module orchestration endpoints stay in Host. Move `AdminAnalyticsService` to Financials.
4. [x] **Split `RetailService`** (characterization tests first) into: Inventory units · Assemblies · Sales (orders, reservation, payments, returns) · Partners · Financials (expenses, reports). Done inside the Retail module (`Domain/ Contracts/ Features/ Persistence/`), endpoints split per area in Host, Retail try/catch blocks removed; EF model unchanged.
5. **Split `CatalogApplication`** (products · taxonomy · media · queries) and `ContentServices` (settings · articles · FAQ · videos · contact).
6. Remove dead code/legacy paths found during the split.

## Phase 2 — Align with owner decisions (backend)
1. **Auth**: remove password login/registration/reset; email OTP only + Google OIDC; step-up code for destructive admin actions; session list + "logout all" + new-device email.
2. **Catalog/intake**: free-text model + category, name normalization + alias suggestions, optional specs, hidden units, context pricing (in-build / with board / alone).
3. **Assemblies**: warranty default + per-part override, swap between builds, remove → back to stock, copies count, sell mode (full / full-or-split).
4. **Sales**: reservation timer + expiry policy, tracking link, blacklist, POS free price + discount limit, source & usage questions, satisfaction score.
5. **Warranty registry** + public lookup (phone / invoice / code), rate-limited and masked.
6. **Invoicing**: online invoice (part / build), shareable quote image/PDF, watermark, QR to warranty lookup.
7. **Shipping**: governorate groups + default price; deposit rule from settings.
8. **Notify-me** (phone required, email optional) + demand report.
9. **Media**: auto-watermark on upload (position/logo/opacity from settings), originals kept.
10. **System**: full backups (DB + media + settings) on schedule, two destinations, restore drill; trash with 30-day purge; data reset (owner + step-up); export (Excel/PDF/CSV/XML).
11. **Audit log UI data**: filters, before/after, "restore previous values".

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
- Reservation length default (24h?) and expiry action.
- Tier conditions and perk values (when tiers are enabled).
- Hosting provider and domain mail.
