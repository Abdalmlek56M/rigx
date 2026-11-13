# RIGX — Backend/UI Contract (ChatGPT frontend workstream)

Last updated: 10 November 2026. All routes are same-origin under `/api/v1`. Unsafe requests require the antiforgery header `X-CSRF-TOKEN` obtained from `GET /api/v1/identity/antiforgery` (`requestToken`). Sensitive responses use `Cache-Control: no-store`.

## 0. Conventions
- Money: decimal EGP, 2-decimal snapshots. Never send client-calculated prices for customer checkout; the server resolves current catalog/assembly prices.
- Concurrency: mutable admin records return `rowVersion`; send it back. Mismatch → `409` with a Problem title.
- Errors: `400` validation, `401` unauthenticated, `403` permission/authorization, `404` missing, `409` state/concurrency conflict, `429` rate limited.
- Private fields (cost, supplier/source, serial, partner share, internal notes) are role-gated and may be `null`; UI must show “Restricted”.
- Ordinary PDFs are not ETA e-invoices. Invoice endpoints remain pending legal configuration.

## 1. Authentication
### Password
`POST /api/v1/identity/sessions` anonymous, rate limit 5/IP/min
```json
{ "identifier": "string", "password": "string" }
```
→ `200 { "sessionScope": "Full" }`; invalid → `401`.

### Email OTP (additional method)
`POST /api/v1/identity/email-otp/login/request` anonymous, 10/hour/IP
```json
{ "email": "customer@example.test" }
```
→ `202 { "status": "accepted" }` (generic even when the email is unknown).

`POST /api/v1/identity/email-otp/login/verify` anonymous, 20/10min/IP
```json
{ "email": "customer@example.test", "code": "123456" }
```
→ `200 { "sessionScope": "Full", "method": "EmailOtp" }`; `401` generic for invalid/expired/attempt-locked codes.

`POST /api/v1/identity/password-reset/otp/request` same request/response as login request.
`POST /api/v1/identity/password-reset/otp/verify`
```json
{ "email": "customer@example.test", "code": "123456", "newPassword": "new password" }
```
→ `200 { "status": "password-reset" }`; policy failure `400`; invalid code `401`.
Password reset bumps authorization version and revokes sessions.

### Google OIDC — pending
No live endpoint exists. Required owner configuration: client ID, client secret, approved redirect URI. Planned contract will be added only with real provider integration; no fake button or endpoint.

## 2. Public catalog / builds / orders
- `GET /api/v1/catalog/products` — public catalog page/filter/sort; no private costs.
- `GET /api/v1/catalog/products/{slug}` — public product.
- `GET /api/v1/store/assemblies` — published builds only:
```json
[{ "id": "uuid", "titleAr": null, "titleEn": "RIGX build", "salePrice": 2500, "units": [{ "productId": "uuid", "name": "GPU", "sku": "GPU-1", "condition": "New" }] }]
```
- `POST /api/v1/orders` authenticated; real order request. Server resolves product/variant/assembly prices and requires a published Terms policy.
```json
{ "customerName": "...", "phoneNumber": "...", "addressText": "...|null", "fulfillmentMethod": "Delivery|Pickup",
  "paymentMethod": "CashOnDelivery|BankTransfer|MobileWalletTransfer|ToBeArranged", "notes": null,
  "termsAccepted": true, "leadSource": "Website",
  "items": [{ "productId": "uuid", "variantId": "uuid|null", "quantity": 1 }, { "assemblyId": "uuid", "quantity": 1 }] }
```
Rules:
  - Delivery: address required; payment must be `CashOnDelivery`, `BankTransfer`, or `MobileWalletTransfer`; `paymentRequired=true`.
  - Pickup/Handover by arrangement: no public payment method; server stores neutral `ToBeArranged`, `paymentRequired=false`, address null. Client payment is ignored.
  - `termsAccepted` must be true and a published Terms policy must exist; server stores acceptance timestamp and policy version. Missing → `400`; no published Terms → `409`.
  - New order is `New` / `Unpaid`; no stock claim. Unknown/inactive/foreign variant → `409`; delivery address missing or invalid payment → `400`.
  - Response item includes `variantId`, `variantName`, `thumbnailStorageKeySnapshot`.
