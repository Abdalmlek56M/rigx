# RIGX — Repository Inventory & Cleanup Report

## Inventory summary
| Area | Count / location |
|---|---|
| Production projects | 10 (`src/Common` 3, `src/Host` 1, `src/Modules` 8 implemented + 3 marker-only) |
| Backend C# files | 296 |
| API endpoint mappings | 210 across 30 endpoint files |
| Backend tests | 38 files in `tests/HardwareStore.Api.Tests` |
| EF migration sets | 8 module locations (Catalog has two directories) |
| Frontend routes | 63 `page.tsx` files |
| Frontend components | 86 `.tsx` files (after cleanup) |
| Frontend unit tests | 9 files / 30 tests |
| Playwright specs | 13 files in `apps/web/tests/e2e` |
| Documentation | `docs/` 46 Markdown files + root reports |
| Generated/build | `bin`, `obj`, `.next`, `test-results`, `playwright-report` (excluded from archives) |
| Backups | `.backups/` (excluded from archives) |
| Historical checkpoints | `archive/` (excluded from archives) |

## Cleanup classification
### Removed (proven unused)
Verified no import, dynamic import, test, or runtime reference before removal:
`storefront-home.tsx`, `storefront-carousel.tsx`, `storefront-category-carousel.tsx`, `storefront-discovery.tsx`, `storefront-rail.tsx`, `storefront-section-heading.tsx`, `catalog-product-card.tsx`, `catalog-products-experience.tsx`, `cart-page.tsx`, `product-detail-gallery.tsx`, `site-header.tsx`, `site-header.test.tsx`, `add-to-cart-button.tsx`, `notification-bell.tsx`.

The obsolete header test was replaced by `rigx-header.test.tsx`, which covers the live header.

### Retained
- All EF migrations and model snapshots — historical database state must remain reproducible.
- `Financials`, `Invoicing`, `Sales` projects — assembly boundaries referenced by the solution; removing them would change the architecture contract.
- `archive/` and `.backups/` — explicit historical/backup evidence, excluded from delivery archives.
- Dated root reports — retained as history, superseded by `docs/canonical/`.
- `globals.css` and legacy `rigx-*` selectors still used by account/admin surfaces.
- `PreviewInitializationService` and `OwnerBootstrapCommand` — development/operations tooling.

### Uncertain / compatibility
- `catalog-admin-v2.tsx` and `catalog-admin.tsx` coexist; both are routed/used by different Admin screens and were not merged in this round.
- Legacy `--ds-*` token names are retained in `rigx-bridge.css` for admin/account surfaces.

## Duplicate/obsolete documentation
Historical reports are indexed in `docs/history/INDEX.md`. No documentation was deleted solely for age; canonical documents define precedence.

## Generated assets
Images under `docs/review/` and `apps/web/public/demo/` are retained as evidence/fixtures. They are excluded from the no-images archive by design.
