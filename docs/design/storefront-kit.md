# Storefront design kit — the written rules

Applies to the storefront (`apps/web/src/components/ui`) and, for buttons, tokens and tables, to the admin kit (`features/admin/kit`). The owner's bar: comfortable, beautiful, professional; cards with titles; little noise; one clear action per area; icons instead of words where the icon is clear; formal Arabic; light and dark both work.
`apps/web/src/components/ui/kit-rules.test.ts` enforces the rules a search can prove (hard-coded buttons, `disabled:opacity-*`, `text-error`, ad-hoc heading sizes, hand-made status pills, hand-made radio cards). A new rule that can be proved by a search goes there.

## 1. Tokens (`app/globals.css`)
- Colour only through tokens (`bg-surface`, `text-ink`, `text-ink-muted`, `border-line`, `bg-paper` …). Never a hex value in a component.
- **Red as text or an icon is `text-error-text`**, never `text-error` (dark theme lifts it to a readable salmon). `bg-error` / `border-error` stay for fills and edges.
- `--muted` is the quiet text colour used on grey fills (disabled buttons, placeholders); it is lifted in dark too.
- Spacing steps: 4 / 8 / 12 / 16 / 24 / 32 (`gap-1 … gap-8`). Use the nearest step, never an in-between value. Card padding comes from `Card` (`p-5 md:p-6`); do not set it again.

## 2. Heading scale
| Utility | Size | Use |
|---|---|---|
| `type-display-title` | 22 → 30 px (from `md`) | The heading of a home section (rails, FAQ, services blocks). One per section. |
| `type-page-title` | 28 px | The `h1` of a page (`PageHeader`). Exactly one per page. |
| `type-section-title` | 17 px | A card title, a panel or drawer title, an empty state title, a build/product card name. |
| `type-value` | 14 px bold | A value in a label/value pair. |
| `type-label` | 12 px muted | A label above a value, a caption, a column of small print (add `font-bold` for a group label). |
Exceptions, on purpose: the hero `h1` (display size, home only), the print documents (invoice, quote) and the footer column headings (13 px caps on the graphite strip). Everything else uses a utility above, never `text-[22px]` or `text-lg` on a heading.

## 3. Label above value
A fact is a **label** (`type-label`) over a **value** (`type-value`), side by side only in `InfoGrid` rows (two columns on `md`, one on phones). Never `label: value` in one run of text, never a dot or a dash between the two words of a label ("اسم التجميعة (اختياري)", not "اسم التجميعة · اختياري"). Optional fields say "(اختياري)" in brackets; counts are a pill.

## 4. Buttons
One kit, five variants (`components/ui/Button.tsx`; the admin has its own copy with the same names in `features/admin/kit/Button.tsx`):
| Variant | When |
|---|---|
| `primary` (graphite) | **The one main action of a screen or form** (send, save, confirm). At most one per screen area. |
| `accent` (orange) | A buying action only (add to cart, order). |
| `secondary` (outlined) | Every other action. |
| `ghost` | Quiet actions inside rows and toolbars. |
| `danger` | Delete, cancel, reset. Never the only button; the safe choice sits next to it. |
- **Icon button** (`IconButton`, or `Button iconOnly`): when the icon explains itself (close, copy, share, heart, trash, edit, WhatsApp, back). It always has `label`: that is the `aria-label` and the hover tip. Words stay on the one main action and on any action whose icon is not obvious. Never an icon plus a word that says the same thing (a globe and "اللغة").
- **Text link vs button.** A link that goes somewhere to read (a policy, another page, "see all") is a text link in the content colour with an underline on hover; a link that *does* something the page is for (go to checkout, open the builder) is a `ButtonLink`. An underlined orange sentence that reads like a broken link is never used for help: use a `HelpLink` pill.
- **Pill / chip.** A pill (`rounded-full`) is a *choice or a fact*, never an action: filter chips and tabs (selected = graphite fill), condition and status badges (`StatusBadge`), counts (`CountBadge`), the "reset filters" chip. A rectangle with `rounded-control` is an action.
- Sizes: `md` (44 px high) by default, `sm` (36 px) inside a table row, a card title row or a toolbar; on phones both keep a 44 px touch target.
- **Disabled.** A disabled button is flat grey with a muted label and a hairline edge (`.btn-off`, light and dark), the cursor says "not allowed", and it does not hover or press. It is never a dimmed copy of the live colour: no `disabled:opacity-*` and no `opacity-50` on a button anywhere. While an action runs, `loading` shows a spinner and keeps the colour. A link that cannot be followed yet uses `aria-disabled` (the kit styles it the same way) and drops its `href`.
- A hard-coded button (`rounded-control` plus a fill) outside the kit is a defect.
- **Status marks.** A status, an availability or a condition is shown only by `StatusBadge` (tone: neutral / success / warning / danger / info), `StockDot` or `ConditionBadge`, in the storefront and in the admin alike. A screen maps its own states to a `StatusTone`; it never draws its own pill, tag or coloured word.
- **Choosing between a few options** (new / used, delivery / pickup, payment method, who can see a build) is a **tile** (`components/ui/ChoiceTile.tsx`): the icon above the word, an orange glowing frame when chosen. A rectangular button is for an action or a link, never for a choice.

