# HARDWARE STORE — مرجع الاستلام الأساسي

آخر تحديث: 10 نوفمبر 2026  
الجذر الحالي: `C:\Users\Abdalmlek\Downloads\DEV - Copy\HARDWARE STORE`  
الحالة الحالية: **V1.9.1 UI layer مطبقة على جميع المسارات القائمة، Checkout جديد، Variant/Terms/Thumbnail، Contact/Finance Admin. بعض شاشات V1.9.1 المخصصة وGoogle/MFA/Invoices ما زالت مطلوبة.**

## آخر دفعة — Round 4 (12 نوفمبر 2026)
- **Backend:** قفل صف التجميع + التحقق من المكوّنات؛ `FOR UPDATE SKIP LOCKED` للمنتجات؛ Concurrency → 409.
- **Security:** Security headers في API/Next؛ مراجعة Cookie/CSRF/Rate limits/Permissions.
- **Docs:** `docs/canonical/*` هي المرجع؛ `docs/history/INDEX.md` للسجلات.
- **Cleanup:** حذف 14 ملفًا غير مستخدم بعد التحقق.
- **التحقق:** Backend 190/190، tsc 0، Vitest 30/30، Next build، Playwright 6/6.
- **الحالة:** جاهز لمرحلة Design/Experience مستقلة، وليس Production Ready.

## آخر دفعة — Backend UI Readiness (11 نوفمبر 2026)
- **Contract:** `docs/api/BACKEND_UI_CONTRACTS.md` محدث ومطابق للمسارات الفعلية.
- **Traceability:** `docs/api/BACKEND_UI_TRACEABILITY.md`.
- **Gaps:** search suggestions محدود، customer order detail IDOR-safe، admin order detail مع serials حسب الصلاحية.
- **التحقق:** Backend 189/189، Retail 15/15، Catalog 8/8.
- **مرجع:** `RIGX_BACKEND_UI_READINESS_2026-11-11.md`.

## آخر دفعة — Remaining Backend Execution (10 نوفمبر 2026)
- **Allocations:** audit append-only، rowVersion، deactivate/history/preview، 100% cap، permission split.
- **Expenses/Reports:** Direct vs Overhead، تقرير أعمال backend مع تسوية COGS/الخصم/المرتجعات/الشركاء.
- **Contact:** inbox + honeypot + dedupe + rate limit + admin status.
- **Article Video:** ربط/ترتيب، public filtering، منع Unpublish لفيديو مستخدم في مقال منشور.
- **Contracts:** `docs/api/BACKEND_UI_CONTRACTS.md`.
- **التحقق:** Backend 187/187، Retail 13/13، Contact 1/1، ArticleVideo 1/1، Authorization 2/2.
- **مرجع:** `RIGX_REMAINING_BACKEND_2026-11-10.md`.

## آخر دفعة — Next Integrated Execution (10 نوفمبر 2026)
- **Media:** `IMediaReferenceGuard` + Retail guard يمنعان حذف صور تاريخية، ويعرضان المرجع عند المنع.
- **Multi-partner:** `partner_unit_allocations` unit/product/category؛ استحقاقات لكل شريك؛ رفض >100%؛ عكس جميع الشركاء في المرتجع.
- **Auth:** Email OTP (6 أرقام، 5 دقائق، 5 محاولات، Outbox مشفّر) للدخول وإعادة التعيين + UI؛ Password login باقٍ.
- **Video:** YouTube URL validator + nocookie embed + إدارة/نشر؛ MP4/WebM القديمة سليمة.
- **التحقق:** Backend 183/183، Retail 12/12، Media 9/9، Authorization 2/2، OTP 1/1، YouTube 13/13، Playwright 6/6، tsc 0، Vitest 37/37، Next build.
- **مرجع:** `RIGX_NEXT_EXECUTION_2026-11-10.md`.

## آخر دفعة — Overnight Master Execution (10 نوفمبر 2026)
- **Finance:** `Finance.ManagePartnerAgreements` و`Finance.PostPartnerSettlements`؛ `Finance.ViewReports` للقراءة فقط.
- **Data masking:** `Inventory.ViewCosts` و`Inventory.ViewSerials` و`Sales.ViewCustomerContact`؛ التكلفة/المصدر/السيريال/الهاتف/العنوان تُحجب عن غير المصرح، مع `no-store`.
- **Unit snapshot:** كل وحدة تحتفظ بـBrand/Model/Category/Description/ExternalCode/Specifications/PurchaseKind/SourceType/PurchaseDate/Image references + cost/condition/warranty/serial/source/receipt.
- **Retail:** Manual/phone order form؛ Public `/builds` مع إضافة التجميعة للسلة وCheckout حقيقي بأسعار الخادم.
- **SEO:** `NEXT_PUBLIC_SITE_URL` إلزامي في production ويرفض localhost؛ Sitemap paginated للمنتجات والمقالات.
- **Tests:** Full backend **164/164** (أُصلحت 11 توقعات قديمة)؛ Retail 10/10؛ Playwright Builds 1/1 + Cart 4/4 + Retail 1/1؛ tsc 0؛ Vitest 37/37؛ Next build.
- **Migrations:** Retail `M3_UnitSnapshotFields`، Identity `M10_RetailLeastPrivilege`؛ QA فقط بعد Backup.
- **مرجع:** `RIGX_OVERNIGHT_EXECUTION_2026-11-10.md` و`PRODUCT_SCOPE_MATRIX.md`.

## دفعة سابقة — Owner Decisions Applied (9 نوفمبر 2026)
- **Retail Module:** `inventory_units`، `purchase_receipts`، `assemblies`، `assembly_units`، `store_orders`، `order_items`، `order_item_components`، `order_events`، `order_returns`، `partner_agreements`، `partner_earnings`، `partner_adjustments`، `partner_settlements` في Bounded Context واحد.
- **Partner basis:** Snapshot لكل وحدة = إيراد مخصص − خصم − مصروف مباشر − تكلفة فعلية لكل الوحدات؛ Overhead منفصل؛ Loss sharing افتراضيًا معطّل؛ النسبة من الاتفاقية السارية بتاريخ الطلب مع Override للوحدة.
- **Assembly allocation:** افتراضي حسب سعر البيع المستقل، قابل للتعديل قبل النشر، مجموع 100%، ولقطة لكل مكوّن عند البيع.
- **Orders/Completion:** حجز المخزون عند `Confirmed`؛ الإكمال من `Delivered` عبر `Sales.CompleteOrder`؛ دفع `Paid` أو `Waived` بسبب وفاعل ووقت، دون تسمية Waived كـPaid.
- **Returns:** `PendingInspection` ثم تصنيف؛ عكس الاستحقاق؛ المدفوع يصبح رصيدًا سالبًا يُقاصّ من الاستحقاقات المستقبلية؛ لا حذف لسداد أو سجل.
- **Media:** لا حذف تلقائي؛ Volume دائم + `scripts/backup-media.ps1 -Verify`؛ دليل `docs/operations/MEDIA_BACKUP_RESTORE.md`.
- **UI:** `/admin/inventory/stock` (سعر + توزيع مكونات)، `/admin/orders` (Waive/Complete/Returns)، `/admin/partners` (Ledger/Recoverable/Adjustments)، `/account/orders`.
- **Migrations:** Retail `M2_RetailAccounting`، Identity `M9_CompleteOrderPermission`؛ QA فقط بعد Backup.
- **التحقق:** Build 0/0، Retail PostgreSQL 9/9، Retail Playwright 1/1 (يشمل مرتجعًا بعد الصرف)، Cart/Checkout 4/4، Main 8/8 (دفعتان)، tsc=0، Vitest 37/37، Next build، Media backup/restore متحقق.
- **مرجع:** `RIGX_PRACTICAL_RETAIL_CORE_2026-11-09.md` و`BUSINESS_RULES.md` القسم 0.

## دفعة سابقة — Main Product Engineering (9 نوفمبر 2026)
- **Storefront:** Header واحد متصل + Offers، Discovery Rails محسّنة، Home Offers، Account Center/Nav/Avatar.
- **Reviews/Offers:** Product/Store Reviews + Moderation + Aggregate؛ Offers Draft/Published بنافذة وسعر صريح.
- **Purchasing:** `purchase_drafts` من RegisteredSupplier/Marketplace/IndividualSeller/Other؛ Draft/Cancelled فقط بلا Stock/Cost/PO/GRN (ADR-027).
- **Analytics:** Dashboard تشغيلي من مصادر حقيقية.
- **Catalog:** Active Route Exact، لا Active مزدوج.
- **Migrations:** Identity M7، Catalog M7، Purchasing M6، Storefront M8؛ QA فقط بعد Backup.
- **التحقق:** Build 0/0، PostgreSQL جديد 5/5، Main Playwright 6/6، Full Suite 26/27 (1 skipped) + Login المعزول 2/2، tsc=0، Vitest 37/37، Next build.
- **مرجع:** `RIGX_MAIN_ENGINEERING_2026-11-09.md`.


هذا المستند مرجع استمرارية عملي للمطور أو النموذج الذي ينضم للمشروع. يصف التصميم المعتمد والتنفيذ الموجود والقرارات المعلقة؛ لا يعتبر وجود Assembly أو Module دليلًا على اكتمال الوظيفة التجارية. عند التعارض، تكون تعليمات `AGENTS.md` وقرار المالك الأحدث والملفات المصدرية الفعلية أعلى من هذا الملخص.

## سياق التشغيل الحالي المعتمد — 9 نوفمبر 2026

