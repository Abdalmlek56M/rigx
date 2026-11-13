# RIGX — Test Strategy & Executed Evidence (12 November 2026)

## Strategy
- Domain/integration tests use PostgreSQL Testcontainers and real EF contexts.
- API-level authorization and workflow tests exercise services and endpoints.
- Frontend unit tests cover cart, password policy, formatting, and header behavior.
- Playwright covers cart/checkout, builds, and the retail Admin journey against the running QA API and dev web.
- Every executed result below is from the current workspace; none is inferred from build success.

## Executed in Round 4
| Command | Environment | Result |
|---|---|---|
| `dotnet build HARDWARE_STORE.slnx --no-restore` | .NET 10, Windows | **Success, 0 errors** |
| `dotnet test tests/HardwareStore.Api.Tests/HardwareStore.Api.Tests.csproj --no-build` | PostgreSQL Testcontainers | **194 passed, 0 failed, 0 skipped** (2m40s) |
| `npx tsc --noEmit` | `apps/web` | **0 errors** |
| `npx vitest run` | jsdom | **9 files / 30 tests passed** |
| `npm run build` (`NEXT_PUBLIC_SITE_URL=https://rigxhardware.com`) | Next.js 16 | **Success** |
| `npx playwright test tests/e2e/cart-demo.spec.ts tests/e2e/builds.spec.ts tests/e2e/retail-core.spec.ts` | Chromium, API 8091, web 3006 | **6 passed** |
| Route sweep (public/account/admin) | Chromium | **0 uncaught page errors** |
| Retail integration subset after concurrency fix | Testcontainers | **16/16 passed** |`n| Concurrent double-sale tests (unit, sufficient stock, assembly) | Testcontainers | **3/3 passed** |`n| Build-cancellation stock release test | Testcontainers | **1/1 passed** |`n| `dotnet list package --vulnerable --include-transitive` | NuGet | **0 vulnerable packages** |`n| `npm audit --omit=dev` | npm | **0 vulnerabilities (20 prod deps)** |

## Not executed / blocked
- Full 13-spec Playwright suite: only the 3 journey specs above were executed; remaining specs were not run this round.
- No external penetration test or dependency CVE scan.
- No production smoke test (no deployment).
- Google OIDC / MFA / invoice flows not testable (not implemented).

## Key coverage
- Orders: pricing, variants, terms, delivery/pickup, status lifecycle, payment, waiver, completion.
- Inventory: stock claim/release, insufficient stock, assembly sale, returns.
- Partners: allocation precedence, earnings, settlements, recovery offsets.
- Identity: login, OTP, reset, sessions, permissions, rate limiting.
- Content: articles, policies, videos, offers, contact, reviews, media guards.

## Runtime header check (12 Nov 2026)
- API /health/live: X-Content-Type-Options: nosniff, X-Frame-Options: DENY.
- Web /: X-Content-Type-Options: nosniff, X-Frame-Options: DENY, Referrer-Policy: strict-origin-when-cross-origin.


## Concurrency proof added in this round
- Two orders for one unit confirmed concurrently: exactly one succeeds, one returns 409, one Sold unit, one order component.
- Two orders with two available units: both confirm and two distinct units become Sold.
- Two orders for one published assembly confirmed concurrently: exactly one succeeds, one returns 409, all components Sold once.
- Stock claim now locks exact unit IDs via FOR UPDATE SKIP LOCKED and then loads only those IDs; no re-selection by product/status.



## Stock release proof
- Cancelling a confirmed build returns its components to InAssembly and republishes the assembly.
- A standalone order for the same product cannot then claim those build components (confirmed by test).