## 5. Cards, tables, states
- **Card** (`Card` with a `title`, optional `icon` and `actions`): every group of content on a page. A title row is the icon (muted, 18) + `type-section-title`; page-level actions go in the page header, card-level actions on the end of the card's title row.
- **Admin table** (`DataTable`): header row on `bg-paper`, bold `text-ink` 13 px with a hairline under it, identical in every list; the first column of people is a `NameCell` (bold name, phone or code under it in mono, never forced to capitals); numbers align to the end; on phones a row becomes a card with the column names above the values.
- **Empty state** (`EmptyState`): an outline icon in a circle, one title, one helping line, one action at most. **Loading**: a `Skeleton` that has the shape of the content; never dim real content to 60 % while it reloads (keep it, mark it `aria-busy`). **Error** (`ErrorBlock`): what failed, in formal Arabic, plus a retry button.
- **Icons** come only from `components/ui/icons` (one stroke). Sizes: 14 inside a badge or a dense row, 16 in a `sm` button, 18 in an `md` button and next to a card title, 20 in a header action, 26 in an empty state. A decorative icon is `aria-hidden`.
- Motion: only `transform` and `opacity`, under 250 ms, and every animation switches off with `prefers-reduced-motion` (the global rule already does it; do not add a `transition` that needs a second opt-out).