- **Workspace المصرح به:** `C:\Users\Abdalmlek\Downloads\DEV - Copy\HARDWARE STORE`.
- **الأصل المحمي:** `C:\Users\Abdalmlek\Downloads\DEV\HARDWARE STORE` — لا تكتب إليه ولا تنقل إليه ملفات.
- **البيئات:** Preview على `3005` مصدره النسخة الأصلية وAPI على `8085`، ولم تُمس. نسخة العمل على `3006`. استُخدم API جديد على `8091` وقاعدة اختبار معزولة `rigx_m5_qa_20261108` على PostgreSQL Preview `5435` للتحقق فقط؛ لا Migration على Preview/Owner DB.
- **Cart/Checkout:** السلة في `localStorage` (`rigx-cart-v1`)، وCheckout حقيقي يتصل بـ`POST /api/v1/orders` بعد تسجيل الدخول. المرجع من الخادم، ولا حجز مخزون قبل `Confirmed`، والدفع يدوي (`CashOnHandover`) بتأكيد إداري.
- **آخر دفعة:** Expanded Storefront Wave 0–1 + Store Settings + New/Used + Permission UX (9 نوفمبر 2026). راجع `PROGRESS.md` و`NEXT_TASK.md` و`RIGX_EXPANDED_STOREFRONT_WAVE_2026-11-09.md`. لا تُعاد الجولات المكتملة كـ`ACTIVE` بناءً على لقطة Patch أقدم.
- **الاستقلالية:** اتبع `AGENTS.md` و`docs/engineering/AUTONOMOUS_ENGINEERING_STANDARD.md`. عالج الأسباب الجذرية والمشكلات المترابطة داخل الهدف، ولا تتوقف عند حدود ملف/قائمة حرفية. لا تبدأ Milestone تجارية جديدة أو تغيّر سياسات/بيانات حساسة دون اعتماد.
- **الأسرار:** لا تقرأ أو تطبع `.env.preview.local` أو `service.json` أو مفاتيح API. استخدم Credentials/Environment آمنًا فقط، ولا تحفظ المفاتيح في Repository أو `opencode.json` أو Logs.
- **ملف الاستمرارية الخاص بين المالك وChatGPT:** يبقى خارج Repository ولا يُرسل إلى Agents. هذا المستند ومعيار الاستقلالية هما مراجع العمل التقنية للوكيل.

## آخر دفعة — Overnight Consolidated Batch (9 نوفمبر 2026)
- **Identity Admin:** `POST /admin/identity/users` invitation بدون كلمة مرور + Email Verification token، و`POST /users/{id}/password-reset` آمن. Profile endpoint + Account pages.
- **Sessions:** PublicId عشوائي + Device/Browser/LastActivity، List/Revoke/RevokeOthers، IDOR/Audit، وصفحة `/account/sessions`.
- **Media:** Original `.original` + `.webp` + Thumbnail، حماية الأصل، Media Audit/Cleanup. Watermark غير مفعل لعدم وجود شعار رسمي.
- **Cart:** `tryAddCartItem` outcomes، حدود 99/50، Variant غير عشوائي، localStorage failure handling.
- **Migrations:** `M5_SessionDeviceMetadata`، `M5_MediaOriginalPreservation`، وStorefront/ProductCondition؛ QA فقط.
- **المؤجل:** Email Change verification، Newsletter Consent، Checkout/Orders، Stock/PO، Watermark، Admin mobile cards.
- **التقرير:** `RIGX_OVERNIGHT_CONSOLIDATED_BATCH_2026-11-09.md` و`docs/operations/M5_INVENTORY_READINESS.md`.

## دفعة سابقة — Expanded Storefront Wave 0–1 + Store Settings + New/Used + Permission UX (9 نوفمبر 2026)
- **Wave 0:** `PreviewInitializationService` يتحقق من تطابق Host/Port/Database بين Runtime وMigrations قبل DDL (مع السماح باختلاف User/Password). PostgreSQL integration test يستدعي التهيئة كاملة مرتين ويتحقق من Identity/Auditing/Catalog/Purchasing/Inventory/Storefront ومن عدم التكرار.
- **Supplier projection:** `GetAsync`/`GetByIdAsync` تستخدمان Projection إلى `SupplierView` دون تحميل أعمدة البنك.
- **Compare removal:** أزيلت الأزرار/الروابط/Provider/Layer/Route/الاختبارات. لا توجد ميزة Compare نشطة أو يتيمة.
- **Storefront visual:** سطح بطاقة المنتج `var(--ds-surface)` بلا Gradient رمادي، ومنطقة صورة محايدة موحدة، وتوحيد خط النماذج/العناصر عبر `font: inherit`.
- **Store Settings:** Module `Storefront` + `store_settings` + Public/Admin API + Admin UI + WhatsApp floating + Footer social links. الحقول الفارغة تُخفي العنصر ولا تُخترع بيانات.
- **New/Used:** `CatalogProductCondition` + `M5_ProductCondition` + Badge + Admin select + `condition` فلتر في API/UI.
- **Permissions UX:** 39 صلاحية بأسماء/أوصاف/مجموعات AR/EN وبحث، دون تغيير المفاتيح.
- **التحقق:** Build 0/0؛ 43/43 focused؛ tsc=0؛ Vitest 36/36؛ Next build ناجح؛ Playwright 1/1؛ pending-model-changes نظيف لـCatalog/Storefront/Purchasing/Inventory.
- **Migrations:** `M5_StorefrontSettings` و`M5_ProductCondition` + السابقة؛ طُبقت على QA فقط (15 إجمالًا) وليست على Preview/Owner DB.
- **الوثائق:** `docs/operations/STOREFRONT_SETTINGS.md` وADR-016/017/018.

## دفعة سابقة — M3 Hardening + M5 Supplier Product Catalog + Warehouse Directory (8 نوفمبر 2026)
- **Supplier financial separation:** `Purchasing.View` لا يعيد Bank/Account/IBAN. القراءة عبر `/suppliers/{id}/financials` بصلاحية `Purchasing.ViewSupplierFinancials`، والكتابة بـ`Purchasing.ManageSupplierFinancials`، مع Audit داخل نفس المعاملة وFail-closed. لا بيانات بنكية في Audit metadata أو Error messages.
- **Supplier concurrency:** `SupplierUpdateRequest` و`SupplierFinancialWriteRequest` يتطلبان `RowVersion > 0`؛ Create منفصل بلا RowVersion. اختبارات PostgreSQL تثبت Stale 409 والتحديثين المتزامنين.
- **Identity hardening:** `IdentityDelegationPolicy`، حماية آخر Owner، اختبار مباشر لاستبدال دور آخر Owner مع بقاء الأدوار/الجلسات/الإصدارات، اختبار تزامن إزالة آخر صلاحية إدارية، و`Identity.AdministrationRejected` للرفض. الفشل في Audit لا يخفي الرفض الأصلي.
- **Identity architecture:** `IdentityAdministrationService` Facade رقيق؛ `IdentityUserAdministrationService` للمستخدمين، `IdentityRoleAdministrationService` للأدوار، و`IdentityAdministrationSupport` للمعاملة/القفل/Audit/التفويض/التطبيع. العقود الحالية ثابتة.
- **Supplier Product Catalog:** `supplier_products` يربط SupplierProfile بـCatalogProductVariant مع SupplierSku/Notes/Activation. Unique على (Supplier, Variant) و(Supplier, SupplierSku). بلا أسعار/MOQ/Lead times/Stock/PO. API وAdmin UI وAudit وPostgreSQL tests.
- **Warehouse Directory:** Module `Inventory`، جدول `warehouses` (Code/Name/Address/IsActive/RowVersion). بلا Stock/Balances/Movements. API وAdmin UI وAudit.
- **Preview integration:** `PreviewInitializationService` يطبق Migrations لـIdentity/Auditing/Catalog/Purchasing/Inventory بالترتيب قبل Seed/Owner؛ لأن Purchasing Supplier Products يعتمد على Catalog Variants.
- **Theme:** Bootstrap يحترم Cookie `hardware-store-theme` عند غياب localStorage بدل تجاوزها بـOS preference.
- **التحقق:** Build 0/0؛ PostgreSQL focused 19/19؛ PermissionAuthorization 11/11؛ tsc=0؛ Vitest 40/40؛ Next build ناجح؛ Playwright Admin M5 1/1؛ Visual QA Desktop/Mobile وAR/EN وLight/Dark. `has-pending-model-changes` نظيف.
- **Migrations:** `M3_SupplierFinancialPermissions`، `M5_SupplierProductCatalog`، `M5_WarehousePermission`، `M5_WarehouseDirectory` — غير مطبقة على Preview/Owner DB.
- **الوثائق:** `docs/operations/M5_SUPPLIER_PRODUCT_CATALOG.md` و`docs/operations/M5_WAREHOUSE_DIRECTORY.md` و`docs/operations/M5_SUPPLIER_DIRECTORY.md` و`docs/adr/ARCHITECTURE_DECISION_RECORDS.md`.