→ `200 OrderView`. Errors as above.
- `GET /api/v1/orders/mine/{id}` authenticated → own `OrderView` or `404`.
- Public search suggestions: existing catalog search contract; no acquisition cost/supplier/serial/internal notes exposed.

## 3. Retail admin — stock, assemblies, orders
Permissions: `Inventory.View` for stock read; `Inventory.ViewCosts`/`Inventory.ViewSerials` for private fields; `Catalog.ManageProducts` for assemblies; `Sales.ViewOrders`, `Sales.CreatePOS`, `Sales.ApproveOrder`, `Sales.CompleteOrder`, `Sales.ProcessReturn`, `Finance.VerifyPayment`.

### Units / receipt
- `GET /api/v1/admin/retail/units?status=&productId=` → `UnitView[]`; cost/source/purchase snapshots/serial may be `null`.
- `POST /api/v1/admin/retail/receipts/from-draft/{draftId}` `Purchasing.ReceiveGoods`, body `{ "notes": null }`; idempotent.
- `GET /api/v1/admin/retail/assemblies` → components + allocationPercent + salePrice; cost fields gated.
- `POST /api/v1/admin/retail/assemblies` `{ "name": "...", "unitIds": ["uuid"] }`
- `PUT /api/v1/admin/retail/assemblies/{id}` body `{ name, publicTitleAr, publicTitleEn, salePrice, rowVersion }`
- `PUT /api/v1/admin/retail/assemblies/{id}/allocations` body `{ "rowVersion": 3, "allocations": [{ "unitId": "uuid", "percent": 60 }, { "unitId": "uuid", "percent": 40 }] }`; percentages must total 100.
- `POST /api/v1/admin/retail/assemblies/{id}/publish|unpublish` `{ rowVersion }`
- `POST /api/v1/admin/retail/assemblies/swap` `{ fromAssemblyId, toAssemblyId, unitId, fromRowVersion, toRowVersion }`
- `POST /api/v1/admin/retail/assemblies/{id}/disassemble` `{ rowVersion }`

### Orders
- `GET /api/v1/admin/retail/orders?status=`
- `POST /api/v1/admin/retail/orders` `Sales.CreatePOS`; server resolves published product/assembly prices. Body:
```json
{ "customerName": "...", "phoneNumber": "...", "addressText": "...", "fulfillmentMethod": "Handover",
  "paymentMethod": "CashOnHandover", "paymentRequired": true, "leadSource": "Phone",
  "items": [{ "productId": "uuid", "quantity": 1 }, { "assemblyId": "uuid", "quantity": 1 }] }
```
- `POST /orders/{id}/status` `{ status, notes, rowVersion }`; allowed New→Confirmed→Preparing→Ready→Delivered and Cancel before Completed. `Completed` is rejected here.
- `POST /orders/{id}/complete` `Sales.CompleteOrder` `{ rowVersion, notes }`; requires Delivered + Paid/valid Waived.
- `POST /orders/{id}/payment` `{ paymentStatus: "Unpaid|PendingVerification|Paid", notes, rowVersion }`
- `POST /orders/{id}/payment/waive` `{ reason, rowVersion }`; sets `Waived`, never `Paid`.

### Returns
- `GET /api/v1/admin/retail/returns?status=`
- `POST /returns` `Sales.ProcessReturn` `{ orderId, orderItemId, unitId, refundAmount, reason }`; unit → PendingInspection.
- `POST /returns/{id}/inspect` `{ condition: "New|Used|Open Box|Scrap", notes, rowVersion }`; restocks or scraps, reverses all partner earnings.
- `POST /returns/{id}/reject` `{ notes, rowVersion }`; restores Sold and keeps the sale.

