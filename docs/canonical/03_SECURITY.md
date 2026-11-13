# RIGX — Security Audit & Remediation (Round 4)

## Verified controls
- Cookie authentication: HttpOnly, `SameSite=Lax`, `__Host-` prefix + `Secure=Always` outside Development, server-side `ITicketStore`, sliding expiration and session revocation.
- CSRF: `CsrfProtectionMiddleware` rejects unsafe methods without a valid antiforgery token; `/identity/antiforgery` issues tokens with `no-store`.
- Permissions: explicit `RequirePermission` on Admin areas; authorization tested in the backend suite.
- Rate limiting: Identity login/OTP/registration/contact policies; tested limits retained.
- Data protection: certificate-protected key ring required outside Development.
- Forwarded headers: restricted to loopback plus configured proxies.
- Preview initialization: dev-only and configuration-gated.
- Secrets: no secrets are committed in source or included in archives; `.env*` excluded.

## Findings and remediation in this round
| Finding | Severity | Action |
|---|---|---|
| Concurrent order confirmation could pass an assembly component check after another sale claimed it | High | Fixed: assembly row locked; component status validated in-transaction |
| Product-unit claim used a plain read before update, allowing two confirmations to select the same units | High | Fixed: `FOR UPDATE SKIP LOCKED` selection inside the order transaction |
| EF `DbUpdateConcurrencyException` surfaced as HTTP 500 | Normal | Fixed: mapped to HTTP 409 with a retry-safe message |
| No baseline browser security headers | Normal | Fixed: API and Next.js now emit `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` |
| No Content-Security-Policy | Normal | Deferred: Next.js inline runtime requires a nonce-based CSP; not enabled blindly |
| No external penetration test / dependency CVE audit this round | High (release gate) | Deferred to independent security review |
| Admin MFA / step-up and Owner recovery policy | High (release gate) | Backend Blocked: owner policy not approved |
| Google OIDC | Normal | Backend Blocked: no approved client/redirect domain |

## Data protection boundaries
No production database, Docker volume, Owner data, or protected original workspace was modified. QA backups remain in `.backups/` and are excluded from archives.

## Round 4 follow-up
- Stock claim now captures the exact locked unit IDs (FOR UPDATE SKIP LOCKED) and loads only those rows; the assembly row is locked before component validation.
- Three real PostgreSQL concurrency tests prove single-sale semantics for units and assemblies and success when stock is sufficient.


## Dependency audit (12 Nov 2026)
- dotnet list HARDWARE_STORE.slnx package --vulnerable --include-transitive: 0 vulnerable packages across all projects.
- 
pm audit --omit=dev: 0 vulnerabilities across 20 production dependencies (equire/Next/React/Tailwind only).
- External penetration testing was not performed; no such claim is made.

## Cancellation integrity fix
- Cancelling a confirmed build now returns component units to InAssembly (not Available) and republishes the assembly, preventing the same components from being sold as standalone stock. Covered by a PostgreSQL test.

