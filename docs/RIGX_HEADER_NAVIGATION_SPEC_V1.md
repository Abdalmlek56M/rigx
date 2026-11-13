# RIGX Header & Navigation — Implementation Specification V1

**Owner intent:** Make the storefront header feel like a polished, familiar PC-hardware retailer (informed by Sigma Computer and Venom Technology), not a generic SaaS navbar. Keep it light, readable, English-first for hardware navigation labels, premium, responsive, and fully functional.

**Scope:** Frontend only, starting from the existing RIGX V2 code. No backend, database, migrations, or new dependencies in this task.

## 1. Reference observations

- Sigma's current indexed storefront exposes prominent shopping routes such as **Build Your PC**, **PC Bundles**, **Laptops**, and brand-powered storefront links. Use this as evidence that a hardware retailer's primary navigation should prioritize shopping/build journeys rather than generic Home/Categories/Products labels. Reference: https://www.sigma-computer.com/ar
- Venom's current storefront exposes **All Departments** with hardware categories, then high-priority routes such as **PC Builder**, **Monitors**, **Accessories**, **Contact Us**, **Warranty**, and **Technical Support**. Its home page also presents three clear customer journeys: build your own PC, choose a ready build, or request direct help. Reference: https://venomtecheg.com/
- RIGX should borrow the information architecture and usability principles only. Do not copy logos, assets, wording, or visual layout literally.

## 2. What is wrong with the current header

The current `apps/web/src/components/site-header.tsx` is a three-row sticky header, but the primary nav currently includes generic Home/Categories/Products, then only the first six categories returned by the API. It uses `font-semibold`, and desktop nav is a horizontally scrolling list. On mobile, it uses a basic `<details>` list. It has no real Components mega-menu, no clear PC Builder/Compare/Support prioritization, and it exposes an Admin Catalog link in the public storefront nav for authorized users.

Replace this behavior with a purposeful hardware-retail navigation system. Do not merely restyle the existing links.

## 3. Approved information architecture

### Utility strip (very slim)

- Height target: 28–32px on desktop; hidden or condensed on small mobile if it adds clutter.
- Keep only genuinely useful utility items: a configured store/service message (only if owner-provided), Account, and language switch.
- Do not invent shipping claims, contact details, delivery times, locations, discounts, or support availability.
- Keep the language switch easy to find; theme switch may live in the main header controls instead of crowding this strip.

### Main header row

- Height target: 68–80px desktop, compact responsive height mobile.
- Brand wordmark/logo at the start edge; logo links to `/`.
- Wide search field is the main central element. Search must submit to the existing real `/products?search=` behavior. Only show a `Ctrl K` hint if the shortcut is actually implemented and tested.
- Utility controls: Compare (with real selected-count badge if non-zero), Account/Login, theme. Do not show cart/wishlist buttons until those features actually exist.
- Align with logical CSS (`start/end`, not hard-coded left/right).

### Primary navigation row

Use a short English-first hardware navigation, with moderate weight and clear spacing. Proposed order:

1. **Components** — opens the mega-menu.
2. **PC Builder** — show as a clearly disabled `Coming soon` item until a real M6 builder route exists. Do not link to a dead route or create a fake builder. If the owner prefers no disabled item, omit until M6.
3. **Compare** — real route `/compare`.
4. **New Arrivals** — real route/listing based on current catalog data.
5. **Brands** — include only if there is a real brands browsing route; otherwise leave out until implemented.
6. **Support** — route to a real contact page, or a configured WhatsApp action only when a valid owner-provided number exists.

Do not put generic `Home`, `Categories`, and `Products` as the main navigation's dominant labels. The RIGX logo already returns Home. Do not show `Deals/Offers` until real promotion data exists. Do not expose Admin links in the public shopping navigation; put an Admin entry in the authenticated account menu only for users with appropriate permissions.

The primary navigation item labels may remain English even when the selected page language is Arabic because they are concise hardware-shopping terms. Other UI labels, descriptions, forms, and content must still follow the selected locale. Avoid accidental mixed-language sentences.

