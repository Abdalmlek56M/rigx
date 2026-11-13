# RIGX UI Replacement — Route & Backend Map (12 November 2026)

Reference: RIGX V1.9.1. This map distinguishes native replacement work from screens that still need bespoke layout work or a missing backend capability.

## Customer routes

| Route | State | Backend |
|---|---|---|
| `/` | Rebuilt V1.9.1 hero, service promos, benefits, categories, latest products, builds, editorial, FAQ | catalog/categories/builds/articles/faqs |
| `/products` | Rebuilt V1.9.1 toolbar, filter rail, live stats, sorting, pagination, product grid | catalog products/categories/brands |
| `/products/[slug]` | Rebuilt gallery/summary, variant pricing, specs, reviews, related | catalog + reviews |
| `/builds` | Rebuilt V1.9.1 builds hero and build cards with real units/prices | `/store/assemblies` |
| `/pc-builder`, `/pc-builder/[id]` | Rebuilt entry with reference slots/summary; builder wizard preserved | pc-builder APIs |
| `/cart` | Rebuilt V1.9.1 stepper, cart lines, summary, clear confirmation | local cart |
| `/checkout` | Rebuilt: Delivery/Pickup, delivery-only payment, terms consent, authenticated order | `/orders` (variant/terms/payment matrix) |
| `/login`, `/register`, `/verify-email`, forgot/reset/resend | Restyled auth; password + Email OTP login | identity APIs |
| `/account`, profile, addresses, orders, order detail, notifications, reviews, security, sessions, wishlist | Restyled existing account routes; new order detail | identity/orders/addresses/reviews |
| `/articles`, `/articles/[slug]`, `/faq`, `/offers`, `/policies/[kind]`, `/videos/[id]` | Restyled existing content routes | CMS/video APIs |
| `/categories` | Category discovery on reference theme | catalog categories |\n| `/contact` | New native contact form + store channels | `POST /api/v1/contact` + settings |

## Admin routes

| Route | State | Backend |
|---|---|---|
| `/admin` | Restyled operational dashboard; demo “future modules” removed | analytics API |
| `/admin/orders`, `/admin/orders/[id]` | Queue + new detail with units/serials permission-gated | retail orders |
| `/admin/inventory/stock`, `/admin/inventory/warehouses` | Restyled stock/receipt/assemblies; warehouse metadata | retail/inventory |
| `/admin/catalog`, products/new/[id], preview, categories, brands | Restyled catalog admin | catalog |
| `/admin/purchasing/drafts`, suppliers, supplier products | Restyled purchasing | purchasing |
| `/admin/partners` | Allocations/earnings/ledger/settlement | retail partner APIs |
| `/admin/reports` | New finance/report screen + expense entry | expenses/business report |
| `/admin/contact` | New contact inbox with status actions | contact APIs |
| `/admin/pc-builder` | Restyled templates | pc-builder admin |
| `/admin/identity/users`, `/roles` | Restyled identity/RBAC | identity admin |
| `/admin/audit` | Restyled audit events | audit API |
| `/admin/content/articles`, faqs, policies, offers, home-content | Restyled CMS | storefront content |
| `/admin/media`, `/admin/media/videos`, `/admin/reviews`, `/admin/storefront/settings` | Restyled existing | media/reviews/settings |

## Reference screens not implemented as bespoke screens
- Admin shipping carrier directory: no backend carrier model; not faked.
- Admin warranty registry/claims: no backend warranty registry; only `WarrantyNote`.
- Admin filters editor: no persisted filter-config backend.
- Admin trash/restore: no generic undo/trash backend.
- Product-specific FAQ association: global FAQ exists; product relation absent.
- Separate public ContactPhone: only `WhatsAppNumber` exists.
- Compare: no compare backend/cart contract; not reintroduced as fake UI.

## Backend blockers closed in this round
- VariantId on order lines, authoritative variant price/SKU/name.
- Delivery vs Pickup matrix; pickup stores neutral `ToBeArranged`, no public payment, no address.
- Server-side Terms acceptance + policy version/timestamp; requires a published Terms policy.
- Immutable order thumbnail snapshot.
- Admin order detail and customer order detail.
- Authorized Admin phone search.

## Still open
- Google OIDC, Admin MFA, legal invoice/ETA, carrier/warranty/FAQ-relation/contact-phone backends, offers checkout pricing, full bespoke V1.9.1 layouts for every reference screen.

