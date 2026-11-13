# RIGX — Security

## Already in place (verified in code/tests)
- Cookie auth: HttpOnly, `__Host-` prefix, `Secure` outside Development, `SameSite=Lax`, server-side ticket store, revocable sessions. Browser session capped at 12h unless "remember this device" (persistent cookie).
- CSRF middleware on all unsafe methods (`X-CSRF-TOKEN`).
- Permission checks per admin route (`RequirePermission`), covered by tests.
- Rate limits: email code request, email code verify (also Google sign-in), email change + confirm, contact, search suggestions.
- Email sign-in code: 6 digits, 5 min, 5 attempts, single use, stored protected (encrypted), resend timer, attempts left returned. Code sign-in creates a customer on first use. No passwords, no SMS.
- Google sign-in (ID-token flow): server validates signature, issuer, audience (`Identity:Google:ClientId`) and lifetime; no client secret stored. First use links to the account with the same Google-verified email or creates a customer; one Google link per account. Without `ClientId` the Google endpoints return 404.
- New-device alert email: sent when the account has earlier sessions but none from the same user agent. First sign-in never alerts. On/off per account (default on).
- Step-up (fresh email code in header `X-Step-Up-Code`, `RequireStepUp` filter) on: create role, change role permissions, change user roles, restrictions, admin Google unlink, linking Google, email change, reset data, purge, customer data export.
- Data-protection key ring must be certificate-protected outside Development.
- Forwarded headers trusted only from configured proxies.
- Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`).
- Double-sale prevention: row locks at order confirmation (PostgreSQL concurrency tests).
- Public DTOs exclude cost/supplier/serial/partner/private data.
- Warranty lookup: anonymous, rate-limited 20 / 10 min per IP (`warranty-lookup`); phone masked to last 3 digits; no name, serial or prices.
- Dependency audit (12 Nov 2026): 0 known vulnerable NuGet packages.

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
| Full audit log (before/after for all entities) | High | 1 |
| Content-Security-Policy (nonce-based with Next.js) | Normal | 4 |
| Off-host encrypted backups + restore drill | High | 2/5 |
| Migration runner outside dev-only "preview" mode | High | 0/1 |
| External penetration test before launch | High | 5 |
| Rate limits for new public endpoints (notify-me, warranty lookup, tracking): partly closed, notify-me left (2.8) | Normal | 2 |
