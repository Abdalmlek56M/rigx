# DeepSeek brief 10 — storefront follows the per-category sale prices

Read `AGENTS.md` first. Work in your worktree on a branch `deepseek/10-price-options` created from the latest `main` (after brief 09 is merged; it touches the same product page and cards). **Web only** (no API, migrations, packages, `features/admin/**`). Regenerate the client first: `npm run generate` in `packages/api-client`. Any new Arabic text is formal Arabic.

## What the API now says
Each category has three switches for its sale prices: **alone** (`Alone`), **with a RIGX motherboard** (`WithBoard`) and **inside a build** (`InBuild`); at least one is on. The storefront answers carry the result:
- `StoreItemCard.priceOptions` and `StoreItemDetail.priceOptions`: the offered options, in order (`["Alone","WithBoard","InBuild"]` when nothing is switched off, e.g. `["WithBoard","InBuild"]` when "alone" is off). The three prices in `prices` are always present; **show and sell only the offered ones**.
- Online checkout refuses an option that is off (400) and, when a line names no option, uses the first offered one. `InBuild` is accepted online only for a build with a processor and a board (the builder), as before.

## 1. A single place for the rule
`features/product/price-options.ts` already builds the radio list from `prices`. Change it to take the offered options: `priceChoices(prices, offered)` returns only offered options (best price first as today, minus the hidden ones); `defaultOption(offered)` is the first offered option (`Alone` when all are offered). Pure, with tests (all offered; "alone" off; only `InBuild`; the old behaviour of "all three prices equal → no choice list").

## 2. Product page (`PurchasePanel.tsx`)
- The option cards come from `priceChoices(prices, item.priceOptions)`; the selected one starts at `defaultOption`.
- Exactly one offered option → no chooser at all; show the price (label "السعر").
- **Only `InBuild` offered** (a part sold inside a build only): hide the add-to-cart / buy-now buttons and the sticky mobile bar's buy buttons; show the price, a short note "تُباع هذه القطعة ضمن تجميعة فقط." and a primary link "جمّع جهازك" to `/builder?parts=<code>`. The WhatsApp question button stays.
- JSON-LD `price` and the page's headline price use the default option's price.

## 3. Cards and lists
`ItemCard`, the mega-menu item price and any other place that shows `prices.alone` as "the price" must show the price of the default option instead (a helper `cardPrice(item)` next to `price-options.ts`), and the card's add-to-cart line uses that option. A card whose only offered option is `InBuild` shows its price, no add-to-cart button, and a small "ضمن تجميعة" pill (the card still links to the product).

## 4. Cart
`features/cart/cart-rules.ts` refreshes lines from `GET /api/v1/store/items/{key}`. If a stored line's option is no longer offered, switch the line to the default option at its price; if the default is `InBuild`-only, drop the line with the existing "no longer available" notice. Tests in `cart-rules.test.ts`.

## 5. Not in scope
The PC builder keeps using its own prices. Admin screens are done. Do not rewrite other copy (brief 07).

## Checks and report
From `apps/web`: `npm run typecheck`, `npm run lint`, `npm test`, `npm run build`. Browser check (375 and 1280, `/ar`): a product with all three prices, one with "alone" off, one with only "inside a build", one with a single price; the catalog card for each; the cart after switching a category's option in `/admin/categories`. Report: files changed, what you could not do, screenshots folder. Leave everything uncommitted on the branch; do not push.
