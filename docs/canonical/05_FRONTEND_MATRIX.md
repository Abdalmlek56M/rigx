# RIGX — Frontend Route & Functionality Matrix

The per-route status is authoritative in `docs/UI_ROUTE_MATRIX_2026-11-12.md` (one status per route). This document records functional verification, not visual parity.

## Functional areas
| Area | Verification |
|---|---|
| Catalog/product | Live API data, variants, search, filters, sorting, pagination, related products |
| Cart | Local persistence, merge, quantity limits, totals, clear confirmation |
| Checkout | Authenticated only; server-side price/variant; Delivery address + COD/Bank/Wallet; Pickup without public payment/address; Terms persisted; order created `New/Unpaid`; no stock claim before confirmation |
| Orders | Customer `mine` IDOR-safe list/detail; Admin queue, status transitions, payment, waiver, returns, completion |
| Account | Profile, addresses, orders, wishlist, reviews, security, sessions, notifications — all API-backed |
| Auth | Password login, OTP login, registration, verify/resend, reset, email change; forms use live endpoints |
| Content | Articles, article detail, FAQs, offers, policies, videos — public APIs only |
| PC Builder | Template list, evaluation, compatibility messages, local save, share link |
| Admin | Catalog, taxonomy, suppliers, drafts, stock, warehouses, orders, partners, reports, contact, content, media, users/roles, audit, reviews, settings — permission-gated APIs |

## Functional gaps found
- Auth form bodies remain legacy compositions (functional, not rebuilt).
- Admin operational panels remain legacy compositions (functional, not rebuilt).
- Compare feature is not implemented and has no backend contract.
- Offers are display-only; checkout does not apply offer prices.
- Product-specific FAQ relation and separate contact phone are absent in the backend.
- Shipping carrier directory, warranty registry, filters editor, and trash/restore have no backend.

## Evidence
- Playwright: cart, builds, retail admin journey (6 executed tests).
- Backend: 190 tests covering orders, allocations, returns, auth, permissions, content, contact, article-video.
- Screenshots: `docs/review/round3-2026-11-12/` (1440 and 390).
