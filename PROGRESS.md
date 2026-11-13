# سجل التقدم ومتابعة المشروع (PROGRESS.md)

آخر تحديث: 12 نوفمبر 2026  
المرحلة الحالية: **Round 4 — Engineering Foundation: تدقيق المستودع، إصلاح تزامن المخزون، Security Headers، توثيق Canonical، وتنظيف Legacy.**  
الحالة العامة: **جاهز لمرحلة Design/Experience مستقلة، وليس Production Ready. راجع `docs/canonical/10_HANDOFF_AND_READINESS.md`.**

## [Round 4 — Engineering Foundation] — 12 نوفمبر 2026
- **Backend:** قفل صف التجميع + التحقق من حالة المكوّنات؛ `FOR UPDATE SKIP LOCKED` لوحدات المنتج؛ `DbUpdateConcurrencyException` → 409.
- **Security:** Security Headers في API وNext.js؛ مراجعة Cookie/CSRF/Rate Limit/Permissions.
- **Cleanup:** حذف 14 مكوّنًا غير مستخدم بعد التحقق؛ استبدال اختبار Header.
- **Docs:** `docs/canonical/*` مرجع موحد + `docs/history/INDEX.md`.
- **التحقق:** Backend 190/190؛ tsc 0؛ Vitest 30/30؛ Next build؛ Playwright 6/6؛ 0 page errors.

## [Backend UI Readiness] — 11 نوفمبر 2026
- **Contract:** تصحيح `docs/api/BACKEND_UI_CONTRACTS.md` وإضافة كل عائلات المسارات والصلاحيات والحدود.
- **Traceability:** `docs/api/BACKEND_UI_TRACEABILITY.md` يوضح Implemented/Partial/Missing/Blocked لكل workflow.
- **Gaps مغلقة:** `GET /catalog/search/suggestions` (name/SKU/brand/category، محدود ومحمي)، `GET /orders/mine/{id}`، `GET /admin/retail/orders/{id}` مع وحدات/سيريالات حسب الصلاحية، وتوسيع Search للبراند/التصنيف.
- **التحقق:** Backend 189/189؛ Retail 15/15؛ Catalog 8/8؛ Authorization 2/2؛ Contact/ArticleVideo/OTP/YouTube خضراء.
- **لا Frontend تغيير:** لم يُعدّل أي ملف React/CSS/ترجمة.
- **التقرير:** `RIGX_BACKEND_UI_READINESS_2026-11-11.md`.  

## [Remaining Backend Execution] — 10 نوفمبر 2026
- **Allocations:** create/update/deactivate مع rowVersion + سبب + Audit append-only + Preview precedence + History؛ Read=ViewReports، Write=ManagePartnerAgreements.
- **Expenses/Reports:** Direct vs GeneralOverhead، فواتير/تقارير backend، صافي المتجر دون توزيع Overhead، واختبار تسوية دقيق.
- **Contact:** inbox + honeypot + dedupe + 5/10min rate limit + status/rowVersion.
- **Article Video:** ربط مقال بفيديو، منع Unpublish لفيديو مستخدم في مقال منشور.
- **API Contract:** `docs/api/BACKEND_UI_CONTRACTS.md` لتسليم ChatGPT.
- **التحقق:** Backend 187/187؛ Retail 13/13؛ Contact 1/1؛ ArticleVideo 1/1؛ Authorization 2/2؛ Build 0.
- **Migrations:** Retail M5/M6، Storefront M9/M11 — QA فقط بعد Backups.
- **التقرير:** `RIGX_REMAINING_BACKEND_2026-11-10.md`.  

## [Next Integrated Execution] — 10 نوفمبر 2026
- **Media guard:** `IMediaReferenceGuard` + `RetailMediaReferenceGuard` يمنعان حذف صور محفوظة في تاريخ وحدات المخزون، مع عرض المرجع.
- **Authorization:** فصل كتابة الشركاء عن `Finance.ViewReports` واختبارات API لدور Finance-read-only وInventory-view/cost.
- **Multi-partner:** `partner_unit_allocations` (unit/product/category)، توزيع 60/40، رفض >100%، استحقاق لكل شريك، وعكس كل الشركاء عند المرتجع.
- **Auth:** Email OTP 6 أرقام (5 دقائق، 5 محاولات، single-use، Outbox مشفّر، rate limits) للدخول وإعادة تعيين كلمة المرور، مع UI على صفحة الدخول.
- **Video:** YouTube URL validation + nocookie embed + إدارة/نشر + الحفاظ على MP4/WebM المرفوعة.
- **التحقق:** Build 0؛ Backend 183/183؛ Retail 12/12؛ Media 9/9؛ Authorization 2/2؛ OTP 1/1؛ YouTube 13/13؛ Playwright 6/6؛ tsc 0؛ Vitest 37/37؛ Next build.
- **Migrations:** Retail M4، Identity M11، Catalog M7 — QA فقط بعد Backups.
- **التقرير:** `RIGX_NEXT_EXECUTION_2026-11-10.md`.

## [Overnight Master Execution — P0/P1 slice] — 10 نوفمبر 2026
- **Finance permissions:** `Finance.ManagePartnerAgreements` و`Finance.PostPartnerSettlements` بدل `Finance.ViewReports` في الكتابة؛ القراءة فقط تبقى ViewReports؛ Migration تمنح Owner فقط.
- **Sensitive projections:** `Inventory.ViewCosts` و`Inventory.ViewSerials` و`Sales.ViewCustomerContact`؛ الخدمة تخفي التكلفة/المصدر/السيريال/الهاتف/العنوان لمن لا يملكها، مع `no-store`.
- **Unit snapshot:** Brand/Model/Category/Description/ExternalCode/Specifications/PurchaseKind/SourceType/PurchaseDate/Image refs + cost/condition/warranty/serial/source عند الاستلام.
- **Test reconciliation:** إصلاح 11 فشل قديم (endpoint/table counts، legacy schema seeding، outbox email assertions، cascade expectations) → Full Suite **164/164**.
- **Retail:** Manual/phone order form بأسعار من الخادم؛ Public `/builds` + إضافة تجميعة للسلة ثم Checkout حقيقي.
- **SEO:** `NEXT_PUBLIC_SITE_URL` مطلوب في production مع رفض localhost؛ Sitemap يجمع كل صفحات المنتجات والمقالات.
- **التحقق:** Build 0/0؛ Full xUnit 164/164؛ Retail 10/10؛ Playwright Builds 1/1، Cart/Checkout 4/4، Retail 1/1؛ tsc 0؛ Vitest 37/37؛ Next build مع production URL.
- **Migrations:** Retail M3_UnitSnapshotFields، Identity M10_RetailLeastPrivilege — QA فقط بعد Backup.
- **التقرير:** `RIGX_OVERNIGHT_EXECUTION_2026-11-10.md`.

## [Owner Decisions Applied] — 9 نوفمبر 2026
- **Partner net profit:** Snapshot لكل وحدة = إيراد مخصص − خصم − مصروف مباشر − تكلفة فعلية لكل الوحدات؛ Overhead لا يوزع. اتفاقية سارية بتاريخ الطلب + Override للوحدة.
- **Loss sharing:** افتراضيًا لا دين على الشريك؛ عند التفعيل تتحول الخسارة إلى رصيد قابل للاسترداد.
- **Assembly allocation:** توزيع افتراضي حسب سعر البيع المستقل، قابل للتعديل قبل النشر، مجموع 100%، ولقطة لكل مكوّن عند البيع.
- **Returns:** `PendingInspection` ثم تصنيف؛ عكس الاستحقاق؛ المدفوع يصبح رصيدًا سالبًا يُقاصّ من الاستحقاقات المستقبلية؛ لا حذف لسداد أو سجل.
- **Completion/Payment:** إكمال من `Delivered` بصلاحية `Sales.CompleteOrder` فقط؛ `Paid` أو `Waived` بسبب وفاعل ووقت، دون تسمية Waived كـPaid.
- **Media:** لا حذف تلقائي؛ `scripts/backup-media.ps1` مع `-Verify`، واستعادة متحققة لعينة صناعية وQA.
- **التحقق:** Build 0/0؛ Retail PostgreSQL 9/9؛ Retail Playwright 1/1 (يشمل مرتجعًا بعد الصرف واستردادًا)؛ Cart/Checkout 4/4؛ tsc=0؛ Vitest 37/37؛ Full Suite 152/163 (11 فشل قديم غير متعلق بـRetail: أعداد Endpoints/Tables متقادمة وLegacy migration fixture).
- **Migrations:** Retail M2_RetailAccounting، Identity M9_CompleteOrderPermission — QA فقط بعد Backup.
- **التقرير:** `RIGX_PRACTICAL_RETAIL_CORE_2026-11-09.md` و`docs/operations/MEDIA_BACKUP_RESTORE.md`.

## [Practical Retail Core — التنفيذ الأول] — 9 نوفمبر 2026
- **Retail Module:** Inventory Units، Assemblies، Orders، Partner Agreements/Earnings/Settlements في Bounded Context واحد للمعاملة الذرية.
- **Purchasing:** استلام Draft ينشئ وحدات فعلية (idempotent)، وحالة Received.
- **Stock/Assemblies:** وحدات فردية، create/publish/swap/remove/disassemble، تحرير سعر البيع من Admin، منع الوحدة من تجميعين أو بيع مزدوج، وإخفاء الأزرار غير الصالحة بعد البيع/التفكيك.
- **Orders:** Checkout حقيقي، مرجع من الخادم، طلب Stock عند Confirm، إلغاء يعيد الوحدات، Admin queue ودفع يدوي.
- **Partners:** Snapshot نسبة/تكلفة عند Completed، Outstanding/Paid/Reversed، Settlements FIFO دون حذف التاريخ.
- **الأمان:** Public assemblies لا تعرض acquisition cost أو serials؛ Admin/Orders/Settlements ترفض الزائر (401)، والCheckout يتطلب CSRF.
- **التحقق:** Build 0/0؛ Retail PostgreSQL 2/2؛ Retail Playwright 1/1؛ Cart/Checkout Playwright 4/4؛ Main Playwright 8/8 في دفعتين؛ tsc=0؛ Vitest 37/37؛ Next build.
- **Migrations:** Retail M1_RetailCore، Purchasing M8_PurchaseReceiptStatus — QA فقط.
- **التقرير:** `RIGX_PRACTICAL_RETAIL_CORE_2026-11-09.md`.

## [PC Builder UI + Video Round] — 9 نوفمبر 2026
- **PC Builder:** `/pc-builder`, `/pc-builder/[id]`, `/admin/pc-builder`؛ وizard بالتوافق والسعر والمشاركة. أُصلح Publish (AsNoTracking bug).
- **Video:** MP4/WebM أول-طرف، Admin، مشغل `/videos/[id]`، Range Streaming. التخزين المحلي ليس Production.
- **Purchasing:** Manual Item UI مُتحقق (اسم/كود خارجي بدون Catalog).
- **التحقق:** Build 0/0؛ tsc=0؛ Vitest 37/37؛ Next build؛ Playwright main 8/8 (مقسمة) + foundation/content 7/7.
- **Migrations:** Catalog M8_ContentVideos على QA فقط.
- **التقرير:** `RIGX_PC_BUILDER_VIDEO_ROUND_2026-11-09.md`.

## [Primary Product Engineering Round] — 9 نوفمبر 2026

- **Storefront:** English افتراضي مع حفظ الاختيار، Header موحّد، Footer بدون Account/Resend، Back-to-Top يمين وWhatsApp يسار، Share Dialog داخلي، Offers في Home/Nav.
- **Taxonomy:** `/admin/catalog/categories` و`/admin/catalog/brands` مستقلان، وإصلاح Active Nav.
- **Account:** Email Change مع Step-up + Token Hash + Outbox + Session Invalidation؛ Account Restrictions (Suspension/Ban/Reinstate) + رفض Login + Audit؛ Notifications Bell/Page + Review notifications.
- **Purchasing:** Manual/unlisted items + PurchaseKind (Component/Prebuilt/Bundle/Other) بلا Stock/Cost.
- **PC Builder:** Backend Templates/Slots/Options + Compatibility (Socket/DDR/PSU)؛ UI غير مكتمل.
- **Migrations:** Identity M8، Purchasing M7، PcBuilder M1؛ QA فقط بعد Backup.
- **التحقق:** Build 0/0، PostgreSQL 5/5 focused، tsc=0، Vitest 37/37، Next build. Playwright لهذه الجولة لم يُعد بالكامل.
- **التقرير:** `RIGX_PRIMARY_PRODUCT_ROUND_2026-11-09.md` و`PRODUCT_SCOPE_MATRIX.md`.

## [Main Product Engineering & Experience Expansion] — 9 نوفمبر 2026

- **Header/Navigation:** دُمج Utility Bar + Search + Actions + Nav في Header واحد متصل بخلفية Surface وحد سفلي، بلا Floating Pills. فُرضت خطوط المشروع (`inherit` بدل Inter القسري) على Navigation وUtility، وأُضيف رابط **العروض** للـDesktop/Mobile.
- **Discovery Rails:** Category/Brand Rails تستخدم Drag/Touch/Keyboard، وأصبحت الأسهم تحرك ~80% من العرض بدل بطاقة واحدة؛ Pause على Hover/Focus/Drag/Hidden + Reduced Motion عبر المكوّن المشترك.
- **Account Center:** `/account` أصبح Overview (Avatar، الاسم، التحقق، الهاتف، ملخص الأمان، العناوين، Wishlist، التقييمات، Orders مؤجل بوضوح) + `AccountNav` موحّد وBreadcrumb رجوع. `/account/reviews` جديد. Avatar Upload/Remove مع Validation/Pipeline الموجود.
- **Reviews:** Product + Store Reviews (Pending/Published/Rejected/Hidden)، مراجعة واحدة لكل مستخدم/منتج، Edits تعيد Pending، Aggregate/Distribution من Published فقط، Avatar اختياري، Admin Moderation مع سبب. لا Verified Purchase قبل Orders.
- **Offers:** `offers`/`offer_items` Draft/Published/Cancelled بنافذة زمنية وسعر صريح من Admin أقل من BasePrice. Public `/offers` + قسم Home + Admin `/admin/content/offers`. لا Coupons/Demo Discounts.
- **Purchase Drafts:** `purchase_drafts`/`purchase_draft_items` من RegisteredSupplier/Marketplace/IndividualSeller/Other، SupplierId اختياري، بيانات بائع خارجي، RowVersion، Audit، Draft/Cancelled فقط. لا Stock/Cost/PO/GRN. Admin `/admin/purchasing/drafts`.
- **Admin Analytics:** Endpoint `/api/v1/admin/analytics/dashboard` + Widgets/Charts من Catalog/Purchasing/Identity/Inventory/Storefront/Auditing، بلا Sales/Profit/Stock turnover المزيفة.
- **Catalog Navigation:** أُصلح Active Route Matching (Exact) فلا يظهر Catalog + New Product نشطين معًا، و`/admin/catalog` يظل List View.
- **Performance:** HomeLayout fetch أصبح على `/` فقط؛ Header/CLS مختبـر؛ Production Build 109MB/51ms مقابل Next Dev 5.4GB/104ms. تقرير الأداء يوضح أن ضغط 28GB ناتج أساسًا عن Next Dev + WSL/Docker + Browsers + Agents.
- **Migrations:** Identity `M7_ProfileAvatar`، Catalog `M7_Reviews`، Purchasing `M6_PurchaseDrafts`، Storefront `M8_Offers`؛ طُبقت على QA فقط بعد Backup `.backups/rigx_qa_before_round2_20261109.dump`.
- **التحقق:** Backend Build 0/0؛ PurchaseDraft 3/3؛ Reviews+Offers 2/2؛ Playwright Main 6/6؛ Full Suite 26/27 (1 skipped) مع نجاح اختبارَي Login المعزولين 2/2 بسبب حد 5/IP/min.
- **التقرير:** `RIGX_MAIN_ENGINEERING_2026-11-09.md`.

## [Consolidated Product Expansion — Addresses + Wishlist + CMS + Media + Audit + Durable Email] — 9 نوفمبر 2026

- **Person Classification:** `PersonClassificationPolicy` تصنيف إضافي (Customer/Employee/Supplier)؛ إنشاء مستخدم Admin يشتق الأعلام من الأدوار بدل `IsEmployee = true` الثابت. اختبار تأكيد لدعوة Staff.
- **Session Minimization:** توقف جمع IP الخام للجلسات الجديدة؛ العمود Legacy فقط ولا يُعرض في API/UI.
- **Durable Email Outbox:** جدول `identity_email_outbox` مع Data Protection، حالات Pending/Processing/Sent/Failed/DeadLetter، Retry/Backoff، `FOR UPDATE SKIP LOCKED`، مسح Payload بعد النجاح، Audit. اختبارات PostgreSQL 4/4 (تشفير، فشل/Backoff، Dead Letter، إعادة تشغيل مع Key Ring). أضيف تثبيت `DataProtection:KeyRingPath` على Volume في `Program.cs` (كان مضبوطًا في Compose لكن غير مستخدم).
- **Customer Address Book:** `customer_addresses` مع Soft delete، عنوان افتراضي واحد (Unique Filtered Index)، RowVersion/Optimistic Lock، Ownership/IDOR، Validation كامل، Audit داخل المعاملة. `/account/addresses` Drawer/List/Default/Delete. اختبارات 2/2. أصلح Bug: `emptyForm` كان null للحقول الاختيارية فينهار Save.
- **Wishlist:** `wishlist_items` Product-level + قراءة فقط Product/Image/Category، Add/Remove/Merge/Clear، منع التكرار، عزل الحسابات، تجاهل المنتجات المحذوفة وإظهار Draft كغير متاح. Guest localStorage + Merge عند الدخول. زر Heart في البطاقة/التفاصيل و`/account/wishlist`. اختبارات 3/3.
- **Homepage CMS:** `homepage_layouts` Draft/Published مع Layout JSON محدود الأنواع، RowVersion، تحقق Product/Category/Brand عند النشر، وPublic endpoint. الواجهة تقرأ المنشور من Backend وليس localStorage؛ Admin يحفظ مسودة/ينشر.
- **Articles CMS:** `content_articles` + `content_article_revisions`، Draft/Published/Unpublish، Slug فريد، AR/EN، SEO، Canonical، Tags، Image من المكتبة، سجل مراجعات، نص عادي فقط (رفض HTML). Public `/articles` و`/articles/[slug]` مع Metadata/OG وSitemap وNoindex عند الغياب.
- **FAQ CMS:** `content_faqs` Draft/Published مع ترتيب وتصنيف، Admin CRUD، Public `/faq` حسب اللغة.
- **Policies:** `content_policies` نسخ مرقمة لكل نوع، Draft→Review→Published، النسخة المنشورة غير قابلة للتعديل، أرشفة السابقة، Public `/policies/[kind]`. لا نصوص قانونية مخترعة؛ الحالة Draft حتى اعتماد المالك.
- **Media Library:** `content_media_assets` + `content_media_asset_usages` في Catalog تحت Volume الصور، Upload/List/Search/Metadata/Replace/Soft Delete، Usage Guard يمنع حذف أصل مستخدم في محتوى منشور، و`IContentMediaUsageSink` من Storefront. الأصل والـDerivatives محفوظان، Watermark معطّل.
- **Admin Audit Viewer:** `SecurityAuditQueryService` Read-only مع Filters/Pagination وWhitelist لمفاتيح Metadata؛ لا Hash/Ticket/IP/Tokens. Endpoint بصلاحية `System.ViewAuditLogs` وصفحة `/admin/audit`.
- **Warehouse Metadata:** `contact_phone`/`manager_name`/`notes` اختيارية مع Validation/RowVersion/Audit وUI. لا Stock/Cost/Movements.
- **QA:** Build 0/0؛ اختبارات PostgreSQL الجديدة: Addresses 2/2، Wishlist 3/3، Outbox 4/4، AuditQuery 1/1، CMS 3/3، Media 1/1، Warehouse 2/2؛ tsc=0؛ Vitest 37/37؛ Next build ناجح؛ Playwright الكامل 22 passed / 1 skipped (18 قديم + 4 جديد) بعد إصلاح Selectors قديمة.
- **Migrations (طُبقت على QA فقط بعد Backup):** Identity `M5_IdentityEmailOutbox`/`M6_CustomerAddressesAndOutbox`، Catalog `M6_ContentMediaLibrary`، Storefront `M6_Wishlist`/`M7_ContentCms`، Inventory `M6_WarehouseOperationalMetadata`. لم تُطبّق على Preview/Owner DB.
- **التقرير:** `RIGX_CONSOLIDATED_EXPANSION_2026-11-09.md`.


## [Overnight Consolidated Batch — Identity Admin + Sessions + Media + Cart] — 9 نوفمبر 2026