## 4. Partners — allocations, earnings, settlement
Read: `Finance.ViewReports`; write: `Finance.ManagePartnerAgreements` / `Finance.PostPartnerSettlements` (never `Finance.ViewReports`).

### Allocations (new)
- `GET /admin/retail/partner/allocations` → rows `{ id, partnerName, unitId, productId, category, percent, lossSharingEnabled, isActive, rowVersion }`.
- `GET /admin/retail/partner/allocations/preview?unitId=&productId=&category=` → `{ source: "Unit|Product|Category|DefaultAgreement|None", allocations, totalPercent, unallocatedPercent }`.
- `GET /admin/retail/partner/allocations/{id}/history` → append-only events `{ action, actorUserAccountId, reason, snapshotJson, createdAtUtc }`.
- `POST /admin/retail/partner/allocations`:
  - Create: `{ partnerName, unitId|productId|category, percent, lossSharingEnabled, isActive }`
  - Update: same + `rowVersion` + `reason` (≥3 chars). Missing rowVersion on existing → `409`.
  - Aggregate active % per effective scope must stay ≤100%.
- `POST /admin/retail/partner/allocations/{id}/deactivate` `{ rowVersion, reason }`.

Precedence: Unit → Product → Category → default agreement. Only active rows count. Preview and earnings use the same resolver. Earnings/return/offset remain per partner.

### Earnings / ledger / payouts
- `GET /partner/earnings?partner=`, `GET /partner/adjustments?partner=`, `GET /partner/ledger`
- `GET /partner/agreements`, `POST /partner/agreements`
- `POST /partner/settlements` `{ partnerName, amount, method, reference, note }` — amount must not exceed cash due after offsets.

## 5. Expenses and business reports (new)
Permissions: write `Finance.ManageExpenses`; read/report `Finance.ViewReports`.
- `POST /admin/retail/expenses`
```json
{ "expenseDate": "2026-11-10", "amount": 50, "category": "Packaging",
  "classification": "Direct|GeneralOverhead", "note": null, "orderId": "uuid|null",
  "unitId": "uuid|null", "attachmentReference": null }
```
→ `ExpenseView`. Amount must be > 0; no silent allocation of overhead.
- `GET /admin/retail/expenses?from=YYYY-MM-DD&to=YYYY-MM-DD`
- `GET /admin/retail/reports/business?from=&to=` → `BusinessReportView`:
  `grossSales, discounts, refunds, costOfGoods, itemDirectExpenses, additionalDirectExpenses, netItemProfit, generalOverhead, storeNetResult, partnerAccrued, partnerPaid, partnerRecoverable, completedOrders, returns`.
Period is Egypt/UTC-day based on completed orders’ last-updated timestamp; returns counted when inspected.

## 6. Contact inbox (new)
- `POST /api/v1/contact` anonymous, 5/10min/IP:
```json
{ "fullName": "...", "email": "...", "phone": null, "subject": "...", "message": "...", "website": "" }
```
`website` is a honeypot; non-empty is silently accepted and discarded. Duplicate email+message within 10 minutes is suppressed. → `202 { status: "accepted" }`; invalid → `400`.
- `GET /api/v1/admin/contact-messages?status=&page=&pageSize=` permission `System.StoreSettings`.
- `POST /api/v1/admin/contact-messages/{id}/status` `{ status: "New|Read|Closed", rowVersion }`; stale → `409`.

## 7. Video library / article references
- `POST /api/v1/admin/media/videos` multipart upload (existing MP4/WebM).
- `POST /api/v1/admin/media/videos/youtube` `{ titleAr, titleEn, descriptionAr, descriptionEn, url }`, permission `Catalog.ManageMedia`; only YouTube hosts, returns provider/ID/embedUrl.
- `POST /admin/media/videos/{id}/publish|unpublish`; unpublish is `409` when a published article references the video.
- `GET /api/v1/content/videos`, `GET /api/v1/content/videos/{id}`; public records include `provider`, `youtubeVideoId`, `embedUrl` (nocookie embed) for YouTube.
- Article references:
  - `GET /api/v1/admin/content/articles/{id}/videos` and `PUT` `{ videoIds: ["uuid"] }`, permission `Catalog.ManageMedia`.
  - `GET /api/v1/content/articles/{slug}/videos` public returns only videos referenced by a published article and themselves Published, ordered by sortOrder.

