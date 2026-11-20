# DeepSeek brief 04 — Admin polish: orders, builds, stock, photos, toasts

Read `AGENTS.md` first. Work in your worktree on branch `deepseek/04-admin-polish` (the owner creates it from the latest `main`). **Web only**: no API, no migrations, no packages. Background: "R5" in `docs/checkpoints/4.10d-owner-feedback.md`.

The owner tested the admin and says it works but looks like "text floating in the air". The goal is a calm, professional back office: every screen made of clear sections (cards with a title row), aligned tables, icon buttons, and real feedback. Reuse the kit (`features/admin/kit/`: Button, Dialog, ConfirmDialog, DataTable, PageHeader, Toast) and icons from `components/ui/icons` (add a missing icon there as a small inline SVG in the same style; no `lucide-react`).

## Files you may change
- `apps/web/src/features/admin/orders/**`, `builds/**`, `stock/**`, `inventory/**`, `users/**`, `kit/**`
- `apps/web/src/components/ui/icons.tsx` (add icons only)
- `apps/web/messages/ar.json` and `en.json`, only under `admin.orders`, `admin.builds`, `admin.stock`, `admin.inventory`, `admin.users`, `admin.kit`

## 1. Order page (`orders/OrderDetail.tsx`, `OrderBody.tsx`, `OrderCustomer.tsx`, actions)
- Lay the page out as a two-column grid on ≥1024 px (main column: items, customer, delivery, money; side column: status, payment, shipping, notes actions), one column on phones. Each block is a card: `rounded-card border border-line bg-surface p-5` with a title row (icon + title) and its content in a label/value grid (label `text-xs text-ink-muted`, value `font-bold`). No loose text between cards.
- Customer card: name, phone (with WhatsApp and call icon buttons), email, how they found us, usage, notes. The address on its own row, written as governorate › area › street › building / floor / flat, each part only when present.
- Money card: products, discount, shipping, total, paid, remaining (remaining in orange when > 0), payment method.
- **Order history** becomes a vertical timeline: a line on the start side with a dot per event, the event text bold, the time under it in small muted text (`dir="ltr"` for the time). Newest first. Payment events show the amount formatted as money ("دفعة نقدية · 5,500 ج.م", not "5500").
- **Record payment** opens a Dialog: method, account, amount pre-filled with the remaining amount, note, and a confirm step showing "تسجيل دفعة 5,500 ج.م نقدًا على الطلب RIGX-…". Disable the confirm button only while sending.
- The manual payment-status select only appears while the order is not fully paid (the API now refuses "unpaid" on a fully paid order; show its message if it comes back).

## 2. Build page (`builds/BuildParts.tsx`, `BuildEditor.tsx`, dialogs)
- Parts table: fixed column widths so nothing overlaps (part · type · cost · price · warranty · partner · share of sale · actions). On < 1024 px turn each row into a small card (title row + 2-column details + actions row). The partner avatars that overlap the "add part" buttons in the screenshot must not happen.
- Row actions become icon buttons with a tooltip/`aria-label` (edit, replace, remove), remove in red. Keep the two "add part" buttons as normal buttons above the table.
- **Replace part**: `ReplacePartDialog` already passes `categoryId`. Show it as a removable chip above the results ("القسم: بوردات ×"), so the owner sees why only boards appear and can widen the search. When the part has no category, show a category picker (`stock/CategorySelect`) first.
- **Change partner** (`PartEditDialog` / `PartnerFields`): the current partner and percent must be pre-selected when the dialog opens (today it opens empty).
- Build card form (`BuildCardForm.tsx`): under the sale price show "مجموع أسعار القطع: 27,500 ج.م" (from the view's parts total) with a small button "استخدم المجموع" that fills the field. A new build already starts with that total.
- Explain "فك التجميعة" in its confirm dialog: "سيتم فصل كل القطع وإرجاعها إلى المخزون كقطع منفصلة، وتُغلق التجميعة." (formal).

## 3. Stock list (`inventory/**` or `stock/**`, whichever renders `/admin/stock`)
- Units that sit inside a build are grouped under a build row: build name, code (`BLD-01`) as a link to `/admin/builds/{id}`, status badge, and the parts indented under it (collapsible). Loose units stay as today. If the list API does not return the build id/name per unit, stop and write it in your report (Opus adds it); do not invent data.

## 4. Product photos (`stock/PhotoGallery.tsx`)
- Grid of equal square thumbnails (`aspect-square object-cover`), 4 per row on desktop, 3 on phones, with order numbers.
- "Main photo": the first photo is the main one, marked with an orange "الصورة الرئيسية" badge; every other photo gets a "اجعلها الرئيسية" icon button (moves it first; use the existing reorder call if the API has one, otherwise report it). Drag or arrow buttons to reorder.
- "معاينة كما يراها العميل": opens a Dialog that shows the photos the way the storefront gallery does (square main image + thumbnails).
- Icon buttons for delete (with confirm) instead of a bare bin under each photo.

## 5. Toasts (`kit/Toast.tsx`, `toast-queue.ts`)
- Success / error / info variants with an icon, a title line and optional detail, a close button and a thin progress bar for the auto-hide time (5 s, paused on hover/focus). Stack bottom-start, max 3. `role="status"` (success/info) or `role="alert"` (error). Enter/exit ≤ 200 ms, none under `prefers-reduced-motion`. Keep the queue API backwards compatible.

## 6. Users table (`users/**`)
- Header cells and values must align (the "الإيميل" header sits far from its values today). Use the same `text-start` and the same column widths for header and body; emails `dir="ltr"` aligned to the start of the cell.

## Rules
- Formal, clear Arabic for every label and message (no "اتسجّل", "بقت", "مفيش"): e.g. history events "تم إنشاء الطلب", "تغيّرت الحالة إلى «قيد التجهيز»", "تم تسجيل دفعة". Only empty states may be short and friendly.
- Light and dark, 375 px and 1440 px, keyboard focus visible, every icon button has `aria-label`.
- No `console.log`, no dead code, no duplicated helpers.

## Done when
- From `apps/web`: `npm run typecheck`, `npm run lint`, `npm test` pass (add tests for any new pure helper, e.g. the timeline ordering or address formatting).
- Report as `AGENTS.md` says, with what you checked at 375 / 1440 px and anything you had to leave because the API lacks data.
