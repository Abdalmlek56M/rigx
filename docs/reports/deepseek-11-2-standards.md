# Report 2: standards review of recent code

Range: `462ef14..HEAD` (17 commits, design system v2 to the Phase G docs). Method: file sizes, an export-usage scan of `apps/web/src` (a script that lists exports nothing else imports), pattern searches, and reading the spots below. No edits.

## Summary
- The code follows the standards well: no `TODO`/`FIXME`, no commented-out code, only 4 files over 300 lines among recent changes (all justified), backend rules have tests, and a test enforces the badge kit.
- Real findings: **7 exports that are dead in production** (3 of them kept alive only by tests), **two copies of the "optional" pill**, and **two button kits that have drifted apart** (touch size rule exists in one only).

## Findings
| Severity | Location | Problem | Fix |
|---|---|---|---|
| Medium | `features/admin/kit/Button.tsx:31-34` and `components/ui/Button.tsx:21,34-36` | The two kits repeat `BASE`, `SIZES`, `buttonClass` and `IconButton`. They have already drifted: the storefront set grows to 44 px on a phone (`max-sm:min-h-11`, `max-sm:size-11`), the admin set stays at 36 px. `InventoryShortcuts.tsx:11` then re-adds the rule locally (`TOUCH`). Same bug found in report 4. | Put the size table in one shared file (`components/ui/button-sizes.ts`) imported by both, with the phone rule in it. Delete `TOUCH`. |
| Medium | `components/ui/EmptyIcon.tsx` | Component with **no reference anywhere** (not imported, not used in a test). | Delete the file. |
| Medium | `features/admin/users/users-api.ts:23` | `fetchUser` is exported and never used (the user page loads data another way). | Delete it. |
| Low | `components/ui/status-badge-rules.ts:58` (`paymentTone`), `features/admin/builds/build-rules.ts:18` (`sellModeKey`), `features/admin/kit/table-sort.ts:9,15` (`parseSort`, `formatSort`), `features/admin/policies/policy-rules.ts:41` (`emptyPolicyForm`) | Exported and used **only by their own test files**. The tests keep dead code looking alive. | Delete each with its test case. If one is planned for the returns screen, add it back when that screen is built. |
| Low | `features/checkout/form-parts.tsx:25` and `features/checkout/ExtrasSection.tsx:54` | The same hand-made "optional" pill (`rounded-full bg-paper px-2 py-0.5 text-[11px]…`) exists twice; `ExtrasSection` should call the one in `form-parts`. | Reuse the `form-parts` component in `ExtrasSection`. If the kit has a neutral badge tone, use `StatusBadge` for both. |
| Low | `features/admin/builds/ReplacePartDialog.tsx:60` | A hand-made orange pill (`rounded-full bg-orange-soft … text-orange-text`) instead of the kit badge, which the rules say every badge must use. | Use `StatusBadge` with the closest tone. |
| Low | `features/admin/settings/shipping-rules.ts:15-17,36-37,48-49` | The settings rules are tested (`settings-rules.test.ts`), but nothing asserts the free-shipping fields (`freeShippingEnabled`, `freeShippingMinimum`), the part of batch 2 the owner cares about. | Added `free-shipping.test.ts` (report 3). |
| Low | `features/admin/orders/order-rules.ts`, `lib/contact.ts`, `lib/format.ts`, `lib/condition.ts`, `lib/usage.ts`, `lib/categories.ts` | Small rule functions with no test that names them (for example `canCancel`, `hasInvoice`, `contactHref`, `distinctPhone`, `formatEgp`, `parseCondition`). | Added four test files (report 3). |
| Low | `app/globals.css` (1,337 lines), `ApiErrors/ApiErrorMessages.cs` (687), `components/ui/icons.tsx` (636), `purchase-rules.ts` (372), `EmailLayout.cs` (342), `pos-rules.ts` (326), `CatalogPersistence.cs` (316) | Over the ~300-line smell. The first three are data tables or a style sheet; the rest are single-theme files. | Leave the tables. Split `purchase-rules.ts` (payments vs lines) and `EmailLayout.cs` (layout vs the separate email bodies) the next time they change. |

## Checked and fine
- No `TODO`/`FIXME`/`HACK` in code (`ORDER_TODOS` is a domain name, not a note).
- Backend rules from the period are covered by tests: `BuildViewRules`, `BuildViewCounter`, `AuditDays`, `CatalogOrdering`, `MoneyRules`, `TrackingRules`, `PosCustomerQuery`, free-shipping `FeeFor`, email `Greeting`.
- Badge, tile and disabled-button rules are enforced by `components/ui/kit-rules.test.ts` and `status-badge-rules.test.ts`.
- New endpoints return typed results and read the user through the shared helpers (checked on the Host diff).
- `CatalogEndpoints.cs` was split into `.Products`, `.Public`, `.Taxonomy` (317 lines moved out); no logic duplicated in the split.

## Questions for the owner
None. All findings are technical clean-up and do not change what the customer sees.

## What I did not check
- Methods longer than 40 lines (no tool for it here).
- Logic inside `.tsx` components that could move to `*-rules.ts` (only sampled).
- Docs consistency (`docs/design/storefront-kit.md` against the code).
- Backend dead code: the build already fails on unused private members.