## 8. Pending / not available
- Google OIDC endpoints and frontend button.
- Admin TOTP enrollment/step-up and recovery codes.
- Invoice/quotation PDF and document numbering; legal seller fields.
- Offer quantity caps and checkout-price application rules; no GPU-specific hard-coding exists.
- Sitemap/SEO page output is frontend-owned; backend sitemap data remains public-only.

## 10. Additional customer/account routes (actual implementation)
All unsafe calls require `X-CSRF-TOKEN`; all authenticated routes require the session cookie.

| Method + route | Auth | Request → response |
|---|---|---|
| `POST /api/v1/identity/registrations` | anonymous, 10/hour/IP | `CustomerRegistrationRequest { fullName, phoneNumber, username, email }` → 202 generic; invalid field → 400 |
| `POST /api/v1/identity/email-verifications` | anonymous, 20/10min/IP | `EmailVerificationRequest { token, password }` → lifecycle result |
| `POST /api/v1/identity/email-verification-resends` | anonymous, 10/hour/IP | `{ email }` → generic 202 |
| `POST /api/v1/identity/password-reset-requests` | anonymous, 10/hour/IP | `{ email }` → generic 202 |
| `POST /api/v1/identity/password-resets` | anonymous, 20/10min/IP | `{ token, newPassword }` |
| `POST /api/v1/identity/password-changes` | authenticated | current password + new password; sessions revoke on success |
| `GET /api/v1/identity/me`, `PATCH /profile` | authenticated | current user/profile DTO |
| `POST /profile/avatar`, `DELETE /profile/avatar` | authenticated | image upload/remove |
| `GET/POST/PUT/DELETE /api/v1/identity/addresses` | authenticated | `CustomerAddressView`; owner-scoped |
| `GET/POST/DELETE /api/v1/wishlist`, `DELETE /wishlist`, `POST /wishlist/merge` | authenticated | wishlist item DTOs; guest merge |
| `GET /api/v1/notifications`, `POST /{id}/read`, `POST /read-all` | authenticated | notifications |
| `GET /api/v1/identity/sessions`, `DELETE /sessions/{id}`, `/sessions/current`, `/sessions/others`, `/sessions` | authenticated | session list/revoke |
| `POST /api/v1/identity/email-change/request`, `/confirm` | authenticated | email-change flow |
| `GET /api/v1/storefront/settings` | anonymous | public WhatsApp/social/pickup/trust settings |
| `GET/PUT /api/v1/admin/storefront/settings` | `System.StoreSettings` | admin settings |

