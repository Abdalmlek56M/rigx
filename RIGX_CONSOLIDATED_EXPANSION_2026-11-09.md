# RIGX — Consolidated Product Expansion Batch (9 November 2026)

## 1. الهدف والنتيجة
نقل RIGX من متجر + حسابات أساسية إلى منصة بها دفتر عناوين، Wishlist دائم، CMS (صفحة رئيسية/مقالات/FAQ/سياسات)، مكتبة صور، Audit Viewer، بريد هوية دائم، وبيانات تشغيلية للمستودعات — مع إبقاء المخزون والمشتريات خلف بوابة قرار المالك.

**ما أصبح قابلًا للاستخدام فعليًا:** Account Address Book، Persistent Wishlist، Homepage Content Management، Articles CMS، FAQ CMS، Reusable Image Media Library، Admin Audit Viewer، Durable Identity Email Delivery، Warehouse Operational Metadata.

**ما لم يُفتح:** Purchase Orders/Goods Receipt/Stock Ledger/Serials/Transfers/Reservations، Orders/Checkout حقيقي، Video Library، Watermark، نصوص سياسات قانونية.

## 2. Workstreams

### 2.1 Person Classification
- **السبب:** إنشاء مستخدم Admin كان يضع `IsEmployee = true` دائمًا بغض النظر عن الدور.
- **القرار:** `PersonClassificationPolicy` تصنيف **إضافي**: Customer وEmployee وSupplier يمكن أن تجتمع. الدور `Customer` يضع `IsCustomer`، وأي دور آخر يضع `IsEmployee`، ولا يمحو علمًا قائمًا.
- **الأثر:** دعوة Staff لم تعد تصنّفه Customer، والحسابات المزدوجة تبقى مشروعة.

### 2.2 Session Data Minimization
- توقف جمع IP الخام للجلسات الجديدة نهائيًا في `DatabaseTicketStore`.
- العمود `IpAddress` بقي Legacy للقراءة فقط، ولا يُعرض في API/UI. لا مسح تاريخي تلقائي (يحتاج سياسة احتفاظ).

### 2.3 Durable Identity Email Outbox
- **السبب الجذري:** `IdentityEmailDispatcher` كان `Channel` في الذاكرة؛ توقف التطبيق = فقد رسالة، بلا Retry/Dead Letter. كما أن `DataProtection:KeyRingPath` كان مضبوطًا في Compose لكن لا كود يقرأه.
- **الحل:** `src/Modules/Identity/IdentityEmailOutbox.cs` + جدول `identity_email_outbox`:
  - Payload مشفّر عبر Data Protection (`HardwareStore.IdentityEmailOutbox.v1`).
  - حالات Pending/Processing/Sent/Failed/DeadLetter، Attempts، NextAttemptAt، Backoff أُسّي، Dead Letter بعد 5.
  - Claim ذرّي `FOR UPDATE SKIP LOCKED` + استرداد Processing المعلّق.
  - مسح الـPayload بعد الإرسال، وAudit للنجاح/الفشل دون Tokens.
  - `Program.cs` يثبت Key Ring على `DataProtection:KeyRingPath` داخل Volume.
- **قيد:** إدراج الرسالة يحدث بعد Commit عبر Context مستقل؛ إذا سقطت العملية في تلك الفجوة يبقى الحساب معلّقًا ويمكن Resend. بعد الإدراج، الرسالة durable.

### 2.4 Customer Address Book
- جدول `customer_addresses` مع Soft Delete، عنوان افتراضي واحد (Unique Filtered Index)، RowVersion، Ownership/IDOR، Validation، Audit داخل نفس المعاملة.
- API: `GET/POST /api/v1/identity/addresses`، `PUT/DELETE /{id}`.
- الواجهة: `/account/addresses` + Drawer + Default + Confirm Delete + حالات Empty/Loading/Error.
- **Root cause أُصلح:** `emptyForm` استخدم `null` للحقول الاختيارية بينما `save()` ينادي `.trim()` → انهيار Save. أصبحت القيم `""`.

