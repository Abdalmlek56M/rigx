# DeepSeek brief 03 — Easier stock intake (purchase form)

Read `AGENTS.md` first. Work in your worktree on branch `deepseek/03-intake` (the owner creates it from the latest `main`). **Web only**: no API, no migrations, no packages. Background: R4-B in `docs/checkpoints/4.10d-owner-feedback.md`.

The owner adds stock from **المشتريات → شراء جديد** (`apps/web/src/features/admin/purchases/`). The backend already does what he wants: a purchase of kind `FullPc` with `keepAsBuild` turns every line into its own stock unit inside a new draft build when it is received. The form just hides it and is tiring to fill. Make it easy.

## Files you may change
- everything in `apps/web/src/features/admin/purchases/` (+ its tests)
- `apps/web/src/features/admin/stock/BrandSelect.tsx`, `CategorySelect.tsx`
- new `apps/web/src/features/admin/kit/ComboBox.tsx` and `apps/web/src/features/admin/kit/use-form-draft.ts` (+ tests in `kit/kit.test.ts` or a new test file next to them)
- `apps/web/messages/ar.json` and `en.json`: only under `admin.purchases` and a new `admin.kit.combo` / `admin.kit.draft`

## 1. Intake type first
Replace the "kind" dropdown + "keep as build" checkbox with two big choice cards at the top of the form (radio group, keyboard accessible):
- **"قطع منفصلة"** (`Parts`): "كل قطعة تدخل المخزون لوحدها."
- **"تجميعة (جهاز كامل)"** (`FullPc` + `keepAsBuild: true`): "القطع تدخل المخزون جوه تجميعة مسودة، وتقدر تبدّل أو تبيع أي قطعة لوحدها بعدين." Under it: the build name field.
- Keep a small secondary option under the build card: "جهاز كامل هيتفصّص قطع" (`FullPc` + `keepAsBuild: false`), as a checkbox "فصّص القطع ومتعملش تجميعة". Do not lose this case.
- `PurchaseReceivedPanel` already links to the new build. Add under the button the hint "تقدر تضيف قطع من المخزون للتجميعة من صفحتها."

## 2. Part cards
- Each line card collapses and expands with a chevron button (`aria-expanded`). Title = the model name when typed (e.g. "i5 12400F"), else the category name, else "قطعة {number}". Show the line total and a small red dot when the card has a missing field.
- New cards start open; "إضافة قطعة" collapses the others when there are more than 3 lines.
- **"كرر السطر"** button on each card: inserts a copy right after it, with an empty serial number and no photo-copy source (`dropPhotoCopy`). Icon `CopyIcon` from `components/ui/icons`.

## 3. Missing fields: never a dead button
- The save and receive buttons stay **enabled**. Add a pure `missingFields(form)` in `purchase-rules.ts` returning `{ field: string; line: number | null; label key }[]` in screen order (seller, then each line: category, model, condition, quantity/serial, cost, price, then payment). Unit-test it.
- Pressing save/receive with something missing: do not call the API. Open the first card that has a missing field, scroll to and focus the first missing input, mark every missing input red (use the existing `error` props), and show one alert above the buttons: "ناقص: السعر في «i5 12400F»، المورد" (list up to 4, then "و{count} غيرهم").
- A build purchase needs at least 2 parts: the same alert says so.

## 4. Draft autosave (purchase form only for now)
- `kit/use-form-draft.ts`: `useFormDraft<T>(key: string, value: T, enabled: boolean)` saves `value` to `localStorage` 800 ms after the last change (wrap every storage call in try/catch; the page must work without storage). Key = `rigx.draft.{userId}.{formKey}` (the signed-in user's `username` from the session store, `features/auth/session-store.ts`).
- Only a **new** purchase (no id yet) autosaves, key `purchase.new`. Once saved on the server, clear the draft.
- On open, if a draft exists and is newer than 7 days: a banner "عندك مسودة لم تُحفظ من {time}." with "استرجاع المسودة" and "تجاهل". Ignore = delete it. Never restore silently.
- Do not store photos or anything from other users.

## 5. Searchable pickers
- `kit/ComboBox.tsx`: a text input with a list under it (`role="combobox"`, `aria-expanded`, `aria-activedescendant`, arrow keys, Enter, Escape). Typing filters (case-insensitive, Arabic and English, matches anywhere in the name). Clicking the field opens the full list. Optional `onCreate(text)` shows a last row "+ إضافة «{text}»".
- `BrandSelect` uses it. With `Catalog.ManageTaxonomy` permission (`usePermissions().has`), `onCreate` calls `createBrand({ name, slug, description: null, logoStorageKey: null, supportUrl: null, isActive: true, rowVersion: null })` from `../brands/brands-api`, adds it to the local list and selects it. Slug: lowercase the name, replace anything not `a-z0-9` with `-`, trim dashes; if that leaves nothing (Arabic name), show the error "اكتب اسم الماركة بالإنجليزي" and do not call the API.
- `CategorySelect` uses it too: options keep the parent path ("كروت الشاشة › NVIDIA"). After picking a category that has children, show a second ComboBox "القسم الفرعي" with only its children (optional).
- Keep the same props, so the other forms that use these two components keep working. Check them: `grep -rn "BrandSelect\|CategorySelect" apps/web/src`.

## 6. Clearer price labels (purchase line only)
- `price` → "سعر البيع جوه تجميعة"; `withBoardExtra` → "زيادة لو اتباعت مع بوردة"; `aloneExtra` → "زيادة لو اتباعت لوحدها". Add a one-line hint under them: "السعر الأساسي هو سعرها جوه تجميعة، والزيادات بتتضاف عليه." (ar.json `admin.purchases.lines` only).

## Rules
- Formal clear Arabic for labels and alerts. Reuse `kit/Button`, `TextField`, icons from `components/ui/icons` (no `lucide-react`, it is not installed).
- Motion (collapse, list) ≤ 200 ms and none under `prefers-reduced-motion`. Light and dark. 375 px and 1440 px.
- No `console.log`, no dead code, no duplicated helpers.

## Done when
- From `apps/web`: `npm run typecheck`, `npm run lint`, `npm test` pass (new tests for `missingFields`, the slug helper, the ComboBox filter and the draft hook's storage logic).
- Report as `AGENTS.md` says, with what you checked at 375 / 1440 px.