## 11. Additional Admin route inventory
| Route | Permission | Purpose |
|---|---|---|
| `GET/POST /api/v1/admin/catalog/categories`, `PUT /categories/{id}`, `POST/DELETE /categories/{id}/image` | `Catalog.View` read; `Catalog.ManageTaxonomy` write; `Catalog.ManageMedia` image | category admin |
| `GET/POST /api/v1/admin/catalog/brands`, `PUT /brands/{id}`, `POST/DELETE /brands/{id}/logo` | same pattern | brand admin |
| `GET /admin/catalog/products`, `GET /products/{id}`, `POST/PUT`, `PATCH /products/{id}/status` | `Catalog.View` read; `Catalog.ManageProducts` write | product admin |
| `POST/PUT/DELETE /admin/catalog/products/{id}/images*` | `Catalog.ManageMedia` | product images, reference guarded |
| `GET /admin/catalog/media-audit`, `POST /media-audit/cleanup` | `Catalog.ManageMedia` | orphan report/cleanup, guarded |
| `GET /admin/media/assets`, `POST`, `PUT /{id}`, `POST /{id}/replace`, `DELETE /{id}`, `GET /{id}/usages` | `Catalog.ManageMedia` | content media library |
| `GET /admin/content/homepage`, `PUT`, `POST /publish` | `System.StoreSettings` | homepage CMS |
| `GET/POST /admin/content/articles`, `GET/PUT /{id}`, `GET /{id}/revisions`, `POST /{id}/publish|unpublish` | `System.StoreSettings` | articles/revisions |
| `GET/POST /admin/content/faqs`, `PUT`, `POST /publish|unpublish` | `System.StoreSettings` | FAQ |
| `GET/POST /admin/content/policies`, `POST /{kind}/versions`, `PUT /{id}`, `POST /{id}/review|publish` | `System.StoreSettings` | policies |
| `GET /admin/content/articles/{id}/videos`, `PUT` | `Catalog.ManageMedia` | article video block |
| `GET /admin/content/offers`, `POST`, `PUT /{id}`, `POST /{id}/publish|unpublish|cancel` | `System.StoreSettings` | offers |
| `GET /admin/pc-builder/templates`, `POST`, `PUT /{id}`, `POST /{id}/publish|unpublish` | `Catalog.ManageProducts` | PC templates |
| `GET /admin/reviews`, `POST /{type}/{id}/moderate` | `Catalog.ManageProducts` | review moderation |
| `GET /api/v1/admin/analytics/dashboard` | `Catalog.View` | operational aggregates |
| `GET /api/v1/admin/audit/events` | `System.ViewAuditLogs` | filters fromUtc/toUtc/eventType/outcome/actor/subject/correlation/search/page/pageSize |
| `GET /api/v1/admin/identity/users`, `GET /{id}`, `POST /users`, `POST /{id}/password-reset`, `PATCH /{id}`, `PUT /{id}/roles` | `System.ManageUsers` | user admin; never returns plaintext passwords |
| `GET /admin/identity/roles`, `POST`, `PUT /{id}/permissions`, `GET /permissions` | `System.ManageUsers` | RBAC |
| `GET /admin/identity/users/{id}/restrictions`, `POST`, `POST /restrictions/{id}/reinstate` | `System.ManageUsers` | account restrictions |
| `GET /admin/purchasing/drafts`, `GET /{id}`, `POST`, `PUT`, `POST /{id}/cancel` | `Purchasing.View` / `Purchasing.CreateOrder` | purchase drafts |
| `GET/POST /admin/purchasing/suppliers`, `GET/PUT /{id}`, `GET/PUT /{id}/financials`, supplier products/variants | `Purchasing.*` per route | supplier directory |
| `GET/POST /admin/inventory/warehouses`, `GET/PUT /{id}` | `Inventory.View` / `Inventory.ManageWarehouses` | warehouse metadata only |

## 12. Response/error notes
- `OrderView` includes items with components (`unitId`, allocationPercent, revenue/discount/direct expense/cost/netProfit), events, payment waiver fields and `rowVersion`.
- `GET /api/v1/orders/mine/{id}` returns `OrderView` scoped to the authenticated account (`404` otherwise).
- `GET /admin/retail/orders/{id}` returns `AdminOrderDetailView { order, units }`; `units[].serialNumber` is null without `Inventory.ViewSerials`.
- `/api/v1/catalog/search/suggestions` returns public `ProductSuggestionView[]` (`id,name,slug,sku,basePrice,condition,categorySlug,brandName,thumbnailStorageKey`) and never cost/source/serial.
- No generic undo endpoint exists. Reversals use compensating business actions (cancel/return/recovery), not deletion.
- No customer installment, Google login, MFA, offer quantity cap or invoice endpoint exists yet.

## 13. Field sensitivity
Always role-gated and nullable for unauthorized callers: acquisition cost, source/supplier name/type, purchase kind/date, serial, partner percent override, assembly component costs/allocated amounts, customer phone/address (admin list), internal notes, audit metadata. Never expose these in public catalog/builds/search/video responses.
