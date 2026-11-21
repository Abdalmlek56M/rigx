# DeepSeek brief 05 — Storefront polish: checkout, cards and gallery, account, warranty, invoice

Read `AGENTS.md` first. Work in your worktree on branch `deepseek/05-storefront` (the owner creates it from the latest `main`). **Web only**: no API, no migrations, no packages. Background: "R5" in `docs/checkpoints/4.10d-owner-feedback.md`.

The owner tested the public site. It works, but some screens feel unfinished: loose text, inconsistent images, a checkout that loses what the customer typed, and informal wording. The goal is a calm, trustworthy shop in **formal Arabic** everywhere. Reuse what exists (`components/ui/*`, `ModalSheet`, `TextField`, `SelectField`, icons from `components/ui/icons.tsx`; add a missing icon there as a small inline SVG in the same style; no icon or carousel packages).

## Files you may change
- `apps/web/src/features/checkout/**`, `catalog/**`, `product/**`, `builds/**`, `account/**`, `warranty/**`, `invoice/**`, `tracking/**`, `auth/**` (only to let the login flow run inside a sheet, see 2.6)
- `apps/web/src/components/layout/LocaleSwitch.tsx`, `apps/web/src/components/ui/**` (small shared pieces and icons)
- `apps/web/messages/ar.json` and `en.json`, outside `admin.*`
- Never `features/admin/**`, `packages/**`, `src/**` (the .NET API).

## 1. Language switch (`LocaleSwitch.tsx`)
- The switch shows "العربية" in the Arabic font and "EN" in the Latin font, each with its own `lang` attribute (`lang="ar"` / `lang="en"`), same height and weight as the other header buttons. No mixed-font fallback.

## 2. Checkout (`features/checkout/**`)
1. **Optional fields:** "اختياري" becomes a small muted pill at the end of the label (not inline text). Under "كيف تعرّفت علينا؟" add the helper line "يساعدنا ذلك على تحسين خدماتنا." Lead source options become selectable chips (one choice, keyboard and screen-reader friendly: a radio group styled as chips).
2. **Coupon box (`CouponBox.tsx`):** the input uses the same font, height and border as the other fields; placeholder "أدخل كود الخصم"; the apply button sits inside the same row; success and error lines are formal.
3. **Missing fields:** when submit fails on the client, show one alert at the top of the form that **names** the missing fields ("يرجى استكمال: رقم الهاتف، المحافظة") and move focus and scroll to the first one. Remove any text like "في بيانات ناقصة فوق". Keep the per-field errors.
4. **Autosave:** the form (customer, fulfillment, address, extras, notes; never coupon results or anything sensitive) is saved to `localStorage` under a versioned key on change (debounced ~400 ms) and restored on load, so a refresh, crash or sign-in round trip keeps it. Cleared after a successful order. Every storage call in try/catch; the page works without storage. Put the read/merge logic in a pure helper with tests (bad JSON, old version, partial data).
5. **Email confirmation:** when an email is entered, a second field "تأكيد البريد الإلكتروني" must match (client-side check only, with a clear formal error). Paste is allowed.
6. **Sign in without leaving checkout:** the "تسجيل الدخول" link in `CustomerSection` opens a `ModalSheet` with the existing email-code / Google flow. On success the sheet closes, the session refreshes, and empty customer fields are filled from the profile without overwriting what the customer typed. If the sheet cannot host the flow cleanly, keep the link (`loginHref("/checkout")`) and rely on the autosave; say which in the report. Google sign-in may need the full-page redirect; that is fine, autosave covers it.
7. **Shipping note under payment:** keep the current behaviour from the last commit (pickup → only the pickup note).
8. Not in this brief (the API has no data yet; Opus adds it): governorate names in English, a separate WhatsApp number field. Leave them as they are.

## 3. Product and build cards, gallery (`catalog/ItemCard.tsx`, `builds/BuildCard.tsx`, `product/Gallery.tsx`)
- Every card image sits in a fixed **square** frame (`aspect-square`), the photo `object-contain` on the surface colour so nothing is cropped or zoomed (a build photo must never be cut). Same frame on every card in a grid.
- Card titles: one step smaller, at most 2 lines (`line-clamp-2`), price and condition badge aligned at the bottom of the card so cards in a row line up.
- Build card: the parts / specs list is collapsed behind a "المواصفات" button with a rotating chevron (`aria-expanded`), open on click, smooth height transition.
- **Gallery** (product and build pages): square main image + a row of square thumbnails; arrows and swipe to move; click the main image to open a full-screen viewer (`ModalSheet` or a `<dialog>`) with the photo `object-contain`, next/previous, Escape to close, pinch/double-tap zoom on phones via CSS `touch-action` and a simple scale toggle. Keyboard: ← → move, focus returns to the thumbnail on close.

## 4. Catalog transitions (`catalog/**`, `builds/BuildsFilters.tsx`)
- Filter chips and the new/used tabs change state with a short transition (≤ 200 ms: background, border, a sliding indicator for the tabs). The result grid fades rather than jumps while a new page of results loads (keep the old results visible with reduced opacity until the new ones arrive). Nothing moves under `prefers-reduced-motion`.

## 5. Account pages (`features/account/**`)
- Text buttons in rows become icon buttons with `aria-label` and a tooltip (view, invoice, delete address, sign out a session, …). Primary page actions stay as labelled buttons.
- **Order history** (`OrdersList.tsx`, `OrderDetail.tsx`): each order is a card (reference, date, status badge, total, item count, actions). Order detail uses the same card + label/value layout as the rest, with the progress steps on top. No text floating between cards.
- All account texts formal.

## 6. Warranty page and invoice (`warranty/**`, `invoice/**`)
- Warranty: a compact search card (code or serial, explained in one formal line), results as cards: product, serial (`dir="ltr"`), start and end date, a clear status badge (ساري / منتهٍ) and days left. Keep the privacy rules from the last commit (public lookup only by code or serial; the link to `/account/warranties` for signed-in customers).
- Invoice: tidy, formal and printable: header with logo and invoice number, customer block, lines table with aligned numbers, totals block, warranty line per item, footer with contact. `@media print` hides site chrome and buttons, fits A4, no cut rows.

## Rules
- Formal, clear Arabic in every label and message you touch (no "اتسجّل", "بقت", "مفيش", "لحد"). Only empty states may be short and friendly. English stays plain and correct.
- Light and dark, 375 px and 1440 px, keyboard focus visible, every icon button has `aria-label`, no horizontal page scroll.
- No `console.log`, no dead code, no duplicated helpers. Do not change API contracts or `@rigx/api-client`.

## Done when
- From `apps/web`: `npm run typecheck`, `npm run lint`, `npm test` pass, plus tests for new pure helpers (autosave merge, missing-field names).
- Report as `AGENTS.md` says: what you checked at 375 / 1440 px in light and dark, whether sign-in runs inside the sheet (2.6), and anything left because the API lacks data.
