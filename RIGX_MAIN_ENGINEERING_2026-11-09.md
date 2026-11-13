# RIGX — Main Product Engineering & Experience Expansion (9 November 2026)

## 1. النتيجة
تم تنفيذ جولة هندسية موسّعة داخل `DEV - Copy\HARDWARE STORE` شملت:
- توحيد Header/Navigation وإضافة العروض.
- Account Center + Avatar + My Reviews.
- Product/Store Reviews مع Moderation وAggregates.
- Offers بإدارة ونشر حقيقي.
- Purchase Drafts متعددة المصادر (Draft-only) بقرار مالك.
- Admin Analytics من بيانات حقيقية.
- إصلاح Catalog Navigation Defect.
- تحسين الأداء القابل للقياس.

**لم يُنفذ:** Orders/Checkout، Stock/Transfers/Reservations/Serials، PO Approved/Sent، Goods Receipt، Ledger/Cost، Payments، Watermark، Video Library، SMTP إنتاجي، نصوص سياسات.

## 2. Workstreams

### A. Storefront Header & Navigation
- دُمج Utility Bar + Search/Account/Cart/Theme/Language + Primary Navigation داخل Header واحد متصل بخلفية Surface وحد سفلي، بدل Floating Rounded Cards.
- أزيل فرض `Inter` على Navigation/Utility، فأصبحت العربية تستخدم IBM Plex Sans Arabic والإنجليزية Inter عبر `html[lang]`.
- أضيف رابط **العروض** Desktop/Mobile، وبقيت Mega Menu/Mobile Drawer/Keyboard/Focus كما هي.
- `HomeLayoutProvider` لا يجلب إعدادات الصفحة الرئيسية إلا على `/` بدل كل Route.

### B. Category & Brand Discovery
- Category/Brand Rails تستخدم Drag بالماوس، Touch Swipe، Keyboard (Arrows/Home/End)، Pause عند التفاعل/إخفاء الصفحة، Reduced Motion.
- أزرار الأسهم تحرك ~80% من عرض الـViewport بدل بطاقة واحدة.
- ترتيب Home الافتراضي: Hero → Categories → Brands → Offers → Discovery → Guide → Latest → Closing، مع ترقية Layouts القديمة بإدراج Offers دون كسر الترتيب المخزّن.

### C. Account Center
- `/account` أصبح Overview: Avatar، الاسم، Username، Email + حالة التحقق، Phone، ملخص الأمان، العناوين، Wishlist، Reviews، Orders مؤجل بوضوح، Marketing مؤجل.
- `AccountNav` موحّد على Profile/Sessions/Addresses/Wishlist/Reviews، مع رجوع إلى Overview.
- Avatar: رفع JPG/PNG/WebP عبر Pipeline الموجود (تحقق فعلي بالمعالجة، Original + WebP + Thumbnail)، حذف، Fallback بالحروف، إظهار بجوار المراجعة باختيار العميل.
- تغيير البريد/الهاتف النهائي لم يُفتح؛ لا قناة تحقق جديدة ولا OTP وهمي.

### D. Reviews
- `product_reviews` (مراجعة واحدة/مستخدم/منتج) و`store_reviews` (واحدة/حساب).
- Rating 1..5، Title اختياري، Body نص عادي 10..4000، ShowAvatar.
- كل إنشاء/تعديل → Pending؛ Public يعرض Published فقط.
- Aggregate: Average/Count/Distribution من Published فقط.
- Admin Moderation: Publish/Reject/Hide + سبب + Actor/Time + RowVersion.
- لا Verified Purchase ولا Reviews وهمية.

### E. Offers
- `offers` + `offer_items`: Draft/Published/Cancelled، Window، Priority، RowVersion.
- OfferPrice صريح من Admin، ويُرفض ≥ BasePrice أو ≤0.
- Publish يتحقق من أن المنتجات Published. Public `/offers` + قسم في Home.
- لا Coupons أو قواعد خصم غير معتمدة.

### F. Purchase Drafts (ADR-027)
- `purchase_drafts` + `purchase_draft_items`.
- Sources: RegisteredSupplier / Marketplace / IndividualSeller / Other.
- SupplierId اختياري (إلزامي فقط للمورد المسجل)، بيانات بائع خارجي مستقلة، Listing URL، Date، Notes، Items/Variants، Planning Unit Price، Total.
- Draft/Cancelled فقط؛ RowVersion؛ Audit؛ لا Stock/Cost/Ledger/PO/GRN.
- Admin `/admin/purchasing/drafts`.

### G. Admin Analytics
- `/api/v1/admin/analytics/dashboard` بصلاحية `Catalog.View`.
- قراءة منفصلة لكل Module DbContext، بدون JOINs عابرة.
- Metrics: Catalog/Purchasing/Customers/Content/Reviews/Media + Product Conditions + Draft Sources + Rating Distribution + 14-day Series + آخر 8 Audit Events.
- UI أعلى `/admin`، بدون Sales/Profit/Conversion/Stock turnover الوهمية.

### H. Catalog Navigation Defect
- **Root cause:** Active Route Matching استخدم `startsWith`، فكان `/admin/catalog` و`/admin/catalog/products/new` نشطين معًا.
- **Fix:** Exact-path matching في `NavItem`. `/admin/catalog` List View، و`/new` صفحة إنشاء فقط، مع اختبار E2E يثبت Active واحدًا.

