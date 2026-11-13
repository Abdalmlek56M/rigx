# RIGX — V1.9.1 UI Replacement & Integration Report (12 November 2026)

## Status: native structural rebuild of the primary journeys, not full reference parity
The approved V1.9.1 visual language is now applied to the live Next.js application, and the checkout/backend blockers are fixed. **Round 2** replaced the actual structures of the header, home, catalog, product detail, cart, checkout, builds, PC Builder entry, contact, and footer with native React implementations driven by the supplied reference CSS and real APIs. The result is a coherent Obsidian/Warm Orange product, but it is **not** a pixel-complete rebuild of every one of the 13 customer and 19 Admin reference screens. Remaining UI gaps and backend blockers are listed explicitly below.

## Round 2 — structural replacement (12 November 2026)
### Reference assets adopted as the design system
- Imported the supplied reference CSS layers into `apps/web/src/styles/rigx/` (tokens, preview, v1.2–v1.9.1).
- Added `rigx-bridge.css` mapping the legacy `--ds-*` tokens to the reference `--rigx-*` tokens so older admin surfaces stay consistent.
- Added `rigx-pages.css` for page-level compositions built from the same tokens.
- No reference HTML, JS, or iframe is shipped; only shared design rules and native React markup.

### Rebuilt page structures
- `RigxHeader`: reference header, brand block, nav, search with live suggestions, contact CTA, theme/locale/account/wishlist/cart actions, mobile nav, and a data-driven mega menu fed by real catalog categories.
- `RigxHome`: hero grid, side promos, service promos, benefits ticker, category taxonomy, latest products, PC Builder promo, published builds, editorial band, closing band, real FAQ accordion.
- `RigxCatalog`: page hero + live stats, toolbar, real category/brand filters, sorting, pagination, results grid.
- Product detail: reference gallery/summary composition, variant selection with real prices, buy-now/add-to-cart, wishlist/share, specification groups, reviews, related products.
- `RigxCart`: reference stepper, cart lines with quantity controls, summary panel, clear-cart confirmation.
- Checkout: reference page hero/stepper on top of the real Delivery/Pickup + Terms + server-priced order flow.
- Builds: reference builds hero and build cards fed by published inventory assemblies.
- PC Builder entry: reference page hero, template slots, and configuration summary.
- `ContactExperience`: new `/contact` route with a real contact-form submission to the backend inbox plus store settings channels.
- `RigxFooter`: reference footer structure with real departments, store/help links, social links, and settings-driven pickup data.

