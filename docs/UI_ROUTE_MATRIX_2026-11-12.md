# RIGX Route Matrix — 12 November 2026

Exactly one status per route. `Rebuilt` = native V1.9.1 structure, not theme-only. `Restyled Legacy` = reference theme/tokens over an existing composition. `Not Implemented` = no route/screen. `Backend Blocked` = UI omitted because the required backend/decision is absent.

## Customer and content

| Route | Status | Notes |
|---|---|---|
| `/` | Rebuilt | Native hero, service promos, categories, products, builds, editorial, FAQ |
| `/products` | Rebuilt | Native toolbar, filters, sorting, pagination, grid |
| `/products/[slug]` | Rebuilt | Native gallery/summary/specs/reviews/related |
| `/builds` | Rebuilt | Native builds hero and real inventory build cards |
| `/pc-builder` | Rebuilt | Native reference slots/summary entry |
| `/pc-builder/[id]` | Rebuilt | Native builder layout; existing evaluation logic preserved |
| `/cart` | Rebuilt | Native stepper, lines, summary, clear dialog |
| `/checkout` | Rebuilt | Native hero/stepper; real Delivery/Pickup/Terms order flow |
| `/contact` | Rebuilt | Native contact form + store channels |
| `/categories` | Rebuilt | Native department grid and sub-departments |
| `/articles` | Rebuilt | Native article grid and empty state |
| `/articles/[slug]` | Rebuilt | Native article document layout |
| `/faq` | Rebuilt | Native FAQ hero and accordion grid |
| `/offers` | Rebuilt | Native offer sections over live public offers |
| `/videos/[id]` | Rebuilt | Native video page and player panel |
| `/policies/[kind]` | Rebuilt | Native policy document layout |
| `/compare` | Not Implemented | No compare backend/contract |

## Authentication

| Route | Status | Notes |
|---|---|---|
| `/login` | Restyled Legacy | Auth shell rebuilt; password/OTP form body legacy |
| `/register` | Restyled Legacy | Auth shell rebuilt; form body legacy |
| `/forgot-password` | Restyled Legacy | Auth shell rebuilt; form body legacy |
| `/reset-password` | Restyled Legacy | Auth shell rebuilt; form body legacy |
| `/verify-email` | Restyled Legacy | Auth shell rebuilt; flow body legacy |
| `/resend-verification` | Restyled Legacy | Auth shell rebuilt; form body legacy |
| `/change-email` | Restyled Legacy | Auth shell rebuilt; form body legacy |

## Account

| Route | Status | Notes |
|---|---|---|
| `/account` | Rebuilt | Reference sidebar + overview panels; placeholders removed |
| `/account/profile` | Rebuilt | Account shell + profile panel |
| `/account/orders` | Rebuilt | Account shell + real order list |
| `/account/orders/[id]` | Rebuilt | Account shell + customer-scoped order detail |
| `/account/wishlist` | Rebuilt | Account shell + wishlist panel |
| `/account/reviews` | Rebuilt | Account shell + reviews panel |
| `/account/addresses` | Rebuilt | Account shell + addresses panel |
| `/account/security` | Rebuilt | Account shell + security panel |
| `/account/sessions` | Rebuilt | Account shell + sessions panel |
| `/account/notifications` | Rebuilt | Account shell + notifications panel |

## Admin

| Route | Status | Notes |
|---|---|---|
| `/admin` | Restyled Legacy | Reference shell/sidebar; dashboard composition legacy |
| `/admin/orders` | Restyled Legacy | Functional queue; not reference panel structure |
| `/admin/orders/[id]` | Restyled Legacy | Functional detail; not reference panel structure |
| `/admin/reports` | Restyled Legacy | Finance report/expense screen legacy composition |
| `/admin/contact` | Restyled Legacy | Inbox screen legacy composition |
| `/admin/partners` | Restyled Legacy | Allocations/earnings legacy composition |
| `/admin/catalog` | Restyled Legacy | Catalog workspace legacy composition |
| `/admin/catalog/categories` | Restyled Legacy | Taxonomy screen legacy composition |
| `/admin/catalog/brands` | Restyled Legacy | Brand screen legacy composition |
| `/admin/catalog/products/new` | Restyled Legacy | Product editor legacy composition |
| `/admin/catalog/products/[id]` | Restyled Legacy | Product editor legacy composition |
| `/admin/catalog/products/[id]/preview` | Rebuilt | Hosts the rebuilt product detail view |
| `/admin/inventory/stock` | Restyled Legacy | Stock screen legacy composition |
| `/admin/inventory/warehouses` | Restyled Legacy | Warehouse screen legacy composition |
| `/admin/purchasing/drafts` | Restyled Legacy | Drafts screen legacy composition |
| `/admin/purchasing/suppliers` | Restyled Legacy | Supplier directory legacy composition |
| `/admin/purchasing/suppliers/[id]/products` | Restyled Legacy | Supplier products legacy composition |
| `/admin/identity/users` | Restyled Legacy | User admin legacy composition |
| `/admin/identity/roles` | Restyled Legacy | Role admin legacy composition |
| `/admin/content/articles` | Restyled Legacy | CMS screen legacy composition |
| `/admin/content/faqs` | Restyled Legacy | CMS screen legacy composition |
| `/admin/content/policies` | Restyled Legacy | CMS screen legacy composition |
| `/admin/content/offers` | Restyled Legacy | CMS screen legacy composition |
| `/admin/home-content` | Restyled Legacy | Home layout editor legacy composition |
| `/admin/media` | Restyled Legacy | Media library legacy composition |
| `/admin/media/videos` | Restyled Legacy | Video library legacy composition |
| `/admin/audit` | Restyled Legacy | Audit log legacy composition |
| `/admin/reviews` | Restyled Legacy | Review moderation legacy composition |
| `/admin/storefront/settings` | Restyled Legacy | Store settings legacy composition |
| `/admin/pc-builder` | Restyled Legacy | PC Builder template admin legacy composition |

## Reference screens without a route

| Capability | Status | Reason |
|---|---|---|
| Admin shipping carriers | Backend Blocked | No carrier model/API |
| Admin warranty registry/claims | Backend Blocked | No warranty registry; only free-text notes |
| Admin filters editor | Backend Blocked | No persisted filter configuration |
| Admin trash/restore | Backend Blocked | No generic trash/undo API |
| Checkout promotions/quantity caps | Backend Blocked | Owner pricing rule not approved |
| Google OIDC | Backend Blocked | No approved credentials/redirect domain |
| Admin MFA / step-up recovery | Backend Blocked | Owner recovery policy not decided |
| Invoice / quotation PDFs | Backend Blocked | Legal/tax/ETA details not approved |