- **Identity Administration:** `POST /admin/identity/users` ينشئ حسابًا بدون كلمة مرور ويصدر Email Verification token (24h) ويرسل رابطًا للبريد؛ لا يعيد Token. `POST /users/{id}/password-reset` يرسل Reset Password آمنًا بدل كلمة مؤقتة. كلاهما يخضع لـDelegation/Audit/CSRF/Rate Limit.
- **Customer Profile:** `PATCH /identity/profile` لتعديل الاسم/الهاتف، و`/account/profile` مع Name/Phone/Username/Email/Email verification state. `/account` يحول إلى Profile.
- **Sessions & Devices:** `AuthSession.PublicId` عشوائي + UserAgent/IP داخلي، `GET /identity/sessions` يعيد Device/Browser/LastActivity/Created/Expiry/IsCurrent فقط (لا Hash/Ticket)، `DELETE /sessions/{id}` و`/sessions/others` مع IDOR protection وAudit. صفحة `/account/sessions` تعرض/تنهي الجلسات.
- **Media Pipeline:** حفظ الأصل `.original` + WebP + Thumbnail، DB `original_storage_key`، حماية الأصل من Public Endpoint، Media Audit/Cleanup يفهم الأصل. Watermark غير مفعل لعدم وجود شعار رسمي.
- **Cart Integrity:** `tryAddCartItem` يعيد Added/Incremented/MaxLines/MaxQuantity/Invalid؛ لا Toast نجاح عند الرفض؛ حدود السلة/الكمية؛ localStorage failures؛ Variant مطلوب عند وجود أكثر من إصدار ولا Quick Add لبطاقة ذات سعر يبدأ من Variant.
- **Admin UX:** إنشاء مستخدم Drawer، اختيار أدوار، إرسال Reset، رسائل بريد/حالة، Permission-aware. ترجمة الصلاحيات الـ39 محفوظة.
- **QA:** Build 0/0، Identity/Session/Admin/Permission/Catalog/Media/Supplier/Warehouse/StoreSettings/Preview tests، tsc=0، Vitest 37/37، Next build ناجح، Playwright Account/Admin M5 2/2 (بالترتيب التسلسلي).
- **Migrations:** `M5_SessionDeviceMetadata` و`M5_MediaOriginalPreservation` + Storefront/ProductCondition. طُبقت على QA فقط (17 إجمالًا) ولا على Preview/Owner DB.
- **Screenshots:** `docs/review/account-wave-2026-11-09/` و`docs/review/m3-hardening-m5-2026-11-08/`.
- **التقرير:** `RIGX_OVERNIGHT_CONSOLIDATED_BATCH_2026-11-09.md`.
- **المؤجل:** Email Change verification flow (يحتاج token payload + confirm page)، Newsletter Consent، Admin mobile cards، Watermark، Real Orders/Checkout، Stock/PO.

## [Expanded Storefront Wave 0–1 + Store Settings + Permission UX] — 9 نوفمبر 2026

- **Wave 0 — Preview stabilization:** أضيف Guard يقارن Host/Port/Database بين `Default` و`Migrations` قبل أي DDL، مع السماح باختلاف المستخدم/كلمة المرور. أضيف PostgreSQL integration test يستدعي `PreviewInitializationService.InitializeAsync` فعليًا، ويتحقق من Identity/Auditing/Catalog/Purchasing/Inventory/Storefront، ومن تكرار التهيئة دون ازدواج.
- **Supplier projection:** قوائم/تفاصيل المورد العامة أصبحت Projection مباشرة إلى `SupplierView` بدل تحميل الكيان كاملًا، فلا تُقرأ أعمدة البنك في المسار العام.
- **Storefront visual:** أزيلت ميزة Compare بالكامل: الأزرار من البطاقات/التفاصيل، روابط Navbar/الهاتف، Provider/Layer، ومسار `/compare` والمكونات/الاختبارات المرتبطة. وُحّدت خطوط النماذج/العناصر، وأصبح سطح بطاقة المنتج `var(--ds-surface)` أبيض/فاتحًا بلا Gradient رمادي غير مقصود، مع منطقة صورة محايدة موحدة.
- **Store Settings module:** Module `Storefront` جديد + جدول `store_settings` + public GET + Admin GET/PUT بصلاحية `System.StoreSettings`. الحقول: WhatsApp/Message، Facebook/Telegram/TikTok/YouTube/Instagram، Pickup Address/Hours/Instructions، Warranty/Service/Consultation summaries، RowVersion/Audit. لا رقم/رابط/وعد مخترع؛ الحقل الفارغ يخفي العنصر.
- **WhatsApp/Floating/Footer:** زر WhatsApp عائم يظهر فقط عند وجود رقم من الإعدادات أو `NEXT_PUBLIC_STORE_WHATSAPP_NUMBER`. Footer يعرض روابط Social الفعلية فقط بأيقونات RIGX متسقة ويخفي غير المضبوط.
- **New/Used:** `CatalogProductCondition` على Product، Migration `M5_ProductCondition`، Admin select، Badge على البطاقة/التفاصيل، وفلتر `condition` في Catalog API وواجهة المنتجات.
- **Permission UX:** كل مفاتيح `PermissionCatalog` الـ39 لها اسم/وصف/مجموعة عربية وإنجليزية، مع بحث بالاسم/الوصف/المفتاح. المفاتيح المخزنة/المرسلة لم تتغير.
- **التحقق الفعلي:** `dotnet build` 0/0؛ 43/43 اختبارًا مركّزًا (Catalog 4، Identity 7، Supplier 7، SupplierProduct 3، Warehouse 2، StoreSettings 2، Preview 6، Permission 12)؛ `tsc`=0؛ Vitest 36/36؛ Next build ناجح؛ Playwright Admin/Storefront 1/1؛ `has-pending-model-changes` نظيف لـCatalog/Storefront/Purchasing/Inventory.
- **Migrations:** `20261108214739_M5_StorefrontSettings` و`M5_ProductCondition`؛ طُبقت على قاعدة QA المعزولة فقط (15 Migration إجمالًا). **لم تُطبق على Preview/Owner DB.**
- **اللقطات:** `admin-store-settings-dark.png`، `storefront-whatsapp-footer-dark.png`، وتحديث لقطات المورد/المستودعات في `docs/review/m3-hardening-m5-2026-11-08/`.
- **الملفات الرئيسية:** `src/Modules/Storefront/*`، `StorefrontEndpoints.cs`، `PreviewInitializationService.cs`، `CatalogPersistence.cs`، `CatalogApplication.cs`، `CatalogEndpoints.cs`، `permission-labels.ts`، `identity-roles-admin.tsx`، `store-settings-provider/admin`، `whatsapp-floating-button.tsx`، `site-chrome.tsx`، `catalog-product-card/view`، `catalog-products-experience.tsx`، واختبارات StoreSettings/Preview/Catalog/Permission.
- **التقرير:** `RIGX_EXPANDED_STOREFRONT_WAVE_2026-11-09.md`.
- **الخطوة التالية:** Wave 2 (Admin user creation/email/reset + sessions/devices + customer account), ثم Wave 3 (real Orders/Checkout) بعد تثبيت قرارات العنوان/الشحن/المخزون.

## [M3 Hardening + M5 Supplier Product Catalog + Warehouse Directory + Identity Split] — 8 نوفمبر 2026

- **Supplier financial protection:** فُصلت البيانات المالية عن عقد العرض العام. `Purchasing.View` لا يعيد Bank/Account/IBAN؛ القراءة عبر `/suppliers/{id}/financials` بصلاحية `Purchasing.ViewSupplierFinancials`، والكتابة بـ`Purchasing.ManageSupplierFinancials`. القراءة الحساسة تُدقق داخل معاملة، وتفشل مغلقة إذا تعذر كتابة Audit. لا تُكتب البيانات البنكية في أي Audit metadata أو Error message.
- **Supplier concurrency:** `SupplierUpdateRequest` و`SupplierFinancialWriteRequest` يتطلبان `RowVersion > 0`، وCreate منفصل بلا RowVersion. اختبارات PostgreSQL تثبت Stale 409 والتحديثين المتزامنين (نجاح واحد و409 للآخر) وعدم فقدان تغيير ناجح.
- **Identity hardening:** `IdentityDelegationPolicy` يمنع منح صلاحية غير مملوكة/تعديل حساب أعلى/تعيين Owner من غير Owner. اختبار مباشر لآخر Owner عند استبدال دوره يثبت بقاء الأدوار والجلسات و`AuthorizationVersion` و`RbacRevision`. اختبار تزامن إزالة آخر صلاحية إدارية يثبت بقاء وصول Owner. رفض 403/409 يُسجل `Identity.AdministrationRejected`؛ فشل Audit لا يخفي الرفض الأصلي.
- **First Owner Bootstrap:** يرفض `stdin` المعاد توجيهه لكلمات المرور، ويوضح أن `IsEmailVerified=true` تعني تأكيد المشغّل المحلي للملكية وليست إثبات دورة بريد. لم تتغير سياسات البريد/Reset.
- **Architecture:** `IdentityAdministrationService` أصبح Facade رقيقًا؛ المسؤوليات في `IdentityUserAdministrationService` و`IdentityRoleAdministrationService` و`IdentityAdministrationSupport`. بقيت Endpoints وعقود الاختبارات كما هي.
- **M5 Supplier Product Catalog:** جدول `supplier_products` يربط `SupplierProfile` بـ`CatalogProductVariant` مع `SupplierSku` وNotes وActivation فقط. Unique على (Supplier, Variant) و(Supplier, SupplierSku). Create/Update/List/Get/Search/Pagination + Audit، بلا أسعار/MOQ/Lead Time/Stock/Purchase Orders.
- **M5 Warehouse Directory:** Module `Inventory` جديد، جدول `warehouses` (Code/Name/Address/IsActive/RowVersion)، Create/Update/List/Get/Search/Pagination + Audit بصلاحيات `Inventory.View` و`Inventory.ManageWarehouses`. لا Stock Items أو Balances أو Movements.
- **Preview integration:** `PreviewInitializationService` أصبح يطبق Migrations لـIdentity/Auditing/Catalog/Purchasing/Inventory بالترتيب قبل Seed/Owner، لأن Purchasing يعتمد على Catalog Variants.
- **Theme fix:** Bootstrap الثيم أصبح يحترم Cookie `hardware-store-theme` عند غياب localStorage بدل تجاوزها بـOS preference.
- **التحقق الفعلي:** `dotnet build` = 0/0؛ `IdentityAdministrationPostgreSqlTests` 7/7؛ `SupplierDirectoryPostgreSqlTests` 7/7؛ `SupplierProductCatalogPostgreSqlTests` 3/3؛ `WarehouseDirectoryPostgreSqlTests` 2/2؛ `PermissionAuthorizationTests` 11/11؛ `tsc --noEmit` = 0؛ Vitest `40/40`؛ Next production build ناجح؛ Playwright Admin M5 `1/1` (Desktop/Mobile، AR/EN، Light/Dark، Console/Network). `has-pending-model-changes` نظيف لـIdentity/Purchasing/Inventory.
- **Migrations:** `20261108143805_M3_SupplierFinancialPermissions`، `20261108162615_M5_SupplierProductCatalog`، `20261108165119_M5_WarehousePermission`، و`M5_WarehouseDirectory`. **لم تُطبق على Preview/Owner DB.** Visual QA استخدم قاعدة اختبار معزولة `rigx_m5_qa_20261108` وAPI منفصل على 8091 وWeb على 3006.
- **اللقطات:** `docs/review/m3-hardening-m5-2026-11-08/` (Supplier financial drawer، Supplier Product drawer/list، Warehouse drawer/list، Desktop/Mobile، Light/Dark، AR).
- **الملفات الرئيسية:** `SupplierDirectoryService/Models`، `SupplierProduct.cs`، `SupplierProductModels.cs`، `SupplierProductCatalogService.cs`، `Warehouse.cs`، `WarehouseModels.cs`، `WarehouseDirectoryService.cs`، `InventoryPersistenceRegistration.cs`، `InventoryEndpoints.cs`، `IdentityAdministration*.cs`، `PreviewInitializationService.cs`، `PurchasingEndpoints.cs`، `PermissionCatalog.cs`، `suppliers-admin.tsx`، `supplier-products-admin.tsx`، `warehouses-admin.tsx`، `admin-shell.tsx`، `api-client.ts`، `i18n.tsx`، `layout.tsx`، واختبارات PostgreSQL و`apps/web/tests/e2e/admin-m5.spec.ts`.
- **التقرير:** `RIGX_M3_HARDENING_M5_CATALOG_WAREHOUSE_REPORT_2026-11-08.md`.
- **الأرشيف:** `HARDWARE_STORE_RIGX_M3_M5_CATALOG_WAREHOUSE_2026-11-08.zip` مع `.sha256` مجاور.
- **الخطوة التالية:** مراجعة بشرية واعتماد تطبيق Migrations على قاعدة اختبار/Preview. لا تبدأ Stock/Transfers/PO قبل قرارات Q2.1–Q2.5 وQ6.3 وQ8.7.

## [RIGX Interaction, Shopping UX & Global UI Polish] — 7 نوفمبر 2026
- **النطاق:** Storefront + Admin داخل `apps/web` فقط. لم يُعدل Backend أو API Contracts أو Database أو Migrations أو Docker أو Dependencies أو `.env*`.
- **Carousels/Rails:** مكوّن `storefront-rail.tsx` مشترك بدل المنطق المنفصل لكل شريط؛ أسهم في الاتجاهين، Pointer drag/swipe مع grab/grabbing، إيقاف Auto-scroll عند Hover/Focus/Drag/إخفاء الصفحة، احترام Reduced Motion، منع Click بعد Drag، وإخفاء/تعطيل الأسهم عند الحدود.
- **Header/Menus:** Mega Menu بحد أقصى للارتفاع مع Scroll داخلي وViewport collision، وإغلاق Outside click/Escape/Route change مع إعادة Focus؛ Mobile Drawer لمسي مع Focus trap/Escape؛ إخفاء Compare/Theme من الهاتف ونقل Theme إلى الدرج.
- **Admin Sidebar:** إزالة `overflow: hidden` من العمود وتحويله إلى Sticky بكامل الارتفاع مع Scroll داخلي واضح، وعدم قص العناصر.
- **Cards/Images:** تثبيت أبعاد البطاقات ومنصة الصورة و`object-fit`، إصلاح تضخم مصغّرات Gallery على الهاتف (`w-16 shrink-0`)، وتحسين Hit-area، مع إبقاء وسم «صورة توضيحية».
- **Theming:** Semantic Tokens لـLight/Dark، Hero فاتح بورق دافئ، إزالة المساحات الداكنة غير المقصودة و`background-attachment: fixed`، دون Global mapping أعمى لكل `bg-slate-*`.
- **Filters:** Desktop Sidebar + Mobile Drawer، Draft/Apply/Reset/Close، Chips قابلة للإزالة وClear All، والفلاتر API-backed فقط (السعر/الفئات/الماركات/Spec Facets) دون تكرار Search/Category/Brand/Price ودون فلاتر مخزون/خصم/جديد/مستعمل. أُصلح Overflow من جذره (`.rigx-filter-trigger` وBreakpoint النموذج و`.rigx-catalog-sort`).
- **Cart/Buy Now:** Add to Cart يعرض Toast فقط ويبقي المستخدم في الصفحة؛ Buy Now منفصل يستخدم Variant المحدد وينتقل إلى `/checkout` Demo، ويظهر اسم الـVariant في Toast وملخص Checkout.
- **قسم جديد:** `StorefrontDiscovery` في منتصف Home يعتمد على أول 5 منتجات من Catalog API الحقيقي (صورة/سعر/مواصفات مسجلة) دون ادعاء توافق أو مخزون ودون تحويله إلى PC Builder.
- **الملفات:** 4 مكونات جديدة (`storefront-rail`, `storefront-discovery`, `storefront-section-heading`, `cart-toast`) و15 ملفًا معدلًا في `apps/web` (المكونات والاختبارات و`globals.css` و`i18n.tsx`)، إضافة إلى `PROGRESS.md` و`NEXT_TASK.md` و`MODEL_HANDOFF.md` والتقرير.
- **التحقق الفعلي:** `npx tsc --noEmit` = 0؛ Vitest `8` ملفات / `28` اختبارًا؛ Next Build ناجح على Next.js 16.4.5 بـ22 Route؛ Playwright الكامل `14 passed / 3 skipped by design` (Admin Catalog وCustomer Identity يحتاجان PostgreSQL test DB، وLogin Security يحتاج Credentials غير متاحة). Visual QA: 36 لقطة After مقابل 12 Before، `issues: 0` و`overflow: 0` في كل اللقطات، Reduced Motion ثابت، وKeyboard Rail يعمل.
- **الحدود:** Admin/Account البصري بجلسة مصادقة حقيقية غير متاح؛ تم التحقق من Login gate وShell code وBuild. لا Git repo، لذا لا Diff. صور المنتجات Placeholders من Seed.
- **النسخ:** نسخة رجوع `.backups/rigx-interaction-ux-2026-11-07-pre/` (93 ملفًا، 813,689 بايت)؛ لقطات Before/After في `.backups/visual-qa-interaction/`. أرشيف التسليم وتفاصيل العدد والحجم وSHA256 في `RIGX_INTERACTION_SHOPPING_UX_REPORT_2026-11-07.md`.

## [Autonomous Engineering Standard & Documentation Overlay] — 7 نوفمبر 2026
- **النطاق:** تطبيق Patch توثيقي/مهارات فقط من `RIGX_AUTONOMOUS_ENGINEERING_PATCH_2026-11-07.zip`؛ لا تغيير في تطبيق أو Backend أو Database أو API أو Migrations أو Docker أو Dependencies.
- **المضاف:** `docs/engineering/AUTONOMOUS_ENGINEERING_STANDARD.md`، وتحديث `AGENTS.md` ومهارات controlled-implementation/design-system/design-tokens/visual-qa/visual-checklist/engineering-handoff، وربط المعيار في README/NEXT_TASK/ROADMAP/MODEL_HANDOFF.
- **الحفاظ على الأحدث:** لم تُستبدل جولة RIGX Visual Identity Unification المكتملة ولا أرشيفها ولا نتائج التحقق الأحدث بحالة "ACTIVE" أو Phase 3 غير المنفذة الموجودة في لقطة الـPatch الأقدم.
- **النسخة الرجوع:** `.backups/rigx-autonomous-standard-2026-11-07-pre/` (12 ملفًا).
- **التحقق:** 89 رابطًا نسبيًا مفحوصًا، 0 مكسور؛ 0 عناوين مكررة أو code fences غير متوازنة؛ 0 أسرار. الملفات المحمية الأربعة SAME، و207 ملفات مصدر تطبيق SAME (باستثناء `apps/web/tsconfig.tsbuildinfo` المولّد). لا Build/تطبيق لأن التغيير توثيقي فقط.

## [RIGX Visual Identity Unification & Footer Root-Cause Fix] — 7 نوفمبر 2026
- **النطاق:** توحيد الهوية البصرية عبر Storefront وAdmin انطلاقًا من مراجع HTML التي اختارها المالك، مع إصلاح خلفية الفوتر من جذرها. لم يتغير Backend أو Database أو API contracts أو Migrations أو Dependencies.
- **القرار التصميمي:** Graphite/Obsidian + Ambient Orange. Light = ورق دافئ/Graphite Ink، Dark = Obsidian، Accent برتقالي واحد وGrid تقني خفيف. المراجع استُخدمت كمبادئ Layout/Hierarchy/Motion فقط، دون نسخ Palettes أو نصوص أو أسعار أو وعود تجارية.
- **السبب الجذري للفوتر:** `SiteFooter` كان يستخدم `bg-slate-950` و`#0b0f15 !important`، و`:root` كان يكرر Gradient موجودًا أصلًا في `.rigx-visual-engine`؛ النتيجة كتلة داكنة تحجب الطبقة المحيطية. أُزيل Gradient من `:root` و`[data-theme]`، وأُبقيت `background-color` فقط، وأُعيد بناء `.rigx-site-footer` كطبقة شفافة/متدرجة فوق نفس Canvas مع Accent Rule وSystem Block وBack-to-top.
- **الخلفية:** `.rigx-visual-engine` ثابت بكامل الـViewport: Grid + 4 Radial Glows + Pointer Glow. `.rigx-app-frame` أصبح Flex Column و`#main-content { flex: 1 0 auto; }` لضمان استمرار الخلفية ودفع الفوتر للأسفل.
- **المكونات:** تحديث Header/Utility/Mega Menu/Mobile Drawer، Products/Categories/Product Detail، Cart/Checkout، Login/Auth، Footer، وAdmin Shell/Dashboard على نفس Semantic Tokens. أُعيد ربط `bg-blue-*`/`text-blue-*`/`border-blue-*`/`cyan-*` بالـAccent، وأُضيف `.rigx-product-breadcrumb` و`.rigx-variant-option` و`.rigx-catalog-page` و`.rigx-page-head` و`.rigx-admin-shell`.
- **الخصوصية:** أُزيل مؤشر M4 من واجهة العميل؛ يظهر Badge "نسخة تجريبية" فقط عند `NEXT_PUBLIC_PREVIEW_MODE=true`.
- **الملفات:** `apps/web/src/app/globals.css`, `site-chrome.tsx`, `site-header.tsx`, `site-header.test.tsx`, `app-chrome.tsx`, `storefront-home.tsx`, `storefront-category-carousel.tsx`, `catalog-products-experience.tsx`, `catalog-product-view.tsx`, `catalog-product-card.tsx`, `product-detail-gallery.tsx`, `product-actions.tsx`, `storefront-carousel.tsx`, `admin-shell.tsx`, `admin-dashboard.tsx`, `ui.tsx`, `i18n.tsx`, `apps/web/tests/e2e/foundation.spec.ts`.
- **التحقق الفعلي:** `npx tsc --noEmit` = 0؛ Vitest `8` ملفات / `28` اختبارًا ناجحًا؛ `npm run build` ناجح على Next.js 16.4.5 بـ22 Route؛ Playwright غير المتطفل `12` ناجحًا و`1` متجاوزًا بالتصميم (Login) على `http://127.0.0.1:3006` بمجموعات Foundation/SEO/Storefront/Comparison/Cart.
- **Visual QA:** 29 حالة/لقطة نهائية على 1440×1000 و390×844 في AR/EN وLight/Dark للصفحات العامة. كل الصفحات `200`، بلا Horizontal Overflow، بلا أخطاء Console غير متوقعة، `footerBg = rgba(0, 0, 0, 0)` في كل الحالات، و`M4` غير موجود. Reduced Motion يعطّل Pointer Effect ويجعل `scroll-behavior: auto` و`transition-duration: 0s`. Variant المحدد يستخدم Accent `rgb(242, 90, 34)`.
- **الحدود:** Admin المحمي لم يُختبر بصريًا بجلسة مصادق عليها لعدم استخدام بيانات دخول/أسرار؛ تم التحقق من Login gate وShell code وBuild. صور المنتجات Placeholders من Seed وليست عيب واجهة. نسخة 3005 الأصلية لم تُمس.
- **النسخ:** نسخة ما قبل إعادة التصميم `.backups/RIGX_REDESIGN_PRE_2026-11-07.zip`. أرشيف التسليم النهائي المحدث: `.backups/HARDWARE_STORE_RIGX_VISUAL_IDENTITY_2026-11-07.zip` (0.7 MB، 256 مدخلًا، 0 أخطاء قراءة، 0 مدخلات محظورة، بلا `.env`/`.env.preview.local` أو Build Outputs أو نسخ داخلية).