### 2.5 Wishlist
- `wishlist_items` Product-level + قراءة فقط Products/Images/Categories (ExcludeFromMigrations).
- Add idempotent، Remove، Merge بلا تكرار، Clear، عزل الحسابات، تجاهل المنتج المحذوف، وإظهار Draft كغير متاح.
- Guest: localStorage Snapshot، Merge عند الدخول، حماية من فقد البيانات.
- UI: Heart في البطاقة والتفاصيل، `/account/wishlist`، Empty/Toast feedback، AR/EN وRTL/LTR.

### 2.6 Homepage Content Management
- `homepage_layouts` بصف Draft وصف Published، Layout JSON محدود الأنواع (7 Sections + Rails brands/categories/products + limit/order/hidden/عناوين).
- `Save draft` يتحقق من الشكل فقط، و`Publish` يتحقق من وجود Brand/Category/Product المشار إليها، مع RowVersion.
- `/admin/home-content` يحفظ وينشر عبر API بدل localStorage. الصفحة الرئيسية تقرأ Published من API و`HomeLayoutProvider` يعرض Fallback الافتراضي عند غياب النشر.

### 2.7 Articles + FAQ + Policies
- `content_articles` + `content_article_revisions`، Draft/Published/Unpublish، Slug فريد، AR/EN، Tags، SEO/Canonical، صورة من المكتبة، Revision لكل حفظ/نشر.
- نصوص عادية فقط: رفض `<...>` وControl Characters؛ لا HTML/JS ولا `dangerouslySetInnerHTML`.
- Public: `/articles`، `/articles/[slug]` (SSR + Metadata/OG + Canonical + Sitemap + Noindex عند الغياب).
- `content_faqs` Draft/Published + Sort + Category، Public `/faq` حسب اللغة.
- `content_policies` نسخ مرقمة: Draft→Review→Published، أرشفة السابقة، ومنع تعديل المنشور (409). Public `/policies/[kind]`.
- **لم يُخترع أي نص قانوني/تجاري:** النسخ تبدأ فارغة وتظل Draft حتى إدخال المالك.

### 2.8 Media Library
- `content_media_assets` + `content_media_asset_usages` في Catalog، ملفات تحت `content/{assetId}` على نفس Volume الحالي (`Catalog__MediaRoot`).
- Upload/List/Search/Metadata AR/EN/Alt/Caption/Tags/Dimensions/Size/Replace.
- Replace يكتب فوق نفس Storage Keys فلا تنكسر المراجع.
- Delete حذف منطقي ومرفوض 409 إذا كان الأصل مستخدمًا في محتوى منشور.
- `IContentMediaUsageSink` يربط Storefront بالمكتبة دون تداخل DbContexts.
- Watermark معطّل لعدم وجود شعار RIGX معتمد.

### 2.9 Admin Security Audit Viewer
- `SecurityAuditQueryService` Read-only، Pagination، Filters (EventType/Outcome/Date/Actor/Subject/Correlation/Search).
- Whitelist لمفاتيح Metadata + حجب أي مفتاح يحتوي token/secret/password/hash/ip/email/url/cookie/ticket.
- لا SessionKeyHash/ProtectedTicket/IP/Tokens في الـDTO أصلًا.
- `/admin/audit` + Drawer، وبصلاحية `System.ViewAuditLogs` فقط.

### 2.10 Warehouse Operational Metadata
- `contact_phone`/`manager_name`/`notes` اختيارية على `warehouses` مع Validation وRowVersion وAudit وUI.
- لا Stock/Cost/Movements. Supplier Directory وSupplier Products لم يُلمسا.

### 2.11 M5 Purchasing Decision Gate
- لم تُنشأ أي جداول/APIs/Migrations لـPurchase Orders/GRN/Stock/Serials/Transfers/Reservations.
- التناقض موثق في ADR-026 مع تعريف الفرق بين Draft Purchase Request وPO Draft وApproved/Sent PO وGoods Receipt وStock Posting.
- القرار المطلوب من المالك مذكور في القسم 8.

