# RIGX Motion Direction V2 — Integration Report

**Date:** 6 November 2026  
**Source:** `HARDWARE_STORE_RIGX_HEADER_V1_2026-11-06.zip`  
**Visual reference:** `RIGX_MOTION_DIRECTION_DEMO_V2.html`  
**Scope:** Frontend presentation integration in `apps/web`; no Backend, database, migration, API contract, or dependency changes.

## Objective

Bring the storefront closer to the supplied Motion Direction prototype: Graphite/Obsidian surfaces, warm orange accent, a floating glass header, a more expressive hero, a real catalog-driven brand strip, a continuously moving category carousel, coherent product cards, and a stronger closing section. The prototype was treated as a visual direction only. Its sample products, logos, brand names, prices, and claims were not copied into the real storefront.

## Implemented changes

1. `apps/web/src/components/site-header.tsx`
   - Restyled the utility strip, search row, primary nav, mega menu, and mobile drawer as one cohesive floating navigation system.
   - Kept the Components taxonomy and existing search, compare, theme, locale, account, and admin-permission behavior.
   - Filtered inactive categories and inactive child categories out of navigation.
   - Added real sign-out invocation through the existing `logoutCurrent()` client function and local session state update.
   - Added mobile dialog focus trapping, initial focus, Escape handling, focus restoration, body-scroll locking with prior-style restoration, and accordion ARIA relationships.
   - Removed the generic Products link from the mobile primary list; New Arrivals remains the catalog route.

2. `apps/web/src/components/storefront-home.tsx`
   - Rebuilt the homepage presentation around actual Catalog API responses for categories, brands, and latest published products.
   - Added a hero collage using up to three actual products, their real images (or category-aware icon fallbacks), names, brands/categories, and catalog prices.
   - Added a trust strip describing existing catalog, pricing, compare, and share features without inventing warranty/inspection/shipping promises.
   - Added an active-brand marquee, category carousel, feature bento section, latest products, and final CTA.
   - Empty/loading/API error states remain explicit. Brand and category sections are omitted when there is no active data.

3. `apps/web/src/components/storefront-category-carousel.tsx` (new)
   - Added an RTL/LTR-aware infinite category carousel driven by active Catalog categories.
   - Includes arrow controls, keyboard arrows, touch swipe, pause on hover/focus, visibility pause, resize measurement, and reduced-motion support.
   - Stops the animation frame loop when the page is hidden or `prefers-reduced-motion` is active; manual movement becomes immediate when reduced motion is requested.

4. `apps/web/src/components/catalog-product-card.tsx`
   - Applied the Graphite/Orange product-card treatment while retaining real product data, actual price formatting, compare, share, and conditional WhatsApp actions.
   - Kept image placeholders honest and category-aware rather than fabricating product imagery.

5. `apps/web/src/components/site-chrome.tsx`
   - Brought the footer and skip-link accent into the shared RIGX orange token.

6. `apps/web/src/app/globals.css`
   - Added the integrated responsive visual layer for header, hero, brand marquee, category carousel, features, product cards, closing CTA, and footer.
   - Uses the project's existing Inter / IBM Plex Sans Arabic fonts and design tokens for the shared storefront surfaces.
   - Added restrained custom menu/drawer transitions and reduced-motion overrides.

## Verification performed in this environment

- TypeScript/TSX syntax transpilation: **70 source files scanned, 0 syntax diagnostics**. This is a syntax-only pass; it is not a semantic TypeScript typecheck.
- CSS parsing with PostCSS: **PASS**, 411 CSS rules parsed.
- Dependency installation was attempted, but the package registry could not be reached in time; the offline retry failed because a required tarball (`xmlchars@2.2.0`) was not cached (`ENOTCACHED`).
- Consequently, a fresh Next.js production build, Vitest, and Playwright E2E suite could **not** be run or independently confirmed in this environment.
- No claim is made that visual screenshots of this modified application were captured. The supplied screenshots were the old/baseline UI; the actual updated app must be launched in the owner's local Preview for visual approval.

## Explicitly unchanged

- Backend C# projects and endpoints
- PostgreSQL schema, EF Core migrations, seed data, and volumes
- API request/response contracts
- `package.json` and lockfiles; no dependencies added
- Admin shell navigation and unrelated page workflows

## Owner-side validation required before acceptance

1. Apply this source archive to a separate branch/copy of the project (do not overwrite uncommitted local work).
2. Run `npm ci` in `apps/web` using the owner's connected environment.
3. Run TypeScript typecheck, `npm test`, `npm run build`, and the relevant Playwright checks.
4. Launch the real Preview and capture 1440/390 screenshots in AR/EN and Light/Dark, plus open Mega Menu and Mobile Drawer states.
5. Verify actual catalog products/images/brands/categories, search, compare, account permissions, logout, RTL/LTR navigation, and no overflow at 360–1920px.
6. Return test output and screenshots for independent review before marking the UI batch accepted.

**Status:** Implementation applied to the supplied source copy; pending owner Preview, full test run, and visual acceptance.
