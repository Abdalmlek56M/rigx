# RIGX — Engineering Standards

Applies to every human or AI contributor. The goal: code that is easy to extend, easy to debug, and reused instead of copied.

## Code
- **Small units.** A file over ~300 lines or a method over ~40 lines is a smell; split by use case, not by "helpers".
- **One place per rule.** A business rule (price floor, allocation = 100%, status transition) exists once, in Domain, and is called from everywhere that needs it.
- **No duplication across modules.** If two modules need the same thing, it goes to `Common` (only if truly generic) or one module exposes it via `Contracts`.
- **Expected failures**: throw a module exception that derives from `AppException` (with the right status). Never catch it in an endpoint — the global handler maps it.
- **Names say the business**: `ConfirmOrder`, `SwapComponent`, `ReserveUnits` — not `Process`, `Handle2`, `Helper`.
- **No magic strings** for statuses, permissions, settings keys — constants/enums.
- **Time via `TimeProvider`**, money via `decimal`, ids via `Guid`.
- **No commented-out code, no dead code, no TODO without an issue/roadmap item.**
- Nullable reference types and warnings-as-errors on (`Directory.Build.props`), with unused private members/parameters (IDE0051/0052/0060) failing the build; TypeScript `strict`, no `any`.
- Read the signed-in user only through `HttpContextUserExtensions` (`TryGetUserId`, `HasPermission`).

## Before writing code, ask
1. Would I write it this way from scratch? If not, fix it now or record why not.
2. Does a module/function for this already exist? Reuse it.
3. Which permission, validation, audit and test does this change need?

## Tests (enough, not maximal)
- **Domain rules**: fast unit tests (no DB).
- **Use cases with money/stock/permissions**: integration tests on real PostgreSQL (Testcontainers).
- **Critical user flows only** in Playwright: browse → cart → order; admin confirm → ship; POS sale; purchase intake; login; warranty lookup.
- Before refactoring risky code: add characterization tests first, then refactor.
- Don't re-run the full suite after every small edit; run the affected module's tests, and the full suite before a commit that touches shared code.

## Git
- `main` is always buildable. Work on short branches: `feat/…`, `fix/…`, `refactor/…`, `docs/…`, `chore/…`.
- Conventional commits: `feat(sales): reserve units on confirm`.
- One logical change per commit; no generated files, archives, logs, secrets.
- No zip snapshots or `.backups` folders — git history is the backup.

## Docs
- `docs/` holds the only truth. When behavior changes, the matching doc changes in the same commit.
- No dated "report" files at repo root. Progress lives in the roadmap checkboxes; decisions in `02-architecture.md §8`.

## Working rules for AI agents (cost discipline)
- Read a large file **once**, take notes, then act — don't re-read it to answer a follow-up.
- No polling loops, no repeated builds/test runs without a code change in between.
- Ask the owner when a business decision is missing; don't guess and don't stall.
- Prefer one well-scoped session per roadmap step.
