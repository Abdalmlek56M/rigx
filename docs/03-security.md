# RIGX — Security

## Already in place (verified in code/tests)
- Cookie auth: HttpOnly, `__Host-` prefix, `Secure` outside Development, `SameSite=Lax`, server-side ticket store, revocable sessions. Browser session capped at 12h unless "remember this device" (persistent cookie).
- CSRF middleware on all unsafe methods (`X-CSRF-TOKEN`).
- Permission checks per admin route (`RequirePermission`), covered by tests.
- Rate limits: email code request, email code verify (also Google sign-in), email change + confirm, customer requests (`customer-request`, 5 / 10 min per IP, replaces contact), search suggestions.
- Email sign-in code: 6 digits, 5 min, 5 attempts, single use, stored protected (encrypted), resend timer, attempts left returned. Code sign-in creates a customer on first use. No passwords, no SMS.
- Google sign-in (ID-token flow): server validates signature, issuer, audience (`Identity:Google:ClientId`) and lifetime; no client secret stored. First use links to the account with the same Google-verified email or creates a customer; one Google link per account. Without `ClientId` the Google endpoints return 404.
- New-device alert email: sent when the account has earlier sessions but none from the same user agent. First sign-in never alerts. On/off per account (default on).
- Step-up (fresh email code in header `X-Step-Up-Code`, `RequireStepUp` filter) on: create role, change role permissions, change user roles, restrictions, admin Google unlink, linking Google, email change, reset data, purge, customer data export.
- Data-protection key ring must be certificate-protected outside Development.
- Forwarded headers trusted only from configured proxies.
- Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`).
- Double-sale prevention: row locks at order confirmation (PostgreSQL concurrency tests).
- Public DTOs exclude cost/supplier/serial/partner/private data, except the invoice link (below).
- Warranty lookup: anonymous, rate-limited 20 / 10 min per IP (`warranty-lookup`); phone masked to last 3 digits; no name, serial or prices.
- Invoice link `GET /api/v1/invoices/{token}`: anonymous, 32-byte unguessable token (the only access control), rate-limited (order-tracking policy), `no-store`. Returns the full customer document by design (name, phone, address, lines, serials, prices).
- Quote `POST /api/v1/store/quote`: anonymous, rate-limited, no customer data, no invoice number.
- Shipping rates `GET /api/v1/storefront/shipping-rates`: anonymous, public shipping settings only (pickup address, governorate fees and delivery times), no customer data.
- Requests `POST /api/v1/requests`: anonymous (links the user when signed in), rate-limited `customer-request` 5 / 10 min per IP, honeypot field, duplicate within 10 min returns the same code. Returns 202 with the public `REQ` code only, no request data. Admin routes need `Requests.View` / `Requests.Manage`.
- Request photo upload `POST /api/v1/requests/photos`: anonymous, authorized only by the upload token (32 random bytes, only the SHA-256 hash stored, 30 min, max 5 photos); rate-limited `request-photo` 20 / 10 min per IP; bad or expired token returns 404. Files are re-encoded to WebP (EXIF/GPS stripped), stored under `private/`, never watermarked. Admin read `GET /api/v1/admin/requests/{id}/photos/{index}` needs `Requests.View`, `no-store`.
- Private media not served publicly: `GET /api/v1/catalog/media/...` returns 404 for `private/` keys; orphan cleanup skips `private/` and `settings/`. Expense receipts `GET /api/v1/admin/retail/expenses/{id}/receipt` need `Finance.ViewReports`, `no-store`.
- Dependency audit (12 Nov 2026): 0 known vulnerable NuGet packages.
- Trash purge: owner-only + step-up (`System.Purge`); restore needs `System.Trash`.
- Exports: audited (`System.Exported`), permission per dataset, cost and contact columns only with their permission, customers export needs step-up, CSV formula neutralization, 50,000-row cap.
- Backup download: owner-only (`System.BackupDownload`) + step-up, `no-store`. Backup, verify and download are audited.
- Data reset: owner-only (`System.ResetData`) + step-up; a successful BeforeReset backup first, else nothing is deleted. Audited, also on failure.
- Owner-only permissions (`System.Purge`, `System.BackupDownload`, `System.ResetData`) cannot be granted to custom roles.
- Business audit log is append-only: a database trigger rejects UPDATE, DELETE and TRUNCATE on `audit_entries` (M47), and the app role lost those grants. Viewing needs `System.ViewAuditLogs`.
- Audit log masks secrets as `"***"`: seller national ID, order tracking token, invoice access token, request photo token hash, and every byte[] field.
- Restore of previous values: owner-only (`Audit.Restore`) + step-up; allowlisted fields only; 409 if the record changed since.

## Rules every change must keep
1. **Never trust the client**: prices, totals, discounts, stock, permissions and tier perks are computed on the server.
2. **Least privilege**: every admin endpoint declares a permission; cost/profit fields only for roles with `Inventory.ViewCosts`/`Finance.ViewReports`.
3. **No secrets in the repo**: `.env*` ignored; production secrets only from the host's secret store.
4. **Every write is audited** (actor, time, before/after); the audit log is append-only.
5. **No hard deletes** of business data — soft delete to trash; purge is owner-only and audited.
6. **Validate every input** (length, range, enum, file type/size); uploads re-encoded server-side (also strips EXIF/GPS).
7. **Customer lookups don't leak**: warranty-by-phone shows masked data; OTP/login responses are generic.
8. **Rate-limit anything public that sends email/WhatsApp or searches by phone** (notify-me, warranty lookup, tracking).
9. Destructive admin actions and the step-up list above require a fresh email code (`X-Step-Up-Code`).

## Gaps to close (tracked in roadmap)
| Gap | Severity | Phase |
|---|---|---|
| ~~Password login still exists~~ | Closed: removed in 2.1 | 2 |
| ~~Google sign-in missing~~ | Closed in 2.1 | 2 |
| ~~Owner/admin step-up for destructive actions~~ | Closed in 2.1 | 2 |
| ~~Full audit log (before/after for all entities)~~ | Closed in 2.11 (business audit log). Open part: `ExecuteUpdate`, `ExecuteDelete` and raw SQL writes bypass the interceptor (listed in the 2.11 checkpoint) | 2 |
| ~~Content-Security-Policy~~ | Closed in 4.8: static allowlist CSP on the web (no nonce, decision 45) and `default-src 'none'` on the API | 4 |
| ~~Off-host encrypted backups + restore drill~~ | Closed in 4.8: daily rclone **crypt** copy to Google Drive (`deploy/offsite-backup.sh`, 30 days); restore drill from 2.10 | 2/5 |
| ~~Migration runner outside dev-only "preview" mode~~ | Closed: `--migrate` (1.2) runs as the one-shot `migrate` service before the API in production (4.8) | 0/1 |
| External penetration test before launch | High | 5 |
| ~~Rate limits for notify-me submissions~~ | Closed in 2.12: notify-me goes through the rate-limited requests endpoint | 2 |
| Rate limits for new public endpoints (warranty lookup, tracking): partly closed | Normal | 2 |