## [Interactive Storefront Demo — Cart & Demo Checkout] — 7 نوفمبر 2026
- **النطاق:** Demo أمامي فقط داخل `apps/web` لإضافة المنتجات إلى سلة محلية، تعديل الكميات، عرض الإجمالي الفرعي، وتجربة Checkout تجريبية. لم يُعدل Backend أو Database أو API contracts أو Migrations أو Dependencies، ولم تُنشأ طلبات أو حجوزات مخزون أو مدفوعات أو شحن حقيقي.
- **السلة:** `src/lib/cart.ts` منطق نقي لدمج نفس المنتج/Variant، حد الكمية `99`، حد `50` سطرًا، حساب Subtotal، وتنظيف `localStorage`. `CartProvider` يحفظ الحالة تحت `rigx-cart-v1`، ويفتح الدرج تلقائيًا بعد الإضافة، ويعرض إشعار `role="status"`.
- **الواجهات:** زر سلة وعدّاد في الهيدر ودرج الموبايل، زر إضافة في Product Card (يستخدم `basePrice` والتكوين الأساسي)، وزر في صفحة المنتج (يستخدم الـVariant المختار وسعره وSKU الخاص به)، ودرج سلة، وصفحة `/cart`، وصفحة `/checkout`، وشاشة نجاح بمرجع `RIGX-DEMO-*` للعرض فقط.
- **الـCheckout:** تحقق أمامي من الاسم والهاتف والمدينة والعنوان، وملخص تجريبي، ثم تفريغ السلة. الحالة لا تُرسل للخادم، ومرجع الطلب لا يُحفظ بعد إعادة التحميل.
- **التصميم:** أنماط جديدة في `globals.css` تعتمد Semantic Tokens فقط، مع Light/Dark وRTL/LTR وResponsive و`prefers-reduced-motion`. أضيف دعم `closeLabel` إلى `SideDrawer` دون تغيير سلوك بقية المستخدمين.
- **الملفات:** `cart.ts`, `cart.test.ts`, `cart-provider.tsx`, `cart-line.tsx`, `cart-drawer.tsx`, `cart-page.tsx`, `checkout-experience.tsx`, `add-to-cart-button.tsx`, `app/cart/page.tsx`, `app/checkout/page.tsx`, `app-providers.tsx`, `app-chrome.tsx`, `site-header.tsx`, `catalog-product-card.tsx`, `catalog-product-view.tsx`, `icons.tsx`, `i18n.tsx`, `ui.tsx`, `globals.css`, `tests/e2e/cart-demo.spec.ts`.
- **التحقق الفعلي:** `tsc --noEmit` = 0؛ `npm test` = 8 ملفات / 28 اختبارًا ناجحًا؛ `npm run build` ناجح على Next.js 16.4.5 و22 Route؛ Playwright `cart-demo.spec.ts` = 3/3 على `http://127.0.0.1:3006` (Desktop، Variants/Checkout، و390px مع Light/Dark).
- **Visual QA:** Screenshots فعلية على 1440×1000 و390×844 للدرج والسلة والـCheckout والنجاح والحالة الفارغة، عربي/إنجليزي وLight/Dark. لا Horizontal Overflow عند 390. لم تظهر أخطاء JavaScript وظيفية؛ سُجلت `401` المتوقعة لـ`/identity/me`، وملاحظة View Transition عابرة عند تبديل اللغة ثم الانتقال الفوري (سلوك سابق في `LocaleProvider` وليس من الـDemo).
- **التشغيل:** نسخة العمل على `http://127.0.0.1:3006` باستخدام `API_ORIGIN=http://127.0.0.1:8085` و`NEXT_PUBLIC_PREVIEW_MODE=true`، دون إعادة بناء Preview الأصلي على `3005` ودون المساس ببياناته.
- **الحدود المعلنة:** السلة في المتصفح فقط، لا Checkout حقيقي، ولا خصم مخزون، ولا دفع/شحن/ضرائب، ومرجع الطلب تجريبي. ربط السلة بالـBackend أو إنشاء Orders يتطلب قرارًا ومرحلة منفصلة.
- **النسخ:** نسخة ما قبل الـDemo محفوظة في `.backups/HARDWARE_STORE_RIGX_PHASE2_CODE_ONLY_2026-11-06.zip` (تم التحقق من خلوها من ملفات السلة). أرشيف التسليم بعد الـDemo: `.backups/HARDWARE_STORE_RIGX_DEMO_CART_2026-11-07.zip` (266 مدخلًا، 0.69 MB، بلا `node_modules/.next/bin/obj/.git/.backups` أو `.env` فعلي، مع ملفات `.example` فقط).

## [RIGX Motion Direction V2 — Storefront Integration] — 6 نوفمبر 2026
- **النطاق:** نقل الاتجاه البصري من `RIGX_MOTION_DIRECTION_DEMO_V2.html` إلى Storefront الحالي باستخدام بيانات Catalog الفعلية، دون تقليد بيانات Demo أو نسخها. لم يتغير Backend أو Database أو API contracts أو Dependencies.
- **المظهر:** Header زجاجي عائم، Hero Graphite/Orange، كولاج لثلاثة منتجات حقيقية كحد أقصى، شريط ماركات نشطة، Category Carousel مستمر بأسهم ولمس وكيبورد وRTL/LTR، Feature Bento، Product Cards، وClosing CTA.
- **البيانات:** Home تسحب المنتجات الأحدث والتصنيفات النشطة والماركات النشطة من Catalog API. لا توجد أسماء/أسعار/ماركات أو وعود تجريبية ثابتة. حالات Loading/Error/Empty محفوظة.
- **السلوك:** Mega Menu يستبعد التصنيفات غير النشطة؛ Mobile Drawer يعرض الشجرة والتصنيفات الفرعية ويطبق Focus Trap/Restore وEscape وScroll Lock؛ تسجيل الخروج يستدعي `logoutCurrent()` الحالي؛ Carousel يوقف RAF عند Reduced Motion أو إخفاء التبويب.
- **الملفات:** `site-header.tsx`, `storefront-home.tsx`, `storefront-category-carousel.tsx` (جديد), `catalog-product-card.tsx`, `site-chrome.tsx`, `globals.css`.
- **التحقق المتاح:** TypeScript/TSX syntax transpilation: 70 ملفًا / 0 syntax diagnostics؛ CSS PostCSS parsing: 411 rule / PASS. هذا لا يمثل Typecheck كاملًا أو Build.
- **التحقق غير المتاح:** `npm ci` تعذر بسبب الوصول إلى npm registry، والمحاولة Offline فشلت بنقص cached tarball (`ENOTCACHED`). لذلك لم يُثبت Next Build أو Vitest أو Playwright لهذه الدفعة، ولم تُلتقط After Screenshots للتطبيق المعدل من هذه البيئة. يجب تشغيلها في Local Preview قبل الاعتماد.
- **الحالة:** تنفيذ على نسخة المصدر المرفوعة، بانتظار تشغيل المالك واختباراته ولقطات After والاعتماد البصري. التقرير: `RIGX_MOTION_DIRECTION_V2_INTEGRATION_REPORT.md`.

## [RIGX Header Navigation & Mega Menu V1] — 6 نوفمبر 2026
- **نطاق التنفيذ:** واجهة المستخدم فقط (`apps/web`) وفق وثيقة المواصفات المعتمدة `RIGX_HEADER_NAVIGATION_SPEC_V1.md`. لم يُعدل أي كود في الـBackend أو قاعدة البيانات أو Migrations أو أي حزم إضافية.
- **الهيكل المكتمل:**
  1. **Utility Bar:** شريط علوي نحيف بارتفاع 32px يحمل الوعد التجاري باللغتين ("مكونات كمبيوتر بمواصفات واضحة وأسعار مباشرة" / "PC components with clear specs and direct pricing")، ورابط الحساب/تسجيل الدخول، ومبدل اللغة مع `startViewTransition` سلس يحترم `prefers-reduced-motion`.
  2. **Main Header Row:** شريط بارتفاع 72px يتضمن شعار RIGX باللون الرمادي وحرف X بلون Cyan المميز، وحقل بحث واسع مع زر مسح واختصار لوحة المفاتيح `Ctrl+K`، وزر المقارنة مع شارة عدد العناصر الحية المربوطة بـ`useComparison`، وزر تبديل الثيم، وقائمة الحساب المنسدلة للعميل المصادق عليه (تحتوي على مساحة الإدارة لحاملي صلاحيات Catalog فقط) أو زر تسجيل الدخول للزوار، وزر القائمة للموبايل.
  3. **Primary Navigation Bar (Desktop):** شريط تنقل أساسي بعنصر `<nav>` دلالي يضم زر Components لفتح Mega Menu، وزر PC Builder كـPill غير مفعل ("قريبًا" / "Coming soon") تمهيدًا لمرحلة M6، وروابط Compare وNew Arrivals.
  4. **Components Mega Menu:** قائمة عريضة من 4 أعمدة مقسمة منطقيًا (المكونات الأساسية Core مع التصنيفات الفرعية لمعالجات AMD/Intel، الذاكرة والتخزين، الطاقة والتبريد، الملحقات وبطاقة ميزة المقارنة الترويجية)، مع أسماء إنجليزية أساسية وترجمة عربية توضيحية، ورابط استعراض كافة التصنيفات، ودعم حالة فشل الـAPI مع زر إعادة المحاولة، وHover Debounce آمن (120ms فتح، 180ms إغلاق)، وإغلاق بـEscape وClick outside.
  5. **Mobile Drawer:** درج جانبي منزلق خفيف وسريع (< 1024px) بـ`role="dialog"` و`aria-modal="true"`، مع Accordion تفاعلي لمكونات الكمبيوتر، وقفل تمرير الصفحة أثناء الفتح، وإغلاق بـEscape، وروابط التنقل والحساب ومبدلات اللغة والثيم.
- **التصميم والرموز البصرية:**
  - الالتزام التام بنظام `var(--ds-*)` Tokens (Brand, Surface, Border, Text, Accent) دون أي ألوان عشوائية أو أصناف `blue-*`.
  - امتداد نظام الأيقونات الخطية أحادية اللون (`cpu`, `gpu`, `motherboard`, `ram`, `hardDrive`, `fan`, `power`, `pcCase`, `peripherals`, `sparkles`, `tools`, `user`, `filter`, `close`, إلخ) بسماكة خط 1.8 ورؤوس دائرية متناسقة.
- **التحقق والاختبارات:**
  - TypeScript: تدقيق الأنواع عبر `tsc.cmd -p tsconfig.json --noEmit` نجح بنسبة 100% دون أي أخطاء.
  - Vitest: تم بناء وتمرير جناح اختبارات متكامل `site-header.test.tsx` (6/6 اختبارات ناجحة)، وكافة ملفات اختبارات التطبيق (7/7 ملفات، 23/23 اختبارًا ناجحًا).
  - Visual QA: تم التقاط وحفظ لقطات الشاشة الأساسية (Baseline) والنهائية (After) عبر مصفوفة 8 تهيئات كاملة (1440px و390px باللغتين العربية والإنجليزية والوضعين النهاري والليلي) إضافة إلى حالتي فتح الـMega Menu وفتح الـMobile Drawer.
- **النسخ والملفات المحفوظة:**
  - Baseline Screenshots: `C:\Users\Abdalmlek\.gemini\antigravity\brain\1e6e891f-0a7c-4c53-9f1f-b8b623123fb6\screenshots\baseline\`
  - After Screenshots: `C:\Users\Abdalmlek\.gemini\antigravity\brain\1e6e891f-0a7c-4c53-9f1f-b8b623123fb6\screenshots\after\`

## [RIGX V2] Storefront Design & Product Experience — 6 نوفمبر 2026

### Batch 1 — RIGX foundation, Home, cards, product details
- **Baseline/rollback:** `.backups/RIGX_V2_PRE_2026-11-06.zip`، 220 مدخلًا، بلا `.env` أو Build Outputs أو Volumes. Baseline البصري السابق محفوظ في `m4-visual-qa-final`، ولقطات RIGX أثناء التنفيذ في `rigx-v2`.
- **Tokens/brand:** أُضيف اتجاه RIGX Graphite/Orange في Semantic Tokens للوضعين. استُبدلت الهوية الظاهرة في Header/Footer/Auth/Admin بRIGX مع الحفاظ على أسماء Namespace والبنية البرمجية. عولج تباعد Arabic السالب، وأصبح SKU/الموديل LTR والمواصفات تقنية.
- **Home:** Hero جديد ببيانات المنتجات الفعلية، وسم واضح لكل Placeholder، معلومات واجهة حقيقية بدل ادعاءات Inspection/Warranty غير المتاحة، Category labels عربية مع Technical label إنجليزي، وFeatured/Latest من API.
- **Cards:** Product Card أخفى SKU وأضاف مشاركة الرابط وWhatsApp Inquiry. WhatsApp يظهر فقط عند إعداد `NEXT_PUBLIC_STORE_WHATSAPP_NUMBER`؛ لا رقم مخترع. عملة العرض `ج.م` في العربية و`EGP` في الإنجليزية.
- **Product Details:** Gallery بThumbnails وLightbox/Zoom وKeyboard، Share، WhatsApp شرطي، وRelated Products من نفس Category. Condition/Warranty/Inspection لا تُعرض لأن الحقول/السجلات غير موجودة.
- **Verification:** Next production Build ناجح وTypeScript ناجح؛ Vitest `9/9`. Visual smoke على 1440/390 AR-RTL بلا Overflow. لا Backend/API/Database/Migration/Dependency جديدة.

### Batch 2 — Product inquiry, sharing and truthful product actions
- WhatsApp CTA مشترك في Card/Details/Comparison، ويظهر فقط عند `NEXT_PUBLIC_STORE_WHATSAPP_NUMBER` صالح. Link يحمل Product Name وSKU وCanonical URL حسب اللغة.
- Share يستخدم Web Share عند توفره ثم Clipboard fallback، ويبني Canonical URL من `NEXT_PUBLIC_SITE_URL`. Playwright أثبت نسخ الرابط الصحيح، وUnit Tests أثبتت بناء WhatsApp message والCurrency.
- لا Condition/Warranty/Inspection badges لأن Catalog لا يوفر هذه البيانات.

### Batch 3 — Admin Categories, Brands and Products
- أضيف جدول Taxonomy فعلي مع Search/Status (Active افتراضيًا)/Result Count/Product Count/Hierarchy/Image/Actions.
- Create/Edit انتقل إلى SideDrawer قابل للوصول مع Escape وFocus Trap؛ Media Image deletion منفصل ويستخدم ConfirmDialog. لم تُنشأ APIs أو Migrations.
- Product table أضاف Search/Category/Brand/Status/Pagination وActions Menu. Product Editor قُسّم إلى Identity/Commercial/Variants/Specifications مع الحفاظ على الحالة عند التنقل.
- E2E Admin نجح بعد إنشاء Category/Brand/Product/Variant/Spec/Image/Preview/Publish وظهور المنتج.

### Batch 4 — SEO, social preview and crawler output
- Product details أصبح Server Component ويجلب Product الحقيقي من API، مع `generateMetadata` وCanonical وOpen Graph/Twitter وProduct JSON-LD.
- أضيف `/sitemap.xml` و`/robots.txt`، مع Noindex لـ`/compare` ومسارات الحساب/الإدارة.
- E2E قرأ Raw HTML وتأكد من Title وOG وJSON-LD وSitemap/Robots.

### Batch 5 — Product Comparison V1
- Provider محلي Versioned `rigx-product-comparison-v1`، حتى 3 منتجات من نفس Category، مع Add/Remove/Clear وTray مستقل.
- `/compare` يعيد جلب المنتجات المنشورة الحالية، ويرفض Stale ID/Slug أو Category mismatch، ولا يثق بأي Product Details قديمة.
- الجدول يعرض Product/Brand/Price/Specs فقط، Grouping للمواصفات، وMissing=`غير متوفر`، ويبرز الاختلافات الفعلية.
- Unit Tests غطت الحد 3، Cross-category، Remove، وتنظيف Local Preference. E2E غطى التحديد والحفظ بعد Reload وCompare/Mobile Overflow.

### Batch 6 — Verification and delivery
- Next Build وTypeScript ناجحان، Vitest `17/17`، وPlaywright للأسطح المتأثرة `11/11`: Foundation `2/2`، SEO `2/2`، Storefront `3/3`، Comparison `3/3`، Admin Catalog/Media/Publication `1/1`. Customer Identity `1/1` مسجل سابقًا ولم يُعد لأن منطق الهوية لم يتغير.
- Visual QA على 1440 و390 و360 للـHome/Product/Compare، مع AR/EN وLight/Dark وAdmin Desktop/Mobile. لا Horizontal Overflow. Reduced Motion: Transform `none`، Transition `0s`، WillChange `auto`.
- Console errors للزائر هي `401` المتوقعة من `/identity/me` فقط. لم تُضف Dependencies، ولم يتغير Backend/API/Database/Migration.
- أُعيد بناء Web فقط على `http://127.0.0.1:3005`؛ Web/API/PostgreSQL Healthy. سجلات E2E التي أُنشئت أُعيدت إلى Draft/Inactive عبر APIs دون حذف.
- نسخة ما قبل RIGX V2: `.backups/RIGX_V2_PRE_2026-11-06.zip`. أرشيف التسليم النهائي: `.backups/RIGX_V2_FINAL_2026-11-06.zip`.

