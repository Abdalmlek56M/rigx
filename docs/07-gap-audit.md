# RIGX — Gap audit: approved design ↔ database ↔ backend API

Date: 13 Nov 2026 · Roadmap step 2.0
Design source: the approved canvas (page "المعتمد", 50 boards). Pages A/B/C ("أرشيف") are rejected directions and are out of scope. Brand, print and social boards (cover, labels, seal, posts, dark mode, UI kit) need no backend and are only noted where they rely on data.
Backend source: every API route in `src/Host` and every EF entity in `src/Modules` as of commit `5c6549c`.

How to read: **Missing** = the design needs it and the backend has nothing. **Partial** = something exists but does not match. **Extra** = the backend has it and the design does not use it. Every item names the roadmap step that closes it.

---

## 1. Structural gaps (decide/fix first — they change the data model)

| # | Gap | Design | Backend today | Step |
|---|---|---|---|---|
| S1 ✓ | **Every stock item has a public code `RX-xxxx`** (and builds `BLD-xx`, purchases `PO-xxxx`, invoices `INV-YYYY-NNNN`) | used on product page, cart, invoice, sticker, warranty lookup, search ("RTX 3060 أو RX-0142") | units/assemblies/drafts/orders have only GUIDs; orders have `RIGX-yyyyMMdd-random` | 2.2 |
| S2 ✓ | **The storefront sells stock items, not catalog products.** A used item is its own page (real photos of that unit, its warranty, "متاح: قطعة واحدة"); a new item is one listing with a quantity | catalog, product page, cart, builder all show RX items with condition + warranty per item | storefront lists `CatalogProduct` (one price per product, `Condition` on the product); `InventoryUnit` is admin-only and never shown | 2.2 ✓ (INV with 2.6) |
| S3 ✓ | **Guest checkout** ("مش لازم تعمل حساب") with a structured Egyptian address | name, phone, "same number on WhatsApp", governorate, area, street, building/floor/flat, landmark, notes | `POST /api/v1/orders` requires login; order stores one `AddressText` | 2.4 ✓ |
| S4 ✓ | **Login = Google or 6-digit email code only**, same login for customers and staff, staff invited by email | X-Login, ADM-Users ("مفيش باسوردات") | ~~password login/registration/reset~~ removed; email code + Google ✓ | 2.1 ✓ |
| S5 ✓ | **Category specification schema** drives filters *and* builder compatibility (socket, power W, integrated GPU…) and is inherited down the tree | ADM-Categories, D-Admin ("بتستخدم في الفلاتر والتوافق"), D-Builder | specs are free text per product; categories have no spec definitions, no code (CPU/GPU), no "show in menu / in builder" flags | 2.2 |
| S6 | **Business audit log with before/after + restore**, immutable | ADM-Audit, ADM-Users mini-log, every "بتتسجل في سجل النشاط" note | only security events (login, sessions, restrictions) | 2.11 |
| S7 | **Trash (30-day restore) for everything deletable** | ADM-System | only content media and addresses have `IsDeleted` | 2.10 |

---

## 2. Storefront (customer side)