## 4. Components mega-menu

### Desktop behavior

- Open from Components on hover **and** click. Hover alone must never be the only way to access it.
- Use a brief open delay (~100–140ms) and close delay (~150–200ms) to prevent flicker while moving from trigger to panel.
- Moving the pointer into the panel keeps it open. Moving between trigger and panel must not close it.
- Escape closes it and returns focus to the trigger. Click outside closes it. Keyboard users can open it with Enter/Space and navigate links with Tab; visible focus states are mandatory.
- Use correct semantics (`button`, `aria-expanded`, `aria-controls`, accessible labels). Do not use an inaccessible hover-only CSS dropdown.
- Panel should align under the navigation container and use a restrained glass/surface treatment; no oversized full-screen overlay on desktop.

### Content structure

Use actual active categories returned by the Catalog API. Group them visually without inventing categories or counts:

- **Core Components:** Graphics Cards, Processors, Motherboards.
- **Memory & Storage:** Memory, Storage.
- **Power & Cooling:** Power Supplies, Cooling, Cases.
- **Peripherals:** Peripherals and any other real active categories.

Each entry uses a consistent monochrome line icon, short English category name, optional small Arabic secondary label in Arabic locale where a verified translation exists, and a real URL `/products?category=<slug>`.

Use a small optional feature area for one genuinely useful route (e.g., Compare components) or a non-clickable PC Builder preview marked Coming soon. Do not fabricate product counts, stock levels, savings, or featured items.

### Mobile behavior

- No hover dependency. Use a mobile drawer with an accessible accordion for Components.
- Keep category rows large enough for touch (minimum 44px target).
- Preserve scroll position and close the drawer after selecting a route.
- Avoid an oversized nested menu that requires excessive scrolling; use short groups and clear separators.

## 5. Typography and icon rules

- Continue locally loaded **IBM Plex Sans Arabic** for Arabic and **Inter** for Latin if verified in the existing app. Do not add another font.
- Primary nav text: 13–14px desktop, 14–15px mobile; font weight 400 or 500 (never extra-bold). Use a comfortable line-height and spacing.
- English labels may use subtle letter spacing (around 0.01–0.02em); Arabic must use normal letter spacing (0), never negative tracking.
- Navigation icons must use the project's existing `Icon` component or its central icon map. If hardware-specific icons are needed, extend the same component centrally with consistent 18–20px size, 1.7–1.8 stroke, rounded line caps/joins, and shared optical alignment.
- No emoji, colored Unicode glyphs, random icon libraries, or one-off SVG styles in primary navigation.
- Use one subtle accent state (RIGX orange) for active/hover; do not make every item a colored button.

## 6. Language transition

When switching Arabic ↔ English:

- Update `lang` and `dir` correctly and persist the selection using the existing locale mechanism.
- Preserve the current route, query string, selected comparison items, product filters, and scroll position where possible.
- Use a restrained transition: a short 160–220ms opacity/position transition for relevant content and a smooth language-toggle indicator. Avoid a dramatic full-screen wipe or blinding flash.
- Progressive enhancement only: use View Transitions API only if supported; provide CSS fallback. Do not add a dependency.
- Do not animate in a way that causes layout shift or leaves the page in the wrong direction.
- Respect `prefers-reduced-motion` and avoid animating large images on initial paint.
- Ensure the nav labels stay English-first by design while the rest of the page changes locale intentionally, not accidentally.

## 7. Behavior and truthfulness

- Search, Compare, Account, theme, and language controls must work.
- `PC Builder` must be disabled with a clear Coming soon label until M6 supplies a real route. Never fake a working builder.
- `Brands` and `Support` appear only when their destinations are real.
- The account menu may show Admin Workspace only for authenticated users who have the required Catalog permissions; customers must never see admin navigation.
- Category data is dynamic from the existing Catalog API. Do not hardcode product counts or active categories. For current Arabic taxonomy fallback use existing `localizedCategoryName` mapping. Record the need for future `name_ar/name_en` schema support separately; do not add a migration in this frontend task.
- On API failure, the menu should degrade gracefully with a clear retry/empty state rather than render fake categories.