## [Checkpoint Overnight 1–6] Preview Owner / Admin Workspace / Unified UI — 6 نوفمبر 2026
- **Preview Owner:** عولج مسار Bootstrap الحالي ليكون Idempotent بصورة صريحة: يعيد مزامنة Username/Email وPerson ودور Owner، يصفّر فشل الدخول والقفل، يعيد Hash من الإعدادات المحلية، يزيد `AuthorizationVersion`، يلغي الجلسات القديمة، ويعيد بناء Login Identifiers و`Catalog.*` داخل Transaction. لا Secret أو Password ظهر في Command Line أو Logs. السبب المباشر لفشل الدخول تعذر حصره في كلمة مرور خاطئة وحدها؛ الحالة السابقة تضمنت فشل دخول واحد و21 جلسة قديمة، وRecovery الآمن أثبت دخول Owner إلى `/admin`.
- **Admin Workspace:** أضيف `/admin` كDashboard حقيقي داخل Shell مستقل، Desktop Sidebar وMobile Drawer مع Escape/Focus Trap/Scroll Lock، Breadcrumbs، Language/Theme/Account/Logout. تعرض المؤشرات بيانات Catalog API الفعلية فقط. الأقسام غير المنفذة تظهر Disabled مع اعتماد M3.3/M5/M7/M8/M9/M11 أو قرار المالك، بلا أزرار وهمية. Catalog CRUD والصلاحيات بقيت على الـBackend الحالي.
- **Unified UI:** استُخدم `next/font` لتحميل IBM Plex Sans Arabic وInter محليًا في Build، وأُضيفت أيقونات SVG مشتركة وSemantic Tokens. صفحات Identity وStorefront وAdmin تشترك في اللغة والاتجاه والثيم، و`/admin` لا يرث Header/Footer الخاصين بالمتجر.
- **Storefront/Carousel:** أُضيفت Carousels أصلية للتصنيفات والمنتجات المنتقاة/الأحدث باستخدام Scroll Snap دون Dependency. تدعم أزرار السابق/التالي، Arrow Keys، Home/End، اللمس، RTL/LTR، وتعطيل الحركة عند `prefers-reduced-motion`. Product Summary يعرض حتى ثلاث مواصفات Product-level حقيقية، وVariant-only specs تظهر في صفحة التفاصيل بعد الاختيار.
- **Visual QA fix:** كشف الفحص الفعلي Horizontal Overflow عند `1024–1440px` بسبب شريط تصنيفات Desktop الممتد خارج الحاوية. أصبح الشريط قابلًا للتمرير داخليًا ولا يوسّع الصفحة. أُعيد الفحص على `360/390/430/768/1024/1280/1440/1920` في Home وProducts دون Overflow. الصفحات الملتقطة: Home/Products/Product/Login/Admin Dashboard/Admin Catalog في AR/EN وLight/Dark وMobile/Desktop.
- **Test/data safety:** لم تُنشأ قاعدة أو Volumes جديدة، ولم تُطبق Migration في هذه الجولة. سجلات Catalog الناتجة عن E2E لم تُحذف؛ أُخفي أثرها فقط عبر Admin APIs: منتج E2E إلى `Draft`، وثلاثة تصنيفات وثلاث ماركات إلى `isActive=false`.
- **Verification:** .NET Build `0 errors / 0 warnings`; .NET non-Docker `51/51`; Catalog PostgreSQL `4/4`; Vitest `9/9`; Next production Build ناجح. Playwright الحالي: Foundation/Carousel `2/2`، Storefront/Search/Variant/Login `3/3`، مع نتائج الجولة السابقة Admin Catalog `1/1` وCustomer Identity `1/1`؛ الإجمالي المسجل `7/7` عبر مجموعات منفصلة بسبب Login Limit. Console errors للزائر كانت `401` المتوقعة من `/identity/me` فقط.
- **Email/Providers:** لا SMTP تجاري ولا Mailpit ولا Google OAuth. أُعد اقتراح Mailpit فقط في [MAILPIT_PREVIEW_PROPOSAL.md](docs/operations/MAILPIT_PREVIEW_PROPOSAL.md)، ويحتاج موافقة منفصلة.
- **Preview convenience:** `open-preview.ps1` و`preview.ps1` يكتشفان Docker CLI داخل `PATH` أو مسار Docker Desktop المحلي. نجح `-Action Status`، وأُعيد بناء `web` فقط من الموارد الحالية؛ API/PostgreSQL والبيانات بقيت دون إعادة إنشاء.
- **Rollback/Archive:** نسخة ما قبل الجولة محفوظة في `.backups/OVERNIGHT_MASTER_TASK_PRE_2026-11-05.zip`. أرشيف المصدر النهائي لهذه الجولة هو `.backups/HARDWARE_STORE_M4_OVERNIGHT_FINAL_R3_2026-11-06.zip`؛ يستبعد `.env*` الفعلية و`.backups` و`node_modules/bin/obj/.next` وLogs وTest Outputs.

## [Checkpoint 1–3] M4 Finalization / Premium UI / Preview Stabilization — 5 نوفمبر 2026
- **Preview root cause:** سجل API أثبت `KeyNotFoundException` أثناء Bootstrap، سببه تمرير اسم التصنيف المعروض بدل Slug إلى قاموس التصنيفات. أُصلحت تعريفات المنتجات السبعة/العشرة/الأربعة عشر، وأُعيد Bootstrap على القاعدة الحالية فأنتج 11 تصنيفًا و10 ماركات و14 منتجًا دون إنشاء DB/Volumes جديدة أو تكرار الصفوف.
- **Admin regression:** إصلاح شرط الأب الذاتي كان يرفض كل Category جذرية لأن `ParentId=null` قُورن بـ`id=null`. أُضيف شرط `id is not null` واختبار Admin E2E أثبت إنشاء Category/Brand/Product صحيحًا.
- **Login security:** أزيلت خصائص `name` الحساسة من Login، ولا توجد Credentials في Query String/History، والمصادقة تمر عبر POST وCSRF داخل API Client. Playwright تحقق من Request Body وURL ولم يظهر Password في URL.
- **PasswordField:** مكوّن مشترك بـEye/Eye-off SVG، `type=button`، Tooltip/ARIA، هدف 40px، حفظ القيمة/الاختيار/التركيز، ويستخدم في كل شاشة تحتوي Password فعلًا. Registration لا يحتوي Password بحكم عقد M3.2c.
- **i18n/Theme:** AR/EN يغيران `lang` و`dir` والنصوص، ويُحفظان في Cookie/LocalStorage؛ Light/Dark يستخدم Tokens مشتركة ويحترم System Preference ويمنع Theme Flash عبر Prepaint Script.
- **Storefront:** Home تجارية ببيانات Preview، Header/Search، Categories، Featured/Latest Products، Brands، Footer. Products لها Toolbar واحدة، ستة Sort Modes، Active Chips، Results Count، Pagination تحفظ الفلاتر، وSidebar Specifications فعلية فقط مع Mobile Drawer/Escape.
- **Design/Motion:** نظام Tech Retail Premium، Product Cards بـPointer Spotlight/Tilt خفيف على Pointer الدقيق فقط، Reduced Motion، Focus واضح، وResponsive Grids. أُضيفت مهارتا `hardware-store-design-system` و`hardware-store-visual-qa` مع Attribution والتراخيص.
- **Preview convenience:** `open-preview.ps1` يفحص `.env` وDocker وHealth، يسأل قبل التشغيل، وينقح Logs؛ `create-preview-shortcut.ps1` ينشئ/يزيل اختصار Desktop دون أسرار أو Startup.
- **Migration/Data:** لا Migration جديدة ولا تغيير مخطط. `has-pending-model-changes` نظيف لـIdentity وCatalog. لم تُطبق Migration على قاعدة خارج Preview.
- **Verification:** `dotnet build HARDWARE_STORE.slnx --no-restore -m:1` بلا أخطاء/تحذيرات؛ .NET non-Docker `50/50`؛ Vitest `9/9`؛ Next production build ناجح. Playwright على Preview: Storefront/Language/Theme/Login `4/4`، Admin Catalog/Media/Publication `1/1`، Customer Identity `1/1`. مجموع الاختبارات الستة نُفذ على دفعات بسبب حد Login `5/IP/min`.
- **Checkpoint 3 UI QA:** أُخذت Screenshots فعلية على Desktop/Mobile وLight/Dark، وكُشفت مشكلة Header الأبيض في Dark Mode وبيانات E2E الظاهرة، ثم أُصلحت الـTheme Tokens وأُخفيت سجلات الاختبار المطابقة لنمط أرقامه دون حذفها.
- **Final Visual QA:** أُعيد التقاط Screenshots بعد الإصلاحات على عرض `360/390/430/768/1024/1280/1440/1920`؛ لم يظهر Horizontal Overflow في Home أو Products. الصفحات الفعلية تشمل Home/Products/Product/Login وAR-RTL/EN-LTR وLight/Dark وMobile Filter Drawer. Console Errors الوحيدة كانت استجابات `401` المتوقعة لفحص `/identity/me` للزائر غير المسجل.
- **Source archive:** `.backups/HARDWARE_STORE_M4_FINAL_R4_2026-11-05.zip`، بلا `.env` أو Volumes أو `node_modules/bin/obj/.next/test-results/.backups`. نسخة ما قبل الجولة الواسعة: `.backups/HARDWARE_STORE_M4_FINALIZATION_PRE_2026-11-05.zip`.

## استكمال M4 وتجهيز Local Preview — 5 نوفمبر 2026
- ثُبتت سياسة التصنيف الفعال: التصنيف يظهر للعامة فقط إذا كان نشطًا وكانت سلسلة آبائه نشطة بالكامل. الابن النشط تحت أب معطل يظل مرئيًا في Admin مع `isEffectivelyActive=false` ولا يمكن نشر منتج تحته.
- أضيفت مواصفات مرتبطة بالـVariant عبر `variant_id`, ومعاينة Storefront تغيّر السعر وSKU والمواصفات حسب Variant المختار. فلترة السعر تعتمد أقل سعر فعلي للمتغيرات النشطة أو السعر الأساسي.
- أصبح Storefront يدعم Search وCategory/Brand/Min-Max Price وSorting (`newest`, `price-asc`, `price-desc`) وPagination من Backend، مع حالات Loading/Empty/Error وتخطيط متجاوب.
- أضيف Admin editing وتفعيل/تعطيل Categories وBrands، ورفع/حذف شعارات الماركات وصور التصنيفات، مع معاينة مباشرة. Primary Image تصبح تلقائية عند أول صورة، وتُنقل إلى التالي عند حذف الصورة الرئيسية.
- أصبحت استجابة `GET /api/v1/identity/me` تعيد الصلاحيات الفعالة المكررة من قاعدة البيانات. تعتمد واجهة الموقع وAdmin وDraft Preview على وجود `Catalog.*` الفعلي بدل اسم Role لإظهار الوصول، بينما يظل فحص Backend هو الضابط النهائي. تحقق Playwright Admin من وصول Owner إلى `/admin/catalog` عبر الصلاحيات الفعلية؛ اختبار lifecycle الخاص بالعقد نفسه ما زال يحتاج تشغيل Testcontainers مستقلًا.
- استُبدل فحص WebP السطحي بمعالجة SkiaSharp حقيقية: فك الصورة، تطبيق اتجاه EXIF، إعادة ترميز WebP، إزالة Metadata، وتوليد Thumbnail. الحدود الافتراضية: 12 MiB رفع، 8192px/40MP مصدر، 2000px تخزين، 480px Thumbnail، 8 MiB ناتج.
- أضيف Media audit يبلّغ عن المفقود واليتيم. Cleanup يحذف فقط غير المرتبط والأقدم من 30 دقيقة، ولا يحذف ملفًا أحدث قد يكون Upload قيد الإكمال. أصبح يعيد قراءة `product_images` و`categories` و`brands` قبل حذف كل ملف مرشح بدل الاعتماد على قائمة Audit القديمة. تبقى نافذة السباق القصيرة بين آخر استعلام وحذف القرص غير ذرية.
- صُححت حزم SkiaSharp إلى Stable `4.153.2`: بقي `SkiaSharp` مرجعًا مباشرًا في Catalog، وانتقل `SkiaSharp.NativeAssets.Linux.NoDependencies` إلى API. نجح Restore وBuild بلا أخطاء أو تحذيرات، وتأكد وجود `libSkiaSharp.so` في `runtimes/linux-x64/native` داخل مخرجات API. أُجري Publish تحققي مؤقت ثم أزيل بعد التحقق.
- أضيف Preview Mode معزول: `.env.preview.example`, `compose.preview.yaml`, `apps/web/Dockerfile.preview`, و`scripts/preview.ps1`. يستخدم منافذ `5435/8085/3005` وVolumes خاصة، ولا يمس Compose القديم.
- Preview bootstrap on-demand only في Development: يطبق Migrations على قاعدة Preview، ينشئ Owner من أسرار `.env.preview.local`، ويمنح `Catalog.*`، ويضيف 14 منتجًا وصورًا مولدة. لا توجد Credentials افتراضية، ولا Endpoint في Production.
- [سجل سابق قبل جولة الإغلاق] لم يُشغّل Docker أو Testcontainers، ولم تُطبق Migration. التحقق حينها: .NET build بلا أخطاء/تحذيرات، Backend non-Docker `50/50`، Vitest `8/8`، Next production build ناجح، وPowerShell syntax parser نجح.
- بعد اكتمال النموذج أُعيد توليد Migration Catalog واحدة `20261105162018_M4_Catalog` بدل إبقاء Migration إكمال منفصلة غير مطبقة. ما زالت غير مطبقة. نسخة رجوع قبل الجولة: `.backups/HARDWARE_STORE_M4_COMPLETION_PRE_2026-11-05.zip`.
- SQL النهائي المولد دون تطبيق محفوظ في `.backups/m4-final-validation-2026-11-05/`؛ فحص `has-pending-model-changes` أفاد بعدم وجود تغييرات غير ممثلة.
- تعارض SKU/Barcode بين Product وVariant سُجل في `Q6.3` ولم تُغيَّر قواعد الفرادة.

## [سجل سابق في نفس اليوم] بدء M4 وإغلاق إصلاح التسجيل — 5 نوفمبر 2026
- عُدل `CustomerRegistrationService` ليستخدم `IIdentityEmailDispatcher.EnqueueVerificationAsync` بعد Commit بدل انتظار `IEmailProvider` داخل طلب التسجيل. الاستجابة العامة لم تتغير، والحساب والرمز يبقيان إذا فشل enqueue، مع Audit فشل آمن.
- أضيف اختبار يثبت أن enqueue يعود دون انتظار مزود بريد محجوب، وحُدّث اختبار فشل enqueue للتحقق من بقاء الحساب المعلق وعدم إرجاع تفاصيل داخلية.
- رُاجع التعارض بين حد كلمة المرور القديم `8` والقرار الحالي `15–128`. لم تتغير السياسة؛ سُجل التعارض كسؤال مالك في `OPEN_QUESTIONS.md`.
- أُنشئت جداول الكتالوج فقط: `categories`, `brands`, `products`, `product_variants`, `product_specifications`, `product_images`. لا توجد جداول مخزون أو مشتريات أو تسعير تكلفة داخل M4.
- أضيفت قيود PostgreSQL للـSlug/الأسعار/الحالات/الفرادة، وفهرس رئيسي واحد لكل منتج، وكل علاقات الحذف `Restrict`. تمنع الخدمة دورات التصنيفات داخل معاملة `Serializable`.
- أضيفت APIs عامة للتصنيفات والماركات والمنتجات المنشورة وتفاصيل المنتج وقراءة Media، وAPIs إدارة محمية بـ`Catalog.View`, `Catalog.ManageTaxonomy`, `Catalog.ManageProducts`, `Catalog.ManageMedia`. إجمالي كتالوج الصلاحيات أصبح 36.
- أضيفت واجهات Storefront: `/categories`, `/products`, `/products/[slug]`، وواجهات Admin: `/admin/catalog`, `/admin/catalog/products/new`, `/admin/catalog/products/[id]`. تشمل إنشاء التصنيف/الماركة، تعديل المنتج، Variants وSpecifications، تحويل الصور في المتصفح إلى WebP، ورفع نسخة أصلية وThumbnail مع معاينة وإدارة الصورة الرئيسية والنشر/الإخفاء.
- Media يخزن خارج قاعدة البيانات بمفاتيح مولدة من IDs، ويستخدم `ICatalogMediaStorage`. الخادم يتحقق من توقيع RIFF/WEBP ومن حدود الحجم ويرفض Path Traversal؛ لم تُضف مكتبة صور خارجية.
- [سجل تاريخي، superseded بعد إكمال M4] كان القصد توليد `20261105135211_M4_Catalog` بجانب Identity permissions migration. أُعيد توليد Catalog migration لاحقًا بعد اكتمال النموذج باسم `20261105162018_M4_Catalog` قبل أي تطبيق.
- نتائج التحقق الفعلية: `dotnet build HARDWARE_STORE.slnx --no-restore -m:1` ناجح بـ0 أخطاء، وBackend non-Docker `45/45`، وVitest `8/8`، وNext production build ناجح بكل مسارات الكتالوج. `has-pending-model-changes` أفاد بعدم وجود تغييرات غير ممثلة لكلا الـDbContextين.
- لم يُشغّل Docker أو Testcontainers ولم تُطبق Migration. اختبار `CatalogPostgreSqlTests` وPlaywright `catalog.spec.ts` جاهزان لكنهما غير منفذين حتى موافقة تشغيل منفصلة؛ `playwright test --list` اكتشف 3 اختبارات E2E بنجاح.
- نسخة رجوع ما قبل الجولة: `.backups/HARDWARE_STORE_M4_PRE_2026-11-05.zip`. فحص SQL المؤقت محفوظ في `.backups/m4-validation-2026-11-05/`. أرشيف الحالة الجزئية: `.backups/HARDWARE_STORE_M4_PARTIAL_2026-11-05.zip` ويحتوي 262 عنصرًا بعد فحص المسارات الممنوعة.

## تنفيذ واختبار M3.2e — 5 نوفمبر 2026
- نُفذت تجربة العميل المتصلة بالـAPI الحقيقي في `apps/web`: `/register`, `/verify-email`, `/resend-verification`, `/login`, `/forgot-password`, `/reset-password`, `/account/security`، مع تحويل `/account` إلى صفحة الأمان وحماية `/account/*` عبر Proxy وفحص جلسة خادمي عند التحميل.
- أضيف عميل API same-origin يستخدم Cookies ويبحث عن CSRF Token قبل POST/DELETE، ومزود جلسة مشترك، وقاموس عربي/إنجليزي مع تبديل RTL/LTR، ومكونات حقول وكلمات مرور ورسائل وحالات تحميل موحدة. لم تُضف مكتبة UI خارجية أو نظام تصميم موازٍ.
- صفحة تأكيد البريد والصفحة المكافئة لاستعادة كلمة المرور تقرآن الرمز من URL Fragment وتزيلانه من شريط العنوان قبل إرساله في Body محمي بـCSRF؛ لا يظهر الرمز في Query String أو السجلات.
- أضيف `GET /api/v1/identity/me` لبيانات الحساب الآمنة فقط، ويستخدمه Frontend لمعرفة الجلسة والاسم والدور دون كشف Password Hash أو Person ID. إبطال الجلسة بعد تغيير كلمة المرور يظهر للمستخدم كحالة انتهاء جلسة عند الطلب التالي.
- أضيف تكامل SMTP إنتاجي عام خلف `IEmailProvider` مع إعدادات Host/Port/TLS/From/Authentication/Timeout قابلة للحقن. التطوير والاختبارات يستخدمان In-Memory Provider، وProduction يفشل عند بدء التشغيل إذا كانت إعدادات SMTP ناقصة. لم يُختر مزود تجاري ولم تُضف Credentials.
- الاختبارات الفعلية: M3.2d/PostgreSQL lifecycle `14/14`، ومجموعة PostgreSQL/Testcontainers الكاملة `47/47`، والاختبارات المحلية غير Docker `30/30`، وVitest `6/6`، وPlaywright `2/2`. `npm run build` نجح وأنتج المسارات الثمانية المطلوبة مع Proxy.
- `dotnet build HARDWARE_STORE.slnx --no-restore -m:1` نجح أخيرًا: 0 أخطاء وتحذير واحد `NU1900` بسبب تعذر جلب بيانات فحص ثغرات NuGet من الشبكة.
- Playwright غطى التسجيل، تأكيد البريد، رفض إعادة استهلاك الرمز، رفض كلمة مرور خاطئة، Login، إبطال جلسة ثانية عند تغيير كلمة المرور، Logout Current، طلب الاستعادة، Reset، الدخول بكلمة المرور الجديدة، وLogout All. لم يُعد اختبار ناجح بعد ذلك دون تغيير متعلق.
- لا Migration جديدة ولا تغيير مخطط. لم تُطبق أي Migration على قاعدة دائمة. PostgreSQL استُخدمت فقط عبر Testcontainers وحاوية اختبار مؤقتة؛ الموارد المؤقتة أزيلت بعد التحقق والموارد القائمة لم تُمس.
- لا توجد خدمة Caddy في `compose.yaml` ولا Rewrite إنتاجي في Next.js. لذلك Same-Origin للإنتاج بقي تجهيز نشر خارجيًا، إلى جانب اختيار مزود البريد وأسراره وعناوين Proxy الموثوقة. التسجيل في Production مغلق.
- نسخة رجوع هذه الجولة: `.backups/m3_2e-resume-2026-11-05-pre/`. الملفات المضافة بعدها موثقة في تقرير التسليم داخل المحادثة.
- أرشيف المصدر النهائي: `.backups/HARDWARE_STORE_M3.2e_SOURCE_2026-11-05.zip`، وتم فحص سلامته وخلوّه من `.env` و`.packages` و`.dotnet-home` و`node_modules` و`bin` و`obj` ومخرجات الاختبارات؛ يحتوي 230 عنصرًا.

