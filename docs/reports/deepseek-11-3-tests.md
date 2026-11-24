# Report 3: tests for pure rules

## Summary
- Added **4 new test files, 32 test cases**, all passing (`node --test` on those files, and `tsc --noEmit` is clean).
- No non-test file was edited. **No bug was found** by the new tests, so nothing is held back.
- Method: a script listed every exported function in `*-rules.ts`, `*-query.ts`, `*-state.ts` and `lib/*.ts` whose name appears in no test file, then I kept the ones that hold real rules. Constants and React hooks were left out. `pos-rules.ts` and `purchase-rules.ts` look untested in that list, but their functions are exercised through `toSaleRequest` and the form checks in the existing tests, so I did not duplicate them.

## New files
| File | Rules covered | Cases |
|---|---|---|
| `apps/web/src/features/admin/orders/order-state.test.ts` | `canCancel`, `holdsReservation`, `hasInvoice`, `hasShipping` (checked over every order status, so a new status forces a decision), `paymentStatusKey`, `orderContent`, `waiverReasonValid` | 7 |
| `apps/web/src/lib/contact-links.test.ts` | `contactHref`, `contactLinkProps` (phone stays in place, others open a new tab with `noopener`), `findLink`, `displayPhone` (international, local and other lengths), `distinctPhone` (same number as WhatsApp is dropped whatever the separators) | 9 |
| `apps/web/src/lib/lookups.test.ts` | `parseCondition`, `parseUsage`, `findCategory`, `categoryById`, `formatEgp` (Arabic and English labels, grouping, two decimals) | 11 |
| `apps/web/src/features/admin/settings/free-shipping.test.ts` | Free shipping in `shippingForm` / `shippingValue`: off by default, minimum as text, zero shown empty, empty sent as zero, switch kept even with an empty minimum, round trip | 6 |

## Findings (what the tests showed)
| Severity | Location | Note |
|---|---|---|
| Low | `features/admin/settings/shipping-rules.ts:36` | With the switch on and an empty minimum the form sends `enabled: true, minimum: 0`. Checked: the server refuses it (`ShippingSettings.cs:65`, 400 "Free shipping needs a minimum order amount above zero"), so free shipping can never apply to every order by mistake. The form could say so before sending (optional). |

## Not covered on purpose
- Constants (`ORDER_STATUSES`, `PRICE_OPTIONS`, …) and React hooks (`useAuditLabels`, `useFormDraft`, `useSectionDraft`).
- `checkout-draft.ts` read/write helpers: they use `localStorage`, which `node --test` does not provide.
- `pos-rules.ts`, `purchase-rules.ts`, `user-rules.ts`: covered through their larger functions.

## Questions for the owner
None.

## What I did not check
- Backend tests (not part of this task) and Playwright flows.
