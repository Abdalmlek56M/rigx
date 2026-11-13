# RIGX — Next Integrated Execution (10 November 2026)

This round continued the verified Overnight checkpoint. It did not rebuild Retail or replace earlier modules.

## Completed

### P0 — Media reference integrity
- Added `IMediaReferenceGuard` in Catalog and `RetailMediaReferenceGuard` in the Host.
- Historical `InventoryUnit.ImageStorageKeysJson` snapshots now prevent generic Catalog cleanup and explicit content-media deletion.
- Failed deletion returns the referencing module/entity/description; `ListUsagesAsync` includes Retail inventory references.
- Catalog product-image deletion is also blocked when a historical unit keeps that image.
- Tests: media/catalog suite 9/9, including blocked delete + reference listing.

### P0 — Authorization matrix
- Added API-level role-matrix tests:
  - `Finance.ViewReports` reader can read partner earnings but gets **403** for agreement writes and settlement posting.
  - `Inventory.View`-only user sees units with cost/serial/source null, gets **403** on assemblies/orders, and sees cost after explicit `Inventory.ViewCosts` grant.
- Sensitive endpoints keep `no-store`.

### P1 — Multi-partner allocation
- New `partner_unit_allocations` table with unit/product/category scope, active flag, percent, loss-sharing and row version.
- Resolution precedence: unit → product → category → default agreement. Multiple partners can share one unit if total ≤100%.
- Earnings now unique per `(order item, unit, partner)`; each partner gets a transaction-time snapshot.
- Returns reverse **all** partner earnings for the returned unit; post-payout returns create one recovery adjustment per partner and offset future earnings per partner.
- Existing single-partner behavior remains the fallback.
- Tests: Retail 12/12, including mixed-partner order, 60/40 split, >100% rejection, and post-payout return for two partners.

### P1 — Email OTP authentication
- Added `EmailLoginCode` and `PasswordResetCode` token purposes, encrypted `protected_code`, attempt counter, 5-minute lifetime, 5-attempt limit, single use, resend invalidation.
- Durable encrypted outbox delivery; codes are never returned by API, logged or placed in URLs.
- Added `POST /api/v1/identity/email-otp/login/request|verify` and `/password-reset/otp/request|verify`; generic 202 responses avoid account enumeration.
- Login page has a password/email-code switcher; password login remains unchanged.
- Password OTP reset changes the password, bumps authorization version, revokes sessions and queues a security notification.
- Rate limits added (10 requests/hour, 20 verifications/10 min per IP); login limit remains 5/IP/min.
- Test: generic response, wrong-code attempts, code invalidation, new code, successful OTP login, reset, and password login with the new password (1/1).

### P1 — YouTube-first video library
- YouTube URL parser accepts only https `youtube.com`/`youtu.be`/`youtube-nocookie.com` video URLs and rejects playlists, http, arbitrary hosts, malformed IDs, `javascript:` and `data:` URLs.
- New YouTube records store provider and canonical ID, are Draft by default, and publish through the existing flow.
- Public player uses `https://www.youtube-nocookie.com/embed/{id}`, lazy iframe, no autoplay, plus a “Watch on YouTube” fallback link.
- Existing MP4/WebM records and local files remain unchanged.
- Admin video page adds a YouTube URL form and shows provider; parser tests 13/13 and live API create/publish/public-embed check.

### P1 — Public builds + manual orders (continuation)
- `/builds` purchase surface and cart → checkout remain verified (Playwright 1/1).
- Manual/phone order form remains and resolves prices server-side.

## Verification executed in this round
| Check | Result |
|---|---|
| `dotnet build HARDWARE_STORE.slnx` | 0 errors |
| Full xUnit/Testcontainers | **183/183 passed, 0 skipped** |
| Retail PostgreSQL | **12/12** |
| Media/Catalog PostgreSQL | **9/9** |
| Authorization matrix | **2/2** |
| Email OTP login/reset | **1/1** |
| YouTube URL parser | **13/13** |
| Playwright builds/cart/retail | **6/6** |
| `npx tsc --noEmit` | 0 |
| Vitest | 37/37 |
| Next production build (`NEXT_PUBLIC_SITE_URL`) | success |
| Live YouTube API check | create 200, publish 200, public embed true, invalid host 400 |

Migrations applied to QA only:
- Retail `M4_PartnerUnitAllocations`
- Identity `M11_EmailOtp`
- Catalog `M7_ContentVideoYouTube`

QA backups were taken before each migration:
`rigx_qa_before_multipartner_20261110.dump`, `rigx_qa_before_otp_20261110.dump`, `rigx_qa_before_youtube_20261110.dump`.

## Repository cleanup
- 13 obsolete checkpoint ZIPs (`187.2 MB`) were moved to `archive/checkpoints-2026-11-10/` with a manifest containing name, size and SHA-256.
- Nothing was deleted. The reference overnight ZIP remains in the workspace; new source archives exclude `archive/` and all build/test artifacts.

## Not completed / blocked
- **Google Sign-In**: no production credentials and no approved redirect domain; OIDC provider implementation remains pending. Not replaced by a fake button.
- **Admin MFA/TOTP and step-up**: policy plus enrollment/recovery procedure still owner-gated.
- **Contact Us form/admin inbox**: not implemented in this round.
- **Sales Invoice / Price Quotation PDFs**: not implemented; legal/tax profile is owner/accountant-gated.
- **Expenses and monthly/yearly reports**: not implemented.
- **Admin allocation-management UI**: multi-partner allocations are available through the API but the admin UI form is still pending; existing allocation rows are not editable in the UI yet.
- **Site-wide motion/accessibility audit beyond captured pages** and full responsive matrix 768/1024/1920 remain.
- **Google Search Console/Bing** cannot be connected without the approved domain.

## Security findings
Closed:
- Media snapshot references no longer bypass cleanup/deletion guards.
- Finance write permissions verified by direct API tests.
- Cost/serial/source/contact masking verified at API level.
- OTP raw codes are encrypted at rest, single-use, attempt-limited and never returned.
- YouTube URL validation rejects arbitrary iframes/URLs.

Residual:
- Google OIDC and admin MFA are absent, so admin protection is password + session only.
- Contact abuse controls do not exist because the route/form is not implemented.
- Allocation API lacks a UI; a mistaken API call can still deactivate an allocation (audited by row version but not by a separate audit event).
- No independent penetration test or dependency CVE audit was run this round.
- Production TLS/SMTP/off-host backup remain unconfigured.

## Owner decisions still required
1. Google OAuth client ID/secret + approved production redirect domain.
2. MFA policy and Owner recovery procedure.
3. Legal/tax seller identity and ETA applicability.
4. Production domain, hosting, SMTP, off-host backup, TLS/reverse proxy.
5. Final logo/brand direction.
