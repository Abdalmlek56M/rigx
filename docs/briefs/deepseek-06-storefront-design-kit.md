# DeepSeek brief 06 — Storefront design kit and a professional pass on every screen

Read `AGENTS.md` and `docs/audits/storefront-ui.md` first. Work in your worktree on branch `deepseek/06-design-kit` (the owner creates it from the latest `main`). **Web only**: no API, no migrations, no packages, no `features/admin/**`.

The owner's bar (his words, summarised): the site must feel **world-class and professional**, not "it works, so it stays". Everything lives in organised cards with a title row; nothing floats in the air; label above value everywhere (like "البريد الإلكتروني" with the address under it, "رقم الموبايل" with the number under it), in the same grid on every page; icons (with a tooltip) wherever an action can be understood from an icon, a word button only for the single main action of a screen; calm spacing; no noise. Wherever you can add a meaningful icon, badge or small status mark, do. Do not leave a screen as it is because it already works.

## Files you may change
- `apps/web/src/components/**`, `apps/web/src/features/**` except `features/admin/**`, `apps/web/src/app/**` (layout and page files only), `apps/web/src/app/globals.css` (tokens and utilities only).
- `apps/web/messages/ar.json` and `en.json` outside `admin.*` (new keys and wording of keys you touch).
- Never `packages/**` or `src/**` (the .NET API).

## 1. The kit (`apps/web/src/components/ui/`) — build this first
Small, typed, documented components; each with one job. Use the existing tokens (`bg-surface`, `border-line`, `text-ink-muted`, `rounded-card`, `bg-carbon`, orange accents). Study `features/admin/kit/Button.tsx` and `SectionCard.tsx` and mirror their API so the two kits feel the same, but keep the storefront kit independent (do not import from admin).
- `Button`, `ButtonLink`, `IconButton`: variants primary (dark), accent (orange), secondary (outline), ghost, danger; sizes md / sm; `icon`, `loading`, `iconOnly`; `IconButton` requires `label` (aria-label + tooltip). Focus ring visible. Press feedback ≤ 120 ms, none under reduced motion.
- `Card`: bordered surface card with an optional title row (icon + title + actions) and `padding` sizes; `as="section"` support.
- `InfoGrid` + `InfoField`: `dl` grid of label (12 px, muted) above value (14 px, bold); `dir="ltr"` option for phone/e-mail/codes, `wide` to span columns, hides empty values, optional copy button.
- `StatusBadge`: tones (neutral, success, warning, danger, info) with an optional icon; one place for order, warranty, build and request statuses (migrate `account-labels.ts` colours into it).
- `PageHeader`: breadcrumb (when given), title (28 px), optional lead line and actions; the only way a page puts a heading on the page background.
- `EmptyState`: icon, title, one-line help, one action.
- `Skeleton` / `ErrorBlock` for lists.
- Type scale as utilities in `globals.css`: page title 28, section title 17, value 14, label 12; spacing rhythm 4 / 8 / 12 / 16 / 24 / 32.
- Tests for any pure helper (for example the badge tone mapping).

## 2. Migrate every screen onto the kit (no inline copies of button / card / field classes may remain; search for `bg-carbon`, `bg-orange text-on-orange`, `rounded-card border border-line bg-surface` under `features/` and `components/`)
- **Header** (`components/layout/Header.tsx`, `nav-links.ts`): fix the overflow documented in the audit: the full nav only from `xl`; from `md` to `xl` the menu button; search `min-w-0`; icon pill always fully visible at 768, 1024, 1280 and 1440. Check at those four widths.
- **Footer**: e-mail and phone as label/value rows with icons, `dir="ltr"`, no mid-word break; bottom padding so the floating WhatsApp and back-to-top buttons never cover text; social icons with `aria-label`.
- **Home**: equal-height cards, one badge style, icons on every card, formal copy is handled in brief 07 (do not rewrite wording here).
- **Catalog and builds**: filter panel without an inner scrollbar (sticky panel that fits, or a drawer); tabs without "0" counts when the count is 0; `PageHeader`; `EmptyState` with an icon and one action ("مسح الفلاتر"); skeleton cards while loading.
- **Product page and build page**: sections as cards (specs, delivery / warranty boxes, price panel) with icons; sticky purchase panel on desktop and a bottom bar on phones; quantity stepper with icon buttons.
- **Cart and cart drawer**: line rows with square image, icon remove button, stepper; totals card with label/value rows.
- **Tracking, warranty, account pages, invoice**: `InfoGrid` everywhere a label/value pair appears; `StatusBadge` for statuses; the invoice keeps its print layout but uses `InfoGrid` with a fixed order, adds the customer e-mail (only when present) and the address on its own row with governorate › area › street › building, and aligned lines/totals paddings (check A4 print).
- **Services, requests (sheets), policies, FAQ**: every block in a card or under a `PageHeader`; policies as a readable article card with a table of contents on desktop; FAQ as an accordion card with an icon per question group.
- **Buttons that are words but should be icons**: close, copy, share, print, remove, edit, compare, favourite, quantity. Keep one labelled primary action per screen.

## 3. Quality rules
- Light and dark, Arabic RTL and English LTR, 375 / 768 / 1024 / 1440 px, keyboard focus visible, every icon button has `aria-label`, no horizontal page scroll anywhere, touch targets ≥ 44 px on phones.
- Motion: ≤ 200 ms, none under `prefers-reduced-motion`.
- OOP-style separation: presentational components take props only; rules / mapping live in `*-rules.ts` with tests; no component over ~150 lines (split it); no duplicated helper; short doc comment on each kit component saying when to use it.
- Do not change API contracts or wording of Arabic copy beyond labels you replace with icons or new labels you need (brief 07 does the wording).

## Process (important)
1. Build the kit and write its tests. Commit nothing (the owner's reviewer commits).
2. Migrate screen by screen. After each group (header/footer, home, catalog, product, cart, account/tracking/warranty/invoice, services/policies/FAQ) start the app against the trial database, open the screens in the browser at the four widths in light and dark, and fix what looks off before moving on. Save screenshots under `%TEMP%\opencode\storefront-06\`.
3. Finish with the full checks.

## Done when
- From `apps/web`: `npm run typecheck`, `npm run lint`, `npm test`, `npm run build` pass.
- Report as `AGENTS.md` says, plus: the list of kit components with their props, a table "screen → checked widths → light/dark", the search results proving no inline button/card classes are left (with the count before and after), and anything you could not do because the API lacks data.