## آخر دفعة UI — RIGX Interaction, Shopping UX & Global UI Polish (7 نوفمبر 2026)
- **النطاق:** Storefront + Admin داخل `apps/web` فقط. لا تغيير في Backend أو API Contracts أو Database أو Migrations أو Docker أو Dependencies أو `.env*`.
- **Rails:** مكوّن `storefront-rail.tsx` مشترك يحل محل Marquee/Interval المنفصل؛ أسهم في الاتجاهين، Drag/Swipe مع Pointer Capture، Pause عند Hover/Focus/Drag/إخفاء الصفحة، Reduced Motion، منع Click بعد Drag، وإخفاء/تعطيل الأسهم عند الحدود. يستخدمه Category Carousel وBrands.
- **Header/Menus:** Mega Menu بـ`max-height` وScroll داخلي وViewport collision، إغلاق Outside click/Escape/Route change مع إعادة Focus؛ Mobile Drawer مع Focus trap/Escape؛ Theme داخل الدرج وإخفاء Compare/Theme من الهاتف.
- **Admin Sidebar:** إزالة `overflow: hidden`، عمود Sticky بكامل الارتفاع مع Scroll داخلي وScrollbar واضح.
- **Cards/Images:** تثبيت أبعاد البطاقة ومنصة الصورة و`object-fit`، إصلاح مصغّرات Gallery على الهاتف (`w-16 shrink-0`)، تحسين Hit-area، وإبقاء وسم «صورة توضيحية».
- **Theming:** Semantic Tokens، Hero فاتح بورق دافئ، إزالة Gradient المكرر و`background-attachment: fixed` والمساحات الداكنة غير المقصودة، دون Global mapping أعمى لكل `bg-slate-*`.
- **Filters:** Desktop Sidebar + Mobile Drawer، Draft/Apply/Reset/Close، Chips وClear All، API-backed فقط (السعر/الفئات/الماركات/Spec Facets)، URL Sync عبر `router.replace`، وإصلاح Overflow الناتج عن `.rigx-filter-trigger` وBreakpoint النموذج.
- **Cart/Buy Now:** Add to Cart = Toast فقط دون فتح Drawer؛ Buy Now منفصل يستخدم Variant المحدد وينتقل إلى `/checkout` Demo، مع `variantName` في Toast وملخص Checkout.
- **Discovery:** `storefront-discovery.tsx` في منتصف Home يعتمد على أول 5 منتجات من Catalog API (صورة/سعر/مواصفات مسجلة)، دون ادعاء توافق/مخزون ودون PC Builder.
- **الملفات:** 4 جديدة (`storefront-rail`, `storefront-discovery`, `storefront-section-heading`, `cart-toast`) و15 معدلة في `apps/web` (منها `globals.css`, `site-header.tsx`, `catalog-products-experience.tsx`, `cart-provider.tsx`, `checkout-experience.tsx`, `product-detail-gallery.tsx`, `admin-shell.tsx`, `i18n.tsx`, واختبارات e2e)، إضافة إلى `PROGRESS.md` و`NEXT_TASK.md` وهذا الملف والتقرير.
- **التحقق:** `tsc --noEmit` = 0؛ Vitest `28/28`؛ Next Build ناجح بـ22 Route؛ Playwright `14 passed / 3 skipped by design`؛ Visual QA `36` لقطة After مقابل `12` Before مع `issues: 0` و`overflow: 0`. Reduced Motion ثابت وKeyboard Rail يعمل.
- **الحدود:** Admin/Account بصريًا بجلسة مصادقة حقيقية غير متاح؛ الاختبارات الثلاثة المتجاوزة تحتاج PostgreSQL test DB أو Credentials. لا Git repo. صور المنتجات Placeholders من Seed.
- **النسخ:** `.backups/rigx-interaction-ux-2026-11-07-pre/` (93 ملفًا)؛ لقطات `.backups/visual-qa-interaction/before|after/`؛ الأرشيف والتقرير: `HARDWARE_STORE_RIGX_INTERACTION_UX_2026-11-07.zip` و`RIGX_INTERACTION_SHOPPING_UX_REPORT_2026-11-07.md`.

## آخر دفعة UI — RIGX Visual Identity Unification (7 نوفمبر 2026)
- وُحّدت الهوية البصرية عبر Storefront وAdmin: Graphite/Obsidian + Ambient Orange، ورق دافئ في Light وObsidian في Dark، Grid تقني وPointer Glow، مع Semantic Tokens واحدة.
- أُصلح الفوتر من جذره: أُزيل تكرار Gradient من `:root`/`[data-theme]`، وأُعيد بناء `.rigx-site-footer` كطبقة شفافة/متدرجة فوق نفس Ambient Canvas بدل `bg-slate-950` و`#0b0f15 !important`.
- أُزيل مؤشر M4 من واجهة العميل؛ Badge النسخة التجريبية يظهر فقط عند `NEXT_PUBLIC_PREVIEW_MODE=true`.
- الملفات الرئيسية: `apps/web/src/app/globals.css`, `site-chrome.tsx`, `site-header.tsx`, `site-header.test.tsx`, `app-chrome.tsx`, `storefront-home.tsx`, `storefront-category-carousel.tsx`, `catalog-products-experience.tsx`, `catalog-product-view.tsx`, `catalog-product-card.tsx`, `product-detail-gallery.tsx`, `product-actions.tsx`, `storefront-carousel.tsx`, `admin-shell.tsx`, `admin-dashboard.tsx`, `ui.tsx`, `i18n.tsx`, `apps/web/tests/e2e/foundation.spec.ts`.
- التحقق: `tsc --noEmit` = 0، Vitest `8` ملفات / `28` اختبارًا، Next Build ناجح بـ22 Route، Playwright غير المتطفل `12` ناجحًا و`1` متجاوزًا بالتصميم على `http://127.0.0.1:3006`، وVisual QA على 1440×1000 و390×844 في AR/EN وLight/Dark.
- الحدود: Admin المحمي لم يُختبر بصريًا بجلسة مصادق عليها؛ لا تغيير في Backend/Database/API/Dependencies. نسخة 3005 الأصلية لم تُمس.

## الدفعة السابقة — RIGX Motion Direction V2 (6 نوفمبر 2026)
- تمت ترجمة مرجع الحركة إلى مكونات Next.js فعلية دون نقل بيانات الـDemo الافتراضية: Hero من أحدث المنتجات، Brands النشطة، Categories النشطة، وأسعار وصور وأسماء الكتالوج الحقيقية.
- الملفات المتأثرة: `apps/web/src/components/site-header.tsx`, `storefront-home.tsx`, `storefront-category-carousel.tsx` (جديد), `catalog-product-card.tsx`, `site-chrome.tsx`, و`apps/web/src/app/globals.css`.
- التحقق اللاحق ضمن جولة توحيد الهوية أكد Build/Typecheck/Vitest/Playwright؛ التفاصيل أعلاه وفي `PROGRESS.md`.

## 1. خريطة المنتج

### الهدف والبوابات

HARDWARE STORE منصة تجارة إلكترونية وإدارة متجر مستقلة لمكونات وأجهزة الكمبيوتر. النموذج المستهدف يفصل بين ثلاث بوابات:

| البوابة | المستخدم والوظيفة المستهدفة | الحالة الفعلية |
|---|---|---|
| Storefront | تصفح عام للمنتجات والتصنيفات والأسعار والبحث؛ PC Builder؛ حساب العميل؛ طلب وشراء دون Guest Checkout؛ متابعة الطلب والتوصيل/الاستلام والمرتجعات والضمان | Home تجارية وCatalog Storefront وSearch/Filters/Six Sorts/Pagination وProduct/Variant Details وLight/Dark وRTL/LTR منفذة. Cart وCheckout Demo منفذان محليًا في المتصفح فقط، وPC Builder لم يُنفذ بعد. Registration API مغلقة في Production. |
| Admin Panel | إدارة المنتجات والموردين والمشتريات والمخزون والتحويلات والطلبات والفواتير والمالية والعروض والمستخدمين والصلاحيات | Admin Workspace وDashboard وCatalog Admin مكتملون لنطاق M4: taxonomy، المنتجات، Variants، Specifications، الصور والنشر وDraft Preview. إدارة المخزون/المبيعات/المالية والأدوار لم تُنفذ وتظهر Disabled. |
| Developer / Operations Panel | صحة الخدمات، سجلات التشغيل، عمليات صيانة مسموحة، النسخ الاحتياطي والاستعادة ومراجعة العمليات | غير منفذة. Health Checks هي نقاط Foundation وليست لوحة تشغيل. |

### العلاقات بين الوظائف

- الكتالوج والـVariants يعرّفان ما يمكن شراؤه وكيف يختلف كل خيار. التسعير والعروض تحسب الإجمالي وفق القواعد المعتمدة.
- الشراء والاستلام يضيفان مخزونًا قابلًا للتتبع بالدفعة والسيريال عند اشتراطهما. سجل حركات المخزون هو أثر التغيير؛ التحويل بين المخازن يحافظ على العهدة والتسوية والتاريخ.
- العميل الموثق بالبريد هو صاحب الطلب؛ الطلب يمر بالمراجعة والدفع والتجهيز ثم Delivery أو Store Pickup. مسار التوصيل مستقل عن الاستلام من المتجر وعن التحويل بين المخازن.
- الفاتورة تصدر كسجل مالي ثابت للطلب/البيع، والمرتجع يعالج عبر مسار فحص وتسوية/إشعار دائن دون محو التاريخ الأصلي.
- المشتريات والفواتير والمدفوعات والخزائن والتكلفة تتصل محاسبيًا؛ قواعد القيمة والتوقيت التي ما زالت مفتوحة لا تُستنتج من التنفيذ.

## 2. خريطة المعمارية والتنفيذ

### التقنيات والإصدارات المسجلة

- Backend: ASP.NET Core وC#، `net10.0`؛ آخر سجل SDK وقت بناء M3.2c هو .NET SDK `10.0.401`.
- قاعدة البيانات: PostgreSQL 17 للاختبارات المؤقتة والتطوير وفق Compose؛ EF Core `10.0.4` وNpgsql EF provider `10.0.3` بحسب ملفات المشاريع المسجلة.
- Frontend: Next.js `^16.0.0`، React `^19.2.0`، TypeScript `^5.9.0`، Tailwind CSS `^4.1.0` بحسب `package.json`؛ استخدم lockfile الفعلي لتأكيد نسخ التثبيت قبل تعديل الاعتماديات.
- Testing: xUnit، `Microsoft.NET.Test.Sdk 17.14.1`، `xunit 2.9.3`، `xunit.runner.visualstudio 3.1.5`، Testcontainers PostgreSQL `4.16.0`، Vitest وPlaywright كما في manifests الحالية.
- أداة EF المحلية: `dotnet-ef 10.0.4` في `.config/dotnet-tools.json`. لا تستخدم Global Tool.
- التطوير المحلي يستخدم Docker Compose، لكن لا تشغله دون تفويض المهمة. لا توجد قاعدة دائمة مصرح بها للاختبارات.

> الإصدارات أعلاه خلاصة ما سُجل وفُحص في المشروع بتاريخ هذا المستند؛ راجع ملفات `.csproj` و`package.json` وlockfiles قبل أي ترقية أو إضافة.

### Modular Monolith وحدود المسؤوليات

