# RIGX — Change History (canonical summary)

## Round 4 — Engineering Foundation (12 November 2026)
- Inventory and baseline verification: build, 190 backend tests, TypeScript, 30 unit tests, production build.
- Inventory-concurrency hardening: assembly row lock + component status validation; product-unit claim with `FOR UPDATE SKIP LOCKED`.
- EF concurrency conflicts map to HTTP 409 instead of 500.
- Security headers added to API and Next.js.
- Dead components removed after reference tracing (14 files); obsolete header test replaced.
- Canonical documentation set created; historical reports indexed.

## Round 3 — UI Parity & Legacy Removal
- Account shell/sidebar for 10 screens; content pages, PC Builder wizard, auth shell, Admin sidebar.
- Claims neutralised; dead component removal; route matrix with one status per route.

## Round 2 — Native UI replacement
- Reference CSS layers, new header/home/catalog/product/cart/checkout/builds/contact/footer.
- Checkout backend gaps: variants, Delivery/Pickup, Terms consent, order snapshots, order details.

Earlier dated reports remain in the repository root and are indexed in `docs/history/INDEX.md`.

## Round 5 - Final Engineering Closure (12 Nov 2026)
- Stock claim uses exact locked unit IDs; build cancellation returns components to InAssembly.
- Added concurrent unit/build and cancellation integrity tests; backend 194/194.
- NuGet and npm vulnerability audits: 0 known vulnerabilities.

