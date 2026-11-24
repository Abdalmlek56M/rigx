# Report 4: RTL, responsive and accessibility static audit

Checked: all `*.tsx` and `*.css` under `apps/web/src` (static search plus reading the suspicious spots). No browser run.

## Summary
- The code base is in good shape: only **3 files** use physical-direction classes, no image is missing `alt`, `<html>` sets `lang` and `dir`, and one global `prefers-reduced-motion` rule covers all CSS motion.
- Real gap: the **admin kit buttons stay 36 px on a phone** (storefront buttons grow to 44 px). One screen patches this by hand.
- Everything else is low.

## Findings
| Severity | Location | Problem | Fix |
|---|---|---|---|
| Medium | `features/admin/kit/Button.tsx:31-34` | `SIZES.sm` is `h-9` / `size-9` (36 px) with no phone rule. The storefront `components/ui/Button.tsx:21,36` adds `max-sm:min-h-11` and `max-sm:size-11`. Every small admin button and icon button (tables, inventory rows, history) is under the 44 px touch target on a phone, the main way the owner works. | Add `max-md:h-11` to `SIZES.sm.label` and `max-md:size-11` to `SIZES.sm.square` (same as storefront). Then delete the local patch `TOUCH = "max-md:size-11"` in `features/admin/inventory/InventoryShortcuts.tsx:11` (that file works around the gap; a second copy of the rule already exists there). |
| Low | `components/ui/Button.tsx:64-67,72-74` | `Button` accepts `iconOnly` with an **optional** `label`. A caller can render an icon-only button with no accessible name and TypeScript will not complain (`IconButton` does require it). Today every use passes a label, so no live bug. | Make the props a union: `iconOnly: true` requires `label`. Or remove `iconOnly` from `Button` and keep `IconButton` only. |
| Low | `features/admin/stock/PhotoGallery.tsx:87,89,99` | Order badge, main-photo star and menu are placed with `left-1`, `left-8`, `right-1`. In Arabic the numbered badge is on the left and the menu on the right, in English the same, so the layout does not mirror with the page. Probably intended (badge is `dir="ltr"` on purpose) but undocumented. | If it should mirror: `start-1`, `start-8`, `end-1`. If not: add a one-line comment saying the corner is fixed. Needs the owner's visual choice (see Questions). |
| Low | `features/builder/QuoteSheet.tsx:87,120,133` | `text-right` / `text-left` chosen with `locale === "ar" ? … : …`. Same result as `text-start` / `text-end` because the sheet already carries `dir` from the locale. | Replace with `text-start` / `text-end` and remove the ternaries (3 lines shorter). |
| Low | `features/admin/AdminSidebar.tsx:46`, `features/admin/kit/DonutChart.tsx:25`, `features/admin/system/ExportPrintSheet.tsx:66` | `text-[10px]`. 24 more places use `text-[11px]`. Arabic at 10-11 px is hard to read on a phone. | Raise the visible ones (sidebar badge, donut labels) to 12 px. The print sheet may stay. |
| Low | `features/admin/builds/BuildActions.tsx:27`, `orders/OrderDetail.tsx:65`, `orders/OrderInvoiceLinks.tsx:22` | `target="_blank"` on an internal `Link` without `rel`. Modern browsers add `noopener` themselves and the target is the same site, so no leak. | Optional: add `rel="noreferrer"` for consistency with `InventoryShortcuts.tsx:45`. |

## Checked and fine (not findings)
- Physical positions kept on purpose: `globals.css:407` (`left:50%` centering), `globals.css:778`, `ScaledPaper.tsx:40` (computed offset), `WatermarkPreview.tsx:69` (photo coordinates), `Rail.tsx` (`scrollLeft` maths, handles the RTL minimum).
- Icon-only buttons: `IconButton` (store and admin) requires `label` and sets `aria-label` plus tooltip. Search input has a `sr-only` label (`SearchForm.tsx:13`). Admin inputs without a visible label (`Brands.tsx`, `FilterBar.tsx`) use `sr-only` labels or `aria-label`.
- Images: no `<img>` or `<Image>` without `alt`.
- Fixed widths: `OrderBody.tsx:13` and `PosCart.tsx:183` (`min-w` tables) sit inside `overflow-x-auto`. `QuoteSheet.tsx:145` is inside the scaled print paper. Other `w-[…px]` values are behind breakpoints.
- Motion: `globals.css:1124` turns animations and transitions off under `prefers-reduced-motion: reduce`; JS timers (`Rail`, `TopBarTicker`, `BackToTop`, `Toast`, count-up) check it too.
- Radios in `RemovePartDialog.tsx:42,46` are 20 px but sit inside a full-width `<label>` row, so the touch area is the row.

## Questions for the owner
1. Photo order badge and menu in the admin photo gallery: should they swap sides in English (mirror), or stay fixed?

## What I did not check
- Real rendering at 375 px, colour contrast and focus order (needs a browser).
- Keyboard traps inside dialogs and menus.
- PDFs and print layouts beyond reading the classes.