المعمارية المستهدفة Modular Monolith بحدود Modules وعقود واضحة. فعليًا، بعض الوحدات ما زالت `ModuleMarker` فقط؛ وجود المشروع أو الـAssembly لا يعني تنفيذ منطقها التجاري.

- `src/Common/Common.Domain`: أنواع ومفاهيم مشتركة على مستوى المجال.
- `src/Common/Common.Application`: خدمات/عقود تطبيق مشتركة.
- `src/Common/Common.Infrastructure`: بنية الوصول المشترك إلى البيانات، ومنها `Persistence/HardwareStoreDbContext.cs`.
- `src/Modules/<Module>`: حدود المشاريع ومسؤوليات الوحدات. Identity وAuditing وCatalog فيهما تنفيذ فعلي، وبقية الوحدات ما زالت حدودًا.
- `src/Host/HardwareStore.Api`: نقطة دخول ASP.NET Core، تسجيل DI والـmiddleware وEndpoints.
- `apps/web`: تطبيق Next.js App Router، ويحتوي الآن على مسارات هوية العميل، عميل API same-origin، Session Provider، ودعم RTL/LTR فوق الهيكل الأساسي.
- `apps/web/src/app/categories`, `products`, `products/[slug]`, و`admin/catalog`: Storefront والـAdmin Vertical Slice للكتالوج.
- `tests/HardwareStore.Api.Tests`: اختبارات API ووحدة وتكامل، ومنها Fixtures وTestcontainers.

الطبقات المقصودة: Domain للقواعد والكيانات؛ Application لحالات الاستخدام والعقود؛ Infrastructure للتخزين والتكاملات؛ API لحدود HTTP والمصادقة والتكوين؛ Frontend للتفاعل والعرض فقط. التنفيذ الفعلي لا يطابق كل مجلد موثق بعد، ولا ينبغي إعادة هيكلته لمجرد توحيد الشكل.

### نقاط الدخول الموجودة

- الحل: `HARDWARE_STORE.slnx`.
- API Host: `src/Host/HardwareStore.Api/Program.cs`؛ ومن ملفات Identity المهمة `IdentityLoginEndpoints.cs` و`IdentityRegistrationEndpoints.cs`.
- Catalog API: `src/Host/HardwareStore.Api/CatalogEndpoints.cs`، والخدمات/الـDbContext في `src/Modules/Catalog/`.
- Preview Bootstrap: `src/Host/HardwareStore.Api/PreviewInitializationService.cs`، ويعمل فقط عند تفعيله صراحةً في Development.
- تسجيل Identity وPersistence وRBAC داخل مشروع Identity تحت `src/Modules/Identity/`، والجلسات/CSRF ضمن مجلد Authentication.
- Auditing: `src/Modules/Auditing/SecurityAuditPersistence.cs`.
- migrations: Identity ضمن `src/Modules/Identity/Persistence/Migrations/`، وسياق/ترحيلات التدقيق ضمن مشروع Auditing.
- Catalog migration النهائية: `20261105162018_M4_Catalog`؛ Identity permissions migration: `20261105134758_M4_CatalogPermissions`. لم تُطبق على قاعدة دائمة أو Preview في هذه الجولة.
- Frontend: `apps/web/src/app/`؛ صفحات الهوية في `register`, `verify-email`, `resend-verification`, `login`, `forgot-password`, `reset-password`, و`account/security`، مع Proxy لحماية `/account/*`.
- `compose.yaml` و`src/Host/HardwareStore.Api/Dockerfile` للتطوير المحلي. Same-origin routing في Next.js للتطوير؛ Caddy للإنتاج جزء من التصميم ولم يُثبت تكوين نشر فعلي في هذه الجولة.
- Local Preview: `compose.preview.yaml`, `apps/web/Dockerfile.preview`, `scripts/preview.ps1`, و`.env.preview.local` غير المحفوظ في Git. راجع `docs/operations/LOCAL_PREVIEW.md`.

اتصال المتصفح بالـAPI يمر عبر نفس المصدر/المسار الوسيط حسب البيئة؛ لا تتصل الواجهة مباشرة بقاعدة PostgreSQL. API هو حد الوصول للبيانات، ويجب أن يبقى الاتصال الخادمي وقيم الأسرار خارج Browser bundles.

## 3. قواعد العمل الموثقة ومصادرها

المراجع التفصيلية هي [BUSINESS_RULES.md](BUSINESS_RULES.md)، و[DATABASE_DESIGN_ERD.md](docs/database/DATABASE_DESIGN_ERD.md)، و[PROCUREMENT_SALES_INVENTORY.md](docs/workflows/PROCUREMENT_SALES_INVENTORY.md)، و[ORDERS_INVOICES_RETURNS.md](docs/workflows/ORDERS_INVOICES_RETURNS.md)، و[PRODUCT_REQUIREMENTS.md](docs/requirements/PRODUCT_REQUIREMENTS.md). `BUSINESS_RULES.md` موسومة حاليًا كمسودة للمراجعة؛ تعامل مع قرارات المالك المسجلة كمحسومة ومع `OPEN_QUESTIONS.md` كمرجع للقرارات المعلقة، ولا تعتبر كل مثال أو اقتراح في المسودة اعتمادًا تجاريًا.

### المنتجات والـVariants والتسعير

- المنتج له Variants/خيارات ذات هوية مستقلة للمواصفات والمخزون؛ يجب ألا يفترض النظام أن تطابق Product وحده يثبت تطابق Variant.
- الأسعار والإجماليات والخصومات تحسب على الخادم. لا تعتمد على إجمالي يرسله العميل.
- البيع بأقل من التكلفة لا يمنع تلقائيًا: يتطلب تحذيرًا وصلاحية `Sales.OverrideMinimumPrice` وتوثيق مبرر العملية في Audit وفق القاعدة المدونة.
- الضرائب وتقييم المخزون وبعض سياسات الخصم ما زالت قرارات مالك مفتوحة؛ لا تطبق تقديرًا محاسبيًا افتراضيًا.
- Q3.2: التوزيع التناسبي لخصم الباقة خيار مطروح فقط، وليس قاعدة معتمدة أو نتيجة اختبار إلزامية.

### المخزون والدفعات والسيريالات والتحويلات

- حركات المخزون وبطاقات/أرصدة الصنف والدفعة والسيريال يجب أن تبقى مترابطة وقابلة للتدقيق؛ كل تغيير كمية يمر من معاملة وقيد/تحقق تزامن مناسب.
- الحجز لا يخفض الكمية الفعلية قبل حدث الصرف/التنفيذ وفق المسار المعتمد. لا يسمح بصرف يؤدي إلى رصيد سالب.
- السيريال إلزامي فقط للأصناف التي تتطلب تتبعًا تسلسليًا، ويجب أن يرتبط بالصنف والدفعة والعهدة الصحيحة.
- التحويل له مصدر ووجهة وبنود وحركة صرف واستلام وتسوية فروقات؛ لا تحذف التاريخ. هل يمكن شحن بند التحويل الواحد في أكثر من دفعة/سيارة ما زال Q2.5 معلقًا.
- تفاصيل سلامة `inventory_movements` والمراجع المصدرية موجودة في ERD ومذكرة تصميم مستقلة؛ لا تعتبر توصية غير معتمدة تغييرًا في ERD.

### المشتريات والموردون

- المسار المستهدف: أمر شراء → استلام/GRN → تسجيل الدفعات والسيريالات → فاتورة المورد والتكاليف المرتبطة → الدفع/التسوية.
- تواريخ الطلب والاستلام والفاتورة والاستحقاق منفصلة. كيفية تقييم التكلفة وربط الشحن تُنفذ فقط وفق القرارات المعتمدة.
- المورد والعميل والموظف قد يكونون علاقات للشخص نفسه، مع فصل صفات الشخص التجارية عن أدواره الأمنية.

### الطلب والدفع والتوصيل والاستلام

- التصفح العام مسموح، لكن Checkout يتطلب حسابًا موثقًا، ولا يوجد Guest Checkout. Backend يستخرج هوية العميل من الجلسة ولا يثق في Customer ID يرسله العميل.
- حالات الطلب والدفع والتنفيذ مستقلة. الدفع اليدوي لا يصبح مدفوعًا بمجرد رفع صورة إيصال؛ يلزم تحقق الجهة المخولة، ما لم يعتمد تكامل موثوق.
- توصيل V1 يدوي باسم شركة الشحن، وTracking اختياري، والموظف يسجل الحالة ووقت التغيير وصاحبه؛ لا API شحن أو GPS أو أسطول أو Webhooks ضمن V1.
- Store Pickup منفصل عن Delivery وعن تحويل مخزون المستودعات.
- Q7.2: توقيت صرف المخزون وإصدار الفاتورة في Delivery ما زال معلقًا؛ لا تحسمه في الكود.

### الفواتير والمرتجعات والضمان

- الفاتورة الصادرة سجل تاريخي غير قابل للتعديل الصامت؛ الإجماليات محسوبة. تصحيح الأثر يتم بمستند عكسي/إشعار دائن حسب القواعد.
- المرتجع يمر بالفحص والحجر أولًا ثم قرار إعادة التخزين/الخردة/التسوية. لا تحول حالة مادية إلى حركة مالية/مخزنية دون مستندها المصدر.
- توزيع مرتجعات الطلب على مخزن واحد أو أكثر ما زال Q3.3 معلقًا. معالجة خصم الباقة الجزئي Q3.2 معلقة.
- الضمان يرتبط بالمنتج/السيريال والفاتورة وفق التصميم؛ تفاصيل السياسة التجارية تُحسم قبل الشريحة التي تعتمد عليها.

### الحسابات والخزائن والمالية

- كل دفعة/سحب/مصروف يجب أن يترك أثرًا تاريخيًا قابلًا للمطابقة. الرصيد مشتق من حركة موثقة، وليس حقلًا حرًا.
- لا تحذف حركة مالية لتصحيحها؛ استخدم العكس/التسوية حسب سياسة معتمدة.
- قواعد تقييم التكلفة والضرائب ورسوم الشحن والفجوات في ترقيم الفواتير وتوقيت الدفع لها Qs مفتوحة موضحة أدناه.

