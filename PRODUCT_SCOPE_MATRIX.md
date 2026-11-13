# RIGX — PRODUCT_SCOPE_MATRIX

Baseline: 100% = الوظائف التجارية المطلوبة لمنصة RIGX كمتجر مصري كامل (Catalog + Accounts + Content + Purchasing + PC Builder + Orders/Invoicing + Inventory + Reporting + Operations/Production).

Weights are functional scope weights, not time estimates. A feature counts as Implemented only when it has backend + UI + authorization; Tested when it has automated evidence. Demo/local-only is not counted as Implemented.

| Area | Weight | Implemented | Tested | Notes |
|---|---:|---:|---:|---|
| Foundation / Architecture / Auth | 8 | 8 | 8 | Modular monolith, sessions, RBAC, CSRF, rate limits |
| Catalog + Taxonomy + Media | 14 | 13 | 12 | Monitors category still requires real catalog records; AVIF/responsive variants partial |
| Storefront UX + Navigation + Discovery | 12 | 11 | 10 | Builds/contact backend; UI owned by ChatGPT |
| Accounts / Security / Addresses / Wishlist | 10 | 9 | 9 | Password + Email OTP + OTP reset; Google/MFA pending |
| CMS / Content / FAQ / Policies | 8 | 8 | 7 | Owner texts still pending |
| Reviews + Moderation | 6 | 5 | 4 | Backend/UI/moderation; rate limiting pending |
| Offers | 4 | 4 | 3 | Admin/publish/public; coupon rules intentionally absent |
| Purchasing Drafts / External Sources / Manual Items | 8 | 8 | 6 | Receive→inventory implemented |
| PC Builder | 8 | 7 | 4 | Admin/Public UI + compatibility |
| Retail stock units / assemblies | 8 | 6 | 6 | Component allocation editable/validated; public builds page pending |
| Orders / Sales / manual payment | 10 | 8 | 6 | Manual order form + server pricing verified; no reports/PDF |
| Partner earnings / settlements | 5 | 5 | 5 | Net profit, per-unit, offsets, recoverable balance |
| Video Media | 3 | 3 | 3 | Uploaded player preserved + YouTube-first embed/library |
| Invoicing / Financials | 6 | 1 | 1 | Expenses/report backend only; invoices pending legal gate |
| Reporting / Analytics | 5 | 5 | 3 | Expenses + business report backend verified |
| Admin Governance / Audit | 6 | 5 | 3 | Restrictions backend; rich user timeline partial |
| Production readiness (separate) | 100 | 45 | — | No TLS/proxy/SMTP/backup/persistent video storage/Payment |

## Calculated scope (excluding Production readiness)
Total functional weight: 121.

Implemented weighted = 106 / 121 ≈ **87.6%** functional implemented.
Tested weighted = 91 / 121 ≈ **75.2%** automated-test evidence.

These are not production-readiness claims. Returns, expenses, installments, invoices/PDF, full reports and financial posting remain incomplete/blocked.

## Delta this round
- V1.9.1 UI layer + checkout rebuild: Delivery/Pickup matrix, server-side terms, variant pricing, thumbnail snapshots, order details.
- New contact inbox and finance/reports admin screens; dashboard fake future modules removed.
- Verified: backend 190/190, tsc 0, Vitest 37/37, Next build, Playwright 6/6, live checkout matrix.
- Remaining: bespoke V1.9.1 layouts for some screens; shipping/warranty/filters/trash/product-FAQ/contact-phone/compare; Google/MFA/invoices.

## Separate readiness (not part of functional 121)

| Area | Readiness | Evidence / gap |
|---|---:|---|
| Security verification | ~74% | Media guard, allocation audit/concurrency, finance split, masking, OTP limits, YouTube validation; MFA/Google/pentest absent |
| SEO readiness | ~55% | Production URL validation + paginated sitemap; localized crawl/CWV audit pending |
| Authentication readiness | ~55% | Password + Email OTP + OTP reset; Google OIDC and admin MFA absent |
| Backend/UI contract readiness | ~92% | Verified route/DTO/permission traceability; Google/MFA/invoice/offer-pricing contracts intentionally absent |
| V1.9.1 visual replacement | ~60% | Obsidian/Warm Orange applied across routes; checkout rebuilt; not every reference screen recreated 1:1 |
| Brand readiness | ~20% | Owner reference kit preserved; final logo unapproved |
| Launch readiness | ~30% | Local persistent volumes/backups; no TLS/proxy/hosting/SMTP/off-host backup/legal invoice profile |