## [سجل تاريخي قبل تشغيل بوابة PostgreSQL] تنفيذ M3.2d — 5 نوفمبر 2026
- نُفذت Email Verification وتعيين كلمة المرور في POST واحد، Resend Verification، Password Reset Request/Consumption، Password Change، Logout Current، وLogout All.
- التزم التنفيذ بـPBKDF2-HMAC-SHA512 عبر Microsoft PasswordHasher IdentityV3 و220,000 iteration، مع `IPasswordHashingService` للترقية. سياسة كلمة المرور 15–128 Unicode Scalar Values، دون Trim أو Normalization أو فرض تركيب.
- `TimeProvider` UTC هو مصدر الوقت الموحد. الرموز عشوائية 32 بايت ويخزن منها SHA-256 فقط. الاستهلاك أحادي عبر Transaction وقفل الحساب ثم الرمز، و`authorization_version` وإبطال الجلسات المطلوبة يحدثان ذريًا.
- أضيف نقل بريد مؤجل داخل العملية للرموز والإشعارات الأمنية، فلا تنتظر استجابة Reset Request مزود البريد. فشل الإرسال لا يلغي الحالة المحفوظة، ويُسجل حدث فشل منفصل.
- أضيف Audit ضمن Transaction العمليات الحساسة عبر Connection/Transaction مشتركة، دون Outbox أو Migration.
- أضيفت سياسات IP موحدة: Login 5/دقيقة، Registration 10/ساعة، Email Verification 20/10 دقائق، Resend 10/ساعة، Reset Request 10/ساعة، Reset Consumption 20/10 دقائق؛ وحد الحساب 60 ثانية و5/24 ساعة للرموز.
- الملفات الأساسية الجديدة: `IdentityAccountLifecycleService.cs`, `IdentitySessionService.cs`, `IdentityEmailDispatcher.cs`, `PasswordPolicy.cs`, `IdentityAccountLifecycleEndpoints.cs`, و`IdentityRateLimiting.cs`. أضيف مرجع Identity → Auditing لدعم الكتابة الجذرية داخل المعاملة.
- نُفذت 3 اختبارات Unit جديدة، و13 حالة API/PostgreSQL موسومة `RequiresDocker`.
- آخر Build كامل: `dotnet build HARDWARE_STORE.slnx --no-restore -m:1 -t:Rebuild` نجح بـ0 أخطاء وتحذير واحد `xUnit2031` في اختبار RBAC سابق. ظهر `NU1900` في محاولات Build جزئية سابقة بسبب تعذر بيانات فحص NuGet، ولم يظهر في Rebuild النهائي.
- الاختبارات المحلية باستخدام `--filter "Category!=RequiresDocker"` نجحت `27/27`. آخر Benchmark للـPassword Hashing بعد التنفيذ: median `131.51 ms`، p95 `174.87 ms`، n=12.
- في تلك الجولة لم تُشغّل Docker أو Testcontainers، ولم تُطبق أي Migration. هذا القيد رُفع لاحقًا ضمن تصريح M3.2e، حيث نجحت بوابة M3.2d على PostgreSQL.
- نسخة الرجوع قبل التنفيذ: `.backups/m3_2d-implementation-2026-11-05-pre/`.

## تثبيت تصميم M3.2d — 5 نوفمبر 2026 (توثيق فقط)
- اعتُمد في التصميم PBKDF2-HMAC-SHA512 عبر Microsoft `PasswordHasher<UserAccount>` بوضع IdentityV3 وبعدد `220,000` iteration كخيار V1 الحالي، مع الإبقاء على `IPasswordHashingService` للترقية. لا تُضاف مكتبة Argon2 أو أي مكتبة تجزئة خارجية في M3.2d. هذا القرار يوثق التنفيذ السابق بدل تركه مخالفًا للتصميم المكتوب.
- اعتُمد `TimeProvider` مصدرًا موحدًا بالـUTC لصلاحية الجلسات والرموز، مع شرط مزامنة ساعة خادم API عبر NTP أو مصدر موثوق. لا تستخدم قرارات الانتهاء وقت قاعدة البيانات ما دام V1 يعمل بـAPI instance واحدة.
- ثُبت تصميم M3.2d API-only: تأكيد البريد وتعيين كلمة المرور في POST واحد، Resend، Password Reset، Password Change، Logout، Logout All، وإبطال الجلسات وزيادة `authorization_version` ذريًا.
- لا Migration مطلوبة لـM3.2d: جداول `identity_tokens` و`auth_sessions` الحالية تحتوي الحقول والفهارس اللازمة. لم يُضف `password_changed_at_utc` لأن Audit والحقول الحالية تكفي، وتجنب الحقل Migration غير الضرورية.
- [سجل تصميم سابق تم اعتماده لاحقًا] Resend `10/IP/hour` مع `60s` و`5/24h` للحساب؛ Reset Request `10/IP/hour`؛ Reset Consumption `20/IP/10m`. اعتمد المالك لاحقًا حد Reset نفسه لكل حساب و`128` Unicode Scalar Value كحد أقصى لكلمة المرور.
- تصحيح تتبع الأدلة: الرقمان `17/17` و`7/7` المنسوبان إلى M3.2a في `MODEL_HANDOFF.md` لا يوجد لهما تفصيل أو أمر تشغيل داخل `PROGRESS.md` الحالي، لذلك لا يُستخدمان كدليل جديد. ملف `PasswordHashingTests.cs` الحالي يحتوي خمس حالات، إحداها Benchmark؛ النتيجة المسجلة `4/4` لا تحدد فلتر التشغيل. لم تُعد هذه الاختبارات في الجولة الحالية.
- رُوجعت وصُححت الملفات `docs/database/DATABASE_DESIGN_ERD.md`, `docs/api/API_DESIGN_SECURITY.md`, `docs/architecture/MODULAR_MONOLITH_ARCHITECTURE.md`, `docs/architecture/USERS_ROLES_PERMISSIONS.md`, `docs/requirements/REQUIREMENTS_TRACEABILITY.md`, `MODEL_HANDOFF.md`, `NEXT_TASK.md`, `OPEN_QUESTIONS.md`, و`PROGRESS.md` بتعديلات نقطية تحفظ السجل التاريخي. نسخ الرجوع قبل التعديل: `.backups/m3_2d-design-freeze-2026-11-05-pre/` و`.backups/m3_2d-design-sync-2026-11-05-pre/`.
- لم يُعدل كود التطبيق أو قاعدة البيانات أو الاختبارات. لم تُشغّل Builds أو Tests أو Docker أو Migrations، ولم يُتصل بأي قاعدة بيانات في جولة تثبيت التصميم.

## نقطة الاستئناف الحالية — تسليم M3.2c (5 نوفمبر 2026)
- M3.2c Customer Registration اكتملت وظيفيًا ضمن نطاقها، ونتائجها معتمدة للتسجيل في السجل: PostgreSQL/Testcontainers `12/12 Passed`، موزعة إلى Migration Preflight `3/3`، تسجيل وتزامن وفشل إرسال البريد وRate Limiting `6/6`، Migration الأساسي `1/1`، ورفض Login للحسابات غير المؤكدة/المعلقة بلا PasswordHash `2/2`.
- Migration `20261105053622_M3_2c_CustomerRegistration` طُبقت واختُبرت داخل PostgreSQL 17 المؤقتة عبر Testcontainers فقط. لم تُطبق على قاعدة دائمة نتيجة هذه الجولة. اختبارات ما قبل الترحيل أثبتت توقفها عند التصادمات أو الحساب المؤكد بلا PasswordHash دون دمج/حذف/تعديل الحسابات القديمة، ونجاح حالة تطابق Username وEmail للحساب نفسه.
- آخر Build تشخيصي انتهى بـExit Code `0`، مع التحذيرين المعروفين `NU1900` (تعذر جلب بيانات فحص الثغرات من NuGet) و`xUnit2031` (تحذير Analyzer في اختبار). لا يُعاد Build أو الاختبارات الناجحة بلا تغيير مؤثر.
- [سجل تاريخي قبل M3.2e] كان التنفيذ يشمل Registration API و`identity_tokens` و`user_account_login_identifiers` وFake/Test Email Provider. لم تكن صفحة تسجيل/تأكيد أمامية منفذة في Next.js في تلك الجولة؛ أُضيفت لاحقًا ضمن M3.2e.
- التسجيل غير متاح في Production. يظل مغلقًا حتى اكتمال M3.2d وM3.2e، ونجاح اختبارات التدفق المتكامل، وفعالية حد التسجيل المعتمد `10` طلبات لكل IP في الساعة، وموافقة عبدالملك الصريحة على فتحه.
- [سجل تاريخي] كانت M3.2d الخطوة المقترحة التالية في تلك اللحظة. نُفذت واجتازت لاحقًا بوابة PostgreSQL كما هو مسجل في قسم M3.2e أعلى الملف.
- Git غير مهيأ، ولم يُنفذ `git init`. `.gitignore` يستبعد `.env` و`.env.*` ويستثني `.env.example` فقط. نسخة رجوع هذه الجولة: `.backups/final-model-handoff-2026-11-05-pre/`. النسخ السابقة محفوظة دون استبدال.
- التحذير التشغيلي المؤجل: تحقق Production الفعلي من استمرارية Data Protection Key Ring والشهادة والنسخ الاحتياطي والاستعادة مطلوب ضمن تجهيز النشر؛ تحذير Development موثق ولا يغيّر نجاح اختبارات M3.2c.
- لا ZIP جديد لهذه الجولة؛ الأرشيف WIP السابق لا يتضمن بالضرورة أحدث الملفات، ولا يُعد مرجع الاستمرارية. مرجع الحالة الأساسي الآن `MODEL_HANDOFF.md` مع هذا السجل و`NEXT_TASK.md`.

## سجل التنفيذ السابق — M3.2b (مكتمل ومعتمد؛ التفاصيل التالية تاريخية)
- نسخة الرجوع السابقة محفوظة في `.backups/m3_2b-pre-2026-11-05/`. تم فحص محتوياتها ومقارنة SHA-256؛ ملفات الحالة `PROGRESS.md` و`NEXT_TASK.md` تطابق النسخة السابقة قبل تحديث هذه الجولة. أضيفت نسخة رجوع غير مستبدلة لـ`IdentityLoginEndpoints.cs` قبل تعديل ترتيب Audit/SignIn.
- التغييرات الموجودة تشمل Normalized Username/Email والفهارس الفريدة وحقول Account Lockout، `PasswordHashingService` و`IdentityLoginService`، Login API، IP Rate Limiting، استدعاء Audit، وMigration `20261105033338_M3_2b_LoginSecurity` المولدة عبر EF CLI. أضيف Assertion لمعرف Migration الجديد ضمن اختبار Migration الموجود.
- مراجعة Login أكدت أن CSRF مطلوب على POST، والـRate Limiter يعمل بعد Forwarded Headers/Routing ويستخدم `RemoteIpAddress`، والاستجابة العامة لبيانات الدخول المرفوضة موحدة. اختبارات API تستخدم `RuntimeConnectionString` بدل اتصال مالك Fixture. يكتب `Identity.Login.Succeeded` بعد نجاح `SignInAsync` فقط؛ عند فشل Audit بعد إصدار التذكرة، يحدد `DatabaseTicketStore` مفتاح الطلب داخليًا في `HttpContext.Items` لإلغاء الجلسة نفسها، وتحذف Cookie الصادرة. لا تُسجل كلمات المرور أو الـHashes أو البريد أو Cookie في SafeMetadata.
- Migration تضيف حقول العدادات والتطبيع، تتحقق من تصادمات Username/Email قبل الفهارس الفريدة، وتملأ الأعمدة الطبيعية ثم تجعلها NOT NULL. اختبار Migration المنفرد ذي Assertion المحدث نجح على PostgreSQL مؤقتة؛ لم تُطبق أي Migration على قاعدة دائمة.
- اختبار `Successful_login_audit_failure_revokes_request_session_and_removes_cookie` نجح: فشل Audit رجّع 503، أُلغي سجل الجلسة، ولم تصدر Cookie صالحة أو يسجل حدث نجاح مضلل.
- Build أخير بعد تعديلات Audit/Test Host: `dotnet build tests/HardwareStore.Api.Tests/HardwareStore.Api.Tests.csproj --no-restore -m:1 --verbosity minimal` نجح، 0 أخطاء، وتحذيران: `NU1900` لتعذر جلب بيانات ثغرات NuGet، و`xUnit2031` في اختبار RBAC سابق غير متعلق.
- اختبارات `PasswordHashingTests`: 4/4 نجحت في التشغيل المستهدف السابق. لا تُعاد دون تغيير متعلق بها. نتائج القياس المحلي: median 108.34 ms وp95 128.41 ms، n=12؛ هذا قياس بيئة التطوير وليس قياس نشر.
- PostgreSQL: اختبارات `PostgreSqlIdentityLoginTests` نجحت 9/9 خلال 16.627 ثانية؛ اختبار Migration المنفرد نجح 1/1 خلال 6.837 ثانية (مدة الاختبار 301 ms). كل التشغيل كان على PostgreSQL 17 مؤقتة من Testcontainers. جرد Docker قبل/بعد: 7 حاويات قائمة، 4 Volumes، 8 Networks؛ القوائم والمعرّفات مطابقة. حاويتا PostgreSQL وResource Reaper المؤقتتان أزيلتا بعد كل Fixture، ولم تُمس الموارد القائمة. لا Compose أو تنظيف عام أو اتصال ناجح بقاعدة دائمة.
- فشل المجموعة السابقة: `IdentityLoginApiFactory` مرر الإعداد عبر Configuration فقط، لكن سياق Login الفعلي وصل إلى `localhost:5432` وقاعدة `hardware_store` دون كلمة مرور. سجلات الخطأ عند `BeginTransactionAsync` تثبت أن API فشل قبل فحص حالة الحساب أو بلوغ Audit؛ لذلك لا تثبت تلك النتائج عيبًا مستقلًا في Locked Account أو Audit، ولم يصدر خلالها Session/Cookie عبر هذا المسار. الاتصال الافتراضي لم ينجح، لكن جرت محاولة إليه.
- المعالجة والتحقق: Test Host يعيد تسجيل `IDbContextFactory<IdentityDbContext>` و`IDbContextFactory<SecurityAuditDbContext>` و`HardwareStoreDbContext` مباشرةً باستخدام سلسلة Fixture. `CreatePreflightedClient` قارن Host وPort وDatabase الفعلية بهذه السياقات مع إعداد Fixture قبل الطلب؛ اجتازت جميع اختبارات API هذا الفحص، ولم يظهر اتصال بـ`hardware_store` الافتراضية. القيم الفعلية طابقت Host `localhost` والمنفذ المخصص وقاعدة `identity_m3_1_test`، دون طباعة كلمة المرور.
- صححت اختباري `Expired_failure_window_starts_a_new_counter_window` و`Tenth_concurrent_failure_locks_for_five_minutes_without_losing_updates` لاستخدام البريد نفسه المحفوظ في الحساب عند تسجيل الدخول. لا تعديل في منطق المنتج.
- Build الخاص بهذه الجولة نجح بـ`dotnet build tests/HardwareStore.Api.Tests/HardwareStore.Api.Tests.csproj --no-restore -m:1`، صفر أخطاء وتحذيران معروفان (`NU1900`, `xUnit2031`). محاولة Build أولى فشلت بسبب namespace خاطئ للاختبار، ثم صُحح ونجح Build النهائي.
- الحالة التالية: مراجعة المالك لنتائج M3.2b. لا تُعِد PasswordHashing 4/4 أو اختبارات M3.1/M3.2a الناجحة، ولا تبدأ M3.2c قبل اعتماد جديد.

## المراجعة النهائية المحدودة — M3.2a (5 نوفمبر 2026)
- Data Protection: Development يستخدم Key Ring منفصلًا ودائمًا على مسار/Volume تطوير، لكن فرع التسجيل لا يفعّل Encryptor؛ لذلك تحذير `No XML encryptor configured` حقيقي من ناحية تخزين مفاتيح Development دون تشفير at-rest، ومتوقع وفق هذا الإعداد. وثيقة الأمان تلزم التشفير الصريح للإنتاج؛ لا يوجد دليل على تسرب Production keys إلى Development.
- Production: بدء التشغيل يرفض غياب `DataProtection:KeyRingPath` أو شهادة PFX وكلمة مرورها، ويستخدم `PersistKeysToFileSystem` مع `ProtectKeysWithCertificate` و`ApplicationName` ثابت باسم البيئة. هذا يثبت إعداد التشفير والحفظ إلى المسار، لكنه لا يثبت أن المسار مركّب على تخزين دائم/مشترك خارج الحاوية؛ ذلك يتطلب تحقق إعداد النشر قبل الإطلاق، وهو ليس عائقًا لإغلاق الكود المحلي.
- اختبار `Migration_creates_only_identity_tables_and_exact_permission_catalog` يتضمن Assertions صريحة لمعرّفات M3.1 وM3.2a في سياقي Identity وAuditing. محاولة التحقق لم تصل إلى نتيجة بسبب بيئة Testcontainers؛ لا يوجد دليل على فشل Assertion. تظل Assertions محفوظة لتُشغّل في أول دورة PostgreSQL مناسبة، ولا تعيد الاختبارات السابقة 7/7.
- Build التشخيصي الوحيد (`--no-restore -m:1 --verbosity diagnostic`) نجح خلال 8.81 ثانية، مع تحذيرين: `NU1900` لتعذر الوصول لمصدر بيانات الثغرات في NuGet، و`xUnit2031` في اختبار RBAC سابق. لا أخطاء. Binary Log: `.backups/m3_2a-final-review-2026-11-05-pre/m3_2a-tests-build.binlog`.
- سبب محاولتي Build السابقتين اللتين انتهتا برمز 1 دون أخطاء غير محسوم؛ لم يكن لهما Binary Log. نجاح التشغيل المتسلسل الحالي لا يثبت وحده أن التوازي كان السبب.
- Data Protection: تحذير Development موثق ولا يتطلب تعديلًا الآن. التحقق التشغيلي من Key Ring وشهادة Production والـBackup/Restore مؤجل لتجهيز النشر.
- نسخة الرجوع لتحديث الحالة: `.backups/m3_2a-acceptance-2026-11-05-pre/`. الحالة التالية M3.2b؛ لا تبدأ Login أو أي عمل ضمنها حتى اعتماد نطاق التنفيذ صراحةً. لا تبدأ M3.3.

## نقطة التنفيذ الحالية — M3.2a (4 نوفمبر 2026)
- تصريح التنفيذ الحالي يقتصر على جلسات Cookie خادمية، `ITicketStore`، CSRF، Data Protection، Same-origin rewrite للتطوير، وأساس `security_audit_events`. لم يبدأ Login أو Register أو Password Hashing أو M3.2b/M3.3.
- أضيف `auth_sessions` بمفتاح `SHA-256` لمعرف عشوائي 32 bytes، وتذكرة محمية بـData Protection، وإنتهاء idle/absolute وإلغاء منطقي وتحديث شرطي يفشل عند الإلغاء/تغير `authorization_version` أو `rbac_revision`. أضيف صف singleton لـ`authorization_state`.
- أضيف `security_audit_events` ضمن Auditing، بمرجعين اختياريين إلى `user_accounts` مع `ON DELETE RESTRICT`، وواجهة كتابة لا تعرض Update/Delete. دور التطبيق يُجهز بـ`INSERT/SELECT` فقط على سجل الأمن؛ دور مالك منفصل لا يملك Login.
- تعديل Compose يضيف `APP_DB_PASSWORD` ودور تشغيل `hardware_store_app` وVolume مفاتيح Development، مع تهيئة الأدوار عند إنشاء PostgreSQL جديدة. سكربت `docker/postgres/init/010-runtime-roles.sh` لا يعمل تلقائيًا على Volume موجود مسبقًا؛ يلزم إعداد الأدوار لها لاحقًا دون حذف البيانات. لم أقرأ أو أعدل `.env` ولم أشغل Compose أو Docker.
- `next.config.ts` يضيف Rewrite فقط أثناء `next dev` إلى `API_ORIGIN`، دون تعديل مسارات الإنتاج. Production Cookie يستخدم اسم `__Host-` وSecure، وData Protection يفرض Key Ring وشهادة عند Production startup؛ Development له حلقة مفاتيح منفصلة ودائمة خارج ملفات المصدر.
- Migrations مولدة بـ`dotnet ef`: `M3_2a_AuthenticationSessions` و`M3_2a_AuthorizationStateSingleton` في Identity، و`M3_2a_SecurityAuditEvents` في Auditing. فحص SQL المولّد أظهر فقط `authorization_version`, `auth_sessions`, `authorization_state` وقيد singleton؛ Audit ينشئ `security_audit_events` ويربط `user_accounts` بـRESTRICT. لم تُطبق أي Migration على قاعدة فعلية.
- Builds النهائية: Identity نجح 0 أخطاء/0 تحذيرات؛ Auditing وAPI وTest project نجحت بلا أخطاء. تحذير NU1900 بسبب تعذر الوصول لمصدر vulnerability data، وتحذير xUnit2031 قائم في اختبار RBAC سابق.
- محاولة Restore لمشروع الاختبارات انتهت برمز 1 بعد "Determining projects to restore" دون رسالة خطأ؛ Build نجح باستخدام الأصول الموجودة.
- الاختبارات المحلية المستهدفة بدأت ولم تعرض اسم اختبار أو نتيجة بعد نحو 30 ثانية؛ أوقفت المحاولة ولم أعدها. لا توجد نتيجة نجاح مسجلة. Testcontainers/PostgreSQL لم تُشغّل احترامًا لشرط موافقة Docker المنفصلة.
- محاولة `dotnet ef migrations remove` لم تصل لقاعدة البيانات بسبب منع loopback إلى `localhost:5432`؛ لم يحصل أي تعديل قاعدة. لتجنب المحاولة نفسها، أضيف قيد singleton كـMigration لاحقة مولدة من EF CLI.
- نسخة الرجوع قبل التعديل محفوظة في `.backups/m3_2a-pre-2026-11-04/` (17 ملفًا). Git غير مهيأ. M3.2a غير مكتملة حتى نجاح اختبارات PostgreSQL ومراجعة الاختبارات المستهدفة.