| Screen | Needed by the design | Backend today | Gap | Step |
|---|---|---|---|---|
| D-Home, D-Catalog | category tree with brand/series levels, search by model or RX code, condition filter, price range, memory/spec filters, availability filter (متاح/خلص), sort newest/price, "وصل حديثًا" rail, counts per condition | category tree ✓, spec filters ✓, price/sort ✓, condition ✓, search on name/SKU/brand/category | search by RX code; availability filter; per-item warranty label; counts per condition | 2.2 |
| D-Product (used) | RX code, real photos of the unit (min 3 to publish), official cover image, condition, warranty RIGX (month/3 months/year/agent), "same model new ≈ price", stock-test results (FurMark/OCCT/game), specs, product-specific Q&A, "يركب معاه" suggestions, add to wishlist | product images ✓ (per product, not per unit), wishlist ✓ | per-unit photos + publish rule; warranty per item; new-price reference; test report fields; product Q&A; compatible suggestions | 2.2, 2.5, 2.13 |
| D-Product-New | quantity available, agent/RIGX warranty and who services it, "want to save? used alternative" link, fit notes (PSU W, length mm, box contents) | — | quantity listing for new lots; warranty provider; cheaper used alternative query | 2.2 |
| X-Product-Price | **context price**: with full build / with board / alone, plus tier perk "alone at build price"; ready WhatsApp message prefilled with name, phone, item, price, link | — | context price fields + rule; WhatsApp template from settings | 2.2, 2.17 |
| X-Store-States | cart drawer (cart stays client-side until checkout), sold-out with "بلغني لما تتوفر" (phone required, email optional), offer countdown + old price struck, floating contact button, EN toggle | offers ✓ (price per product, dates) | notify-me requests; offer end-by-quantity + countdown | 2.8 ✓, Phase 5 |
| X-Sold ✓ | "اتباع قبل كده": sold items/builds without prices or names, governorate only if enabled | — | public sold feed + setting | 2.4 ✓ |
| D-Builds ✓ | ready builds with BLD code, usage (ألعاب/مونتاج/مذاكرة), budget filter, parts list, "مناسبة لـ" text, price incl. assembly, order or "عدّل فيها" (open in builder) | `GET /store/assemblies` returns title, price, parts | BLD code, usage, budget, fit text, photos ✓; open-in-builder → 2.13 ✓ | 2.3 ✓, 2.13 ✓ |
| D-Builder ✓ | 8 slots (CPU/BOARD/RAM/GPU/SSD/PSU/CASE/COOL), new or used per slot, only compatible + in-stock options, power estimate, conflicts, total, saved by link, add all to cart, quote PDF/PNG, send on WhatsApp | PC-builder templates with fixed options per slot | free builder over stock items with compatibility from the category spec schema; saved builds; quote export | 2.13 ✓ |
| D-Checkout ✓ | guest checkout; pickup (العجمي, by appointment) or shipping; governorate → fee; deposit rule (orders > 10,000 EGP → 10%, shipping paid upfront, nothing upfront for pickup); payment: cash on delivery, InstaPay, wallet; usage question; "عرفتنا منين؟" list; terms consent | order create ✓, terms consent ✓, payment methods = CashOnDelivery/BankTransfer/MobileWallet, lead source = Website/Phone/Social/Manual | S3; fee by governorate group; deposit calc; InstaPay as a method; usage + source lists as in design | 2.4 ✓ (deposit + shipping quote from settings), 2.7 ✓ |
| D-Order-Success, X-Track ✓ | public tracking link `/t/{token}` without login: timeline (received → confirmed → preparing → shipped → delivered), courier, waybill no., remaining to pay | customer order list/detail need login; no shipped status, courier, waybill | tracking token, Shipped status, shipping fields | 2.4 ✓ |
| D-Account, X-Profile | my orders with timeline, my warranties (end date, status, report, "عندي مشكلة"), my devices + last maintenance, saved builds, addresses ✓, profile (name, phone, email, governorate), login methods (Google link/unlink, email code), devices list ✓ + sign out one/all ✓, "email me on new device", subscriptions (WhatsApp/email offers, new arrivals per category), tier | addresses ✓, sessions ✓, profile partial | warranty registry; Google link ✓; new-device email ✓; subscriptions; saved builds ✓; maintenance history | 2.1 ✓, 2.5 ✓, 2.13 ✓, Phase 5 |
| X-Warranty ✓ | public lookup by phone (masked, last 3 digits per invoice) / invoice no. / RX code or serial; per item: warranty end, status, days left | — | warranty registry + rate-limited lookup | 2.5 ✓, invoice no. 2.6 ✓ |
| D-Services ✓ | sell / trade-in request form (part type, model, how long owned, sell or swap, photos, phone); service bookings (consult, build, test 150 EGP, maintenance, sourcing, upgrade); "register interest" for future services | contact form only | requests inbox (see ADM-Requests) | 2.12 ✓ (photos: 2.9 ✓) |
| X-Footer, D-Home footer | social links, WhatsApp hours, pickup area — all from settings | settings has WhatsApp + 5 social URLs + pickup | links table with order/visibility (see ADM-Settings) — done ✓ | 2.17 ✓ |

## 3. Admin

