# RIGX — Project Overview & Current State

## Product
RIGX is a PC hardware retail platform: published catalog, per-unit inventory, retail orders, assemblies/builds, partner allocations, purchasing, expenses, storefront content, accounts, and an Admin workspace. It is a modular monolith deployed as ASP.NET Core + PostgreSQL API with a Next.js App Router frontend.

## Verified current state (12 November 2026)
- Solution: 10 production projects + 1 test project; 11 module projects. `Financials`, `Invoicing`, and `Sales` are **assembly boundaries only** (`ModuleMarker.cs`), not implemented modules.
- Backend: 296 C# files, 210 endpoint mappings, 190 backend tests, all passing.
- Frontend: 63 routes, 86 components, 30 unit tests, 13 Playwright specs (6 executed in the current smoke set), production build succeeds.
- Database: PostgreSQL via EF Core; migrations exist for Auditing, Catalog (two locations), Identity, Inventory, PcBuilder, Purchasing, Retail, Storefront.
- Auth: cookie sessions with server-side ticket store, CSRF middleware, rate limiting, role/permission authorization.
- UI baseline: Round 3 native V1.9.1 structures for customer/content/account/PC Builder; Admin panels and auth form bodies are Restyled Legacy.
- Production: **not** declared ready. See `10_HANDOFF_AND_READINESS.md`.

## Runtime used for evidence
- QA database: `rigx_m5_qa_20261108` on port 5435 (Preview container).
- API: `http://127.0.0.1:8091`; web dev: `http://127.0.0.1:3006`.
- No production deployment, paid service, database reset, or destructive migration was performed.

## Authoritative boundaries
- Allowed workspace: `C:\Users\Abdalmlek\Downloads\DEV - Copy\HARDWARE STORE`.
- Protected original: `C:\Users\Abdalmlek\Downloads\DEV\HARDWARE STORE` (untouched).
