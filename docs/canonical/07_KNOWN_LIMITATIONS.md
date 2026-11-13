# RIGX — Known Limitations & Deferred Capabilities

## Release blockers
1. **Admin UI parity incomplete** — 28 Admin routes are Restyled Legacy, not reference-structure rebuilds.
2. **Auth screen bodies** — shell rebuilt, form composition legacy.
3. **No independent external penetration test** — dependency audits (NuGet + npm) passed with 0 known vulnerabilities, but an external pentest was not performed.
4. **Admin MFA / Owner recovery** — blocked pending owner policy.
5. **Legal invoice/quotation output** — blocked pending seller/tax/ETA decisions.
6. **Production data** — QA contains seeded catalogue/review text; must be replaced before launch.

## High priority (not blockers for design phase)
- CSP not enabled (requires nonce-based Next.js integration).
- Admin analytics still describes sales reporting as unavailable even though retail data exists; the dashboard needs a data-source decision.
- Offer pricing/quantity engine not implemented (owner pricing rule absent).
- Product-specific FAQ, separate contact phone, compare, shipping carriers, warranty registry, filters editor, trash/restore have no backend.
- Unit archive/retire without return/scrap flow is not implemented.

## Backend Blocked
- Google OIDC credentials/redirect domain.
- Admin MFA/step-up and recovery.
- Invoice/quotation PDF + legal identity.
- Production SMTP recipient for contact notifications.
- Production deployment/hosting.

## Maintainability risks
- `RetailService` (1,319 lines) and `CatalogApplication` (1,180 lines) are large service classes with multiple responsibilities; splitting them is a future refactor, not a release-scope change.
- Legacy `--ds-*` token bridge remains for admin/account surfaces.
- Some dated documentation at root is historical and only indexed, not moved.