## الحالة التنفيذية الحالية — M3.1 (4 نوفمبر 2026، تاريخية ومعتمدة)
- أُعيد التحقق من `.config/dotnet-tools.json`؛ `dotnet tool restore` نجح، و`dotnet tool list --local` و`dotnet ef --version` يعرضان `dotnet-ef 10.0.4`.
- إصدارات EF الفعلية هي `Microsoft.EntityFrameworkCore 10.0.4` و`Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`. أضيف `Microsoft.EntityFrameworkCore.Design 10.0.4` إلى API Host بـ`PrivateAssets=all` و`IncludeAssets` القياسي؛ لم تُحدّث إصدارات EF/Npgsql.
- Identity Module يحتوي `IdentityDbContext` وكيانات `Person`, `UserAccount`, `Role`, `Permission`, `UserRole`, `RolePermission` وعداد `PersonCodeCounter`. نموذج EF يعرّف سبعة جداول ضمن M3.1 فقط، وعلاقات `ON DELETE RESTRICT`، وفهارس الفرادة والعلاقات المعتمدة.
- `permissions` تُزرع من معرفات `PermissionCatalog` الـ32 فقط، ودور `Customer` موجود دون أي `role_permissions`. لا يوجد إدخال تلقائي عند تشغيل API.
- تم تصحيح علاقة واحدة في ERD لتطابق القرار المعتمد والقيد الفريد: `Person` له صفر أو حساب واحد كحد أقصى. نسخة ما قبل تعديل ERD في `.backups/m3_1-erd-cardinality-2026-11-04-pre/`.
- `PersonCodeGenerator` يحوّل وقت `TimeProvider` من UTC إلى `Business:TimeZone` (`Africa/Cairo` افتراضيًا، قابل للتجاوز عبر إعدادات ASP.NET Core مثل `Business__TimeZone`) ثم يخصص العداد بأمر PostgreSQL parameterized `INSERT .. ON CONFLICT .. RETURNING` عبر اتصال EF المفتوح وداخل معاملة تضم إنشاء الشخص. يوقف التوليد عند 99,999، ولا يحذف العداد عند حذف شخص. UUID الداخلي يُنشأ بـUUIDv7.
- أُنشئت Migration عبر EF CLI: `src/Modules/Identity/Persistence/Migrations/20261104134655_M3_1_IdentityPersistence.cs` مع Designer وModel Snapshot. مراجعتها أظهرت الجداول السبعة المطلوبة فقط، بلا جداول تجارية. اختبرت Migration على PostgreSQL مؤقتة عبر Testcontainers؛ أكد اختبارها ظهور رقم Migration ووجود الجداول السبعة فقط. `dotnet ef migrations has-pending-model-changes` أفاد بعدم وجود تغييرات غير مدرجة في الـMigration.
- الاستعادة نجحت لمشروعي API والاختبارات. Build API نجح بصفر تحذيرات/أخطاء. Build الاختبارات نجح بصفر أخطاء؛ ظهر تحذير xUnit2031 واحد في إحدى عمليات البناء الكاملة السابقة داخل `PermissionAuthorizationTests.cs`، بينما آخر بناء بعد تعديل M3.1 أبلغ صفر تحذيرات.
- اختبارات M3.1 المحلية: `6/6` ناجحة، وتشمل شكل النموذج وعدد الجداول، تطابق Seed مع الكتالوج، دور Customer بلا صلاحيات، قيود metadata، توقيت `Africa/Cairo` عبر حد الشهر والتوقيت الصيفي، ورفض المنطقة غير الصحيحة.
- اختبارات PostgreSQL/Testcontainers: `7/7` ناجحة؛ كل اختبار منفصل: `Migration_creates_only_identity_tables_and_exact_permission_catalog`, `Duplicate_accounts_and_role_assignments_are_rejected`, `Person_deletion_is_restricted_while_an_account_exists`, `Concurrent_person_code_allocation_has_no_collisions`, `Person_code_is_not_reused_after_person_deletion`, `Person_insert_failure_rolls_back_the_monthly_counter`, `Monthly_person_code_limit_stops_at_99999`.
- الاختبارات المحلية دون Docker: `6/6` ناجحة بعد جولة PostgreSQL، وتشمل النموذج وكتالوج الصلاحيات والمنطقة الزمنية وحدود الشهر والتوقيت الصيفي والتحقق من المنطقة.
- فشل تشغيل PostgreSQL الأول في اختبار التحقق من خلو جدول `role_permissions` بالكامل، لأن اختبار تعيين منفصل أضاف صلاحية لدور اختباري. ضُيّق الفحص ليتحقق من عدم وجود صلاحيات لدور `Customer` تحديدًا؛ ثم نجحت الاختبارات السبعة جميعًا. لا يوجد فشل متبقٍ في الجولة النهائية.
- شُغّل Docker Desktop لهذا النطاق فقط. جرد ما قبل/بعد الاختبار محفوظ في `.backups/m3_1-docker-baseline-2026-11-04/`: لم تختفِ أي من الـ4 Containers أو الـ3 Volumes أو الـ6 Networks السابقة، ولم يبقَ أي Container أو Volume أو Network جديد من Testcontainers. لم تُستخدم Compose ولم تُوقف أو تُحذف موارد قائمة. Docker Desktop بقي يعمل.
- نسخ الرجوع: `.backups/m3_1-implementation-2026-11-04-pre/`, `.backups/m3_1-postgresql-results-2026-11-04-pre/`, ونسخ الاختبار/العداد وERD المذكورة في سجل التنفيذ. لم يُعِد تشغيل اختبارات RBAC السابقة 7/7.
- Git غير مهيأ في جذر المشروع؛ لا يوجد Git Diff لهذه الجولة.
- حالة M3.2 الحالية موثقة في سجل الاعتماد أدناه: التصميم معتمد، والتنفيذ ينتظر تصريحًا صريحًا منفصلًا؛ لا تبدأ M3.3.

## سجل تحديث تصميم M3.2 الأول — تاريخي
- استُبدل وصف JWT/Refresh Tokens القديم في وثيقة API بتصميم Cookie Sessions خادمية عبر ASP.NET Core Cookie Authentication و`ITicketStore` مدعوم بقاعدة البيانات. لا توجد جلسات أو APIs مصادقة منفذة حتى الآن.
- [سجل تاريخي] وثّق التصميم Cookie `HttpOnly` و`Secure` في الإنتاج و`SameSite=Lax`، وCSRF token، ومسار Same-origin عبر Next.js Rewrite محليًا وCaddy إنتاجيًا. أُضيف Rewrite للتطوير لاحقًا في M3.2a؛ Caddy ما زال غير مهيأ في Compose.
- حُددت كيانات/حقول مقترحة لـ`auth_sessions` و`identity_tokens` وتوسعة `user_accounts`، ودورة الإبطال والتحقق وتحديث الصلاحيات، ونطاق Password/Registration/Email Verification/Owner Bootstrap وواجهاتها واختباراتها.
- [سجل تاريخي] تم تثبيت قيم جلسات Customer (14 يوم خمول/30 يوم حد أقصى) وAdmin (30 دقيقة/8 ساعات)، وعرض Argon2id كخط أساس. القرار الحالي هو PBKDF2-HMAC-SHA512 بوضع IdentityV3 و220,000 iteration وفق القسم الحالي أعلى الملف.
- [سجل تاريخي، superseded] كانت قرارات التطبيع والبريد والحماية وbackoff وسياسة الجلسة قيد المراجعة في تلك الجولة؛ حُسمت القرارات ذات الصلة في M3.2 والتصميم الحالي.
- وثائق التصميم المحدثة: `docs/api/API_DESIGN_SECURITY.md` و`docs/architecture/USERS_ROLES_PERMISSIONS.md`. نسخة الرجوع قبل التعديل: `.backups/m3_2-auth-design-2026-11-04-pre/`. لم تُشغّل Builds أو اختبارات أو Docker، ولم تتغير ملفات التطبيق.
- الخطوة التالية: مراجعة التصميم المحدث واتخاذ قرارات النقاط المعلقة؛ لا يبدأ تنفيذ M3.2 دون اعتماد صريح مستقل.

## اعتماد تصميم M3.2 وقرارات الهوية والبريد — 4 نوفمبر 2026
- عُدلت وثيقة API لتخزين Hash فقط لمفتاح Session عشوائي 32 bytes، مع Ticket محمية بـData Protection في `auth_sessions`، وانتهاء Idle/Absolute وإلغاء منطقي وتحديث ذري آمن من سباق Renew/Logout.
- أصبح إبطال الصلاحيات يعتمد `user_accounts.authorization_version` لتغييرات الحساب و`authorization_state.rbac_revision` عامًا لتغييرات `role_permissions`. أي mismatch يبطل الجلسة والـPrincipal القديم ويطلب تسجيل الدخول مجددًا؛ لا يوجد إصدار لكل Role.
- ثُبت Key Ring دائم ومشفّر، و`ApplicationName` مختلف وثابت لكل بيئة، وإجراءات Backup/Restore وإبطال الجلسات/الرموز قبل إعادة فتح المرور.
- أضيف تصميم `security_audit_events` Append-only بصلاحيات DB محدودة، وربط نجاح Bootstrap بسجل الحدث في معاملة واحدة. Auditing ما زال بلا تنفيذ فعلي.
- ثُبت Caddy كـPublic Proxy؛ API يثق فقط في KnownProxies/KnownNetworks محددة، وRate Limiting يقرأ `RemoteIpAddress` بعد Forwarded Headers. لم يُضف إعداد Caddy فعلي.
- استُبدل اقتراح الجدول المستقل `auth_login_throttles` باستخدام Rate Limiter وحقول القفل المؤقت بالحساب مبدئيًا. قائمة الجداول تشمل `auth_sessions`, `identity_tokens`, `security_audit_events`, `authorization_state` وتوسعات `user_accounts`.
- [سجل تاريخي، superseded] كان `TopSecret.Cryptography.Argon2 2.2.0` مرشح التقييم الأول فقط. القرار الحالي لـV1 هو PBKDF2-HMAC-SHA512 بوضع IdentityV3 و220,000 iteration، ولا Argon2 dependency.
- اعتمد المالك تطبيع Username وEmail وعدم حساسية حالة الأحرف مع حفظ القيم الأصلية، دون حذف `+tags` أو النقاط من البريد. فحص التصادم بعد التطبيع مطلوب قبل الفهارس ولا يدمج الحسابات تلقائيًا.
- اعتمد Abstraction مستقلة لإرسال البريد وFake/Test Provider للتطوير والاختبارات؛ اختيار المزود التجاري وأسراره مؤجل لتجهيز الإطلاق.
- [قرار سابق، superseded في M3.2c] كان يسمح بجلسة محدودة قبل التأكيد. القرار التنفيذي الأحدث: لا ينشأ Login Session قبل تأكيد البريد، ولا PasswordHash قبل التأكيد؛ راجع نقطة الحالة الحالية أعلى الملف.
- اعتمدت صلاحية رمز التحقق 24 ساعة، ورمز Reset 30 دقيقة، وإعادة الإرسال بعد 60 ثانية لكل حساب، واستخدامًا أحاديًا مع Hash فقط وإبطال السابق وحدود إرسال للحساب وIP.
- الملفات المعدلة: `docs/api/API_DESIGN_SECURITY.md`, `docs/architecture/USERS_ROLES_PERMISSIONS.md`, `OPEN_QUESTIONS.md`, `PROGRESS.md`, `NEXT_TASK.md`. نسخة الرجوع قبل هذه الجولة: `.backups/m3_2-owner-decision-finalization-2026-11-04-pre/`، مع الاحتفاظ بنسخة الجولة السابقة `.backups/m3_2-owner-security-decisions-2026-11-04-pre/`. لم تُشغّل Build أو Restore أو Tests أو Docker، ولم يتغير كود أو DB.
- عُدلت `OPEN_QUESTIONS.md` لتسجيل قرارات M3.2 المعتمدة وتصحيح Q6.1 إلى محسوم. الأسئلة التجارية الأخرى تظل كما هي.
- لا توجد قرارات مالك معلقة لإغلاق التصميم. اختيار مزود البريد التجاري وأسراره، عناوين/شبكات Caddy، مكان ومفتاح Key Ring، ومدة الاحتفاظ تفاصيل تُحسم عند تجهيز الإطلاق.
- الحالة: تصميم M3.2 معتمد؛ لا كود أو قاعدة أو Migration أو Build/Restore/Tests/Docker في هذه الجولة. الخطوة التالية تنتظر تصريحًا صريحًا منفصلًا لبدء M3.2؛ لا تبدأ تلقائيًا ولا تبدأ M3.3.

---

## 1. سجل سابق: تحديثات V3.2 (مرجع تاريخي، وليست شهادة إغلاق حالية)

كان هذا سجلًا لما وثقته الجولة السابقة. كشف التدقيق اللاحق ثغرات لم تكن قد أغلقت فعليًا في القيود/المعادلات، لذلك لا يُقرأ ادعاء الاكتمال أو أي علامة قديمة أدناه كإثبات تنفيذي. تحديثات V3.3 الحالية موضحة في القسم التالي.

سجلت الجولة السابقة التصحيحات التالية بوصفها نوايا تصميمية:

1. **توحيد مصدر الحقيقة للسيريالات وإلغاء الازدواجية:**
   - اعتماد حقل `serial_numbers.status` كمصدر وحيد ومطلق لدورة حياة القطعة الفيزيائية (`InStock`, `Reserved`, `InTransit`, `InQuarantine`, `Sold`, `ReturnedToSupplier`, `Scrapped`).
   - حذف الحقل المكرر `in_transit_transfer_item_id` من جدول `serial_numbers`؛ حيث يُسجل تخصيص السيريال حصرياً في جدول الربط التفصيلي `stock_transfer_item_serials(stock_transfer_item_id, serial_number_id)`.
   - حذف حقول الاستلام وحالة الاستلام المكررة من `stock_transfer_item_serials`؛ وتوثيق الاستلام بالوجهة حصرياً في `stock_transfer_receipt_item_serials(stock_transfer_receipt_item_id, transfer_item_serial_id, serial_number_id, condition_disposition)`.
   - فرض قيد الفرادة الصارم `uq_receipt_transfer_item_serial UNIQUE (transfer_item_serial_id)` لمنع استلام نفس السيريال المصروف في أكثر من حدث استلام واحد على الإطلاق.
   - تحديث مخطط Mermaid ERD بحذف العلاقة القديمة المباشرة ورسم جداول الربط التفصيلية الفعلية.

2. **تصحيح معادلات البضاعة قيد النقل ورصيد الحجر الفني:**
   - الفصل الرياضي الصارم بين:
     - الرصيد الفعلي المتاح للبيع بالمستودعات: $\sum \text{inventory\_batch\_balances.remaining\_quantity}$.
     - رصيد الحجر الفني بالمستودعات (التالف الواصل فعلياً أو المرتجع تحت الفحص): $\sum \text{inventory\_batch\_balances.quarantine\_quantity}$ (وللسيريالات: القطع بحالة `InQuarantine`).
     - صيغة V3.2 التاريخية استبدلت في V3.3 بالمفردات الموحدة: `D` المصروف، `A` المقبول، `R` المرفوض الواصل، `L` الفقد المعتمد، و`RemainingInTransit = D-A-R-L`.
     - إجمالي عهدة المتجر للدفعة: $\text{Total Batch Custody} = \text{Salable in Warehouses} + \text{Quarantined in Warehouses} + \text{In-Transit}$.
   - ضمان عدم اختفاء القطع المحجورة من عهدة المتجر وعدم احتسابها مرتين.

3. **سلامة المفاتيح الأجنبية المركبة وتفادي ثغرة PostgreSQL `MATCH SIMPLE`:**
   - توثيق المفاتيح الأجنبية بأعمدة NOT NULL حتمية لضمان مطابقة المستودع والمنتج دائماً دون تخطي الفحص عند وجود NULL في `variant_id`:
     - `FOREIGN KEY (stock_item_id, warehouse_id) REFERENCES stock_items(id, warehouse_id)`
     - `FOREIGN KEY (stock_item_id, product_id) REFERENCES stock_items(id, product_id)`
     - `FOREIGN KEY (batch_id, product_id) REFERENCES inventory_batches(id, product_id)`
   - إضافة تريجر دفاعي حتمي `trg_batch_balance_variant_integrity` للتحقق من تطابق الـ `variant_id` (NULL أو UUID) بين الدفعة والصنف ورصيد الدفعة.
   - منع الرصيد السالب في المتاح وفي الحجر: `CHECK (remaining_quantity >= 0 AND quarantine_quantity >= 0)`.
   - قفل الصفوف الحصري `SELECT ... FOR UPDATE` أثناء حركات الصرف لمنع التحديثات المتزامنة والصرف المزدوج.

4. **إعادة تصنيف مصفوفة صلاحيات التسويات واعتبارها مسألة معلقة (Q2.4):**
   - نقل مصفوفة الصلاحيات والحدود المالية لاعتماد عجز وفروق التحويلات إلى حالة **`Pending Owner Approval`** صريحة تحت المسألة الجديدة **`Q2.4`** في [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md).
   - إيضاح أن القيمة (2000 ج.م) وتوزيع الأدوار (مدير المستودعات مقابل المدير المالي) هي مجرد نموذج مقترح للدراسة وليست قراراً معتمداً.
   - إيضاح ارتباط تقييم تكلفة العجز بسياسة تقييم COGS المعلقة في **Q1.1**.
   - إزالة أي افتراض يختبر قيمة 2000 ج.م كقرار قطعي في استراتيجية الاختبارات.

5. **توحيد حالات أمر التحويل والإغلاق:**
   - اعتماد قائمة حالات معيارية موحدة لـ `stock_transfers.status`:
     `Draft`, `Approved`, `PartiallyDispatched`, `InTransit`, `PartiallyReceived`, `Completed`, `ClosedWithDiscrepancy`, `Cancelled`.
   - التمييز الصريح بين الإغلاق النظيف بكامل الكمية (`Completed`) والإغلاق بعد اعتماد محضر تسوية العجز المفقود وشطبه (`ClosedWithDiscrepancy`).

6. **حماية السجلات التاريخية والمالية (ON DELETE RESTRICT):**
   - استبدال كافة علاقات `ON DELETE CASCADE` في التحويلات وبنودها والاستلامات والتسويات بـ **`ON DELETE RESTRICT`** امتثالاً للمادة 8 من [AGENTS.md](AGENTS.md).
   - حظر الحذف الفيزيائي للمسودات بعد توليد رقم المستند وإلغاؤها كـ `Cancelled` حفاظاً على سجل التدقيق `audit_logs`.

7. **تحديث نطاق جداول V1 وسلامة الحركات:**
   - إضافة جداول `stock_transfer_item_serials` و `stock_transfer_receipt_item_serials` و `transfer_discrepancy_settlements` إلى قائمة جداول V1 الإلزامية في الكتالوج.
   - توحيد قائمة أنواع مستندات حركات المخزون المعيارية `reference_document_type`:
     `GoodsReceipt`, `Invoice`, `SalesReturn`, `StockTransfer`, `StockTransferReceipt`, `StockCountAdjustment`.
   - ربط حركة الصرف `TransferOut` بـ (`StockTransfer`, `stock_transfers.id`, `stock_transfer_items.id`).
   - ربط حركة الاستلام `TransferIn` بـ (`StockTransferReceipt`, `stock_transfer_receipts.id`, `stock_transfer_receipt_items.id`).

8. **نتائج فحص الروابط والإحصائيات الفعلية المؤكدة:**
   - فحص آلي شامل للنسخة المفكوكة في مجلد نظيف مستقل:
     - **107 رابط ملفات نسبي سليم بنسبة 100% (0 مكسور)**.
     - **41 رابط عنوان داخري Anchor سليم بنسبة 100% (0 مكسور)**.
     - **0 مسارات مطلقة للملفات**.

---

## 2. إغلاق V3.3 التوثيقي الجاري

