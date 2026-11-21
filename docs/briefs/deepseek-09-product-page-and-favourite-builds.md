# DeepSeek brief 09 — one language button, product page polish, "more like this" strip, hearts on builds

Read `AGENTS.md` first. Work in your worktree on a branch `deepseek/09-product-page` created from the latest `main` (it already contains briefs 06 and 08 and the API change in section 5). **Web only**: no API, no migrations, no `features/admin/**`. Regenerate the client first: `npm run generate` in `packages/api-client` (the generated schema is git-ignored). Wording is brief 07's job, but every new Arabic text is **formal Arabic** (no "دلوقتي", no colloquial words). The owner reviews on a phone: check 375 first.

Owner's quality bar: organised cards, label above value, icons with a tooltip instead of word buttons (one labelled primary action per screen), nothing floating, consistent sizes.

## 1. One language button (header + mobile menu)
`components/layout/LocaleSwitch.tsx` shows two buttons side by side ("العربية" and "EN"). Replace it with **one** button that shows the language you would switch **to** (on `/ar` it reads "EN" in the mono font; on `/en` it reads "العربية" in the Arabic font), with a small globe icon, a tooltip/aria-label naming the target language, `hrefLang`/`lang` of the target, and the same keep-the-path behaviour. Same height as the other header icon buttons (44 px target, round). In the mobile menu it is a full-width row with the globe and the language name. Keep the `englishEnabled` gate. Update the `locale` messages (remove what is no longer used).

## 2. Product page top row and purchase options (`p/[slug]/page.tsx`, `PurchasePanel.tsx`)
1. **Top row consistency:** today the heart is a 44 px square next to 28 px pills, so the row looks uneven. Move the heart onto the **gallery photo corner** (the same place and size as on the product cards) and leave only the condition and availability pills in the row, both the same height. On the build page do the same if it has a heart (section 5).
2. **Price options** (the three radios "مع تجميعة كاملة من عندنا / مع بوردة من عندنا / منفردة"): keep them as selectable cards, but make them lighter and clearer:
   - Two lines per card: the option name (bold) and, under it, a small muted line saying what it means (add messages: `inBuild` → "عند شرائها ضمن تجميعة كاملة من RIGX", `withBoard` → "عند شرائها مع لوحة أم من RIGX", `alone` → "شراء القطعة وحدها"). Price at the end of the card.
   - A small green "وفّر X ج.م" pill on the cheaper options (difference from the "alone" price; no pill on "alone").
   - Selected card: orange border + soft orange background (not the heavy black ring); the radio stays visible. The default stays "alone". Use `type-label` / `InfoGrid` conventions, no new colours.
   - When there is only one price there are no options (already the case, keep it).
   - The hint "ينخفض السعر…" becomes the legend's helper line under "كيف ستشتريها؟", not a line under the list.
3. **Total block:** the label under the options repeats "منفردة". Show "السعر" (or "الإجمالي" when quantity > 1) above the big price; the quantity stepper sits at the other end, aligned to the price's baseline.
4. **WhatsApp button:** the outline button's text is too faint (grey on dark). Use the kit's WhatsApp variant with readable contrast (check both themes).
5. "هل تتوافق مع جهازك؟ جرّبها في «جمّع جهازك»": keep, but as a small bordered link row with the CPU icon (not a floating ghost button).

## 3. "معلومات الشراء" card (`ProductInfoBoxes.tsx`)
Problems seen on the phone: the warranty value reads "الضمان ما الذي يغطيه" (the label repeated, then a link); the returns value is just the link "الشروط"; the pickup/shipping value is one long sentence squeezed into half a row; payment methods are a comma list that breaks the bidi ("كاش، InstaPay، محفظة").
Redesign as **one column of rows** (icon in a soft round tile at the start, label above the value, a thin divider between rows; at `md+` keep one column too, the card is narrow):
- **الضمان:** value = the item's warranty label (e.g. "12 شهرًا") followed by the link "ما الذي يغطيه الضمان؟" to the warranty policy. When the item has no label, show only the link (never repeat the word "الضمان" as the value and never write "بدون ضمان": that wording is a pending business decision).
- **الاسترجاع:** value = link "شروط الاسترجاع والاستبدال".
- **طرق الدفع:** one small pill per enabled method (كاش عند الاستلام / InstaPay / محفظة إلكترونية), wrapping, each pill `dir` correct (use the existing payment messages; InstaPay stays Latin, the others Arabic).
- **الاستلام والشحن:** two short lines: "الاستلام من المحل: {address}" (only when pickup is enabled) and "الشحن لجميع المحافظات". Address is a label/value pair, not a sentence.
Keep the card title row with the info icon. Same card padding as the others.

## 4. "More like this" strip at the bottom of the product page
After the FAQ card add a full-width section (outside the narrow columns, same `shell` width) titled "قد يعجبك أيضًا": a horizontal strip of product cards using the existing `Rail` (`features/home/Rail.tsx`): swipe on touch, drag with the mouse, **arrows to go forward and back**, and a **"عرض المزيد"** link (the Rail's `action` slot) to the category page (`/products?category=<slug>`; `/products` when the item has no category). No auto-advance on this page.
- Data: published items of the **same category**, excluding the current one, available first, up to 12; when there are fewer than 6, fill with the parent category's items, then the newest items. Load it on the server next to the page's other data (reuse the catalog query helpers; write the selection as a small pure, tested function, e.g. `features/product/related-items.ts`: input the candidate lists, output the ordered, de-duplicated, capped list).
- The cards are the normal `ItemCard` inside `<li>` (same width rules as the home rails). Hide the whole section when there is nothing to show.
- Do the same on the **build page** (`builds/[code]/page.tsx`): "تجميعات أخرى" with `BuildCard`s of the other published builds (same usage first), "عرض المزيد" → `/builds`.
- The sticky mobile buy bar must not cover the strip: keep the existing bottom padding.

## 5. Hearts on builds (the API is ready)
`PUT /api/v1/account/favourites/{code}` and the merge call now accept build codes (`BLD-07`) next to item codes (`RX-0142`); `GET /api/v1/account/favourites` (and merge) return `builds` (published build views, same shape as the builds list) next to `items`. The list is limited to 200 for both together.
- `favourite-rules.ts`: `parseStoredCodes` accepts `BLD-<digits>` too (update the tests; the export comment says "RX codes only").
- Heart on `BuildCard` (photo corner, same as item cards) and on the build page (gallery corner). `FavouriteButton` already works with any code; give it the build's title as `modelName`.
- Favourites page (`FavouritesList.tsx`): show saved builds after the items (a section title only if both lists are non-empty; otherwise just the grid), each as a `BuildCard`, filtered by the live heart list like the items. The empty state shows only when both are empty.
- Message keys for the heart on a build: reuse `favourites.add` / `favourites.remove`.

## 6. Small card fix seen in the review of brief 08
On the 2-column phone catalog the price wraps ("10,000" above "ج.م") and the condition pill is pushed beside it. Keep the price on one line (`whitespace-nowrap`, slightly smaller on 375 if needed) and put the pill on its own row above or below it so cards in a row stay aligned. Check `ItemCard` and the home rails at 375 / 768.

## Checks and report
From `apps/web`: `npm run typecheck`, `npm run lint`, `npm test`, `npm run build`. Browser check at 375 / 768 / 1024 / 1440 in `/ar` and `/en`, light and dark at 375: header (both languages), product page (item with three price options, item with one price, sold-out item, used item, item without warranty label), build page, favourites page (signed-out is enough), catalog card at 375. Report: files changed, what you could not do, and the screenshots folder. Leave everything uncommitted on the branch; do not push.
