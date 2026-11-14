# Checkpoint: Phase 3 API contract

Date: 14 Nov 2026

## Done
- OpenAPI 3.1 (`Microsoft.AspNetCore.OpenApi` 10.0.12, `Host/OpenApi/OpenApiSetup.cs`), served at `/openapi/v1.json` in Development only. 247 paths / 302 operations, all with typed success bodies. Each operation has a `default` ProblemDetails response, an area tag, and its permission and step-up need in the description (`StepUpRequired` metadata). It also documents the cookie and CSRF security schemes. Numbers are `number` only.
- Typed results: every handler returns `Ok<T>` / `Created<T>` / `Accepted<T>` / `NoContent` via `TypedResults`. Files use `.Produces<Stream>(200, contentType)`. Anonymous bodies became named records with the same JSON (`SignInResponse`, `AntiforgeryTokenResponse`, `AvatarUploadResponse`, `EmailChangeConfirmResponse`, `PublicProductReviews`, `PublicStoreReviews`); the duplicate `ProfileResponse` was removed in favour of `CurrentIdentityUser`.
- One error shape (ProblemDetails):
  - `AppException.Extensions` adds members such as `attemptsLeft`.
  - `UnauthenticatedException` (401) and `RequireUserId()` replace 111 hand-written 401 branches. Step-up uses it too.
  - `ApiProblemException` covers Host-level upload and filter errors.
  - CSRF rejection and sign-in failures (email code, Google) are ProblemDetails; they were `{ error }` before.
  - `UseStatusCodePages` gives empty 401/403/404/429 responses a ProblemDetails body.
  - The last endpoint try/catch blocks are gone (Offers, Reviews, ContentMedia, Inventory, Catalog media).
- Paging: `Common.Domain.Paging.PagedResult<T>` `{ items, page, pageSize, total }` replaces about 17 page records. `PageRequest.Of` replaces the copied clamping. `StoreItemPage` (`counts`) and `CustomerRequestPage` keep their extra members after the standard four.
- Contract: `packages/api-client/openapi.v1.json` is committed. `ApiContractTests` (3) checks that it matches the code, that every operation is typed with a ProblemDetails error, and that 401 and CSRF failures are ProblemDetails. Regenerate it with `UPDATE_API_CONTRACT=1`.
- Client `packages/api-client` (`@rigx/api-client`, openapi-fetch 0.17.0, openapi-typescript 7.13.0, TypeScript 5.9.3): generated types (`npm run generate`, also on install, not committed), `createRigxClient` (cookies, CSRF token fetched once and refreshed after identity calls or a 400), `unwrap` → `ApiError` (`status`, `problem`, `fieldErrors`), `STEP_UP_HEADER`. `npm run typecheck` (strict) is clean and `npm test` passes 4/4.
- Tests: full suite 961/961 (5 min, real PostgreSQL), build 0 warnings; client typecheck clean, 4/4 wrapper tests.
- Wire changes (no frontend existed yet):
  - `totalCount` → `total` on admin users, warehouses and the product search page.
  - The sold feed now returns `total` (one extra COUNT).
  - Sign-in and CSRF errors are ProblemDetails.
  - A disabled sold feed and unknown warehouses, users or POS customers now return a 404 with a title.

## Uncommitted
- none

## Remaining
- Phase 4 (frontend) starts by adding `apps/web` and consuming `@rigx/api-client`. A workspace link or `transpilePackages` is decided there.
- `NotificationPage` is `(items, unreadCount)`, not a paged list (left as is).

## Decisions — delegated to Claude (14 Nov 2026), defaults chosen
- The contract file is committed (it is the reviewed contract, checked by a test); the generated TypeScript is not.
- Path-based typed client (openapi-fetch) instead of a generated SDK with operation ids: no naming for 302 operations, smaller bundle.
- The OpenAPI document is served only in Development (the admin surface is not published in production).
