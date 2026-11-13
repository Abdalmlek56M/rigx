# RIGX — Security

## Already in place (verified in code/tests)
- Cookie auth: HttpOnly, `__Host-` prefix, `Secure` outside Development, `SameSite=Lax`, server-side ticket store, revocable sessions.
- CSRF middleware on all unsafe methods (`X-CSRF-TOKEN`).
- Permission checks per admin route (`RequirePermission`), covered by tests.
- Rate limits: login, OTP, registration, contact, search suggestions.
- Email OTP: 6 digits, 5 min, 5 attempts, single use, hashed at rest.
- Data-protection key ring must be certificate-protected outside Development.
- Forwarded headers trusted only from configured proxies.
- Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`).
- Double-sale prevention: row locks at order confirmation (PostgreSQL concurrency tests).
- Public DTOs exclude cost/supplier/serial/partner/private data.
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
9. Destructive admin actions (reset data, purge, export of customer data) require a fresh email code (step-up).

## Gaps to close (tracked in roadmap)
| Gap | Severity | Phase |
|---|---|---|
| Password login still exists | High (decision) | 2 |
| Google sign-in missing | Normal | 2 |
| Owner/admin step-up for destructive actions | High | 2 |
| Full audit log (before/after for all entities) | High | 1 |
| Content-Security-Policy (nonce-based with Next.js) | Normal | 4 |
| Off-host encrypted backups + restore drill | High | 2/5 |
| Migration runner outside dev-only "preview" mode | High | 0/1 |
| External penetration test before launch | High | 5 |
| Rate limits for new public endpoints (notify-me, warranty lookup, tracking) | Normal | 2 |
