# DeepSeek brief 08 — English names, WhatsApp number at checkout, and fixes from the review of brief 06

Read `AGENTS.md` first. Work in your worktree on a branch `deepseek/08-english-names` created from the latest `main` (which already contains brief 06). **Web only**: no API, no migrations, no packages, no `features/admin/**`. Wording is brief 07's job: do not rewrite existing copy, only add the keys you need (any new Arabic text is formal Arabic, no colloquial words).

The API already returns what is needed (generated client is up to date; regenerate with `npm run generate` in `packages/api-client` if your copy is stale):
- `GET /api/v1/catalog/categories` → each category has `nameEn` (nullable).
- Shipping rates (checkout governorate list) → each `GovernorateShippingRate` has `nameEn`.
- `OrderCustomer` (checkout request) accepts `whatsAppNumber` (optional, digits; used only when `phoneHasWhatsApp` is false).

## 1. English names on the English site
- Add one helper (pure, tested) `localizedName(item: { name: string; nameEn?: string | null }, locale: string)`: on `/en` use `nameEn` when it is a non-empty string, otherwise `name`; on `/ar` always `name`. Put it in `apps/web/src/lib/` and use it everywhere a category or governorate name is shown: header mega menu and mobile menu, home category grid and rails, catalog filters and breadcrumbs, product breadcrumb, footer links, checkout governorate select (also the order summary and success page), account addresses, and any chip that shows a category path. Do not change slugs or URLs.
- The governorate shown on an order the customer already placed comes from the order (a stored name): leave those as they are.
- Tests for the helper (both locales, null, empty and whitespace `nameEn`).

## 2. Checkout: a separate WhatsApp number
In the checkout customer card: when "رقمي عليه واتساب" is **unchecked**, show an optional field "رقم واتساب آخر للتواصل" (label above the value, `dir="ltr"`, `inputMode="tel"`, the same phone validation as the main phone, "اختياري" pill). Send it as `customer.whatsAppNumber` only in that case; when the checkbox is checked send nothing. Include it in the checkout autosave draft (and clear it with the rest). Add it to the request builder test.

## 3. Fixes found while reviewing brief 06 (screenshots in `%TEMP%\opencode\storefront-06\`)
1. **Product specs card** (`features/product/SpecsTable.tsx`): the two specs sit at the far ends of a full-width row with a big gap. Use a 2-column grid on `md` and up (3 from `xl`), each cell a label above the value, left-aligned to its column start, with the card padding the same as the other cards.
2. **Invoice header** (`features/invoice/InvoiceSheet.tsx`): the contact line ("rigxhardware.com · 0128…") floats between the title and the logo, and the address sits under the logo on a different baseline. Make the header two clear blocks: the logo block (logo, address, phone, e-mail as label/value rows, `dir="ltr"` for phone and e-mail) on one side and the invoice title + number + date on the other, aligned to the same top edge; no text in the middle. The `INV-…` number stays `dir="ltr"`.
3. **Product cards** (`features/catalog/ItemCard.tsx`, home rails): a tall empty gap between the picture and the title. The picture must be a square (1:1) and the title starts directly under it with the same gap on every card; the favourite heart sits on the picture corner, not in the text area.
4. **Floating buttons**: the WhatsApp button and the back-to-top button must never cover card text on phones; keep the page bottom padding and move them above the purchase bottom bar on product/build pages (they overlap it today).
5. Search the storefront for any remaining word-only buttons that an icon could carry (with `IconButton` + tooltip) and for any heading that sits on the page background outside `PageHeader`; fix what you find and list it in your report.

## Checks and report
From `apps/web`: `npm run typecheck`, `npm run lint`, `npm test`, `npm run build`. Browser check at 375 / 768 / 1024 / 1440 in `/ar` and `/en`: home, catalog, product, checkout (both checkbox states), invoice, warranty. Report: files changed, what you could not do, and screenshots folder. Leave everything uncommitted on the branch; do not push.