## 6. Motion and atmosphere (4.11 C)
All of it is CSS first, decorative, and off or still under `prefers-reduced-motion`. No animation library.
- **Hover effects belong to a mouse.** Anything that depends on hover (card lift and glow, the 3° tile tilt `Tilt`, the card's second photo, the photo magnifier `.zoom-photo`, the service gauge) is inside `(hover: hover) and (pointer: fine)` or is only started by a mouse `pointerenter`. A touch screen never sees it and never downloads what it needs.
- **Nothing extra before the first interaction.** The second card photo is mounted on the first pointer-enter, not in the page; a tab panel rises in only after the visitor switched tabs; a counter starts when it scrolls into view and its server markup already holds the final value.
- **Sliding indicator** (`useTabIndicator` + `.tabs-slide` + `.tab-indicator`): for a row of tabs of unequal width. The live tab is marked `data-active="true"`; before the first measurement it paints its own fill, so there is no flash. A row of equal tabs (condition tabs, the grid/list toggle) uses a plain `translateX` instead. The indicator is the one element allowed to transition its size as well as its position (it is absolutely positioned).
- **State pulses** (`.step-pulse`, `.dot-pulse`) mark the current step of an order only; **the warranty battery** (`.battery`) is green above 50 %, amber to 20 %, red below, empty when expired.
- **Section titles** (`type-display-title` + `title-hook`) carry a short orange line that draws in as the title scrolls into view (scroll-driven, only where supported).
- **Dark glow** (`.page-glow`): two very faint lights (orange and blue, 10–11 % alpha) drift behind the whole storefront; one fixed CSS element, no canvas, off under reduced motion and in print.
- **Dark circuit canvas** (`Atmosphere`): one canvas behind the home hero and the hero-like page headers (`PageHeader atmosphere`: catalog, builder, builds, services). Never on cart, checkout, account, login or policies (the page glow stays everywhere in the storefront). The parent is `isolate`; the canvas chunk is only downloaded in the dark theme; it is still for reduced motion, phone-sized screens and low-end devices (4 cores or fewer), absent with save-data, paused when hidden or off screen, and capped at 30 frames a second.
- **Dark photos**: a product photo sits on `.photo-frame` (soft backdrop, multiplied) so white studio backgrounds do not glare; the light theme is untouched.
- Copy: a section title is a short hook, not a label ("ما القطعة التي تبحث عنها؟"). It makes no claim the store cannot back.

## 7. Builder and shared builds (4.11 D)
- **Choice tile** (`ChoiceTile`, `ChoiceRadioTile` for forms, `ChoiceLinkTile` for a link; `.choice-card`): a pick between a few options, with an icon above the word. The chosen tile glows (orange edge, soft shadow, accent fill); under a mouse it lifts 2 px; a tile that cannot be chosen is flat grey and says why in its note. It is a card (`rounded-card`), not an action, so it never uses a button variant.
- **Wizard** (home "اقترح لي تجميعة"): one question per step, a step indicator that says where the visitor is, choosing lights the card for 280 ms and slides the next step in from the reading side (`.wizard-panel[data-direction]`, 220 ms, flipped in RTL; no slide on the first paint). Every step can be skipped and the last one shows the choices with an edit pencil each.
- **Status dot** (`.status-dot` + `data-tone`): a dot that sends out a ring. Green = compatible, amber = a warning, red = a conflict (a build that cannot be ordered is red, not amber), grey and still = nothing to judge yet.
- **Power meter** (`PowerMeter`): the estimated draw against the output of the power supply, with a tick at the 80 % headroom the API warns at. The sentence under it is the advice: enough / tight, look for N W / not enough, look for N W / choose a supply of N W. N is a multiple of 50 W that keeps the estimate under 80 %. The two power problems are not repeated in the problem list.
- **Case blueprint** (`CaseBlueprint`): one SVG, one part per builder slot, named in the singular (`slot-names.ts`). A part is a dashed outline until a part is chosen, then it fills with the accent, glows and carries its name; the slot being chosen glows; a slot in a conflict is tinted red, in a warning amber. A click on a part chooses its slot (the slot list stays the keyboard way). Only slots the store has are drawn. Drawings live in `blueprint-shapes.ts`, so a new slot is one entry.
- **Visibility** (`VisibilityPicker`): private / by link / public as three choice cards with the sentence of the chosen one under them. A public build says where the store's decision stands (waiting, shown, not approved) next to it. A visitor who is not signed in sees only a sign-in note.
- **Reactions** (`BuildReactions`): a heart and a like, each with its count, as small secondary buttons (`aria-pressed`). The screen changes at once and the server's answer replaces the counts; a failed save flips back and says so; a visitor who is not signed in is sent to sign in.

## 8. Documents and customer feedback (4.11 E2)
- **Document** (invoice, build quote): a fixed-size light paper (`.doc-paper` forces the light palette, so screen, PDF and picture agree). The invoice is 860 px wide (`INVOICE_WIDTH`), the quote 1080 × 1350; `DocumentFrame` scales the invoice to the screen (`transform`, anchored and centred) and drops the scale in print (`data-document-frame` / `data-document-paper` in the print CSS). Header band in graphite with the logo and the document title, boxed groups (store, customer, details), a lines table with a **condition column** (a build's parts carry theirs): only the words "جديد" / "مستعمل" (`ConditionBadge`), then totals with a graphite grand-total band.
- **Copy as picture** (`CopyImageButton` in `components/document`, rendering in `lib/document-image`): one button next to the print button for every document; the picture is made at the paper's design size, so a phone gets the same image as a desktop. Where the clipboard cannot take a picture it saves a file; Safari and iOS may fail (the PDF stays).
- **Order lines** in the account list open in place (`OrderItemsAccordion`: a full-width button with a chevron, `aria-expanded`); the full order page stays one click away.
- **Notifications** (`NotificationBell`): the panel hangs under the bell from `sm`, and on a phone is as wide as the screen less its gutters (`fixed`, `inset-x-3`) instead of hanging off the bell; each entry has a picture for its kind (order, back in stock, store reply) and an orange dot while unread.
- **Feedback form** (`/feedback`): one form, a square choice tile pair (complaint / suggestion), name and phone filled from the account, optional email, subject and order or item reference. It lands in the admin inbox; the store's written reply shows in "طلباتي الخاصة" and in the bell.
- **View count** (`BuildReactions`): an eye and the count before the heart and like, as a quiet label, not a button. The count comes with the reactions, so a gallery card and the shared page show it the same way.