### PC Builder

- التوافق يعتمد بيانات القطع: Socket وجيل/فتحات الذاكرة وتوافقها؛ تقدير Wattage إرشادي. المواصفات غير المعروفة تظهر كتحذير لا كضمان.
- تفاصيل فوترة/ضمان التجميعة Q3.1 لم تُحسم؛ لا تنشئ Kit/SKU تجاريًا استنتاجًا.

### الهوية والأشخاص والصلاحيات

- `Person` منفصل عن `UserAccount`: شخص واحد يرتبط بحد أقصى بحساب واحد؛ UUID للمفتاح الداخلي، و`PersonCode` بصيغة `PER-YYYYMM-XXXXX` مع عداد شهري ذري وتوقيت `Africa/Cairo`.
- `PermissionCatalog` في الكود هو المصدر الوحيد لتعريف الصلاحيات الـ39 الحالية؛ جدول Permissions انعكاس له. لا تضف صلاحيات خارج الكتالوج ولا تتجاوز Authorization بامتياز SuperAdmin.
- Customer يعين من الخادم فقط. حساب التسجيل المعلق لا يملك كلمة مرور ولا جلسة؛ يختار صاحب البريد كلمة المرور بعد تأكيده في الشريحة المخططة.
- M3.2c Registration API متصلة الآن بواجهة Next.js ضمن M3.2e، لكن API وصفحة التسجيل غير مفتوحتين للجمهور في Production.

## 4. حالة التنفيذ حسب Milestone

| المرحلة | الحالة الفعلية |
|---|---|
| M1 — الوثائق والتصميم | التوثيق الأساسي موجود؛ سجل `OPEN_QUESTIONS.md` يحتوي قرارات تحتاج مالكًا، لذا مراجعة القرارات مستمرة. |
| M2 — Foundation | منفذة ومقبولة؛ API/Web/PostgreSQL وبيئة التشغيل المحلية موثقة. |
| M3.1 — Persistence للهوية وRBAC | منفذة ومعتمدة؛ Person/Account/Role/Permission persistence وPersonCode. |
| M3.2a — Sessions/CSRF/Audit foundation | منفذة ومعتمدة وظيفيًا. الأرقام `17/17` و`7/7` وردت في ملخصات سابقة، لكنها لا تظهر بتفصيل قابل للتتبع داخل `PROGRESS.md` الحالي؛ لا تُستخدم كدليل جديد. تحقق التشغيل الإنتاجي من Key Ring/الشهادة/النسخ مؤجل لتجهيز النشر. |
| M3.2b — Password/Login/Lockout | منفذة ومعتمدة؛ نتائج Login PostgreSQL `9/9` واختبار Migration `1/1` وPasswordHashing `4/4` في دوراتها المسجلة. |
| M3.2c — Customer Registration API | التنفيذ واختبارات PostgreSQL مكتملة حسب التسليم: `12/12` (3 Preflight + 6 Registration + 1 Migration + 2 Login security). آخر Build تشخيصي `Exit Code 0`؛ التحذيران `NU1900`, `xUnit2031`. ليست واجهة أمامية ولا فتحًا للإنتاج. |
| M3.2d — Email Confirmation/Recovery | مكتملة ومقبولة تقنيًا بعد PostgreSQL lifecycle `14/14` وTestcontainers الكامل `47/47`: Email Verification/Password Set، Resend، Password Reset/Change، Logout Current/All، وAudit ذري. لا Migration جديدة. |
| M3.2e — Frontend Registration/Verification/Integration | منفذة في Next.js وتتصل بالـAPI الحقيقي. نجحت Vitest `6/6`، Playwright `2/2`، وNext production build. فتح التسجيل في Production ينتظر إعداد البريد/Proxy واعتماد المالك. |
| M3.2f — First Owner Bootstrap | غير منفذة؛ تصميم Bootstrap المحلي والأدوار/الصلاحيات الدقيقة لا تنفذ قبل موافقة النطاق المطلوبة. |
| M3.3 — إدارة الهوية وRBAC التشغيلية | غير منفذة؛ لا توجد APIs/واجهات تشغيلية لإدارة المستخدمين والأدوار والصلاحيات. |
| M4 — Catalog & Media | مكتملة وظيفيًا وبصريًا على Local Preview: Storefront Home، Search/Filters/Specifications/Six Sorts/Pagination، Variant Selector، Skia image processing، Media audit، Admin Catalog، Light/Dark، RTL/LTR، Preview shortcut، Demo سلة/Checkout محلي، وتوحيد هوية RIGX. واجهة الإدارة تعتمد على الصلاحيات الفعلية من `/identity/me`. التحقق الحالي: Vitest `28/28` وNext build ناجح بـ22 Route وPlaywright غير المتطفل `12` ناجحًا و`1` متجاوزًا بالتصميم. تنتظر اعتماد المالك. |
| M5 — Inventory/Serials/Procurement/Transfers | غير منفذة تجاريًا. |
| M6 — PC Builder/Templates | غير منفذة تجاريًا. |
| M7 — Orders/Fulfillment | غير منفذة تجاريًا؛ قواعد Delivery/Pickup موثقة. |
| M8 — Invoicing/POS | غير منفذة تجاريًا. |
| M9 — Financials/Treasury | غير منفذة تجاريًا. |
| M10 — Promotions/Coupons | غير منفذة تجاريًا. |
| M11 — Support/Ops/Backup | غير منفذة؛ ضوابطها موثقة. |
| M12 — Final Quality Gates/Delivery | لم تبدأ. |

في جولة M3.2e شُغّلت الاختبارات المحلية غير الموسومة `RequiresDocker`: `30/30`، وبوابة M3.2d على PostgreSQL `14/14`، ومجموعة PostgreSQL/Testcontainers الكاملة `47/47`. نجح Playwright `2/2` وVitest `6/6` وNext production build. استُخدمت قواعد مؤقتة فقط، ثم أزيلت موارد الاختبار.

## 5. القرارات المفتوحة

الحالة النهائية وتفاصيل الخيارات في [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md). لا تحسم أي سؤال نيابة عن عبدالملك.

| السؤال | سبب أهميته وموعد الحسم |
|---|---|
| Q1.1 تقييم المخزون وCOGS/الربح | يغيّر التكلفة والأرباح المحاسبية؛ قبل M8/M9. |
| Q1.2 معالجة الضرائب | يغيّر الأسعار والفواتير والالتزام المالي؛ قبل M8. |
| Q1.3 دورة الورديات والخزائن | يحدد ضبط النقدية؛ يمكن تأجيله بأمان حتى شريحة M9 حسب السجل. |
| Q1.4 ترقيم الفواتير والفجوات | يؤثر في المستندات المالية؛ قبل M8. |
| Q1.5 المعاملة الضريبية للشحن | تؤثر في إجمالي الفاتورة؛ قبل M8. |
| Q2.1 إدخال شرط السيريال للأصناف الموجودة | يحكم سلامة تحويل المخزون الحالي؛ قبل M5. |
| Q2.2 مدة حجز المخزون الافتراضية | تؤثر في إتاحة المخزون؛ قبل M5. |
| Q2.3 سياسة تمديد الحجز | تؤثر في العدالة وتحرير المخزون؛ قبل M5. |
| Q2.4 حدود/صلاحيات تسوية فروق التحويل | قرار مالي وتشغيلي؛ قبل M5. حد 2,000 جنيه وصلاحياته غير معتمدين. |
| Q2.5 صرف بند التحويل مرة أم على شحنات متعددة | يحدد بنية أحداث الصرف والجرد؛ قبل M5. |
| Q3.1 فوترة وضمان التجميع/الطقم | يحدد مسؤولية القطع والضمان؛ قبل M6/M8. |
| Q3.2 استرداد خصم الحزمة جزئيًا | يحدد مبلغ الاسترداد؛ قبل M8. التوزيع التناسبي خيار فقط. |
| Q3.3 إيداع المرتجع بمخزن واحد أو أكثر | يؤثر في حركة المرتجع والتتبع؛ قبل M7. |
| Q4.1 ترخيص/توزيع نسخ مستقلة | معمارية مستقبلية، يمكن تأجيلها لما بعد V1. |
| Q5.1 بوابة الدفع الأولى | تحدد تكامل الدفع والتحصيل؛ قبل M7. |
| Q5.2 ضرورة/مزود SMS | قرار تكلفة واتصال؛ مؤجل حسب سجل الأسئلة. |
| Q5.3 توقيت التحصيل مقارنة بموافقة الطلب | يؤثر في دورة الطلب والمال؛ قبل M7. |
| Q7.1 مستهدف RPO/WAL والنسخ | يحدد مخاطر فقد البيانات وكلفة التشغيل؛ قبل M11. |
| Q7.2 توقيت صرف المخزون/الفاتورة لـDelivery | يؤثر في المخزون والإيراد والفواتير؛ قبل M7/M8. |

Q6.1 PersonCode محسوم ومنفذ، وقرار إعداد المنطقة الزمنية التجارية `Africa/Cairo` معتمد؛ لا تعِد فتحهما دون تعارض تقني مثبت.

## 6. خارطة الطريق M1–M12

ترتيب مراحل المنتج في `ROADMAP.md` هو المرجع. الارتباطات العامة:

1. M1 توحيد المتطلبات والقرارات والوثائق؛ يظل مرجعًا مستمرًا.
2. M2 تجهيز البنية والاختبارات والتشغيل، وقد أُغلقت.
3. M3 الهوية والأشخاص وRBAC؛ M3.2d وM3.2e مكتملتان تقنيًا. M3.2f Bootstrap وما بعده لم يبدآ.
4. M4 الكتالوج والوسائط؛ كودها المحلي مكتمل ويحتاج بوابة PostgreSQL/Playwright وتشغيل Preview ومراجعة المالك.
5. M5 المخزون والسيريالات والمشتريات والتحويلات؛ تعتمد على الكتالوج وحسم Q2.*.
6. M6 قوالب التجميع وPC Builder؛ تعتمد على مواصفات المنتجات وحسم Q3.1.
7. M7 الطلبات والتنفيذ والتسليم؛ تعتمد على الحساب والكتالوج والمخزون، وحسم Q3.3 وQ5.* وQ7.2 قبل الجزء المتأثر.
8. M8 الفواتير وPOS؛ تعتمد على الطلب/المخزون والسياسات الضريبية والتكلفة وترقيم الفواتير.
9. M9 المالية والخزائن والمدفوعات؛ تعتمد على قرارات المحاسبة وتدفق الفواتير والمدفوعات.
10. M10 العروض والكوبونات؛ تعتمد على الكتالوج والتسعير وسياسات الخصم.
11. M11 الدعم وOps والنسخ والاستعادة؛ يعتمد على قرارات التشغيل وRPO.
12. M12 بوابات الجودة النهائية والتسليم؛ بعد اكتمال الشرائح المعتمدة.

كل مرحلة/شريحة تحتاج نطاقًا معتمدًا واختبارات مناسبة وتسليمًا قبل الانتقال؛ ترتيبها لا يمثل تصريح تنفيذ.

## 7. الأمن والتشغيل

- Authentication V1: ASP.NET Core Cookie Authentication وجلسات خادمية عبر `ITicketStore`، وليس JWT/Refresh Tokens. Session key عشوائي 32-byte، يخزن Hash فقط، وTicket محمية بـData Protection. يستخدم `TimeProvider` UTC لصلاحية الجلسات والرموز في V1 أحادي الخادم، مع إلزام مزامنة ساعة الخادم.
- Password Hashing V1: Microsoft `PasswordHasher<UserAccount>` بوضع IdentityV3 وخوارزمية PBKDF2-HMAC-SHA512 وبعدد 220,000 iteration، خلف `IPasswordHashingService`. لا Argon2 أو مكتبة تجزئة خارجية في M3.2d.
- الجلسات تدعم Idle/Absolute Expiry والإلغاء المنطقي والتحديث الآمن عند التزامن. Cookie `HttpOnly` و`Secure` في الإنتاج و`SameSite=Lax` مع CSRF فعلي.
- RBAC يتحقق من صلاحيات المستخدم المستمدة من DB عبر UserAccount→UserRoles→Roles→RolePermissions→Permissions. Catalog هو المرجع؛ يوجد account `authorization_version` وglobal `rbac_revision`. لا SuperAdmin bypass.
- Fallback Authorization تحمي Endpoint غير المصنف افتراضيًا؛ Endpoints العامة فقط تستثنى صراحةً. المتجر العام مقصود، لكن Checkout والبيانات الشخصية والإدارة لها متطلبات مصادقة/صلاحية.
- Rate Limiting المعتمد داخل الذاكرة يقتصر على API instance واحدة في V1؛ التسجيل 10/IP/ساعة، Login 5/IP/دقيقة، تأكيد البريد 20/IP/10 دقائق، إعادة الإرسال 10/IP/ساعة مع 60 ثانية و5/24 ساعة للحساب، طلب Reset 10/IP/ساعة مع 60 ثانية و5/24 ساعة للحساب، واستهلاك Reset 20/IP/10 دقائق. لا تفتح التسجيل قبل تفعيل حد الإنتاج واستيفاء بوابة الفتح.
- Data Protection: إعداد Development منفصل؛ Production يتطلب Key Ring دائمًا مشفرًا و`ApplicationName` ثابتًا لكل بيئة. تحقق المسار والشهادة والنسخ/الاستعادة فعليًا مؤجل لتجهيز النشر، ولا تنقل أسرارًا أو Keys إلى المستودع.
- Forwarded Headers لا تثق إلا في KnownProxies/KnownNetworks المحددة؛ Caddy هو Public Proxy في تصميم النشر، ولم يُثبت إعداد إنتاج حي هنا.
- Audit سجل Append-only لأحداث الأمن الحساسة؛ ممنوع تسجيل Password/Hash/Cookie/Token أو روابط تحقق كاملة. عمليات M3.2d الحساسة تستخدم Connection وTransaction مشتركة عبر `ISecurityAuditTransactionWriter`. M3.2d لا يحتاج Migration جديدة.
- لا Docker Compose أو Testcontainers أو قاعدة دائمة دون تصريح متعلق بالمهمة. اختبارات PostgreSQL المعتمدة تستخدم Testcontainers المؤقت فقط. لا حذف عام لموارد Docker.
- الأسرار داخل `.env` لا في Repository؛ `.gitignore` الحالي يستثني `.env` و`.env.*` ويستثني `.env.example`. Git غير مهيأ؛ لا تهيئه دون طلب.
- نسخ PostgreSQL والوسائط وسياسات الاستعادة موثقة، لكن Backup/Restore تشغيلي غير منفذ بعد. لا تدّعِ استعادة مجربة دون اختبار فعلي.

## 8. تعليمات المشروع والمهارات

اقرأ `AGENTS.md` أولًا في كل محادثة/مهمة. Skills المحلية السبع تحت `.agents/skills/`:

| Skill | متى تستخدمها |
|---|---|
| [`hardware-store-docs-audit`](.agents/skills/hardware-store-docs-audit/SKILL.md) | تدقيق مصدر الحقيقة والتعارضات والقرارات المعتمدة/المعلقة. |
| [`database-integrity`](.agents/skills/database-integrity/SKILL.md) | تصميم أو مراجعة PostgreSQL/EF Core والعلاقات والقيود والتزامن. |
| [`controlled-implementation`](.agents/skills/controlled-implementation/SKILL.md) | تنفيذ تغيير محدد معتمد مع نسخة رجوع وضبط النطاق. |
| [`engineering-handoff`](.agents/skills/engineering-handoff/SKILL.md) | توثيق الملفات والاختبارات والحالة والخطوة التالية. |
| [`security-and-data-protection`](.agents/skills/security-and-data-protection/SKILL.md) | مراجعة الهوية والـAPI والجلسات والملفات والأسرار والنسخ. تم توضيحها لتراجع Cookie Authentication و`ITicketStore` الفعليين؛ لا تفترض JWT. |
| [`hardware-store-design-system`](.agents/skills/hardware-store-design-system/SKILL.md) | Tokens والخطوط والمسافات وRTL/LTR والثيمات والحركة وقواعد Storefront/Admin البصرية. |
| [`hardware-store-visual-qa`](.agents/skills/hardware-store-visual-qa/SKILL.md) | Playwright Screenshots وResponsive وAccessibility وConsole وVisual QA. |

وجود Skill لا يثبت أمان المنتج. استخدم الإجراء الملائم للمهمة ولا تدّعِ قراءة أو تشغيل تعليمات لم تُستخدم فعلًا.

## 9. اتفاق العمل

- عبدالملك صاحب المنتج والقرار النهائي، بما في ذلك التجاري والمالي والقانوني وفتح التسجيل.
- ChatGPT في المحادثة الأخرى هو المهندس المعماري والمراجع المستقل.
- **سجل تاريخي:** DeepSeek كان المنفذ الأساسي بعد الانتقال إلى محادثة Codex. الإعداد الحالي هو OpenCode، والموديل حسب جلسة التشغيل الفعلية؛ راجع سياق التشغيل أعلى الملف.
- لا يوجد منفذان يكتبان في Workspace نفسه بالتوازي. هذه المحادثة انتهت بتحديث هذا التسليم؛ أي كتابة لاحقة تبدأ بعد استلام المحادثة الجديدة.
- لا قرارات مالية/تجارية بالتخمين، ولا انتقال لمرحلة لاحقة دون اعتماد نطاقها الصريح.
- المالك لا يتحمل Code Review؛ المراجع المستقل يراجع التغييرات قبل اعتمادها.
- لا تعدّل أو تعِد تنفيذ شيء مكتمل من غير تغيير مرتبط أو دليل على خلل. كل Build/Test يُذكر بنتيجته الفعلية وسجله؛ نتائج سابقة لا تعني تشغيلها في الجولة الحالية.

## 10. حالة تنفيذ M3.2d وM3.2e

اعتمد المالك النطاق ونُفذ في API فقط لتأكيد البريد واستهلاك الرمز وإعادة الإرسال واستعادة كلمة المرور وإدارة الجلسات. التفصيل الملزم في `docs/api/API_DESIGN_SECURITY.md`، والقواعد الأساسية:

1. **تأكيد البريد:** يأخذ العميل الرمز من URL fragment ويرسله مع كلمة المرور الجديدة في POST محمي بـCSRF؛ لا تستهلك GET الرمز. يجرى فحص مبدئي خفيف قبل PBKDF2، ثم يعاد التحقق النهائي بعد قفل الحساب والرمز داخل Transaction. استهلاك الرمز وتأكيد البريد وإعداد Hash وزيادة `authorization_version` وAudit ذرية، ولا Session أو Cookie بعد التأكيد.
2. **إعادة الإرسال:** استجابة عامة غير كاشفة لوجود الحساب، تحقق من حد 60 ثانية وبحد أقصى 5 خلال 24 ساعة لكل حساب وحد IP المعتمد 10/ساعة. إصدار البديل يبطل السابق؛ خزّن Hash فقط وأرسل بعد Commit عبر Email Provider abstraction الحالي. فشل الإرسال لا يمحو الحالة المحفوظة ويظل المسار قابلًا لإعادة المحاولة.
3. **Password Reset:** طلب عام لا يكشف الحساب؛ رمز مستقل الغرض صالح 30 دقيقة، أحادي الاستخدام، Hash فقط. التغيير الذري لكلمة المرور واستهلاك الرمز وتصفير عدادات الفشل والقفل وزيادة `authorization_version` وإبطال كل جلسات الحساب.
4. **Password Change وLogout:** تنفيذ تغيير كلمة المرور من جلسة مصادق عليها، وإلغاء جميع الجلسات وتغيير Cookie. تنفيذ `DELETE /api/v1/identity/sessions/current` لإلغاء الجلسة الحالية و`DELETE /api/v1/identity/sessions` لإلغاء كل جلسات الحساب الحالي، مع CSRF؛ لا تترك Cookie صالحة بعد الإلغاء.
5. **التزامن والإبطال:** تحديث مشروط/معاملة تمنع نجاح استهلاك الرمز مرتين عند طلبين متزامنين. بعد كلمة المرور الجديدة لا تقبل الجلسات القديمة. لا تعيد تفعيل الحساب أو تضف أدوارًا خلال المسار.
6. **Rate Limiting وAudit:** تأكيد البريد 20/IP/10 دقائق، إعادة الإرسال 10/IP/ساعة مع 60 ثانية و5/24 ساعة للحساب، طلب Reset 10/IP/ساعة مع 60 ثانية و5/24 ساعة للحساب، واستهلاك Reset 20/IP/10 دقائق. يتطلب V1 نسخة API واحدة للـin-memory limiter. سجل أحداث أمنية في Transaction العمليات الحساسة دون Password/Hash/Token/رابط كامل/بيانات اعتماد.
7. **الاختبارات:** الحالات الصالحة والمنتهية والمستخدمة والملغاة والغرض الخاطئ، إعادة إرسال وإبطال السابق، محاولات متزامنة، استجابات عامة تمنع تعداد الحسابات، CSRF وحدود IP/الحساب، فشل البريد بعد Commit، منع Session قبل التحقق، وإبطال الجلسات بعد Reset/Change نُفذت ضمن `14/14` لبوابة M3.2d. مجموعة PostgreSQL الكاملة سجلت `47/47`.
8. **M3.2e:** الصفحات الحقيقية تستخدم عميل API وCSRF وCookies وتسأل `/me` عن الجلسة الحالية. Fragment token لا يدخل Query String. تغيير كلمة المرور أغلق جلسة متصفح ثانية في Playwright، وLogout Current/All ظهرا بنتيجة صحيحة. Playwright `2/2` وVitest `6/6` وNext build ناجحة.
9. **مزود البريد:** التطوير والاختبار يستخدمان In-Memory Provider. تكامل SMTP الإنتاجي يدعم إعدادات Host/Port/TLS/From/Auth/Timeout خلف `IEmailProvider`، ويفشل بأمان عند نقص الإعدادات. اختيار المزود الحقيقي وأسراره قرار تشغيل للمالك.
10. **حدود الشريحة:** لا Migration جديدة ولا تغيير مخطط. لا Caddy في Compose ولا production API rewrite؛ same-origin وProxy وTLS وفتح Registration العام تظل تجهيز نشر واعتمادًا منفصلًا.

بوابة Production منفصلة عن اكتمال الشريحة: اجتازت M3.2d وM3.2e الاختبارات المحلية والمتصلة، لكن التسجيل يظل مغلقًا حتى تجهيز البريد والـProxy والتحقق التشغيلي وموافقة المالك الصريحة.

## 10.1 حالة M4 Catalog & Media

- النطاق المنفذ: Categories هرمية، Brands، Products، Variants، Product/Variant Specifications، Product Images، Public Storefront APIs، وAdmin APIs.
- الحالات: `Draft` و`Published`. واجهات Storefront تعيد المنتجات `Published` فقط ولا تعرض منتجًا تحت Category/Brand غير نشط أو تحت سلسلة تصنيف بها أب غير نشط.
- الصلاحيات الجديدة: `Catalog.View`, `Catalog.ManageTaxonomy`, `Catalog.ManageProducts`, `Catalog.ManageMedia`. لا يوجد SuperAdmin bypass، ولوحة الإدارة تعتمد على Backend للتحقق النهائي.
- `GET /identity/me` يعيد `permissions` الفعالة والمكررة من قاعدة البيانات؛ تستخدمها واجهات Storefront/Admin/Preview لإظهار أدوات الكتالوج فقط. اختبار PostgreSQL lifecycle يثبت العقد، لكن تشغيله في هذه الجولة ينتظر موافقة Docker.
- التصنيفات تمنع الأب الذاتي وتتحقق من الدورات داخل معاملة Serializable. Products تطبق Optimistic Concurrency عبر `row_version`، وقيود SKU/Barcode/Slug موجودة.
- Media يُفك ويعاد ترميزه على الخادم باستخدام SkiaSharp Stable `4.153.2` إلى WebP، مع تطبيق اتجاه EXIF وتوليد Thumbnail وحدود للحجم/الأبعاد/عدد البكسلات وحماية المسار. Metadata لا يعاد نسخها. مرجع SkiaSharp المُدار داخل Catalog، و`SkiaSharp.NativeAssets.Linux.NoDependencies` مرجع مباشر داخل API؛ Restore وBuild ناجحان و`libSkiaSharp.so` موجود في مخرجات API.
- Cleanup اليتامى يستخدم Audit كقائمة مرشحات فقط، ثم يعيد الاستعلام من `product_images` و`categories` و`brands` قبل حذف كل ملف. المهلة 30 دقيقة وتحذير عدم تشغيل Cleanup متزامن يقللان نافذة سباق DB/Filesystem غير الذرية.
- تمت إضافة عقد API عامة للتصنيفات/الماركات/المنتجات/التفاصيل/Media، وعقود Admin للإنشاء والتعديل والنشر والوسائط.
- Migration Identity للصلاحيات وMigration Catalog مولدة ومفحوصة SQL offline فقط. لا تطبيق دائم أو مؤقت في هذه الجولة.
- Storefront Home تعتمد Product/Category/Brand APIs الفعلية؛ Toolbar واحدة تجمع Search/Category/Brand/Min-Max/Sort، وستة Sort Modes مدعومة في Backend. Sidebar تعرض Specification Facets الحقيقية فقط، وPagination تحفظ الفلاتر.
- Admin Workspace مستقل في `/admin`: Dashboard بمؤشرات Catalog حقيقية، Sidebar/Desktop، Mobile Drawer مع Focus Trap/Escape، Breadcrumbs، Language/Theme/Account/Logout، وروابط مبنية على صلاحيات `Catalog.*` الفعلية. الوحدات غير المنفذة معروضة كغير متاحة مع Milestone/قرار المالك.
- أضيفت Carousels أصلية للتصنيفات والمنتجات في Home باستخدام Scroll Snap بلا Dependency؛ تدعم RTL/LTR، الأزرار، Arrow Keys، Home/End، اللمس، Reduced Motion، وتعطّل التنقل عند حدود المجموعة.
- Product Summary يعيد حتى ثلاث مواصفات Product-level فعلية، بينما Variant-specific specs تظهر فقط بعد اختيار Variant في صفحة التفاصيل.
- Login لا يضع Username/Password في URL ولا يستخدم `name` للحقول الحساسة. PasswordField مشترك بأيقونات Eye/Eye-off وحفظ التركيز، وAR/EN وLight/Dark محفوظان مع تغيير `lang`/`dir` و`data-theme`.
- Bootstrap 500 أُصلح بعد اكتشاف استخدام Category display name بدل Slug؛ أُعيد تشغيله على Preview الحالي وأنتج 11 Category و10 Brands و14 Products دون حذف أو تكرار. أُصلح أيضًا رفض Category جذرية جديد بسبب مقارنة `ParentId=null` مع `id=null`.
- Preview Owner Recovery أصبح Idempotent: يعيد مزامنة Login Identifiers وPerson والحساب، يصفّر Lockout/Failure state، يعيد Hash من إعدادات `.env.preview.local`، يزيد `AuthorizationVersion`، يلغي الجلسات القديمة ويعيد `Catalog.*` داخل Transaction. لا يحوّل حسابًا غير Owner إلى Owner.
- Playwright على Preview: Carousel/Foundation `2/2`، Storefront/Search/Variant/Login `3/3`، Admin Catalog/Media/Publication `1/1`، Customer Identity `1/1`؛ الإجمالي `7/7` عبر مجموعات منفصلة بسبب حد Login `5/IP/min`. Vitest `9/9`، Backend non-Docker `51/51`، Catalog PostgreSQL `4/4`.
- Visual QA على `360/390/430/768/1024/1280/1440/1920` لم يجد Horizontal Overflow بعد إصلاح شريط تصنيفات Desktop. Console errors للزائر تقتصر على `401` المتوقعة من `/identity/me`. Carousels وReduced Motion اختُبرا فعليًا.
- Local Preview مستقل ويعمل على `http://127.0.0.1:3005`، ويحتوي Owner محليًا وShortcut على Desktop. `open-preview.ps1` يتحقق من Health ويسأل قبل التشغيل، و`create-preview-shortcut.ps1 -Remove` يزيل الاختصار فقط.
- المتبقي قبل Production: Media Cleanup concurrency test، SMTP/Outbox، Caddy/HTTPS/Trusted Proxies، Data Protection backup/restore، وقرار `Q6.2/Q6.3`. لا Migration جديدة ولا تغيير Schema في هذه الجولة.

## 10.2 حالة RIGX V2 Storefront