## 3. Migrations
| Module | Migration | المحتوى |
|---|---|---|
| Identity | `M5_IdentityEmailOutbox` | `identity_email_outbox` |
| Identity | `M6_CustomerAddressesAndOutbox` | `customer_addresses` + Outbox في Snapshot |
| Catalog | `M6_ContentMediaLibrary` | `content_media_assets` + `content_media_asset_usages` |
| Storefront | `M6_Wishlist` | `wishlist_items` |
| Storefront | `M7_ContentCms` | `homepage_layouts` + `content_articles` + revisions + faqs + policies |
| Inventory | `M6_WarehouseOperationalMetadata` | `contact_phone`/`manager_name`/`notes` |

**التطبيق:** على QA `rigx_m5_qa_20261108` فقط، بعد Backup:
`.backups/rigx_qa_before_cms_20261109.dump` (PostgreSQL custom dump).
ثم مُنحت صلاحيات `hardware_store_app` على الجداول الجديدة (QA DB لم يرث Default Privileges القديمة).
**Preview (3005/8085) وOwner DB لم تُمسّا.**

## 4. الاختبارات الفعلية
### Backend
- `dotnet build HARDWARE_STORE.slnx` → **Build succeeded، 0 Warning، 0 Error**.
- PostgreSQL Testcontainers:
  - `CustomerAddressPostgreSqlTests` → **2/2**
  - `WishlistPostgreSqlTests` → **3/3**
  - `IdentityEmailOutboxPostgreSqlTests` → **4/4** (تشفير، Retry/Backoff، Dead Letter، Restart مع Key Ring)
  - `SecurityAuditQueryPostgreSqlTests` → **1/1**
  - `ContentCmsPostgreSqlTests` → **3/3**
  - `ContentMediaPostgreSqlTests` → **1/1**
  - `WarehouseDirectoryPostgreSqlTests` → **2/2**

### Frontend
- `npx tsc --noEmit` → **0 أخطاء**.
- `npx vitest run` → **37/37**.
- `npm run build` → **نجح**، بما فيها `/articles`, `/articles/[slug]`, `/faq`, `/policies/[kind]`, `/admin/content/*`, `/admin/media`, `/admin/audit`.

### Playwright (QA على 3006/8091)
- `npx playwright test --workers=1` → **22 passed، 1 skipped**.
- الاختبارات الجديدة `content-commerce.spec.ts` → **4/4**: Wishlist، Addresses، FAQ Publish/Unpublish، وAdmin Content surfaces.
- **إصلاح Test Drift قديم:** Selectors الـCarousel/Mega Menu تغيّرت بعد إعادة تصميم الهيدر؛ اختبار Compare كان يختبر ميزة أُزيلت؛ واختبارات Catalog Admin تستهدف واجهة v2 القديمة.
  - حُدثت Foundation/Storefront/Comparison لتطابق السلوك الحالي.
  - `catalog.spec.ts` أُبقي **skipped برسالة صريحة** لأنه يحتاج إعادة كتابة على Admin v2، وليست الميزة معطلة (admin-m5 يغطي إدارة الكتالوج).

## 5. Screenshots (Before/After)
المجلد: `docs/review/content-expansion-2026-11-09/`
- `home-1440-ar-light.png` / `home-1440-ar-dark.png` / `home-390-ar-light.png`
- `articles-1440-ar-light.png` / `faq-1440-ar-light.png`
- `account-wishlist-1440-ar-light.png` / `account-addresses-1440-ar-light.png`
- `admin-home-content-1440-ar-light.png` / `admin-articles-1440-ar-light.png` / `admin-policies-1440-ar-light.png`
- `admin-media-1440-ar-light.png`
- `admin-audit-1440-ar-light.png` / `admin-audit-390-ar-light.png`

## 6. أهم الملفات
- Backend:
  - `src/Modules/Identity/IdentityEmailOutbox.cs`، `CustomerAddressService.cs`، `PersonClassificationPolicy.cs`، `Authentication/DatabaseTicketStore.cs`
  - `src/Modules/Storefront/{WishlistService,ContentPersistence,ContentServices,StoreSettings,StorefrontPersistenceRegistration}.cs`
  - `src/Modules/Catalog/{ContentMediaPersistence,ContentMediaService,CatalogMediaStorage,CatalogPersistence}.cs`
  - `src/Modules/Auditing/SecurityAuditQueryService.cs`
  - `src/Modules/Inventory/{Warehouse,WarehouseModels,WarehouseDirectoryService}.cs`
  - `src/Host/HardwareStore.Api/{CustomerAddressEndpoints,ContentEndpoints,ContentMediaEndpoints,AuditEndpoints,StorefrontEndpoints,Program}.cs`
