# Admin UI review (22 Nov 2026)

Method: the admin run locally (API on a throw-away PostgreSQL with the demo data, web dev server) and clicked through at 1280 px and 375 px: dashboard, inventory list and the new item page, categories, POS, builds list and a build, orders. Not every screen was opened: purchases, expenses, partners, users, settings, articles, audit, backups and the stock import still need a pass (see "Still to review").

## Fixed in this pass
1. **The "عرض / تحكم" eye of the inventory list was off screen at 1280 px** (the table is 1230 px wide inside a 967 px area, so the last column needed a sideways scroll). It is now the second column, next to the checkbox, so it is always visible.
2. **Tables scrolled sideways on a phone** (the model name was cut by the screen edge). `DataTable` now turns every row into a card under 768 px: column names above the values (the label-above-value rule), the header row hidden, empty cells hidden; a column can take the whole card width (`phoneFull`, used for the inventory model name). This applies to every admin list at once (orders, users, partners, purchases, ...), so each of them should be looked at once on a phone.
3. **Photo "main" badge covered the delete button** on a 105 px thumbnail (a 69 px orange text badge). It is now a small star badge with a tooltip.
4. **Item page:** the units table was squeezed into half the width and its action was cut ("تغيير الشريك"); units now have a full-width card and the action is an icon with a tooltip. The three price fields are aligned at the bottom (their labels wrap to different heights).
5. **Build page:** the build name floated under the header with the action buttons loose below it; both are now in one card.

## Found, not fixed
- **Admin copy is partly colloquial Egyptian** (about 170 strings under `admin.*`: POS "بتبيع إيه؟", "ضيف حاجة للسلة عشان…", dashboard, system, partners...). Brief 07 already covers `admin.*` after the storefront.
- **The build parts table still scrolls sideways at 1280 px** (many columns: swap, edit, partner %, costs). On a phone it is cards. Consider dropping the share-amount sub-line or moving partner columns behind the part's edit.
- The category tree shows names exactly as stored ("Intel", "AMD" in English) next to Arabic ones; fine, but the English-name field is empty for most demo categories.
- POS right column ("الملخص") is narrow at 1280 px and the summary button is disabled-grey until something is in the cart (fine) but looks inactive rather than waiting.

## Second pass (22 Nov, later)
Opened at 1280 px: purchases list and the new-purchase form, stock import, expenses, accounts, partners, users, settings, audit log, system (trash and backups), articles.

### Fixed
6. **Extra-price fields of a price the category switched off are hidden** in the stock-item form, the purchase line and the "add a new part to a build" panel (they still read all three prices from the data; only the fields and the hint disappear). Pure rule `offeredPrices(tree, categoryId)` with tests.
7. **Stock import used the browser's own file button** ("Choose File / No file chosen" in English next to Arabic text). It is now a normal primary button "اختر الملف" with the file name beside it.

### Found, not fixed
- **Empty states and some hints are colloquial** ("مفيش مشتريات بالشكل ده", "مفيش شركاء لسه", "موزعة إزاي"): brief 07 (`admin.*`).
- **Purchase line price fields:** the three price fields sit in a two-column grid at 1280 px, so the third one is alone on its row; a sub-group "أسعار البيع" with three equal columns would read better.
- **Audit log filters** wrap into three uneven rows at 1280 px (view, module / entity / action, then the two dates each on its own line); the date inputs show the browser's English placeholder (mm/dd/yyyy) in an English browser.
- **Accounts table** scrolls sideways by a few pixels at 1280 px.
- The settings page is one long form with a section menu on its side; fine on a desktop, not opened on a phone.

## Still to review
Purchase receive and pay screens with real data (the demo data has no purchases), POS with a full cart and payment, order detail, customers, offers and coupons, requests, policies and FAQ editors, backups with a real backup, dashboard on a phone. The local setup used here: a PostgreSQL container with the runtime roles from `docker/postgres/init/010-runtime-roles.sh`, the API from `.claude/launch.json` (`api-4.10`) and the web from `web-4.10`; sign-in with the preview owner and the code from `/api/v1/development/preview/email-code`.