## 8. Visual character

- Keep RIGX Graphite/Obsidian surfaces and restrained Warm Orange accent.
- Header may use a soft translucent surface with a subtle 1px border and mild blur. Keep legibility first; blur must not make text low-contrast.
- Hover/focus effect: a small underline/indicator glides into place, a restrained border/glow appears, and an icon shifts by a tiny amount. No large scale jumps, bouncing, or heavy neon.
- Dropdown opens with a short fade + 6–8px translate; no layout reflow that moves the page content unexpectedly.
- Keep enough vertical breathing room. No crowded strip of too many links.

## 9. Mandatory workflow before and during implementation

1. Read root `AGENTS.md`, `apps/web/AGENTS.md`, and existing relevant skills: `ui-batch-loop`, `hardware-store-design-system`, `hardware-store-visual-qa`, `react-best-practices`, and frontend testing/debugging. Inspect actual installed skill locations; do not duplicate skills.
2. Read the current private/project state docs and `docs/GLOSSARY.md` only as needed for this header task.
3. Inspect actual usage and callers before changing:
   - `apps/web/src/components/site-header.tsx`
   - `apps/web/src/components/icons.tsx`
   - `apps/web/src/lib/i18n.tsx`
   - `apps/web/src/lib/category-labels.ts`
   - `apps/web/src/lib/api-client.ts`
   - `apps/web/src/components/theme-provider.tsx`
   - `apps/web/src/components/comparison-provider.tsx`
   - current routes for `/compare`, `/products`, `/login`, `/account/security`, `/admin`
   - `apps/web/src/app/globals.css` and all relevant header/mobile styles
4. Capture baseline screenshots before coding at 390px and 1440px, AR/EN and Light/Dark.
5. Write a short design brief: final nav labels and priority, desktop/mobile behavior, current components to reuse, files to change, and risks. Do not ask the owner to repeat known decisions.
6. Implement the header as reusable, maintainable components (e.g. `SiteHeader`, `DesktopPrimaryNav`, `ComponentsMegaMenu`, `MobileNavigationDrawer`) only if the existing structure benefits from this split. Avoid needless component fragmentation.
7. Do not alter Admin Shell navigation into a shopping mega-menu. The Admin shares tokens, fonts, icon language, and transition quality, but retains a task-oriented sidebar.
8. No backend changes, migrations, Docker/Preview database changes, or new package dependencies.
9. Run the relevant TypeScript, unit, build, and Playwright tests. Add focused tests for mega-menu keyboard/hover/click, outside click, Escape, mobile accordion, language switch persistence, route preservation, admin link permissions, and category API failure.
10. Capture after screenshots at the same matrix. Verify no horizontal overflow at 360/390/430/768/1024/1280/1440/1920.
11. Report exact files changed, before/after screenshot paths, test command outputs, accessibility checks, remaining gaps, and any unverified item. Update project README/STATE/PROGRESS/NEXT_TASK/MODEL_HANDOFF as applicable.
12. Stop after this navigation batch and wait for owner review before starting unrelated UI or M5 work.

## 10. Acceptance criteria

- Header feels like a premium hardware retailer, not a SaaS template.
- Navigation labels are short, English-first, readable, and medium/light weight.
- Primary links follow shopping priority and all active destinations work.
- Components opens a real categorized mega-menu on desktop and an accessible accordion/drawer on mobile.
- Pointer can move from trigger to panel without flicker; keyboard and touch access both work.
- Locale transition is noticeable but restrained, preserves route and state, and does not cause layout shift.
- Icons are consistent in size, stroke, shape, and alignment.
- Admin links are not in public nav and are permission-gated in the account menu.
- PC Builder is visibly marked Coming soon or omitted until M6; never a dead clickable link.
- No fake offers, shipping claims, categories, counts, stock, or support contacts.
- No new dependency or Backend/API/DB change.
- TypeScript, tests, build, and relevant Playwright checks pass; screenshots before/after are attached.