### I. Media & Performance
- Media Pipeline الحالي: تحقق فعلي بفك الصورة (ليس الامتداد)، Original محفوظ، WebP عرض، Thumbnail، Replace فوق نفس Keys.
- لم يُفعّل AVIF/Watermark (لا شعار معتمد، ولا فائدة AVIF مثبتة في هذا النطاق).
- **القياس:**
  - RAM الكلي 31.9GB / Free 5.5GB وقت القياس.
  - Next Dev (PID 16228): ~5.4GB.
  - Docker (كل الحاويات): <1.5GB.
  - Production Next (`next start`): 109MB، Home 51ms.
  - Dev: Home 104ms، Long Tasks 391/97/148ms، CLS 0.183.
  - Production: بلا Long Tasks في القياس، CLS 0.046.
- **الخلاصة:** ضغط الـ28GB ليس من RIGX Runtime وحده؛ Next Dev + WSL/Docker + المتصفحات + أدوات الخلفية هي العامل الأساسي. أُصلح Fetch غير الضروري للـHome Layout، وتحسّن الـRails/Header.

## 3. Migrations
| Module | Migration |
|---|---|
| Identity | `M7_ProfileAvatar` (`avatar_storage_key`) |
| Catalog | `M7_Reviews` (`product_reviews`, `store_reviews`) |
| Purchasing | `M6_PurchaseDrafts` |
| Storefront | `M8_Offers` |

طُبقت على QA فقط `rigx_m5_qa_20261108` بعد Backup:
`.backups/rigx_qa_before_round2_20261109.dump`
**Preview 3005/8085 وOwner DB لم تُمسّا.**

## 4. التحقق الفعلي
### Backend
- `dotnet build HARDWARE_STORE.slnx` → **Build succeeded، 0 Error**.
- `PurchaseDraftPostgreSqlTests` → **3/3**.
- `ReviewsAndOffersPostgreSqlTests` → **2/2**.
- Dashboard endpoint تحقق فعليًا: 200 مع 8 Catalog metrics + 5 Purchasing + 14-point series.
- Offers/Reviews End-to-End عبر HTTP: Create/Publish/Public/Moderation → 200.

### Frontend
- `npx tsc --noEmit` → **0**.
- `npx vitest run` → **37/37**.
- `npm run build` → **نجح**.
- Playwright Main Engine → **6/6**:
  - Offers nav/page.
  - Account Center + Avatar.
  - Catalog Active Route.
  - Review Publish flow.
  - Purchase Draft create/cancel.
  - Admin Analytics.
- Full Playwright: **26 passed / 1 skipped** في تشغيل واحد؛ الاختباران المعتمدان على Login المكثف (`customer-identity` و`storefront login`) يمران **2/2** عند تشغيلهما في مجموعة معزولة بعد إعادة تشغيل API، بسبب حد Login المعتمد `5/IP/min` الذي تستهلكه بقية السويت داخل الدقيقة نفسها. هذا قيد تشغيل اختبارات موثّق وليس عيبًا في الوظيفة.

## 5. Screenshots
`docs/review/main-engineering-2026-11-09/`
- home 1440 Light/Dark، home 390.
- offers، product reviews.
- account center، account reviews.
- admin analytics، admin reviews، admin purchase drafts، admin offers، admin catalog nav.

## 6. أهم الملفات
- Backend:
  - `src/Modules/Purchasing/{PurchaseDrafts,PurchaseDraftService}.cs`
  - `src/Modules/Catalog/Reviews.cs`
  - `src/Modules/Storefront/Offers.cs`
  - `src/Host/HardwareStore.Api/{PurchaseDraftEndpoints,ReviewEndpoints,OfferEndpoints,AdminAnalyticsService,AdminAnalyticsEndpoints,ProfileAvatarEndpoints}.cs`
  - `IdentityCurrentUserService`, `IdentityPersistence`, `CatalogMediaStorage`.
- Frontend:
  - `site-header.tsx`, `globals.css`, `home-layout.ts`, `storefront-rail.tsx`, `storefront-home.tsx`
  - `account-overview/account-nav/account-reviews/profile-avatar`
  - `product-reviews`, `content-reviews-admin`
  - `offers-experience`, `content-offers-admin`
  - `purchase-drafts-admin`, `admin-analytics`, `admin-shell`
  - `api-client.ts`, `i18n.tsx`
- Tests: `PurchaseDraftTests`, `ReviewsAndOffersTests`, `main-engine.spec.ts`, updated `home-layout.test.ts`.

## 7. القيود والمخاطر
1. Reviews لا Rate Limiter مخصص بعد؛ الحماية عبر Auth + مراجعة واحدة/منتج + Moderation.
2. Avatar public exposure يحتاج سياسة/موافقة صريحة إذا أراد المالك.
3. Draft Purchases بصلاحية `Purchasing.CreateOrder` مؤقتًا؛ قد تحتاج `Purchasing.ManageDrafts`.
4. لا PO/GRN/Stock/Cost؛ بوابة المالك ما زالت مطلوبة.
5. Offers تعتمد سعر Admin صريح؛ لا قواعد خصم/كوبونات.
6. Media لا AVIF/Responsive srcset متعدد بعد؛ Watermark معطّل.
7. Production deployment (Proxy/TLS/SMTP/Storage/Backup) ما زال غير ممتد.

## 8. الخطوة التالية
- اعتماد سياسة Reviews/Avatar Rate Limiting.
- اعتماد بوابة Approved PO/GRN/Stock (Q2.1–Q2.5/Q6.3/Q8.7).
- تزويد الشعار/نصوص السياسات/مزود البريد/Storage دائم عند فتح Production.
- معالجة Flake أخير في Account Management E2E عند التسليم النهائي.