1. توحيد معادلات التحويل: `D` للمصروف، `A` للمقبول، `R` للمرفوض الواصل للحجر، و`L` للفقد المعتمد؛ `RemainingInTransit = D - A - R - L`. المقبول وحده يزيد المتاح، والمرفوض وحده يزيد الحجر، والفقد المعتمد خارج العهدة. لا تُحتسب الفئات مرتين.
2. توثيق FK مركب يثبت تطابق سيريال الاستلام مع تخصيص الصرف، وعلاقات رأس/بنود التحويل والاستلام وتوافق كروت المخزون مع المستودعات والمنتج.
3. تعريف تخصيص نشط للسيريال وفهرس جزئي فريد `uq_active_serial_transfer`، مع قفل صف السيريال ومعاملة للإسناد والتحرير. ما يزال هذا تصميمًا يحتاج migrations واختبارات PostgreSQL.
4. إبقاء Q2.4 وQ2.5 وQ3.2 وQ3.3 وQ7.2 وسائر القرارات مفتوحة حسب ROADMAP؛ لا يوجد سقف 2000 ج.م أو دور اعتماد معتمد، ولم تتغير سياسة توقيت الصرف والفاتورة في Q7.2.
5. ربط سيناريوهات TESTING_STRATEGY بالضمان المتوقع، وبيان عدم تشغيل الاختبارات.
6. فحص الروابط وإنشاء المهارات الأربع: **تم** في الجولة السابقة. الأرشيف الذي يسجله التاريخ لم يكن موجودًا في مساحة العمل عند الفحص؛ أُعيد إنشاء الملف المرجعي بنفس الاسم `HARDWARE_STORE_DOCS_V3.3.zip` من وثائق Markdown الحالية، والتحقق من 28 مدخلًا وقراءة كل محتواها. لم يُنشأ رقم إصدار جديد.
7. **تحديث هذه الجولة:** أُنشئت المهارة الخامسة `security-and-data-protection`، وأضيفت لقائمة README. المهارة تحول المصادر الأمنية إلى خطوات مراجعة، وتطلب استخدامها مع `controlled-implementation` للوظائف الحساسة؛ وجودها لا يثبت أمان تطبيق غير موجود.
8. أُنشئت [مذكرة مراجعة تصميم مراجع حركات المخزون](docs/database/INVENTORY_MOVEMENT_REFERENCE_DESIGN_REVIEW.md)، وقورنت فيها بدائل Typed FKs المباشرة والسجل الموحد وTriggers. التوصية المقترحة Typed FKs المباشرة مع مصدر مستقل مشترك لتسويات الجرد والتخلص؛ وهي **غير معتمدة**. لم يتغير ERD ولم يُنفذ DDL.
9. **فجوات تمنع الإغلاق:** يلزم مراجعة المعماري المستقلة واعتماد صاحب المشروع لنموذج المصادر والعلاقات المقترح، وحسم القرارات التجارية المفتوحة. الوثائق التابعة محدثة للمراجعة دون اعتبار المقترحات معتمدة. لم يُعثر على الأرشيف السابق، فأُعيد إنشاء الملف المرجعي بالاسم نفسه من الوثائق الحالية؛ ولا تُعد V3.3 مغلقة.
10. **نطاق توصيل V1:** وثّق يدويًا اسم شركة الشحن وتحديث الحالة وسجل الموظف/الوقت، وجعل بيانات التتبع اختيارية؛ لا API أو GPS أو إدارة أسطول أو Webhooks. لم يتغير `Q7.2`.

## 2.1 توحيد الوثائق — 4 نوفمبر 2026

- وحّدت خريطة مصادر `inventory_movements` حسب نوع الحركة، مع التصريح بأن المراجع polymorphic الحالية لا تملك FKs فعلية، وأن خريطة Typed FKs وعمود `product_id` المساعد وهوية الحدث مقترحات تنتظر المراجعة المعمارية.
- أزيل الاعتماد على UNIQUE لرأس/بند/نوع الحركة بوصفه Idempotency عامًا؛ التوصية هوية حدث ثابتة مع إعادة استخدام `Idempotency-Key` و`Command Inbox/Idempotency Store`. لم تُكتب DDL أو Migrations.
- أُبقيت ضمانات `D/A/R/L` والتسوية المعتمدة كما هي، وأضيف توضيح أن `StockCountAdjustment` لا ينتج من تسوية غير معتمدة؛ Q2.4 ما زالت معلقة ولا مبلغ/صلاحية معتمدة.
- أضيفت `Q2.5` لصرف بند التحويل على شحنات متعددة، و`Q3.3` لتوزيع مرتجع الطلب على مخزن أو أكثر؛ كلاهما `Pending Owner Approval`، بأولوية قبل Milestone 5 و7 على الترتيب.
- أُبقي `Q7.2` كما هو وبقي توقيت الصرف والفاتورة في Delivery قرارًا معلقًا.
- كُتبت سيناريوهات اختبار تغطي الحركة والتوصيل اليدوي؛ لم تُنفذ اختبارات قاعدة بيانات أو تطبيق في هذه الجولة.
- فحص روابط Markdown وAnchors للوثائق المعدلة: **89 رابطًا و37 مرجع Anchor، ولا توجد مراجع مكسورة**.
- سُجلت نسخ رجوع للوثائق المعدلة تحت `.backups/documentation-consolidation-2026-11-04-pre/` قبل الكتابة.

## 2.2 سجل تاريخي: تصحيح سلامة مراجع الحركات — 4 نوفمبر 2026

- استكملت مذكرة مراجعة `inventory_movements` بمفاتيح مركبة تربط حركة التحويل بكارت المصدر/الوجهة، والدفعة برصيدها في الكارت والمستودع، والسيريال بدفعته الأصلية مع قيد كمية الوحدة الواحدة.
- ألزمت التوصية مصدرًا تفصيليًا لكل نوع حركة، وأصلحت حصرية المصدر في `TransferIn`، ووضحت أن إلزام الدفعة والتخصيص يمنع تجاوز FK المركب بسبب `MATCH SIMPLE`.
- صححت فقرة خصم الحزمة في `BUSINESS_RULES.md` لتعرض التوزيع التناسبي كخيار مقترح فقط وفق `Q3.2` المعلقة.
- حُفظت نسخ رجوع للملفات المعدلة والأرشيف الحالي تحت `.backups/V3.3-integrity-correction-2026-11-04-pre/`؛ وأُعيد بناء `HARDWARE_STORE_DOCS_V3.3.zip` من الوثائق الحالية بعد التعديل.
- فحص الروابط وAnchors تم بعد التعديل. لم تُشغّل اختبارات تطبيق أو قاعدة بيانات، ولم يُنفذ DDL أو Migration؛ تصميم المذكرة ما زال مقترحًا غير معتمد.
- **حالة وقت كتابة السجل (تاريخية):** كانت مراجعة المعماري واعتماد المالك بوابة مفتوحة قبل تصريح Foundation. لم تتغير أسئلة `Q2.4`, `Q2.5`, `Q3.2`, `Q3.3`, أو `Q7.2`.

## 2.3 استكمال Q3.2 وVariant وسيريالات التحويل — 4 نوفمبر 2026

- عُدلت فقرة المرتجع الجزئي في `ORDERS_INVOICES_RETURNS.md` لتعرض بدائل `Q3.2` بدون تطبيق التوزيع التناسبي، وعُدلت `TESTING_STRATEGY.md` بحيث ينتظر الاختبار خيار المالك ولا يفترض نتيجة مالية مسبقًا.
- استكملت مذكرة مراجعة الحركة بفجوات `variant_id`: وثقت Trigger الموجود بالفعل لتطابق variant في `inventory_batch_balances`، وغياب ضمان كامل لباقي الروابط وغياب `variant_id` من `inventory_movements`، واقترحت مفاتيح مركبة مع تحقق NULL آمن دون تعديل ERD.
- أضافت المذكرة سلسلة ربط مقترحة لسيريالات `TransferOut` من سجل تخصيص الصرف، ولسيريالات `TransferIn` من سجل الاستلام المرتبط بتخصيص الصرف الأصلي؛ أضيفت اختبارات رفض للمراجع المتقاطعة والتطابق الخاطئ، بدون فرض حركة واحدة لكل بند تحويل.
- حُفظت نسخ ما قبل التعديل للوثائق والأرشيف تحت `.backups/V3.3-variant-serial-integrity-2026-11-04-pre/`. أعيد بناء الأرشيف الحالي بالاسم نفسه والتحقق من محتوياته وبصماته.
- روابط/Anchors الوثائق المعدلة فُحصت؛ لم تُشغّل اختبارات تطبيق أو قاعدة بيانات ولم يُنفذ DDL أو Migration. ERD لم يتغير، والتصميم لا يزال بانتظار المراجعة المعمارية واعتماد المالك.
- **حالة وقت كتابة السجل (تاريخية):** كانت `Q3.2` و`Q2.4` و`Q2.5` و`Q3.3` و`Q7.2` معلقة، ولم يكن تصريح Foundation قد صدر بعد. تظل القرارات معلقة.

## 3. حالة الوثائق بعد التوحيد (مسودة للمراجعة والاعتماد)

| الوثيقة | المسار النسبي | الحالة الحالية |
|---|---|---|
| **تعليمات وقواعد العمل** | [AGENTS.md](AGENTS.md) | معتمد حاكم (لم يُعدل) |
| **الدليل الشامل** | [README.md](README.md) | مسودة نهائية محدثة ومتسقة |
| **سجل القرارات المفتوحة** | [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) | محدث؛ Q2.4 وQ2.5 وQ3.2 وQ3.3 وQ7.2 وغيرها معلقة حسب الأولوية |
| **قواعد وسياسات العمل** | [BUSINESS_RULES.md](BUSINESS_RULES.md) | محدثة؛ المصدر polymorphic ومقترحات العلاقات معلنة وغير معتمدة، والتوصيل V1 يدوي |
| **مصفوفة تتبع المتطلبات** | [docs/requirements/REQUIREMENTS_TRACEABILITY.md](docs/requirements/REQUIREMENTS_TRACEABILITY.md) | محدثة لتشمل تفاصيل Delivery V1 والقرارات Q2.5 وQ3.3 |
| **متطلبات المنتج والوظائف** | [docs/requirements/PRODUCT_REQUIREMENTS.md](docs/requirements/PRODUCT_REQUIREMENTS.md) | محدثة لتحديد نطاق التوصيل اليدوي V1 |
| **الهوية والأشخاص والصلاحيات** | [docs/architecture/USERS_ROLES_PERMISSIONS.md](docs/architecture/USERS_ROLES_PERMISSIONS.md) | مسودة نهائية مكتملة ومصححة |
| **معمارية المونوليث النمطي** | [docs/architecture/MODULAR_MONOLITH_ARCHITECTURE.md](docs/architecture/MODULAR_MONOLITH_ARCHITECTURE.md) | مسودة نهائية مكتملة (Invoicing وحدة مستقلة) |
| **تصميم قاعدة البيانات وERD** | [docs/database/DATABASE_DESIGN_ERD.md](docs/database/DATABASE_DESIGN_ERD.md) | خريطة حركة ومقترحات العلاقة/Idempotency محدثة ومعلّمة غير معتمدة؛ لا DDL أو تنفيذ |
| **تصميم API والأمان** | [docs/api/API_DESIGN_SECURITY.md](docs/api/API_DESIGN_SECURITY.md) | مسودة نهائية مكتملة ومصححة |
| **دورة الشراء والمخزون** | [docs/workflows/PROCUREMENT_SALES_INVENTORY.md](docs/workflows/PROCUREMENT_SALES_INVENTORY.md) | محدثة مع تعريف هوية الحركة ومصادرها وتعدد الاستلام؛ تفاصيل FK/dispatch لا تزال قيد الاعتماد |
| **دورة الطلبات والفواتير والتسليم** | [docs/workflows/ORDERS_INVOICES_RETURNS.md](docs/workflows/ORDERS_INVOICES_RETURNS.md) | محدثة بنطاق Delivery اليدوي V1 وفصل Q7.2 وسؤال المستودعات Q3.3 |
| **الإدارة المالية والخزائن** | [docs/financials/FINANCIAL_MANAGEMENT.md](docs/financials/FINANCIAL_MANAGEMENT.md) | مسودة نهائية مكتملة (تعليق خيارات تقييم المخزون Q1.1) |
| **مجمّع الحواسيب وقوالب التجميع** | [docs/pc_builder/PC_BUILDER_COMPATIBILITY.md](docs/pc_builder/PC_BUILDER_COMPATIBILITY.md) | مسودة نهائية مكتملة ومصححة |
| **مواصفات وتجربة الواجهات** | [docs/ui_ux/UI_UX_DESIGN_SPECIFICATIONS.md](docs/ui_ux/UI_UX_DESIGN_SPECIFICATIONS.md) | مسودة نهائية مكتملة ومصححة |
| **استراتيجية الاختبارات** | [docs/testing/TESTING_STRATEGY.md](docs/testing/TESTING_STRATEGY.md) | سيناريوهات الأعمال المستقبلية كما هي؛ بُنيت واختُبرت فقط Smoke Tests الخاصة بـFoundation |
| **النشر والتشغيل والنسخ الاحتياطي** | [docs/operations/DEPLOYMENT_OPERATIONS_BACKUP.md](docs/operations/DEPLOYMENT_OPERATIONS_BACKUP.md) | مسودة نهائية مكتملة ومصححة |
| **سجل التوصيات المعمارية (ADR)** | [docs/adr/ARCHITECTURE_DECISION_RECORDS.md](docs/adr/ARCHITECTURE_DECISION_RECORDS.md) | مسودة نهائية مكتملة ومصححة |
| **خارطة طريق التنفيذ (Vertical Slices)** | [ROADMAP.md](ROADMAP.md) | مصدر الحقيقة لترتيب المراحل الـ 12 |
| **وثيقة المهمة التالية** | [NEXT_TASK.md](NEXT_TASK.md) | اكتمل Foundation؛ التسليم ينتظر الاعتماد المعماري؛ Milestone 3 غير مصرح ببدئها |
| **سجل التقدم العام** | [PROGRESS.md](PROGRESS.md) | يسجل تنفيذ Foundation ونتائج الفحوص الفعلية والقيود المتبقية |

---

## 4. الخطوة التالية
اكتمل التنفيذ والتحقق الفني لنطاق Foundation، ويظل التسليم بانتظار المراجعة والاعتماد. لا تبدأ Milestone 3. أسئلة الأعمال ومقترحات `inventory_movements` تظل كما هي ولم تُحسم بهذا التصريح.

### 4.1 سجل تنفيذ Foundation — 4 نوفمبر 2026
- أُنشئ `HARDWARE_STORE.slnx` ومضيف `HardwareStore.Api` ومشروعات مشتركة وحدود Assemblies للوحدات التسع. لا يحتوي أي مشروع على كيانات أو Endpoints تجارية.
- أُعد اتصال EF Core/Npgsql، وفحوص `/health/live` و`/health/ready`، وProblem Details، وSerilog، والتحقق المدمج لـ Minimal APIs.
- أُنشئت واجهة Next.js App Router أساسية بـ React وTypeScript وTailwind، مضبوطة `lang=ar` و`dir=rtl`، دون صفحات وظيفية وهمية.
- أُعد `compose.yaml` لـ PostgreSQL 17 وAPI وWeb مع فحوص صحة، وأُضيف `.env.example` فقط. لم يُنشأ `.env` ولم تُستخدم أسرار حقيقية.
- أُعد xUnit وWebApplicationFactory وTestcontainers، وVitest وPlaywright، واختبارات Smoke أولية.
- الفحص الفعلي قبل التنفيذ: SDK `10.0.401` وRuntime `10.0.12`، Node `v24.22.1`، npm `11.19.1`، Docker CLI `29.9.2` وCompose `v5.6.0`. لا يوجد Git مهيأ.
- تمت استعادة NuGet وnpm. نجح `dotnet build HARDWARE_STORE.slnx --no-restore -m:1` بلا تحذيرات أو أخطاء، ونجح `dotnet test` باختبارين من xUnit، بينهم اختبار Testcontainers لاتصال PostgreSQL من غير إنشاء مخطط.
- نجح `npm test` (Vitest: اختبار واحد)، و`npm run build` بـNext.js/Webpack، واختبار Playwright على Chromium (اختبار واحد). `npm audit` وجد صفر ثغرات بعد تحديث Vitest إلى 5.1.0 وحذف ESLint الاختياري. `dotnet list package --vulnerable --include-transitive` لم يجد حزمًا معرضة.
- نجح `docker compose ... up -d --build --wait`: الخدمات الثلاث وصلت `healthy`، `/health/live` و`/health/ready` و`/api/health` رجعت سليمة، وعدّاد جداول `public` كان 0. نجح restart ثم health checks، ثم `down` مع الإبقاء على Volume.
- أول build بـTurbopack أخفق بسبب خطأ PostCSS worker في البيئة؛ ضبطت أوامر Next على Webpack المدعوم، وبعدها نجح build. `dotnet new` لم يعمل بسبب منع الكتابة في `.templateengine` تحت ملف المستخدم؛ تم إنشاء الملفات يدويًا. فحص Git أكد أنه غير مهيأ، ولم يتم تهيئته.
- `npm install` ضمن العمل ثبّت الاعتمادات، وPlaywright Chromium نُزّل مؤقتًا داخل `.playwright-browsers` لتشغيل E2E ثم حُذف. لم يُنشأ `.env` ولم تُستخدم أسرار حقيقية.
- حُفظت نسخ ما قبل التعديل للوثائق في `.backups/Foundation-M2-2026-11-04-pre/`.
- أُنشئ الأرشيف الكامل `HARDWARE_STORE_FOUNDATION_M2.zip` بعد استبعاد الاعتمادات المستعادة ومخرجات البناء والاختبار والنسخ الاحتياطية والأرشيفات السابقة؛ تم فتحه وقراءة محتوياته والتحقق من وجود ملفات Foundation وعدم وجود `.env` أو أسرار.
- فُحصت الروابط المحلية وAnchors في ملفات Markdown المعدلة للمهمة؛ لم تظهر مراجع محلية مكسورة. الأرشيف السابق `HARDWARE_STORE_DOCS_V3.3.zip` محفوظ دون تغيير.
- الاختبارات الفعلية: `dotnet build` (0 تحذيرات/أخطاء)، xUnit (2/2)، Vitest (1/1)، Next.js build، Playwright (1/1)، Compose config، وDocker Compose health/restart؛ PostgreSQL ظل بلا جداول عامة. لا Git diff لأن Git غير مهيأ.
- ملاحظة بيئية: أول تشغيل لـ.NET أنشأ شهادة HTTPS تطويرية غير موثوقة في ملف المستخدم؛ لم يتم تشغيل `--trust` ولم تُحذف الشهادة تجنبًا للمساس بشهادة سابقة.

### 4.2 استكمال مراجعة Foundation النهائية — 4 نوفمبر 2026
- عُدّل Dockerfile الخاص بـAPI بإضافة `USER app` في مرحلة Runtime فقط؛ صورة `mcr.microsoft.com/dotnet/aspnet:10.0` قبلت الإعداد.
- أُعيد بناء صورة API بنجاح. أثبت الفحص الفعلي أن المستخدم داخل الحاوية `app`، وحالة Docker Health Check هي `healthy`، وأن `/health/ready` أعاد HTTP 200 و`Healthy`.
- وُحّدت الحالة في ROADMAP وNEXT_TASK وPROGRESS وREADME: تنفيذ Milestone 2 مكتمل، التسليم ينتظر الاعتماد المعماري، وMilestone 3 غير مصرح بها. حُفظ السجل القديم مع وسم الحالات المتقادمة كتاريخية.
- صُحح README: لا توجد حزمة FluentAssertions في مشروع الاختبارات، وتستخدم اختبارات Foundation `xUnit Assert` المدمج.
- حُفظت نسخة ما قبل التعديل للـDockerfile والوثائق والأرشيف في `.backups/M2-final-review-2026-11-04-pre/`، وأُعيد بناء `HARDWARE_STORE_FOUNDATION_M2.zip` والتحقق من محتوياته وسلامته. لم تُشغّل اختبارات موسعة.

### 4.3 تشغيل المالك ونقطة الاستئناف
- أكد المالك نجاح التشغيل المحلي على Windows وDocker Compose: PostgreSQL وAPI وWeb جميعها `Healthy`، والموقع فتح في المتصفح. هذا تأكيد المالك بعد فحوصنا السابقة، وليس اختبارًا أُعيد في هذه الجولة.
- الأرشيف النهائي: `HARDWARE_STORE_FOUNDATION_M2.zip`، ويضم ملفات المشروع والوثائق وملفات الإعداد النموذجية؛ لا يضم `.env` أو الاعتمادات ومخرجات البناء والنسخ الاحتياطية أو الأرشيفات الأخرى.
- `.env` مستبعد بواسطة `.gitignore` وسياسة إنشاء الأرشيف. لم يُحذف PostgreSQL Volume.
- M2 مكتملة. M3 لم تبدأ ولم تحصل على موافقة. الخطوة التالية مراجعة واعتماد الانتقال إلى **M3 — Identity & Party**.

### 4.4 بدء M3 — RBAC Authorization Core
- بدأ تنفيذ الجزء المصرح به من M3 فقط: كتالوج الصلاحيات الـ32 من USERS_ROLES_PERMISSIONS، Permission Requirement/Handler، تسجيل DI، وامتداد `RequirePermission` لـMinimal API.
- أضيفت اختبارات مركزة للمنح والرفض والمستخدم المجهول والصلاحية غير المعروفة وفحص صلاحيات متعددة.
- التحقق: `dotnet test tests/HardwareStore.Api.Tests/HardwareStore.Api.Tests.csproj -m:1 -v:minimal` نجح (7/7، يتضمن الموجود واختبارات RBAC). استعادة NuGet احتاجت تشغيل الأمر بصلاحية تسمح بالوصول للشبكة بعد فشل DNS في المحاولة المقيدة.
- لم تُنفذ Login/Accounts/JWT ولا تخزين أدوار/صلاحيات. التحقق الحالي يقرأ Permission claims الموثوقة فقط؛ ربطها بهوية فعلية لاحقًا ما زال خارج هذه المهمة.
- نسخة الرجوع: `.backups/m3-rbac-core-2026-11-04-pre/`. M3 لم تكتمل؛ لا تبدأ وظائف أخرى.
- أكد المالك قبول M2. بدأ M3 بتصريح محدود لـRBAC Authorization Core فقط؛ نجح الاختبار المستهدف 7/7. M3 لم تكتمل. تسجيل الدخول والحسابات وJWT ومصدر Permission Claims الدائم لم تُنفذ؛ الخطوة التالية مراجعة الكود معماريًا وأمنيًا.

