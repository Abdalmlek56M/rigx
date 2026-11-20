# DeepSeek brief 01 — Admin sidebar user menu + small fixes (trial task)

Read `AGENTS.md` first. This is a trial: small, UI only, no API changes. Work in the folder and branch the owner gave you (`deepseek/01-admin-shell`).

## Goal
The bottom of the admin sidebar now shows loose controls: the user's name and role, a theme toggle, an "EN" chip, a "المتجر" chip and a "خروج" text link. They look scattered. Replace them with **one user menu**, and fix three small display issues.

## Files you may change
- `apps/web/src/features/admin/AdminSidebar.tsx`
- `apps/web/src/features/admin/AdminShell.tsx` (only if needed for the top bar)
- a new `apps/web/src/features/admin/AdminUserMenu.tsx`
- `apps/web/messages/ar.json` and `apps/web/messages/en.json` (add keys only under `admin.shell`; fix the one key in step 4)
- the customers table component under `apps/web/src/features/admin/customers/`
- the stock list table component under `apps/web/src/features/admin/inventory/` (header contrast only)

## 1. User menu (sidebar bottom)
- One button: avatar circle (first letter), name, role under it, and a chevron.
- Clicking it opens a small menu **above** the button (popover). Items, each with an icon (`lucide-react`, already installed):
  1. Theme: one row with light / dark choice (reuse the logic of `components/layout/ThemeToggle`; do not duplicate it).
  2. Language: "English" / "العربية" (reuse the existing locale switch logic).
  3. "الملف الشخصي": link to `/account/profile`.
  4. A divider, then "تسجيل الخروج" in a red tone (calls the existing `onSignOut`).
- Keyboard: opens with Enter/Space, arrow keys move between items, Escape closes, focus returns to the button. Clicking outside closes it. Use `role="menu"` / `menuitem`.
- Motion: fade + 4 px slide, 150 ms, none under `prefers-reduced-motion`.
- Works in the collapsed mobile drawer too.

## 2. "عرض المتجر" button
- Remove the "المتجر" chip from the bottom.
- Add a clear button at the **top of the sidebar under the logo**: icon `Store` + "عرض المتجر", opens `/` in a new tab (`target="_blank" rel="noopener"`). Use `kit/Button.tsx` styles (secondary).

## 3. Table header contrast
In the stock list, the "الموديل" header text is hard to read. Make all table headers in `kit/DataTable.tsx` use the muted-but-readable header token used by the other headers (check light and dark). Do not change anything else in DataTable.

## 4. Customers: "آخر طلب"
The empty value shows "ماجاش". Change the `ar.json` value of that key (line ~1778, key `never`) to "لا يوجد", and `en.json` to "None".

## Text (formal Arabic)
- "عرض المتجر", "المظهر", "فاتح", "داكن", "اللغة", "الملف الشخصي", "تسجيل الخروج".
- English: "View store", "Appearance", "Light", "Dark", "Language", "Profile", "Sign out".

## Done when
- `npm run typecheck`, `npm run lint`, `npm test` pass from the repo root.
- Checked at 1440 px and 375 px, light and dark (describe what you saw in the report).
- No other files changed. Report as `AGENTS.md` says.