| Screen | Needed by the design | Backend today | Gap | Step |
|---|---|---|---|---|
| ADM-Dashboard ✓ | period switch + compare; sales, net profit after expenses and partner share, orders + average, stock value at cost, partner dues, **cash available (cash + wallets)**; 30-day sales/profit chart; to-do counts; lead-source breakdown; top categories with margin; new vs used margin; full-build vs split sales; avg days to sell used; usage breakdown; notify-me demand; stock aging > 30 days; partner profits this month | dashboard = catalog/content/review counts only; business report (sales, COGS, expenses, partner accrued/paid) | money KPIs, breakdowns, aging, demand; money accounts for "cash available" (**owner decision D3**) | 2.15 ✓ (notify-me demand → 2.8 ✓) |
| ADM-POS ✓ | sell an item or a build; build sold **full or split** (rest returns to stock and build closes); **free sale price per line**, warning below cost, discount limit per role; customer search by phone / new customer; source; pickup or shipping; payment: cash, InstaPay, wallet, bank transfer, **split**; paid/remaining; partner share preview; confirm + print, save as pending, send invoice on WhatsApp | manual order takes catalog prices only (no free price), no split build sale, no split payment | all listed | 2.4 ✓ (partner preview → 2.14 ✓, print / WhatsApp invoice → 2.6 ✓) |
| ADM-Purchase | seller = person/trader/company with name, phone, optional national ID (internal), governorate, area, source, notes, search by phone; lines: category path, **free-text model with alias suggestion**, condition, serial, cost, suggested price, partner, visibility; "bought a full PC" → split into parts or keep as a build; photos; receipt image; payment to seller: cash/InstaPay/wallet/**trade-in**/cash+trade, paid/remaining; convert from a sell request; save + print stickers | purchase drafts (supplier or marketplace seller), receive draft → units | seller fields; per-line serial/partner/visibility; full-PC intake; trade-in + payment to seller; request → purchase | 2.2 |
| ADM-Inventory, D-Admin | list: code, model, category path, condition, qty, cost, price, profit, partner %, state (available / in build / **reserved** / sold), visibility, days in stock; filters; bulk hide/show/price/print stickers/export; add item: warranty choice, cost/price/profit, partner + %, source PO, context prices, optional specs, internal note, box present, photos (min 3 to publish), visibility, print sticker | units: cost, standalone price, partner override, status (no Reserved), source fields, images JSON | RX code; warranty; Reserved state; visibility; box flag; internal note; context prices; bulk actions; export | 2.2, 2.10 |
| ADM-Build ✓ | BLD code; name; usage; sell mode (full only / full or per part); build warranty (applied to all parts, per-part override); **copies available**; visibility; parts with cost, price, partner, % (override); swap from stock or from another build ✓; add part not in stock; remove → back to stock or move to another build; totals incl. partner shares; photos + "مناسبة لـ" | assemblies with allocations, swap ✓, remove ✓, disassemble ✓ | all done ✓ (visibility = publish/unpublish); part drag order not stored; partner record per part → 2.14 ✓ | 2.3 ✓ |
| ADM-Orders ✓ | statuses: awaiting review / confirmed / preparing / shipped / delivered / cancelled; **reservation** (hold until time; on expiry: notify only or release); courier, waybill, tracking link; WhatsApp message; invoice; cancel; **blacklist** (by phone/email/address, reason; their orders need full prepayment) | New/Confirmed/Preparing/Ready/Delivered/Completed/Cancelled; stock claimed at Confirmed ✓ | Shipped state; reservation timer + expiry action; shipping fields; blacklist | 2.4 ✓ |
| ADM-Requests ✓ | one inbox: sell, upgrade, notify-me, support ticket, contact; statuses (new / following / closed + per-type steps); customer, phone, source, governorate; initial offer, decision, expected resale (internal); WhatsApp reply template; **convert to purchase** | contact messages only | requests inbox | 2.12 ✓ (photos: 2.9 ✓) |
| ADM-Partners | partner record: name, phone, **capital**, join date, default %, profit basis (**financed parts only / whole shop profit**), notes; capital deposits/withdrawals; payouts; statement PDF; financed parts with per-part % | partner = name string; agreements, per-unit/product/category allocations, earnings, settlements | partner entity, capital ledger, basis "whole shop" (**owner decision D4**), statement | 2.14 ✓ (statement PDF and admin page → Phase 4) |
| ADM-Categories | unlimited tree ✓ with drag order; code (CPU/GPU); URL slug ✓; show in menu; show in builder; spec definitions (name, type: choice/number/yes-no, options, filter flag, compatibility flag), inherited by children | tree ✓, slug ✓, sort ✓, active ✓ | S5 | 2.2 |
| ADM-Expenses ✓ | period + compare; fixed monthly expenses; % of sales; types list (editable); linked to order / item / **build**; **paid from** cash/InstaPay/wallet/partner money; recurring monthly; receipt image; export | expenses: category, direct/overhead, order/unit link, attachment ref | build link, paid-from, recurring, type list, export | 2.15 ✓ (receipt image: 2.9 ✓, Excel/PDF export → 2.10) |
| ADM-Offers | offer on category / item / build / whole store; new/used filter; % / fixed / special price; ends by date, by quantity or both; countdown; old price struck; stackable with coupons; **never below cost** guard; coupons: code, %/fixed/free shipping, min order, total uses, per customer, source attribution | offers: items with offer price, dates, priority | scopes, end-by-quantity, guard, coupons | Phase 5 |
| ADM-Customer | customer profile: contact, governorate, login methods, source, usage, total spent, order count, internal notes; actions: view as customer (read-only), send login code, link/unlink Google, sign out everywhere, **merge accounts**, blacklist, suspend; orders, warranties (**extend warranty**), requests, history | user admin, restrictions (suspend), sessions | customer 360 view, merge, extend warranty, impersonation (read-only) | 2.18 ✓ |
| ADM-Users | users with role, login method, last login, status; editable role × permission matrix incl. **discount limit %**; invite by email (code or Google, no passwords) | roles/permissions ✓, users ✓ (password based) | ~~invite flow~~ ✓, discount-limit permission value | 2.1 ✓ (invite), discount limit open |
| ADM-Articles ✓ | list with category, status (published/draft/**scheduled**), **views**; editor blocks (H2/H3, bold, link, image, table, list, **store product card**, callout); cover image; SEO slug/description/keywords; YouTube embed | articles bilingual + SEO + revisions ✓; body is plain text (HTML rejected); article videos ✓ | scheduled publish, views, structured blocks incl. product card | 2.16 ✓ |
| ADM-FAQ ✓ | scope: general / category / product; "excluded in" list; YouTube video per answer; help page only option | FAQ with a free-text category | scope + exclusions + video | 2.16 ✓ |
| ADM-Settings | store info (name, domain, email, WhatsApp hours, pickup address, logo); **social links table** (platform, link, show on site, show on invoice, order, WhatsApp+call pair, floating button); shipping (courier name, pickup on/off, **governorate groups with fee + delivery time**, default); payment & deposit (threshold, %, InstaPay no., wallet no., method toggles); **watermark** (logo, position, opacity, size, articles too, re-apply to old images); store controls (maintenance mode, announcement bar, show sold-out, show offer counter, guest checkout on/off, homepage section order, minimum order); **WhatsApp message templates** per source; reviews settings; language; invoice (prefix, background logo, QR, terms line); emails; login | WhatsApp no. + message, 5 social URLs, pickup address/hours/instructions, 3 summaries; homepage layout JSON (sections) | almost all of it — done in 2.17 ✓ except logo uploads and re-applying the watermark (2.9 ✓); email/login settings already in Identity (2.1) | 2.17 ✓ (+2.7 ✓, 2.9 ✓) |
| ADM-Campaigns | subscribers (WhatsApp/email, interests, tier, source); compose with `{الاسم} {اللينك} {الطبقة}`; audiences (all / category interest / tier / past buyers / notify-me); linked offer/item/build; image; schedule; email auto-send; **WhatsApp prepared list** with "sent" marks; automatic messages (follow-up 3 days after delivery + satisfaction, warranty ends in 7 days + upgrade offer, back in stock, tier upgrade) | — | everything | Phase 5 (Messaging) |
| ADM-Tiers | tiers off by default; conditions over last 12 months; perks with their own switches (extra warranty, part at build price N/month, early offers, priority, fixed discount, maintenance perks, faster reply); manual promotion; demotion rule | — | everything | Phase 5 |
| ADM-Audit | filters (user, section, action), before/after per field, device + IP, **restore previous values**, immutable, export | security audit only | S6 | 2.11 |
| ADM-System | trash with purge after 30 days, restore; backups (schedule, time, retention, destinations server + Google Drive, list, download, restore); data reset by section with step-up code; export Excel/PDF/CSV/XML per dataset with date range | — | everything | 2.10 |

## 4. Documents and emails

| Board | Needed | Backend today | Step |
|---|---|---|---|
| INV-Part / INV-Build | number `INV-YYYY-NNNN`, customer, phone, address, date+time, order ref, fulfillment, payment, **seller name**; lines: RX code, part type, name, S/N, condition, warranty + end date, qty, price; build invoice groups parts under BLD; subtotal, shipping, discount, total, paid, remaining; QR → warranty lookup; online link `/i/{code}`; emailed + in account | none | 2.6 ✓ |
| INV-Quote | shareable quote (no invoice number): build name, usage, parts with condition + price, total, date of prices | none | 2.6 ✓ / 2.13 ✓ |
| X-Email ✓ | order received, order confirmed (with tracking link), follow-up after 3 days with 5-step satisfaction, warranty ends in 7 days + upgrade offer | identity emails only (outbox ✓, reusable) | 2.4 ✓ (received, confirmed, shipped), Phase 5 (follow-up) |
| X-Labels | static stickers; the QR always opens the warranty lookup | — (print only) | 2.5 ✓ |

## 5. Extra in the backend (not used by the design)

| Item | Recommendation |
|---|---|
| Password login, registration, password reset, admin password reset | remove in 2.1 ✓ (owner decision 3) |
| Warehouses (`Inventory` module, multi-warehouse CRUD) | design has one pickup location → remove, or keep hidden until a branch exists (**D7**) |
| Supplier profiles with bank/tax/IBAN, supplier product catalog | folded into the seller record in 2.2 ✓ (**D7**) |
| PC-builder templates with fixed options | replaced by the free builder (2.13) + ready builds (assemblies) → removed in 2.13 ✓ |
| Catalog product variants | removed in 2.2 ✓ (**D7**) |
| Uploaded video hosting (`ContentVideoAsset` files + streaming) | design uses YouTube links only → keep YouTube, drop file hosting (**D7**) → removed in 2.16 ✓ |
| In-app notifications (`UserNotification`) | not in the design (email + WhatsApp only) → remove or keep for staff (**D7**) |
| Homepage layout JSON (rails, hidden items) | design only orders homepage sections from settings → removed in 2.17 ✓ (section order + visibility in store controls) |

---

## 6. Owner decisions — all approved as proposed (13 Nov 2026)

| # | Question | Decision |
|---|---|---|
| D1 | New items: one RX code per lot of identical new pieces (page shows "متاح: [الكمية]"), used items: one RX code per piece? | yes, as drawn |
| D2 | Order statuses shown to the customer: استلمنا → اتأكد → بيتجهز → اتشحن → اتسلم (+ اتلغى). "مكتمل" (payment settled, partner profit posted) stays internal? | yes |
| D3 | Dashboard "الكاش المتاح": track balances per money account (cash box, InstaPay, wallet), moved by sales, purchases, expenses, partner payouts? | yes, with an opening balance per account |
| D4 | Partner basis "ربح المحل كله": partner gets X% of the store's net profit for the period (after expenses), instead of per financed part? | yes, monthly |
| D5 | Lead sources: فيسبوك، Marketplace، إنستجرام، تيك توك، يوتيوب، تليجرام، جوجل، صاحبي رشحكم، حاجة تانية (editable in settings)? | yes |
| D6 | Warranty options: شهر / ٣ شهور / سنة / ضمان الوكيل (editable list)? | yes |
| D7 | Remove the "extra" items in §5 (warehouses, supplier bank details, PC-builder templates, variants, video file hosting, in-app notifications)? | remove all except reviews |
| D8 | Product Q&A on the product page = answers written by you (scoped FAQ), not public customer questions? | yes |
| — | Reservation length + expiry action: decided 13 Nov 2026 (24h default, notify only unless the order is set to release), built in 2.4 ✓. Still open: tier rules, hosting/mail | — |

## 7. Order of work (proposed Phase 2)

1. **2.1 Auth** (S4) ✓ done — unblocks guest/customer flows and staff invites.
2. **2.17 Settings v2** ✓ done — shipping groups, deposit, payments, links, templates, watermark, invoice settings are read by later steps.
3. **2.2 Catalog & intake** (S1, S2, S5) ✓ done 13 Nov 2026 — RX codes, stock-item listings, category spec schema, intake form. The biggest step; split into 2.2a codes + listing model, 2.2b category specs, 2.2c purchase/intake.
4. **2.3 Assemblies** ✓ done 13 Nov 2026, **2.4 Sales/POS/orders** (S3) ✓ done 13 Nov 2026, **2.5 Warranty** ✓ done 13 Nov 2026, **2.6 Invoicing** ✓ done 13 Nov 2026, **2.7 Shipping** ✓ done 13 Nov 2026.
5. **2.12 Requests inbox** ✓ done 13 Nov 2026, **2.13 Builder v2** ✓ done 13 Nov 2026, **2.14 Partners v2** ✓ done 14 Nov 2026, **2.15 Money & dashboard** ✓ done 14 Nov 2026, **2.16 Content v2** ✓ done 14 Nov 2026, **2.18 Customers admin** ✓ done 14 Nov 2026.
6. **2.10 System**, **2.11 Audit** (S6, S7) — can start any time after 2.1; the audit interceptor is better early so later steps are logged.
7. **2.8 Notify-me** ✓ done 14 Nov 2026, **2.9 Media watermark** ✓ done 14 Nov 2026.
Phase 5 keeps offers/coupons, tiers, campaigns.
