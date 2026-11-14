# @rigx/web

RIGX storefront: Next.js (App Router), React, Tailwind CSS v4, next-intl (Arabic at `/`, English at `/en`).

## Scripts (run from the repo root)
- `npm run dev` / `npm run build` / `npm run lint` / `npm run typecheck`
- `npm test` runs the pure-logic tests (`node --test`, e.g. cart rules).

## Environment
- `RIGX_API_ORIGIN` (default `http://localhost:8080`): the API. `/api/*` is rewritten to it so the browser sees one origin; server components call it directly. See `.env.example`.

## Structure
- `src/app/[locale]` routes and layout; `src/proxy.ts` locale routing.
- `src/components/layout` site frame (top bar, header, footer, floating controls); `src/components/ui` shared pieces.
- `src/features/cart` local cart (localStorage `rigx.cart.v1`): rules in `cart-rules.ts`, store, drawer, toast.
- `src/lib/api` server and browser API clients (`@rigx/api-client`); `src/lib/storefront.ts` layout data with safe fallbacks.
- `messages/{ar,en}.json` every UI string; design tokens in `src/app/globals.css`.