- Frontend:
  - `apps/web/src/components/{account-addresses,account-wishlist,wishlist-provider,wishlist-button}.tsx`
  - `apps/web/src/components/{content-articles-admin,content-faq-admin,content-policies-admin,content-media-admin,content-audit-admin,home-content-admin,home-layout-provider,faq-experience}.tsx`
  - `apps/web/src/app/{articles,faq,policies,admin/content,admin/media,admin/audit,account/wishlist,account/addresses}`
  - `apps/web/src/lib/{api-client,server-content,home-layout,i18n}.ts(x)`
- Tests:
  - `tests/HardwareStore.Api.Tests/{CustomerAddressTests,WishlistTests,IdentityEmailOutboxTests,SecurityAuditQueryTests,ContentCmsTests,ContentMediaTests}.cs`
  - `apps/web/tests/e2e/content-commerce.spec.ts`

## 7. المخاطر والقيود
1. **Outbox Enqueue بعد Commit:** فجوة صغيرة عند سقوط العملية بين Commit والـEnqueue؛ Resend متاح.
2. **Media usage بعد حفظ المقال:** فشل تسجيل الاستخدام لا يُفشل الحفظ؛ قد تسمح المكتبة بحذف أصل نادرًا حتى إعادة الحفظ.
3. **Data Protection Key Ring:** يعتمد على Volume؛ Backup/Restore للمفاتيح مطلوب قبل Production وإلا تفقد الجلسات والرسائل المشفّرة.
4. **Unknown email delivery provider:** لا SMTP حقيقي؛ provider الحالي In-Memory/تطويري، فلا ادعاء بتسليم بريد إنتاجي.
5. **Video Library:** لا Storage دائم مناسب؛ لم يُخترع مزود.
6. **Watermark:** لا شعار معتمد.
7. **Policies:** لا نصوص معتمدة؛ الصفحات العامة 404 حتى النشر.
8. **Media storage:** يعتمد على Volume محلي؛ ليس ادعاء Production readiness.
9. **Catalog legacy E2E:** skipped ويحتاج إعادة كتابة.
10. **Rate limit** أثر على تشغيل Playwright المتكرر؛ الاختبارات النهائية شُغّلت بانقطاع مناسب.

## 8. قرارات المالك المطلوبة
1. **M5 Purchasing Gate:** اعتماد Draft-only Purchase Request لا يمس Stock/Cost، أم إبقاء Purchasing معطّلًا حتى Q2.1–Q2.5/Q6.3/Q8.7؟
2. **نصوص السياسات:** Shipping/Returns/Exchanges/Warranty/Terms مسودة عند المالك.
3. **شعار RIGX الرسمي:** لتفعيل Watermark لاحقًا.
4. **مزود بريد إنتاجي + مفاتيحه** قبل أي ادعاء تسليم.
5. **Storage دائم للصور/الفيديو** قبل Production.
6. **سياسة احتفاظ IP القديم** (حذف/إخفاء) إن رغب المالك.

## 9. ما لم يكتمل ولماذا
- Email Change Verification وNewsletter Consent: مؤجلة من الدفعة السابقة.
- Orders/Checkout حقيقي: يحتاج Sales/Inventory وقرارات مالك.
- Video Library: لا Storage دائم معتمد.
- Watermark: لا شعار معتمد.
- نصوص السياسات: لا نصوص معتمدة.
- Legacy Catalog E2E: إعادة كتابة على Admin v2.
- تعريف Resend داخل Outbox: البريد الحالي لا يمنع إعادة إرسال Token منتهي عند طلب Resend جديد (Token جديد يُصدر، والقديم يُبطل عبر Identity).

## 10. الخطوة التالية
مراجعة المالك للنتيجة على QA (`3006` / API `8091`)، ثم:
1. اعتماد بوابة M5 (ADR-026).
2. تزويد نصوص السياسات + الشعار + مزود البريد + Storage دائم عند الرغبة في فتح Production.
3. اعتماد فتح Sales/Inventory/Orders في Milestone لاحق منفصل.