### Hardening أمني محدود لـRBAC — 4 نوفمبر 2026
- ضُبطت `FallbackPolicy` لتطلب مصادقة افتراضيًا، وأضيف `AllowAnonymous()` صراحةً لـ`/health/live` و`/health/ready`.
- أصبح Handler يقبل Permission claim من `ClaimsIdentity` مصادق عليها فقط مع استمرار فحص `PermissionCatalog`.
- أضيفت اختبارات للهوية المختلطة واختبار إعدادات Fallback والاستثناءات الصحية. بناء المشاريع المتأثرة اكتمل ضمن تشغيل الاختبارات، لكن عملية الاختبار علقت أثناء اختبار Fallback/Health وأُوقفت؛ لا توجد نتيجة نجاح نهائية لهذه الجولة.
- نسخة الرجوع: `.backups/rbac-hardening-2026-11-04-pre/`. نقطة الاستئناف: تشخيص سبب تعليق اختبار `WebApplicationFactory` ثم إعادة تشغيل اختبارات RBAC المستهدفة فقط.

### قرار الوصول العام للمتجر
- اعتماد التصفح العام للمنتجات والأسعار والبحث والتصنيفات؛ Checkout وإنشاء الطلب والبيانات الشخصية تتطلب المصادقة، والإدارة تتطلب الصلاحية. لم تُنشأ Endpoints تجارية.
- FallbackPolicy باقية للمنع الافتراضي، مع `AllowAnonymous` الصريح للعام فقط. لم تُنفذ الهوية أو JWT أو مصدر claims دائم.
- نسخة الرجوع: `.backups/public-store-auth-2026-11-04-pre/`. اختبار WebApplicationFactory السابق ما زال غير محسوم؛ لا تدّعِ نجاح اختبارات هذا القرار.
- متابعة 1%: شُغّل فلتر PermissionAuthorizationTests مع استبعاد اختبار Fallback/WebApplicationFactory باستخدام `--no-build --no-restore`؛ بقي بلا مخرجات نتيجة بعد 30 ثانية وتم إيقافه. لا توجد اختبارات ناجحة جديدة مسجلة. يلزم تشخيص تعليق test host/runner بعد Reset.
- تشخيص الاستئناف: اختبار Fallback/Health منفرد مع `--blame-hang-timeout 00:00:30 --diag` لم يصل إلى تنفيذ الاختبار؛ سجل VSTest توقف عند `ProxyDataCollectionManager.Initialize` منتظرًا اتصال DataCollector بمهلة 90 ثانية. أوقفت التشغيل قبل انتظار أعمى. هذا يحدد التعليق في بدء DataCollector الخاص بـVSTest، ولا يثبت أن WebApplicationFactory هو السبب الجذري. لا توجد نتيجة اختبار مؤكدة.
- إعادة تحقق دون Hang DataCollector: لم توجد ملفات `.runsettings` أو تعريف DataCollector/Coverlet/Collect ضمن ملفات المشروع والاختبار. استخدمت `--no-build --no-restore`, `--logger console;verbosity=detailed` ومهلة خارجية 30 ثانية. السجل `.backups/rbac-fallback-stdout.log` وصل إلى `A total of 1 test files matched` واسم DLL، ولم يعرض بدء/نهاية الاختبار قبل انتهاء المهلة؛ الاختبار لم يثبت النجاح. يشير ذلك إلى تعليق بعد اكتشاف ملف الاختبارات وقبل تقرير حالة الاختبار، ولا يحسم هل داخل تهيئة Test Host أم الاختبار نفسه.
- عزل إضافي: اخترت `Unknown_permission_is_denied`، وهو لا يستخدم WebApplicationFactory ولا خدمات خارجية. مع `--no-build --no-restore` وDetailed Console ومهلة PowerShell خارجية 30 ثانية، وصل stdout إلى اكتشاف DLL فقط ولم يظهر اسم الاختبار أو نتيجته؛ أوقفت عملية dotnet/testhost. هذا يثبت أن التعليق أوسع من WebApplicationFactory، ويرجح مشكلة Test Runner/TestHost بدءًا قبل تنفيذ الاختبار. السجلات: `.backups/rbac-simple-test-command.log`, `rbac-simple-test-stdout.log`, `rbac-simple-test-stderr.log`. لم تُشغل بقية اختبارات Handler ولم يُعدل كود التطبيق.
- نسخة ما قبل تحديث السجلات: `.backups/rbac-runner-isolation-2026-11-04-pre/`. التوقف الآمن: لا تغيّر Authorization؛ شخّص بيئة VSTest/TestHost أو صلاحيات/اتصال runner أولًا.
- تشخيص مباشر من DLL: الحزم Microsoft.NET.Test.Sdk 17.14.1 وxunit 2.9.3 وxunit.runner.visualstudio 3.1.5 على net10.0. لا توجد إعدادات runsettings/DataCollector في المشروع. سجل VSTest السابق مع blame توقف قبل testhost عند اتصال DataCollector.
- محاولة `dotnet vstest` المباشرة للـUnknown_permission_is_denied دون blame/DataCollector أظهرت VSTest 18.7.0 و`Starting test execution` وملف DLL المطابق، لكن لم يظهر اسم الاختبار أو نتيجة في المخرجات خلال نافذة 30 ثانية. لا يمكن الجزم هل الاختبار بدأ فعليًا؛ لا توجد نتيجة نجاح. فشل أخذ process snapshot عبر CIM بسبب منع الوصول، لذلك لم تتوفر معلومات عملية موثوقة.
- لا دليل يثبت خلل إصدار الحزم؛ لا تغيّر الإصدارات. نقطة الاستئناف: استخدم TestLogger/diagnostic محلي أو process inspection مسموح لتحديد testhost بعد بدء VSTest، ثم اختبر قبل أي تغيير إعداد.
- محاولة الاستئناف المحددة: `dotnet vstest <DLL> --ListTests` بمهلة PowerShell 30 ثانية انتهت بالمهلة. Stdout احتوى فقط `VSTest version 18.7.0` و`The following Tests are available:` دون أي أسماء؛ لم يكتمل Discovery. لم يُشغّل أي اختبار بعد ذلك امتثالًا للضابط. لا توجد معلومات من هذه المحاولة عن إنشاء TestHost أو تحميل Adapter/Assembly أثناء التنفيذ.
- حفظت stdout/stderr في `.backups/rbac-listtests-stdout.log` و`.backups/rbac-listtests-stderr.log`، ونسخة قبل تحديث السجل في `.backups/rbac-listtests-2026-11-04-pre/`. السبب لا يزال غير محسوم عند مرحلة Discovery/runner.
- Diagnostic Test Runner معزول: لم توجد عمليات dotnet/testhost/vstest قبل الاختبار (Get-Process). فشل استخدام CIM لرفض الوصول، ولم تُوقف أي عملية قائمة.
- المشروع الأصلي يستهدف net10.0 ويستخدم Microsoft.NET.Test.Sdk 17.14.1 وxUnit 2.9.3 وxunit.runner.visualstudio 3.1.5.
- أُنشئ مشروع مؤقت خارج Workspace بهذه الإصدارات. المحاولة الأولى لم تصل للـRunner بسبب وسيط `--ignore-failed-sources` غير المقبول عبر `dotnet test` (MSB1001)، ثم صُحح الأمر مرة واحدة. الاختبار التجريبي نجح 1/1 خلال 1.366 ثانية، وسجله أظهر تحميل Adapter v3.1.5 ثم Discovering/Discovered/Starting/Finished.
- بالمقارنة، تشغيل DLL الأصلي عبر `dotnet vstest` أظهر بدء VSTest ومطابقة ملف DLL لكن لم يظهر سطر Adapter/Discovery أو اسم اختبار؛ كما أن `--ListTests` لم يعرض أسماء. هذا يرجح أن Adapter الخاص بالمشروع الأصلي لم يُحمّل/يُكتشف عند التشغيل المباشر للـDLL، بينما Test SDK/الـRunner العام يعملان. السبب المحدد قد يكون مسار اكتشاف Adapter عند `dotnet vstest` المباشر أو إعدادات مخرجات المشروع؛ لا يوجد دليل كافٍ لتغيير الإصدارات أو اتهام WebApplicationFactory.
- لم تُشغّل اختبارات المشروع الأصلية مجددًا، ولم يتغير كودها أو Dependencies. السجلات التجريبية في `%TEMP%\\HardwareStore-Rbac-TestRunner-Diagnostic`; سجلات DLL السابقة محفوظة تحت `.backups/rbac-*`. نسخة ما قبل تحديث الحالة: `.backups/rbac-runner-isolation-final-2026-11-04-pre/`.
- مقارنة Read-only: csproj الأصلي يحدد `IsTestProject=true`, `net10.0`, Test SDK 17.14.1، xUnit 2.9.3، Adapter 3.1.5. يضيف أيضًا MVC.Testing 10.0.12 وTestcontainers.PostgreSql 4.16.0 ومراجع Host/Identity، بخلاف المشروع التجريبي الأدنى. لا يظهر اختلاف بالإصدارات الأساسية أو خصائص IsTestProject.
- أصول المشروع الأصلية تؤكد وجود الحزم في `obj/project.assets.json`، و`.deps.json` يسجل الحزم، و`bin/Debug/net10.0/xunit.runner.visualstudio.testadapter.dll` موجود (1,253,376 bytes). إذن غياب الحزمة/ملف Adapter ليس تفسيرًا مثبتًا.
- سجل `dotnet vstest` المباشر لا يتضمن رسائل xUnit Adapter/Discovery؛ سجل `dotnet test` السابق وصل فقط إلى DLL. لا تكفي السجلات لتحديد هل Adapter لم يُحمّل أم توقف TestHost قبل رسائل Adapter.
- مشروع TEMP نجح سابقًا بسجل Adapter صريح، لكن مساره لم يعد متاحًا في بيئة القراءة الحالية، لذا لا يمكن مقارنة project.assets/.deps الخاصة به الآن. لا تغيير في الإصدارات أو إعدادات المشروع موصى به قبل تسجيل تشخيص جديد.
- نسخة ما قبل تحديث السجلات: `.backups/rbac-readonly-comparison-2026-11-04-pre/`.
- محاولة مع `--TestAdapterPath` صريح: المسار الفعلي `tests/HardwareStore.Api.Tests/bin/Debug/net10.0/`، والـAdapter موجود. المحاولة الأولى فشلت بسبب تقسيم Start-Process للمسار ذي المسافة؛ لم تصل إلى Discovery.
- المحاولة المصححة ذات الاقتباس الصحيح وDiagnostic Log انتهت بمهلة خارجية 45 ثانية. سجل `.backups/vstest-adapter-discovery-corrected.diag.log` يثبت تحميل مسار extension الخاص بـxunit adapter وتحديد `testhost.exe` ثم `Test Runtime launched`، وبعدها توقف عند `TestRequestSender.WaitForRequestHandlerConnection` بمهلة داخلية 90 ثانية. `--ListTests` لم يعرض أسماء؛ لم يتم تشغيل اختبار. النتيجة: Adapter تم تحميله وTestHost أُطلق، لكن الاتصال بين VSTest وTestHost لم يكتمل.
- لا تكرر التشغيل؛ لا تغيّر الحزم أو التطبيق دون مراجعة. نسخة قبل تحديث السجل: `.backups/rbac-explicit-adapter-2026-11-04-pre/`.
- حُسم سبب التعليق: Host Diagnostic Log من تشغيل `dotnet vstest` السابق سجل `System.Net.Sockets.SocketException (10013): An attempt was made to access a socket in a way forbidden by its access permissions` عند `testhost` أثناء الاتصال بـVSTest على loopback. هذا كان منعًا من Sandbox، لا غياب Adapter ولا دليلًا على خلل WebApplicationFactory.
- مع صلاحية تنفيذ تسمح بـloopback، شُغل `dotnet test ... --no-build --no-restore` لاختبار `Unknown_permission_is_denied` منفردًا بمهلة 45 ثانية؛ نجح 1/1 خلال 1.423 ثانية، مع Adapter Discovery وTestHost.
- شُغلت بقية `PermissionAuthorizationTests` (باستثناء الاختبار الذي نجح منفردًا) بنفس القيود؛ نجحت 6/6 خلال 1.340 ثانية، ومنها Fallback/Health ورفض هوية غير موثوقة. إجمالي RBAC المستهدف المؤكد: 7/7. لا Build/Restore أو Docker/PostgreSQL.
- السجلات Runner/Host محفوظة: `.backups/dotnet-test-rbac.diag.log`, `.backups/dotnet-test-rbac.diag.host.*`, و`dotnet-test-rbac-remaining.diag*` إضافة إلى stdout/stderr. نسخة ما قبل تحديث السجل `.backups/rbac-sandbox-socket-fix-2026-11-04-pre/`.
- لم تُختبر Checkout/Admin endpoints لأنها غير موجودة؛ الاختبارات الحالية تثبت requirement/policy والـhealth metadata فقط. لا Login/JWT/Accounts أو تنفيذ Slice أخرى.


## [Phase A UX] 7 نوفمبر 2026

- **Header/Search:** حذف Utility strip وCtrl+K؛ بحث فوري من Catalog API مع Debounce 220ms وStale guard وحالات Loading/No-results/Error، وعرض صورة/اسم/SKU/سعر، وKeyboard/ARIA. عناصر اللغة والحساب والثيم مرة واحدة (Desktop من `lg`، والدرج تحته).
- **Mega Menu:** زر `كل الأقسام`، شجرة أقسام حقيقية، لوحة ثنائية (قائمة الأقسام + أبناء القسم المحدد بجانبه)، Hover/Click/Keyboard، Escape/Outside/Route close، وإعادة التركيز. يفتح افتراضيًا أول قسم لديه أبناء.
- **Media:** 9 موديلات بصور محلية موثقة + Samsung بصورتين، و3 موديلات Fallback فئوي. `ProductImage` واحد للبطاقات والبحث والسلة والمعرض والـHero وDiscovery وThumbnails. Brand rail نصي لأن شعارات DB Placeholders. MANIFEST محدث.
- **Rail:** أسهم منتصف الجانبين، بطاقة واحدة لكل خطوة، RTL/LTR، Drag/Swipe، بلا Progress وهمي.
- **Home:** إزالة Trust strip والعداد وLive dot ووعد المقارنة، وإضافة دليل اختيار عملي من 3 خطوات.
- **Footer/Login:** Footer بأربعة أعمدة حقيقية فقط بلا Live/Back-to-top/Newsletter/Social/Address. Login بلا Aside وبلا Owner/Session details.
- **Comparison:** `comparisonAvailable=false` + Badge `قريبًا` غير قابل للنقر، مع إبقاء الصفحة والكود والبيانات.
- **Admin:** Row Actions مشتركة (Portal/One-open/Outside/Escape/Route/Scroll/Focus). شاشة `محتوى الصفحة الرئيسية` Prototype بـlocalStorage لترتيب/إظهار/إخفاء الأقسام وتحرير الشرائط. تنظيف الجداول/الحوارات/المحرر/Dashboard إلى Semantic Tokens.
- **i18n:** الأقسام، العلامات التجارية، إصدار المنتج حسب السياق.
- **التحقق:** `tsc` نجح، Vitest 40/40، `next build` نجح (23 Route)، Playwright Public/Admin QA نجح مع 0 Console Errors، ولقطات After في `docs/review/phase-a-2026-11-07/`.
- **التقرير:** `RIGX_PHASE_A_UX_REPORT_2026-11-07.md`.
- **قيود:** Homepage Content محلي فقط؛ 3 موديلات Fallback؛ Brand logos نصية؛ 21st MCP يحتاج Restart؛ Context7 DNS failure.
- **التالي:** Phase B verification ثم M3.2f ثم M3.3. **لا تبدأ M5.**


## [M3.2f / M3.3 Identity] 8 نوفمبر 2026

- **M3.2f — First Owner Bootstrap:** أضيف `FirstOwnerBootstrapService` + `--bootstrap-owner` كأداة محلية فقط (بلا Endpoint عام). Advisory transaction lock، منع تشغيل ثانٍ عبر Role/Owner أو Audit، Audit ذرّي داخل نفس المعاملة، Rollback عند فشل Audit، 36 صلاحية، وPassword تفاعلي غير مسجل. الاختبارات: PostgreSQL/Testcontainers 3/3.
- **M3.3 — Operational Identity & RBAC:** APIs تحت `/api/v1/admin/identity` (Users/Roles/Permissions) تتطلب `System.ManageUsers`، وشاشتا `/admin/identity/users` و`/admin/identity/roles`. حماية آخر Owner، إبطال الجلسات عند التعطيل/تغيير الأدوار، زيادة `authorization_version`/`rbac_revision`، Audit لكل تغيير، حظر تعديل Owner/Customer، ومنع الصلاحيات خارج `PermissionCatalog`.
- **التحقق:** Backend focused tests 15/15، Frontend Vitest 40/40، `tsc` = 0، Next build = 25 Route، Visual QA للشاشتين في Dark/AR بلا Console Errors.
- **التقارير:** `RIGX_M3_IDENTITY_REPORT_2026-11-08.md` و`docs/operations/FIRST_OWNER_BOOTSTRAP.md`.
- **القيود:** لا حذف أدوار، لا تعديل Email/Username، لا Admin Password Reset، Owner/Customer محميان، لا Migration جديدة.
- **التالي:** مراجعة بشرية. لا تبدأ M5 قبل حسم Q2.1–Q2.5 وQ6.3 وQ8.7.




## [M3 Hardening + M5 Supplier Directory] 8 نوفمبر 2026

- **M3 hardening:** أضيف `IdentityDelegationPolicy` كقواعد نقية: لا يمنح المدير صلاحية لا يملكها، ولا يعدل حسابًا/دورًا أعلى منه، ولا يعين/يزيل Owner إلا Owner. يُمنع تعطيل الذات، ويجب بقاء Owner نشط وSystem.ManageUsers عند مستخدم نشط. كل محاولة 403/409 تُسجل `Identity.AdministrationRejected` بعد Rollback.
- **M3 tests:** 4 اختبارات إدارة هوية، منها تصعيد ممنوع، آخر Owner، تزامن، وAudit رفض. OwnerBootstrap 3/3 وPermissionAuthorization 10/10. إجمالي focused backend 21/21.
- **M5 slice:** Supplier Directory كأول شريحة M5 آمنة. جدول `supplier_profiles`، Person من Identity، CRUD بدون Delete، بحث/تفعيل/RowVersion، ومنع تكرار Tax/Commercial Registration. API تحت `/api/v1/admin/purchasing/suppliers`، وشاشة `/admin/purchasing/suppliers` مع Mobile Cards.
- **M5 tests:** PostgreSQL/Testcontainers 4/4: إنشاء/بحث/تحديث/تعطيل/Audit، تزامن RowVersion، Rollback عند فشل Audit، ورفض مدخلات.
- **Migrations:** أضيفت Purchasing migration `20261108092628_M5_SupplierDirectory` ولم تُطبق على Preview الدائم. `has-pending-model-changes` نظيف.
- **UI:** Admin Workspace يسمح بأي صلاحية إدارية معروفة، ويعرض فقط مجموعات المسارات المتاحة؛ Dashboard يعرض حالة محدودة لمن لا يملك Catalog.View. Supplier table يتحول إلى Cards على الموبايل.
- **التحقق:** Backend build 0/0، Frontend tsc=0، Vitest 40/40، Next build ناجح (26 Route)، Visual QA بلا Console Errors.
- **التقارير:** `RIGX_M3_HARDENING_M5_SUPPLIER_REPORT_2026-11-08.md` و`docs/operations/M5_SUPPLIER_DIRECTORY.md`.
- **التالي:** مراجعة، ثم تطبيق Migration على قاعدة اختبار/Preview بموافقة. لا Stock/Transfers/Purchase Orders قبل Q2.1–Q2.5 وQ6.3 وQ8.7.




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


## [Round 5 - Product Completion] - 12 Nov 2026
- Concurrency: exact locked IDs via FOR UPDATE SKIP LOCKED; assembly row lock and component validation.
- Tests: 3 real concurrency tests; backend 193/193.
- UI: unified Admin content shell + two-column Auth login; Admin inner panels remain Legacy.
- Verified: tsc 0, Vitest 30/30, Next build, Playwright 6/6.

## [Round 6 - Final Closure] - 13 Nov 2026
- Stock claim now loads only the exact locked unit IDs.
- Build cancellation restores components to InAssembly and republishes the assembly.
- Added concurrency + cancellation tests: Backend 194/194.
- NuGet/npm vulnerability audits: 0 known vulnerabilities.
- UI: Admin content shell + Auth login shell; Admin inner panels remain Legacy.
- Verified: tsc 0, Vitest 30/30, Next build, Playwright 6/6.

