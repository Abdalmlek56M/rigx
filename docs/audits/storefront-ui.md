# Storefront UI audit (21 Nov 2026)

Method: the live site (https://rigxhardware.com, the build before brief 05) at tablet width, plus a read of the storefront code (`apps/web/src/components`, `features/*`). The live catalog is empty (no stock entered yet), so product screens were audited from code. Bar: `docs/checkpoints/4.10d-owner-feedback.md` → "Owner's quality bar".

## Root cause: the storefront has no design kit
The admin has a kit (`features/admin/kit`: Button, IconButton, Dialog, SectionCard, DataTable, Toast). The storefront has none, so every screen re-types its own classes:
- 36 files hard-code the dark button classes and 18 the orange ones (`ItemCard` even keeps a local `buttonBase`).
- 35 places repeat the card classes `rounded-card border border-line bg-surface p-…`.
- `Field` (label above value) is re-written in `checkout/form-parts.tsx` and `invoice/InvoiceSheet.tsx`; account, tracking and warranty each lay out label/value their own way.
- Result: slightly different radius, padding, weight and spacing on every page, which is what reads as "simple / just works" instead of "professional".
Fix once, in `apps/web/src/components/ui/`: `Button` / `IconButton` / `ButtonLink`, `Card` (title row with icon + actions), `InfoGrid` + `InfoField` (label above value, the same everywhere), `StatusBadge`, `PageHeader`, `EmptyState`. Then every screen uses them (brief 06).

## Defects found (live site)
1. **Header loses its icons at ~1000 px.** Logo + 6 nav links + a 240 px search + the icon pill do not fit: account, cart and notifications are pushed off the left edge (measured: icons at x = −90…81 while the viewport starts at 0; document is 95 px wider than the screen). Fix: show the full nav only from `xl`; between `md` and `xl` use the menu button; give the search `min-w-0`.
2. **Footer:** the e-mail address breaks in the middle ("…gmail.co / m"); the floating WhatsApp and back-to-top buttons sit on top of the footer text (no bottom room); the contact rows have icons but no consistent label/value rhythm.
3. **Catalog:** the filter panel has its own inner scrollbar (a scroll inside a scroll); the condition tabs show "0" counts; the page subtitle is tiny and underlined like a link; the empty state is plain text with a link, not an illustration + one action.
4. **Home hero and sections** are written in colloquial Egyptian ("عايز"، "محتار"، "بنجمّع وبننضّف"، "هتستخدم"). The owner wants formal Arabic everywhere. Cards on the home page have an arrow and an icon, but different heights and badge styles.
5. **Floating text headings:** several pages put a heading directly on the page background with loose paragraphs below it (policies, FAQ, services, tracking). They should sit in a card or a clear page header.

## Defects found (code)
6. Buttons that are words but should be icons: copy / open / delete / edit in account lists (done in 05), "share", "print", quantity steppers, remove-from-cart, remove-from-compare, close buttons. Rule: icon + tooltip + `aria-label`; a word button only for the one main action.
7. Invoice (label above value is right, as in the owner's example) but: no customer e-mail row, the address is one comma line, the three-column grid shifts when a field is empty, and the lines table and totals use different paddings. Use `InfoGrid` with fixed order and a placeholder-free layout.
8. Product page, cart, tracking and warranty each have their own heading size and spacing scale (15 / 17 / 20 px headings mixed). Define one scale: page title 28, section title 17, label 12, value 14 bold.
9. No shared loading skeleton or error block for lists (each screen invents one).

## Proposed order
- **Brief 06** (DeepSeek, web only): the design kit, then migrate header, footer, home, catalog, product, cart, tracking, services, requests, policies, FAQ, invoice and account onto it; fix defects 1–3 and 5–9.
- **Brief 07** (DeepSeek, web only): the formal-Arabic copy pass over `ar.json` (defect 4) and the same in the e-mail templates' text, with a list of every changed key.
- **Opus** meanwhile: API queue (English names, WhatsApp number, deposit setting, badges, emails, payment methods edit/delete, build favourites, price options, Arabic error titles).
