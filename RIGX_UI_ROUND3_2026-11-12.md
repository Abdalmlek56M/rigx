# RIGX — Round 3: UI Parity & Legacy Removal (12 November 2026)

## Result in one line
Customer, content, PC Builder, account, and contact journeys are native React compositions of the supplied V1.9.1 reference. **Admin content panels and authentication form bodies remain Restyled Legacy**, not full 1:1 rebuilds. No route is labelled Rebuilt on the basis of tokens alone; see `docs/UI_ROUTE_MATRIX_2026-11-12.md`.

## 1. Real CSS architecture (corrected)
`apps/web/src/app/layout.tsx` imports, in order:
1. `./globals.css` (Tailwind + legacy app styles)
2. `@/styles/rigx/rigx-design-tokens.css`
3. `@/styles/rigx/preview.css`
4. `@/styles/rigx/v1.2-polish.css`
5. `@/styles/rigx/v1.3-refinement.css`
6. `@/styles/rigx/v1.4-services.css`
7. `@/styles/rigx/v1.5-complete-footer-analytics.css`
8. `@/styles/rigx/v1.6-commerce-admin.css`
9. `@/styles/rigx/v1.9-complete.css`
10. `@/styles/rigx/v1.9.1-owner-refinements.css`
11. `./rigx-bridge.css`
12. `./rigx-pages.css`

There is **no** `src/app/rigx-v191.css` in this archive. It was removed in Round 2; documentation that referenced it has been corrected.

## 2. Round 3 structural work
- Account: new `AccountShell` + reference `account-nav` / `account-tab` rail; all 9 account screens moved into it. Disabled orders/marketing placeholders removed.
- Content: `/articles`, `/articles/[slug]`, `/faq`, `/offers`, `/categories`, `/policies/[kind]`, `/videos/[id]` rebuilt as page-hero + reference grids/documents/accordions.
- PC Builder: template entry and inner wizard rebuilt with `builder-layout`, `builder-slots`, `builder-slot`, `builder-summary`; logic unchanged.
- Auth: `AuthPanel` now renders `auth-page` / `auth-card` / `auth-aside` reference shell; auth form bodies are still the legacy compositions.
- Admin: sidebar converted to reference `admin-sidebar` / `admin-nav` / `admin-nav-group`; empty Future Modules group and stale footer copy removed. Admin content panels remain Restyled Legacy.
- Legacy removal (verified unused by imports, dynamic imports, or tests): `storefront-home`, `storefront-carousel`, `storefront-category-carousel`, `storefront-discovery`, `storefront-rail`, `storefront-section-heading`, `catalog-product-card`, `catalog-products-experience`, `cart-page`, `product-detail-gallery`, `site-header`. The obsolete `site-header` test was replaced by `rigx-header.test.tsx` covering the live header.
- Public claims neutralised: removed “Real technical support”, “After-sales service”, “Documented warranty”, and “inspected components” from live surfaces. Replacement wording only references contact, order status, order data, and published inventory units.

## 3. Verification actually run
| Check | Command | Result |
|---|---|---|
| TypeScript | `npx tsc --noEmit` | 0 errors |
| Unit tests | `npx vitest run` | 9 files / **30 tests passed** |
| Production build | `npm run build` with `NEXT_PUBLIC_SITE_URL=https://rigxhardware.com` | success |
| Playwright journeys | `npx playwright test tests/e2e/cart-demo.spec.ts tests/e2e/builds.spec.ts tests/e2e/retail-core.spec.ts` | **6/6 passed** |
| Runtime page errors | Playwright sweep of public, account, and admin routes | **0 page errors** |
| Backend | unchanged in Round 3; previous verified suite | **190/190** |

Screenshots: `docs/review/round3-2026-11-12/` — 1440px and 390px captures for customer, content, PC Builder, account, auth, and Admin routes as listed in the matrix.

## 4. Remaining gaps (not hidden)
- **Admin panels are Restyled Legacy.** The shell/sidebar uses reference structure, but the 19 operational areas were not re-composed panel-by-panel.
- **Authentication form bodies are Restyled Legacy.** Shell is rebuilt; inner layouts are unchanged.
- Reference-only Admin capabilities with no backend: shipping carriers, warranty registry/claims, filters editor, trash/restore.
- No Google OIDC, Admin MFA, invoice/ETA PDFs; these remain Backend Blocked / owner-gated.
- Offers remain display-only; no checkout pricing engine.
- QA data still includes seeded catalogue text; production data must replace it before launch.

## 5. Definition of Done assessment
- Customer/content/account/PC Builder parity: substantially met.
- All 19 Admin areas at 1:1 parity: **not met**.
- Legacy removal and placeholders: met for the listed components/groups.
- Business/security behaviour: unchanged; Playwright and backend evidence remains green.
Therefore this round is delivered as **partial parity with an accurate route matrix**, not as a Production Ready declaration.