- الهوية الظاهرة `RIGX` مؤقتة حتى اعتماد trademark/domain. أسماء .NET Namespaces وقاعدة البيانات لم تتغير.
- الاتجاه البصري Graphite/Warm Orange مع Light/Dark وAR/EN، وWhatsApp Green محجوز للـCTA.
- Home/Cards/Product Details/Gallery/Share/Related Products تعمل من APIs الحالية. WhatsApp CTA/float يظهر عند إعداد الرقم من Admin Store Settings أو `NEXT_PUBLIC_STORE_WHATSAPP_NUMBER`.
- Admin Categories/Brands جدول + Drawer + Search/Status/Counts/Actions. Products لها Filters/Pagination/Actions، وEditor مقسم إلى Sections. لم تُضف APIs أو Migration.
- Product Comparison أُزيلت بالكامل في 9 نوفمبر 2026: لا Provider/Layer/Route/Components/Tests. لا يوجد مسار `/compare`.
- Product Details أصبح Server-rendered للـSEO مع Metadata وOpen Graph وJSON-LD. أضيفت `sitemap.xml` و`robots.txt`.
- Product Condition (New/Used) أُضيف في 9 نوفمبر 2026 مع Badge/Filter/Admin. Warranty/Inspection ما زالت غير معروضة لأن المخطط لا يحتوي سجلاتها.
- **جولة 7 نوفمبر 2026:** وُحّدت الهوية Graphite/Obsidian + Ambient Orange عبر Storefront وAdmin. أُصلح الفوتر من جذره (إزالة Gradient المكرر و`bg-slate-950`/`#0b0f15 !important`) وأصبح شفافًا فوق Ambient Canvas واحد. أُزيل مؤشر M4؛ Badge النسخة التجريبية شرطي عبر `NEXT_PUBLIC_PREVIEW_MODE`.
- **Demo السلة:** `rigx-cart-v1` في `localStorage`، درج سلة وعدّاد، `/cart` و`/checkout`، دعم Variants، ومرجع `RIGX-DEMO-*` للعرض فقط. لا Backend/Orders/Stock/Shipping/Payment.
- **التحقق بعد الجولة:** `tsc --noEmit` = 0، Vitest `28/28`، Build ناجح بـ22 Route، Playwright غير المتطفل `12` ناجحًا و`1` متجاوزًا بالتصميم، وVisual QA على 1440/390 في AR/EN وLight/Dark. Admin المحمي لم يُختبر بصريًا بجلسة مصادق عليها.
- **Wave 0–1 (9 نوفمبر 2026):** Store Settings/WhatsApp/Social/Pickup/Trust + WhatsApp Floating + Footer social، سطح بطاقة فاتح، إزالة Compare، New/Used + فلتر، وترجمة الصلاحيات. التحقق: 43/43 focused، tsc=0، Vitest 36/36، Build ناجح، Playwright 1/1.

## 11. Prompt تاريخي لبدء محادثة Codex جديدة مع DeepSeek (لا يُستخدم كتعليمات تشغيل حالية)

النص التالي محفوظ كسجل تاريخي لإعداد Codex السابق؛ لا تنفذه في OpenCode الحالي. استخدم `AGENTS.md` و`docs/engineering/AUTONOMOUS_ENGINEERING_STANDARD.md` و`NEXT_TASK.md` بدلًا منه.

```text
أنت المنفذ الهندسي الأساسي لمشروع HARDWARE STORE داخل مجلد Workspace الحالي. لا تنقل المشروع ولا تنشئ Workspace جديدًا.

ابدأ بجولة فهم Read-only فقط:
1. اقرأ AGENTS.md كاملًا أولًا.
2. اقرأ MODEL_HANDOFF.md وPROGRESS.md وNEXT_TASK.md.
3. اقرأ Skills الملائمة من .agents/skills/؛ لا تدّعِ استخدام Skill لم تقرأها.
4. طابق الملخصات مع Workspace الفعلي: حالة الملفات والكود والاختبارات والمigrations وGit، ولا تفترض أن هذا التسليم أحدث من الملفات.
5. لا تعِد تأسيس المشروع أو إعادة تنفيذ M2 أو M3.1 أو M3.2a-d. نتائج M3.2d المسجلة: Lifecycle PostgreSQL 14/14 ومجموعة PostgreSQL الكاملة 47/47.
6. M3.2e منفذة ومختبرة. M4 Catalog Vertical Slice وAdmin Workspace مكتملان على Local Preview، وDemo السلة/Checkout وجولة توحيد هوية RIGX منفذة. التحقق الحالي: Vitest `28/28`، Next build ناجح بـ22 Route، Playwright غير المتطفل `12` ناجحًا و`1` متجاوزًا بالتصميم، وVisual QA على 1440/390 في AR/EN وLight/Dark.
7. لا تفتح التسجيل في Production ولا تبدأ M5 أو M3.2f أو M3.3 تلقائيًا. اختيار مزود البريد وCaddy/Outbox/Key Ring واعتماد فتح التسجيل قرارات تنتظر المالك.

سلّم تقرير فهم ونقطة بدء فقط: المسار الفعلي، ما تطابق/اختلف مع الملفات، الحالة المؤكدة، القرارات المفتوحة ذات الصلة، والخطوة التي تنتظر اعتماد المالك. لا تنفذ تغييرات حتى يصدر طلب واعتماد صريحان.
```


## Phase A UX Delivered — 7 نوفمبر 2026

- التقرير الكامل: `RIGX_PHASE_A_UX_REPORT_2026-11-07.md`.
- Header موحد: بحث فوري من Catalog API، Mega Menu أقسام ثنائي اللوحة، Mobile Drawer، بلا Ctrl+K وبلا Utility strip.
- الصور: `apps/web/src/lib/demo-media.ts` + `ProductImage` + `public/demo/hardware/MANIFEST.md`. الصور المحلية تتغلب على Placeholders في DB، و3 موديلات Fallback فئوي.
- Comparison: أُزيلت بالكامل في 9 نوفمبر 2026 (UI/Provider/Route/Tests) بدل الاكتفاء بـ`comparisonAvailable=false`.
- Homepage Content Prototype: `lib/home-layout.ts` + `HomeLayoutProvider` + `/admin/home-content` + `docs/storefront/HOME_CONTENT_PROTOTYPE.md`. المفتاح `rigx-home-layout-v1`، محلي فقط.
- Admin Row Actions: `components/row-action-menu.tsx` مع اختبارات.
- Design tokens: تنظيف الجداول/الحوارات/المحرر/Dashboard، و`--ds-on-accent` داكن في Light.
- التحقق: tsc، Vitest 40/40، Next build، Playwright Public/Admin QA، 0 Console Errors.
- الخطوة التالية: Phase B verification ثم M3.2f ثم M3.3. لا M5.


## M3.2f / M3.3 Delivered — 8 نوفمبر 2026

- راجع `RIGX_M3_IDENTITY_REPORT_2026-11-08.md`.
- **M3.2f:** `src/Modules/Identity/OwnerBootstrap.cs` + `OwnerBootstrapCommand.cs` + `--bootstrap-owner`. لا Endpoint عام. Audit ذرّي، Advisory Lock، منع تشغيل ثانٍ، 39 صلاحية.
- **M3.3:** `IdentityAdministrationService` + `IdentityAdministrationEndpoints.cs` + `identity-users-admin.tsx` + `identity-roles-admin.tsx`. APIs تحت `/api/v1/admin/identity`، صلاحية `System.ManageUsers`، حماية آخر Owner، Audit، وإبطال جلسات.
- **الاختبارات:** OwnerBootstrap 3/3، IdentityAdministration 3/3، PermissionAuthorization 8/8، Frontend 40/40، tsc=0، Next build 25 Route.
- **القيود:** لا حذف أدوار، لا تعديل Email/Username، لا Admin Password Reset، Owner/Customer محميان.
- **التالي:** مراجعة بشرية ثم بوابة M5 فقط بعد قرارات المالك. لا تبدأ M5.




## M3 Hardening + M5 Supplier Directory — 8 نوفمبر 2026

- راجع `RIGX_M3_HARDENING_M5_SUPPLIER_REPORT_2026-11-08.md`.
- **M3:** `IdentityDelegationPolicy.cs` + checks داخل `IdentityAdministrationService`. منع تصعيد الصلاحيات، حماية آخر Owner وManageUsers، منع تعطيل الذات، وAudit للرفض.
- **M5:** `src/Modules/Purchasing/SupplierProfile.cs` + `SupplierDirectoryService.cs` + `IdentityAdministrationEndpoints` style Purchasing endpoints + `suppliers-admin.tsx`. Migration غير مطبقة على Preview.
- **الاختبارات:** Focused backend 21/21، Frontend 40/40، Next Build 26 Route، Visual QA 0 Console.
- **القيود:** لا Stock/Transfers/Purchase Orders؛ Q2.1–Q2.5 وQ6.3 وQ8.7 معلقة. Supplier Profile يعتمد على Identity Party داخل معاملة واحدة (ADR-011).
- **التالي:** تطبيق Migration على قاعدة اختبار/Preview بموافقة، ثم Warehouse/Purchase Orders بعد البوابات. لا M5 موسع تلقائيًا.




## إغلاق جولة M3 Hardening + M5 Supplier Directory — 8 نوفمبر 2026

- **تم الاستئناف بعد شحن الرصيد، ولم يُعد تنفيذ العمل المكتمل.**
- **M3:** قواعد تفويض تمنع تصعيد الصلاحيات، حماية آخر Owner وManageUsers، منع تعطيل الذات، وAudit للرفض. اختبارات IdentityAdministration 4/4.
- **M5:** Supplier Directory (Person + SupplierProfile + Audit) مع API/UI/Mobile Cards. اختبارات PostgreSQL 4/4.
- **التحقق النهائي:** Backend build 0/0، Focused tests 21/21، tsc=0، Vitest 40/40، Next build 26 Route، Visual QA 0 Console Errors.
- **Migration:** `20261108092628_M5_SupplierDirectory` لم تُطبق على Preview/Owner DB؛ بانتظار مراجعة وموافقة.
- **اللقطات:** `docs/review/m3-hardening-m5-2026-11-08/`.
- **التقرير:** `RIGX_M3_HARDENING_M5_SUPPLIER_REPORT_2026-11-08.md`.
- **التالي:** مراجعة بشرية، تطبيق Supplier migration على قاعدة اختبار/Preview بموافقة، ثم Warehouse/Purchase Orders فقط بعد Q6.3/Q2.x. لا Stock/Transfers/Serials قبل Q2.1–Q2.5 وQ8.7.
- **Archive SHA:** منفصل في `.sha256` بعد إنشاء الأرشيف النهائي.