### Verified this round
| Check | Result |
|---|---|
| `npx tsc --noEmit` | 0 errors |
| `npx vitest run` | 37/37 |
| `npm run build` (NEXT_PUBLIC_SITE_URL=https://rigxhardware.com) | success |
| Playwright cart/builds/retail | 6/6 passed |
| Screenshots | `docs/review/v191-rebuild-2026-11-12/` (`rebuild-*`, `final-*`, 390px variants) |

Backend code was not modified in Round 2; the previously verified backend suite remains **190/190**. No migration was applied in Round 2.

### Honest remaining gaps
- Admin keeps its existing functional structure with the reference dark theme; it is not re-composed screen-by-screen into the 19 reference admin panels.
- Account area keeps its own pill navigation rather than the reference sidebar tabs.
- Content routes (articles/offers/videos/policies) inherit the reference theme but keep existing layouts; QA currently has little published content, so their empty states are what is visible.
- Product-specific FAQ, compare, shipping carriers, warranty registry, filters editor, trash/restore, and a separate contact phone remain absent in the backend.
- Reference-only visual assets (product cut-outs, logos, promotional renders) are not copied; real catalog media/fallbacks are used instead.

## 1. Workspace and safety
- Worked only in `DEV - Copy\HARDWARE STORE`.
- No production/Owner database or Docker volume changed.
- QA backup before the new migration: `.backups/rigx_qa_before_checkout_gaps_20261112.dump`.
- No frontend prototype HTML/CSS/JS was pasted or iframed.

## 2. Backend changes required by checkout
### Variant-aware orders
- `OrderLine` accepts `VariantId`.
- Customer endpoint resolves the active variant, verifies it belongs to the published product, uses variant price when set (base price fallback), and snapshots variant name/SKU/price.
- Order item now stores `VariantId`, `VariantName`, `ThumbnailStorageKeySnapshot`.

### Fulfillment/payment matrix
- Canonical fulfillment: `Delivery` / `Pickup` (legacy `Handover` maps to Pickup).
- Delivery: address required; payment only `CashOnDelivery`, `BankTransfer`, `MobileWalletTransfer`; `PaymentRequired=true`.
- Pickup: no public payment method accepted; stored as `ToBeArranged`; `PaymentRequired=false`; no address.
- New order remains `New` / `Unpaid`; no stock reservation before Admin confirmation.

### Terms consent
- Customer order requires `termsAccepted=true` and a published Terms policy.
- Server stores acceptance timestamp and policy version; client cannot supply either.
- Missing consent → 400; unpublished Terms → 409.

### Order snapshots/details
- Thumbnail snapshot stored from the authoritative primary catalog image.
- Added customer order detail `GET /orders/mine/{id}` and admin detail `GET /admin/retail/orders/{id}` with serial gating.
- Admin user search now also matches phone.

### Migration
- Retail `M7_OrderVariantsTermsThumbnails` applied to QA only.
- Existing rows remain valid (nullable columns).

## 3. Frontend replacement delivered
- Round 1 introduced `src/app/rigx-v191.css`; it was removed in Round 2 and is **not** part of the current archive. The live design layers are now `src/styles/rigx/*` (tokens, preview, v1.2–v1.9.1), `src/app/rigx-bridge.css`, and `src/app/rigx-pages.css`.
- Customer shell, catalog, product detail, cart, auth, account routes, builds, PC Builder, content pages now render in the new theme through shared tokens.
- Checkout rebuilt natively:
  - Delivery/Pickup selector.
  - Delivery address and delivery-only payment choices.
  - Pickup shows no payment selector and no address.
  - Terms/Privacy checkbox with links; server persists consent.
  - Item summary, delivery fee “to be confirmed by RIGX”, no fake totals.
  - Login redirect preserves cart and returns to `/checkout`.
- Order confirmation differentiates Delivery and Pickup and links to real order history.
- Customer order detail page added.
- Admin shell/dashboard/orders/catalog/inventory/purchasing/partners/identity/audit/content/media/reviews/settings restyled through the same theme.
- New Admin contact inbox and finance/reports screens wired to real APIs.
- Admin order detail page added with permission-gated serials.
- Removed the misleading dashboard “future modules” placeholder block.
- Admin manual order now uses Delivery/Pickup and valid payment allowlist.

## 4. Test evidence actually run
| Check | Result |
|---|---|
| `dotnet build HARDWARE_STORE.slnx` | 0 errors |
| Full backend xUnit/Testcontainers | **190/190 passed, 0 skipped** |
| `npx tsc --noEmit` | 0 errors |
| `npx vitest run` | 37/37 |
| `npm run build` with `NEXT_PUBLIC_SITE_URL` | success |
| Playwright cart/builds/retail | **6/6 passed** |
| Live checkout matrix | variant price 40,990; pickup `ToBeArranged/false/null`; invalid payment 400; missing terms 400 |

Screenshots: `docs/review/v191-rebuild-2026-11-12/` (home dark/light/390, products, product detail, cart, checkout delivery/pickup, login, admin dashboard/orders/detail/contact/partners, builds). Before baseline: `docs/review/overnight-2026-11-10/`.

## 5. Not completed / gaps
- Full bespoke V1.9.1 layout parity for all 13 customer and 19 Admin screens. Shared theme and existing layouts are replaced visually, but several reference-specific compositions are not recreated one-by-one.
- Admin shipping carrier, warranty registry, filters editor, trash/restore screens: no backend capability; not faked.
- Product-specific FAQ relation, separate ContactPhone, compare feature: backend absent.
- Offers are still display-only; no checkout discount/quantity engine.
- Google OIDC, Admin MFA, invoice/ETA PDFs remain blocked/out of scope.
- No production deployment; `rigxhardware.com` is only a build-time candidate URL.

## 6. Risks
- QA contains demo catalog/review text; production data must be real before launch.
- Full visual acceptance still requires owner/ChatGPT review against the V1.9.1 checklist.
- Terms policy must exist and be published in each environment before checkout can create orders.
- Admin manual-order operator must choose a valid fulfillment/payment combination.
