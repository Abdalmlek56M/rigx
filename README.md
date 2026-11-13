# HARDWARE STORE / RIGX — متجر مكونات الحواسيب

مشروع ويب متكامل ومستقل لإدارة وتجارة مكونات وأجهزة الحواسيب والإلكترونيات. **RIGX** هي هوية الواجهة العاملة في Local Preview وتنتظر اعتماد المالك النهائي للعلامة التجارية والنطاق، بينما تبقى أسماء الحل والـNamespaces الحالية مستقرة.

> **المرجع الموحد للحالة الحالية:** [`docs/canonical/00_INDEX.md`](docs/canonical/00_INDEX.md). التقارير المؤرخة في جذر المشروع سجلات تاريخية وليست مصدر حقيقة.

> **آخر دفعة — Phase A UX (7 نوفمبر 2026):** تم توحيد Header وSearch الفوري وMega Menu للأقسام، وتثبيت صور المنتجات المحلية الموثقة مع Fallback محايد، وتوحيد أسطح البطاقات والمعرض والسلة، وتنظيف Footer/Login، وتعطيل كل مداخل المقارنة مع الإبقاء على الكود، وإضافة Prototype محلي لترتيب محتوى الصفحة الرئيسية من Admin. راجع [تقرير Phase A](RIGX_PHASE_A_UX_REPORT_2026-11-07.md) و[متطلبات إدارة محتوى الرئيسية](docs/storefront/HOME_CONTENT_PROTOTYPE.md).
>
> **M3.2f/M3.3 (8 نوفمبر 2026):** تم تنفيذ First Owner Bootstrap المحلي وأدوات إدارة المستخدمين والأدوار والصلاحيات مع Audit وحماية آخر Owner. راجع [تقرير الهوية والصلاحيات](RIGX_M3_IDENTITY_REPORT_2026-11-08.md) و[دليل Owner Bootstrap](docs/operations/FIRST_OWNER_BOOTSTRAP.md). بدأت M5 لاحقًا وتوسعت إلى ثلاث شرائح بيانات رئيسية.
>
> **M3 hardening + M5 Supplier Directory (8 نوفمبر 2026):** أضيفت قواعد تفويض تمنع تصعيد الصلاحيات، وبدأت M5 بشريحة **Supplier Directory** كبيانات تجارية حقيقية بلا Stock/SKU/Transfers. راجع [تقرير الجولة](RIGX_M3_HARDENING_M5_SUPPLIER_REPORT_2026-11-08.md) و[نطاق الشريحة](docs/operations/M5_SUPPLIER_DIRECTORY.md).
>
> **M5 Supplier Product Catalog + Warehouse Directory + Architecture Split (8 نوفمبر 2026):** فُصلت البيانات المالية للمورد عن العرض العام بصلاحيتي `Purchasing.ViewSupplierFinancials` و`Purchasing.ManageSupplierFinancials` مع Audit fail-closed، وأصبح `RowVersion` إلزاميًا في كل تحديث. أُضيف **Supplier Product Catalog** (ربط Supplier بـProductVariant + Supplier SKU + Notes + Activation) و**Warehouse Directory** (Code/Name/Address/Activation فقط)، مع Admin UI واختبارات PostgreSQL وVisual QA. قُسّمت إدارة الهوية إلى Facade رقيق + خدمة مستخدمين + خدمة أدوار + Support مشترك. راجع [تقرير الجولة](RIGX_M3_HARDENING_M5_CATALOG_WAREHOUSE_REPORT_2026-11-08.md) و[docs/operations/M5_SUPPLIER_PRODUCT_CATALOG.md](docs/operations/M5_SUPPLIER_PRODUCT_CATALOG.md) و[docs/operations/M5_WAREHOUSE_DIRECTORY.md](docs/operations/M5_WAREHOUSE_DIRECTORY.md).
>
> **Expanded Storefront Wave 0–1 + Store Settings + New/Used + Permission UX (9 نوفمبر 2026):** أُضيف Preview DB target guard واختبار تهيئة PostgreSQL كامل، وأُزيلت Compare بالكامل، ووُحّدت خطوط النماذج وسطح بطاقة المنتج. أُضيف Module **Storefront** لإعدادات WhatsApp/Social/Pickup/Trust، وزر WhatsApp عائم وروابط Footer ديناميكية. أُضيفت حالة **New/Used** إلى Catalog مع Badge وفلتر، وترجمة/بحث لمفاتيح الصلاحيات الـ39. راجع [تقرير الجولة](RIGX_EXPANDED_STOREFRONT_WAVE_2026-11-09.md) و[إعدادات المتجر](docs/operations/STOREFRONT_SETTINGS.md).
>
> **Overnight Batch — Identity Admin + Sessions + Media + Cart (9 نوفمبر 2026):** أُضيف إنشاء مستخدم من Admin بدعوة بريد آمنة بدون كلمة مرور، وإرسال Reset Password إداري، وصفحتا Profile وSessions/Devices مع إبطال جلسة/الجلسات الأخرى. أُكمل Media Pipeline بحفظ الأصل + Derivative + Thumbnail وحماية الأصل من المسار العام، مع تعطيل Watermark لعدم وجود شعار رسمي معتمد. أُصلحت سلامة السلة (نجاح فقط عند الإضافة، حدود الكميات/الخطوط، Variant غير عشوائي، وlocalStorage errors). راجع [تقرير الجولة](RIGX_OVERNIGHT_CONSOLIDATED_BATCH_2026-11-09.md) و[Media Pipeline](docs/operations/MEDIA_PIPELINE.md) و[Inventory Readiness](docs/operations/M5_INVENTORY_READINESS.md).
>
> **Consolidated Product Expansion — Addresses + Wishlist + CMS + Media + Audit + Durable Email (9 نوفمبر 2026):** أُضيف دفتر عناوين العميل، Wishlist دائم مع Guest Merge، إدارة صفحة رئيسية حقيقية Draft/Published، Articles/FAQ/Policies بنسخ مرقمة، مكتبة صور قابلة لإعادة الاستخدام مع Usage Guard، Audit Viewer بصلاحية `System.ViewAuditLogs`، Outbox بريد مشفّر دائم، وبيانات تشغيلية اختيارية للمستودعات. ظل المخزون/المشتريات بوابة قرار Read-only. راجع [تقرير الدفعة](RIGX_CONSOLIDATED_EXPANSION_2026-11-09.md) و[ADR-023..026](docs/adr/ARCHITECTURE_DECISION_RECORDS.md) ولقطات `docs/review/content-expansion-2026-11-09/`.
>
> **Main Engineering & Experience Expansion (9 نوفمبر 2026):** Header/Navigation موحّد + Offers، تحسين Category/Brand Rails، Account Center/Avatar/My Reviews، Product/Store Reviews مع Moderation، Offers بإدارة ونشر، Purchase Drafts متعددة المصادر (Draft-only، ADR-027)، Admin Analytics من بيانات حقيقية، وإصلاح Active Route Defect في Catalog Nav. راجع [التقرير](RIGX_MAIN_ENGINEERING_2026-11-09.md) ولقطات `docs/review/main-engineering-2026-11-09/`.
>
> **Primary Product Engineering Round (9 نوفمبر 2026):** English default مع حفظ الاختيار، Header/Footer/Share Dialog، Taxonomy Routes، Email Change مع تحقق جديد، Account Restrictions، Notifications Center، Manual/Unlisted Purchase Items، وPC Builder Backend/Compatibility. UI الخاص بـPC Builder وVideo وOrders/Financials ما زال غير مكتمل. راجع [التقرير](RIGX_PRIMARY_PRODUCT_ROUND_2026-11-09.md) و[مصفوفة النطاق](PRODUCT_SCOPE_MATRIX.md).
>
> **Practical Retail Core (9 نوفمبر 2026):** أُضيف Module **Retail** كسياق واحد للمخزون والتجميعات والطلبات واستحقاقات الشركاء. الاستلام من Purchase Draft ينشئ وحدات فعلية (idempotent)، والتجميعات تُبنى/تُنشر/تُفكك بتوزيع مكوّنات 100%، وCheckout أصبح حقيقيًا بمرجع من الخادم ويحجز المخزون عند `Confirmed` فقط، مع دفع يدوي مؤكد أو معفى، إكمال إداري بصلاحية `Sales.CompleteOrder`، واستحقاق شريك على صافي ربح كل وحدة، ومرتجعات بعد الصرف برصيد قابل للاسترداد. راجع [التقرير](RIGX_PRACTICAL_RETAIL_CORE_2026-11-09.md) و[قواعد العمل](BUSINESS_RULES.md) و[lقطات QA](docs/review/retail-core-2026-11-09/).
>
> **Overnight P0/P1 (10 نوفمبر 2026):** فصل صلاحيات كتابة الشركاء عن `Finance.ViewReports`، حجب التكلفة/السيريال/بيانات العميل حسب الصلاحية، Snapshot كامل لوحدات الشراء، إصلاح 11 اختبارًا (Full Suite 164/164)، Manual/phone order، صفحة `/builds` وشراء التجميعات عبر السلة، وSEO production URL + Sitemap paginated. راجع [التقرير](RIGX_OVERNIGHT_EXECUTION_2026-11-10.md).
>
> **V1.9.1 UI Replacement (12 نوفمبر 2026):** طُبقت لغة Obsidian / Warm Orange على الواجهة الحالية، وأُعيد بناء Checkout بقواعد Delivery/Pickup والدفع والتوصيل والموافقة على الشروط، مع VariantId والسعر من الخادم وThumbnail Snapshot، وإضافة Order Details وContact Inbox وFinance/Reports. ليست كل شاشات V1.9.1 منفذة بتصميم مخصص 1:1. راجع [التقرير](RIGX_UI_REPLACEMENT_2026-11-12.md) و[خريطة الشاشات](docs/UI_REPLACEMENT_MAP_2026-11-12.md).

---

## 1. أجزاء النظام الرئيسية (System Portals)

يتكون النظام من ثلاثة تطبيقات رئيسية متصلة بواجهة برمجية موحدة:
1. **Storefront (متجر العملاء الإلكتروني):** واجهة ويب سريعة تدعم محركات البحث (SEO)، واستعراض المواصفات، ومجمّع الحواسيب وقوالب التجميعات التفاعلية، وتتبع الطلبات والضمان للعملاء المسجلين (تسجيل إلزامي، لا يوجد Guest Checkout).
2. **Admin Panel (لوحة إدارة المتجر):** لوحة تحكم شاملة لإدارة العمليات اليومية: المخزون والأرقام التسلسلية الاختيارية بكارت الصنف، المشتريات، المبيعات وقائمة مراجعة الطلبات والتواصل، كاشير نقطة البيع (POS) بالمعرض، الخزائن والحسابات، والتقارير.
3. **Developer & Operations Panel (لوحة التشغيل والصيانة):** لوحة تحكم للمسؤول التقني لمراقبة صحة الخوادم، واستعراض السجلات المركزية، وتنفيذ الإجراءات التشغيلية المصرح بها (Allowlisted Actions)، والنسخ الاحتياطي الشامل لقاعدة البيانات والوسائط.

---

## 2. المنظومة والتوجه التقني المعتمد مبدئياً (Technology Stack)

* **Backend:** ASP.NET Core / C# على **.NET 10**.
* **Architecture:** Modular Monolith (مونوليث نمطي بشرائح رأسية Vertical Slices وحدود واضحة وعزل تام بين الوحدات).
* **Database:** **PostgreSQL 17+** عبر **Entity Framework Core** مع مقترح مفاتيح `UUIDv7` المتسلسلة زمنياً.
* **Frontend:** **Next.js (App Router)**، **React**، **TypeScript**، **TailwindCSS**، و**IBM Plex Sans Arabic / Inter** عبر `next/font` Self-hosted.
* **Image Processing:** **SkiaSharp 4.153.2** لفك وإعادة ترميز WebP وتوليد Thumbnails على الخادم، مع Linux Native Assets في مشروع API التنفيذي.
* **Local Development & Deployment:** **Docker Compose** كمنظومة مستقلة معزولة (Single-Tenant).
* **Testing:** xUnit (`Assert` المدمج)، Testcontainers for PostgreSQL، Vitest، Playwright. لا تحتوي مشاريع الاختبار الحالية على FluentAssertions.

---

## 3. الفهرس الشامل لوثائق المشروع (Documentation Directory)

جميع وثائق المشروع مترابطة بروابط نسبية صالحة للنقل بين الأجهزة:

### 3.1 وثائق التأسيس وقواعد العمل
* [AGENTS.md](AGENTS.md) — تعليمات وقواعد العمل الحاكمة للمشروع.
* [AUTONOMOUS_ENGINEERING_STANDARD.md](docs/engineering/AUTONOMOUS_ENGINEERING_STANDARD.md) — معيار التنفيذ المستقل داخل الهدف المعتمد، وحدود التوسع، ودورة العمل والتقرير النهائي.
* [BUSINESS_RULES.md](BUSINESS_RULES.md) — القواعد الصارمة لإدارة المخزون، السيريالات، التسعير، والضمان وسياسات الاسترجاع.
* [ROADMAP.md](ROADMAP.md) — خطة التنفيذ المرحلية بشرائح رأسية متكاملة (12 مرحلة قابلة للاختبار والتسليم).
* [PROGRESS.md](PROGRESS.md) — سجل متابعة التقدم اليومي ومؤشرات الإنجاز وحالة المشروع الحالية.
* [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) — سجل القرارات المفتوحة والمسائل المعلقة المطلوب اعتمادها من صاحب المشروع.
* [NEXT_TASK.md](NEXT_TASK.md) — وثيقة إغلاق مرحلة التوثيق وتحديد شروط الانتقال البرمجي القادم.
* [MODEL_HANDOFF.md](MODEL_HANDOFF.md) — مرجع الاستلام الأساسي للمشروع، خريطة المنتج والمعمارية والحالة والخطوة التالية.
* [V3.3_CHANGELOG.md](V3.3_CHANGELOG.md) — مقارنة موثقة قبل/بعد لجولة إغلاق الوثائق V3.3.

> **خصوصية الاستمرارية:** ملف الاستمرارية الخاص بين المالك وChatGPT محفوظ خارج المشروع عمدًا، ولا يُنسخ إلى Repository أو أرشيفات التسليم أو Agents. استخدم `MODEL_HANDOFF.md` و`docs/engineering/AUTONOMOUS_ENGINEERING_STANDARD.md` كمرجع تقني للوكيل.

### 3.2 وثائق التصميم الهندسي والمعماري (`docs/`)
* [REQUIREMENTS_TRACEABILITY.md](docs/requirements/REQUIREMENTS_TRACEABILITY.md) — مصفوفة تتبع المتطلبات الـ 13 ومصدر الحقيقة لكل منها وحالتها.
* [PRODUCT_REQUIREMENTS.md](docs/requirements/PRODUCT_REQUIREMENTS.md) — مواصفات ومتطلبات وظائف المتجر واللوحات والوسائط والدعم.
* [USERS_ROLES_PERMISSIONS.md](docs/architecture/USERS_ROLES_PERMISSIONS.md) — نموذج الطرف (Party Model)، الحسابات، ومصفوفة الصلاحيات الدقيقة.
* [MODULAR_MONOLITH_ARCHITECTURE.md](docs/architecture/MODULAR_MONOLITH_ARCHITECTURE.md) — خريطة الموديولات، والشرائح الرأسية، والاتصال بالأحداث.
* [DATABASE_DESIGN_ERD.md](docs/database/DATABASE_DESIGN_ERD.md) — تصميم الجداول، الفهارس، ومخطط الكيانات الشامل (Mermaid ERD).
* [API_DESIGN_SECURITY.md](docs/api/API_DESIGN_SECURITY.md) — معايير REST، التوثيق الآمن عبر الـ Cookies، والـ Rate Limiting.
* [PROCUREMENT_SALES_INVENTORY.md](docs/workflows/PROCUREMENT_SALES_INVENTORY.md) — دورة الشراء، حركة المخزون، وسجل السيريالات.
* [ORDERS_INVOICES_RETURNS.md](docs/workflows/ORDERS_INVOICES_RETURNS.md) — دورة الطلبات (11 خطوة)، التواصل، ومسارات Delivery vs Pickup.
* [FINANCIAL_MANAGEMENT.md](docs/financials/FINANCIAL_MANAGEMENT.md) — الخزائن، الحسابات البنكية، المصروفات، ومعادلات حساب الأرباح.
* [PC_BUILDER_COMPATIBILITY.md](docs/pc_builder/PC_BUILDER_COMPATIBILITY.md) — قوالب التجميعات والبدائل، قواعد التوافق الأساسية، وتقدير الطاقة الاسترشادي.
* [UI_UX_DESIGN_SPECIFICATIONS.md](docs/ui_ux/UI_UX_DESIGN_SPECIFICATIONS.md) — توجه التصميم الهادئ والمريح، دعم RTL بالخصائص المنطقية، والسمات.
* [TESTING_STRATEGY.md](docs/testing/TESTING_STRATEGY.md) — هرم الاختبارات، اختبارات التسجيل والتواصل والاستلام من المحل، والمستهدفات المقترحة.
* [DEPLOYMENT_OPERATIONS_BACKUP.md](docs/operations/DEPLOYMENT_OPERATIONS_BACKUP.md) — النشر عبر Docker Compose، لوحة التشغيل المصرح بها، والنسخ الاحتياطي الشامل.
* [LOCAL_PREVIEW.md](docs/operations/LOCAL_PREVIEW.md) — بيئة Preview معزولة وآمنة على Windows مع Seed وOwner من إعدادات محلية وإجراء إعادة ضبط آمن.
* [FIRST_OWNER_BOOTSTRAP.md](docs/operations/FIRST_OWNER_BOOTSTRAP.md) — إنشاء أول Owner محليًا مع رفض stdin المعاد توجيهه وتوضيح حدود إثبات البريد.
* [M5_SUPPLIER_DIRECTORY.md](docs/operations/M5_SUPPLIER_DIRECTORY.md) — دليل الموردين، فصل البيانات المالية، الصلاحيات، وRowVersion.
* [M5_SUPPLIER_PRODUCT_CATALOG.md](docs/operations/M5_SUPPLIER_PRODUCT_CATALOG.md) — ربط المورد بـProductVariant وكود المورد بدون أسعار أو مخزون.
* [M5_WAREHOUSE_DIRECTORY.md](docs/operations/M5_WAREHOUSE_DIRECTORY.md) — دليل المستودعات كبيانات رئيسية بدون أرصدة أو حركات.
* [STOREFRONT_SETTINGS.md](docs/operations/STOREFRONT_SETTINGS.md) — WhatsApp/Social/Pickup/Trust settings وإدارة المحتوى العام بأمان.
* [MEDIA_PIPELINE.md](docs/operations/MEDIA_PIPELINE.md) — حفظ الأصل والـDerivatives وحماية الأصل وحدود الرفع.
* [M5_INVENTORY_READINESS.md](docs/operations/M5_INVENTORY_READINESS.md) — مراجعة Read-only وخطة M5 Inventory القادمة.
* [MAILPIT_PREVIEW_PROPOSAL.md](docs/operations/MAILPIT_PREVIEW_PROPOSAL.md) — اقتراح غير مطبق لصندوق بريد محلي آمن للاختبار.
* [STOREFRONT_CONTENT_PROPOSAL.md](docs/ui_ux/STOREFRONT_CONTENT_PROPOSAL.md) — حدود اختيار محتوى الصفحة الرئيسية والقدرة المستقبلية المقترحة.
* [ARCHITECTURE_DECISION_RECORDS.md](docs/adr/ARCHITECTURE_DECISION_RECORDS.md) — سجل القرارات المعمارية والتوصيات المقترحة.

### 3.3 مهارات المشروع (OpenCode / Agent Skills)
* [hardware-store-docs-audit](.agents/skills/hardware-store-docs-audit/SKILL.md) — تدقيق الوثائق ومصادر الحقيقة والقرارات.
* [database-integrity](.agents/skills/database-integrity/SKILL.md) — قيود PostgreSQL وEF Core وسلامة المخزون والتزامن.
* [controlled-implementation](.agents/skills/controlled-implementation/SKILL.md) — تنفيذ مستقل داخل الهدف المعتمد، مع نسخ رجوع ومعالجة الأسباب الجذرية.
* [engineering-handoff](.agents/skills/engineering-handoff/SKILL.md) — تسليم هندسي قابل للمراجعة مع التحقق واللقطات والقيود.
* [security-and-data-protection](.agents/skills/security-and-data-protection/SKILL.md) — مراجعة الهوية والصلاحيات وواجهات API وحماية البيانات والنسخ الاحتياطية.
* [hardware-store-design-system](.agents/skills/hardware-store-design-system/SKILL.md) — نظام RIGX البصري وSemantic Tokens والخلفية المشتركة وRTL/LTR والثيمات.
* [hardware-store-visual-qa](.agents/skills/hardware-store-visual-qa/SKILL.md) — Visual QA للخلفية والفوتر وResponsive وRTL/LTR وLight/Dark وReduced Motion.

المهارات العامة المستخدمة كمراجع هي Anthropic `frontend-design` (Apache-2.0) وOpenAI `frontend-app-builder` و`frontend-testing-debugging` و`react-best-practices`. تحفظ ملفات المصدر والترخيص والـAttribution داخل المهارتين المحليتين المرتبطتين.

مذكرة [مراجعة تصميم مراجع حركات المخزون](docs/database/INVENTORY_MOVEMENT_REFERENCE_DESIGN_REVIEW.md) مقترح تقني غير معتمد؛ لا تعدّل ERD قبل مراجعته واعتماده.

---

## 4. شجرة ملفات المشروع الحالية

```
HARDWARE STORE/
├── AGENTS.md                                   <-- تعليمات العمل الأساسية
├── .agents/skills/                              <-- مهارات الوكلاء الخاصة بالمشروع
├── README.md                                   <-- هذا الدليل الفهرسي
├── HARDWARE_STORE.slnx                          <-- حل .NET
├── compose.yaml                                 <-- PostgreSQL وAPI وWeb للتطوير المحلي
├── compose.preview.yaml                         <-- Preview معزول مع Ports وVolumes مستقلة
├── scripts/preview.ps1                          <-- تشغيل/إيقاف/حالة Preview
├── scripts/open-preview.ps1                     <-- Launcher يتحقق من الصحة قبل فتح الموقع
├── scripts/create-preview-shortcut.ps1          <-- إنشاء/إزالة اختصار سطح المكتب
├── .env.example                                 <-- أسماء الإعدادات المحلية بلا أسرار فعلية
├── .env.preview.example                         <-- قالب أسرار Preview؛ انسخه إلى .env.preview.local
├── .gitignore
├── .dockerignore
├── BUSINESS_RULES.md                           <-- قواعد وسياسات العمل المعتمدة
├── ROADMAP.md                                  <-- خطة التنفيذ المرحلية (Vertical Slices)
├── PROGRESS.md                                 <-- سجل التقدم وحالة المشروع
├── OPEN_QUESTIONS.md                           <-- القرارات غير المحسومة بانتظار الاعتماد
├── NEXT_TASK.md                                <-- شروط وبوابة الانتقال للمرحلة التالية
├── MODEL_HANDOFF.md                            <-- مرجع الاستلام والاستمرارية للنموذج/المطور التالي
├── src/
│   ├── Common/                                   <-- Domain وApplication وInfrastructure المشتركة
│   ├── Host/HardwareStore.Api/                    <-- مضيف ASP.NET Core وHealth Checks
│   └── Modules/                                  <-- Identity/Auditing/Catalog وسائر حدود الوحدات
├── apps/web/                                     <-- Next.js App Router وواجهة RTL الأساسية
├── tests/HardwareStore.Api.Tests/                 <-- xUnit وWebApplicationFactory وTestcontainers
└── docs/
    ├── requirements/
    │   ├── PRODUCT_REQUIREMENTS.md             <-- متطلبات المنتج والوظائف والوسائط
    │   └── REQUIREMENTS_TRACEABILITY.md        <-- مصفوفة تتبع المتطلبات الـ 13
    ├── architecture/
    │   ├── USERS_ROLES_PERMISSIONS.md          <-- نموذج Party والصلاحيات
    │   └── MODULAR_MONOLITH_ARCHITECTURE.md    <-- معمارية المونوليث النمطي
    ├── database/
    │   └── DATABASE_DESIGN_ERD.md              <-- تصميم قاعدة البيانات ومخطط ERD
    ├── api/
    │   └── API_DESIGN_SECURITY.md              <-- تصميم API والأمان
    ├── workflows/
    │   ├── PROCUREMENT_SALES_INVENTORY.md      <-- دورة الشراء والمخزون
    │   └── ORDERS_INVOICES_RETURNS.md          <-- دورة الطلبات والتواصل والتسليم
    ├── financials/
    │   └── FINANCIAL_MANAGEMENT.md             <-- الحسابات والخزائن والأرباح
    ├── pc_builder/
    │   └── PC_BUILDER_COMPATIBILITY.md         <-- قوالب التجميعات وفحص التوافق V1
    ├── ui_ux/
    │   └── UI_UX_DESIGN_SPECIFICATIONS.md      <-- مواصفات الواجهات والتصميم الهادئ
    ├── testing/
    │   └── TESTING_STRATEGY.md                 <-- استراتيجية الاختبارات
    ├── operations/
    │   └── DEPLOYMENT_OPERATIONS_BACKUP.md     <-- النشر والتشغيل والنسخ الاحتياطي
    └── adr/
        └── ARCHITECTURE_DECISION_RECORDS.md    <-- سجل القرارات المعمارية المقترحة
```

---

## 5. الحالة الحالية وتشغيل بيئة التطوير
اكتملت واعتمدت **M2 وM3.1 وM3.2a وM3.2b**، واكتملت نتائج **M3.2c وM3.2d** بعد تشغيل بوابة PostgreSQL المصرح بها. **M3.2e منفذة** وتشمل صفحات العميل المتصلة بالـAPI، وتغيير/استعادة كلمة المرور، وحماية جلسات الحساب. التسجيل غير متاح في Production، وما زال ينتظر اعتماد المالك وإعداد البريد والـProxy التشغيلي.

**M4 Catalog & Media:** الشريحة الرأسية مكتملة وظيفيًا وبصريًا للمعاينة المحلية: صفحة رئيسية تجارية ببيانات Preview، Search/Filters/Sorting/Pagination، Toolbar موحدة بستة أوامر ترتيب، Sidebar تقني مبني على مواصفات منشورة فعلًا، Carousels أصلية للموبايل/RTL/LTR، Variant Selector، معرض صور، RTL/LTR، Light/Dark، Admin Workspace مستقل `/admin` مع Dashboard حقيقي وShell متجاوب، إدارة تصنيفات وماركات ومنتجات، معالجة WebP على الخادم، وMedia audit. SkiaSharp Stable `4.153.2` مع Linux Native Assets في مخرجات API. لا Migration جديدة.

**RIGX V2 Storefront:** الهوية البصرية Graphite/Orange، Product Cards وProduct Details محسّنان، Gallery/Lightbox، Share، WhatsApp Inquiry شرطي برقم المالك، Admin taxonomy tables/drawers/filters، وProduct SEO/Open Graph/JSON-LD/Sitemap/Robots. Product Condition (New/Used) أُضيف في 9 نوفمبر 2026، بينما Warranty/Inspection لا تظهر لأن المخطط الحالي لا يملك سجلاتها. Compare أُزيل بالكامل بطلب المالك.

**Interactive Storefront Demo:** سلة تسوق محلية Versioned (`rigx-cart-v1`) ودرج سلة وعدّاد في الهيدر، وصفحتا `/cart` و`/checkout`، مع دعم Variants وتعديل الكميات وإجمالي فرعي وCheckout تجريبي. الحالة في المتصفح فقط: لا طلبات حقيقية، ولا حجز مخزون، ولا دفع أو شحن أو ضرائب، ومرجع الطلب `RIGX-DEMO-*` للعرض فقط. ربط السلة بالـBackend أو إنشاء Orders يحتاج قرارًا ومرحلة منفصلة.

تعيد `GET /api/v1/identity/me` الصلاحيات الفعالة للحساب. تستخدم واجهة الموقع هذه الصلاحيات، ومنها `Catalog.*`، لإظهار أدوات الإدارة المناسبة؛ ومع ذلك يظل فحص الصلاحية داخل Backend هو الحماية الفعلية.

التحقق المسجل بعد Demo السلة وجولة توحيد الهوية: `tsc --noEmit` = 0، Vitest `28/28`، Next production build ناجح بـ22 Route، Playwright غير المتطفل `12/12` (اختبار Login متجاوز بالتصميم) على `http://127.0.0.1:3006`، وVisual QA على 1440×1000 و390×844 في AR/EN وLight/Dark. التحقق التفصيلي الأقدم (Backend non-Docker `51/51` وCatalog PostgreSQL `4/4` وVisual QA) محفوظ في [PROGRESS.md](PROGRESS.md).

التحقق المسجل بعد جولة M3 hardening + M5 (8 نوفمبر 2026): `dotnet build` = 0 أخطاء/0 تحذيرات، `IdentityAdministrationPostgreSqlTests` 7/7، `SupplierDirectoryPostgreSqlTests` 7/7، `SupplierProductCatalogPostgreSqlTests` 3/3، `WarehouseDirectoryPostgreSqlTests` 2/2، `PermissionAuthorizationTests` 11/11، `tsc --noEmit` = 0، Vitest `40/40`، Next production build ناجح، وPlaywright Admin M5 `1/1` مع Visual QA على Desktop/Mobile وAR/EN وLight/Dark. تحقق `has-pending-model-changes` نظيف لـIdentity وPurchasing وInventory. لم تُطبق أي Migration على Preview/Owner DB.

التحقق المسجل بعد Expanded Storefront Wave 0–1 (9 نوفمبر 2026): `dotnet build` = 0/0، 43/43 اختبارًا مركّزًا (Catalog 4، Identity 7، Supplier 7، SupplierProduct 3، Warehouse 2، StoreSettings 2، Preview 6، Permission 12)، `tsc --noEmit` = 0، Vitest `36/36`، Next production build ناجح، Playwright Admin/Storefront `1/1`، و`has-pending-model-changes` نظيف لـCatalog/Storefront/Purchasing/Inventory. Migrations الجديدة طُبقت على قاعدة QA فقط (15 إجمالًا) وليست على Preview/Owner DB.

التحقق المسجل بعد Overnight Batch (9 نوفمبر 2026): `dotnet build` = 0/0، **69/69** اختبارًا مركّزًا (Identity Admin/Sessions/Permission/Catalog/Media/StoreSettings/Preview/Supplier/Warehouse)، `tsc --noEmit` = 0، Vitest **37/37**، Next production build ناجح، Playwright **6/6** (Account + Admin M5 + Cart Demo على Desktop/Mobile)، و`has-pending-model-changes` نظيف. قاعدة QA تضم 17 Migration، ولم تُطبق على Preview/Owner DB. Email Change verification وNewsletter Consent وWatermark وCheckout/Orders وStock/PO مؤجلة.

### Local Preview على Windows

هذه هي الطريقة الموصى بها لمعاينة وظائف الكتالوج. تفاصيل كاملة في [LOCAL_PREVIEW.md](docs/operations/LOCAL_PREVIEW.md).

```powershell
Copy-Item .env.preview.example .env.preview.local
notepad .env.preview.local
.\scripts\preview.ps1 -Action Start
```

ثم افتح `http://127.0.0.1:3005`. بيانات Owner وكلمة المرور تُقرأ من `.env.preview.local` فقط، ولا توجد كلمة مرور Admin ثابتة.

> ملاحظة مساحة العمل الحالية: نسخة التطوير المنسوخة المعتمدة تعمل على `http://127.0.0.1:3006` مقابل API `http://127.0.0.1:8085`؛ الـPreview الأصلي على `3005` منفصل ويجب عدم تعديل مصدره عند العمل على النسخة المنسوخة. راجع `MODEL_HANDOFF.md` و`NEXT_TASK.md` للحالة الفعلية.

الإيقاف:

```powershell
.\scripts\preview.ps1 -Action Stop
```

لدعم WhatsApp Inquiry، يضيف المالك رقم المتجر الرسمي في `STORE_WHATSAPP_NUMBER` داخل `.env.preview.local` ثم يعيد تشغيل Web. بدون الرقم يظل الزر مخفيًا ولا يُنشأ رابط وهمي.

لإنشاء اختصار سطح المكتب:

```powershell
.\scripts\create-preview-shortcut.ps1
```

الاختصار لا يحتوي أسرارًا ولا يبدأ مع Windows. للإزالة:

```powershell
.\scripts\create-preview-shortcut.ps1 -Remove
```

### Production-like Compose

`compose.yaml` يبني Web بوضع Production، بينما Next API rewrite مفعّل في Development فقط. لذلك لا تُستخدم هذه الخدمة لمعاينة API محليًا قبل تجهيز Caddy؛ استخدم `compose.preview.yaml` للمعاينة.

1. انسخ `.env.example` إلى `.env` إذا كنت تختبر Stack الأساسي.
2. لا تستخدم `down -v` على مشروع يحتوي بيانات تحتاجها.
3. لا تنشئ موارد Docker إضافية أو تحذف Volumes دون تفويض محدد؛ Preview الحالي معزول باسم `hardware-store-preview`.

لو كان PostgreSQL Volume مهيأ من تشغيل سابق، تغيير `POSTGRES_PASSWORD` في `.env` وحده لا يغيّر كلمة مرور الدور المخزنة داخل قاعدة البيانات، وقد يفشل اتصال API. استخدم كلمة المرور الحالية المطابقة للـVolume، أو ادخل إلى PostgreSQL بكلمة المرور الحالية وشغّل `\\password hardware_store` تفاعليًا لتغييرها، ثم حدّث `.env` بالقيمة الجديدة وأعد تشغيل الخدمات. لا تحذف الـVolume ولا البيانات لحل اختلاف كلمة المرور.

ملف `.env` مستبعد من `.gitignore`، كما يستبعده إعداد إنشاء الأرشيف؛ لا يُضاف للأرشيف أو Git مستقبلًا.

التحقق البرمجي المحلي: `dotnet test HARDWARE_STORE.slnx --filter "Category!=RequiresDocker"`، ومن `apps/web`: `npm test`. اختبارات PostgreSQL وPlaywright المتصلة تحتاج بيئة معتمدة.

الصفحات المتاحة: `/categories`, `/products`, `/products/[slug]`, `/cart`, `/checkout`, `/admin`, `/admin/catalog`, `/admin/catalog/products/new`, `/admin/catalog/products/[id]`, `/admin/catalog/products/[id]/preview` وصفحات الحساب. `/admin` هو مساحة إدارة محمية بالصلاحيات وتعرض فقط الوظائف الموجودة، مع تعليم وحدات M3.3/M5+ كغير متاحة. روابط الإدارة داخل Storefront تظهر فقط عندما تعيد `/identity/me` صلاحيات `Catalog.*` الفعلية.

## 6. RIGX Visual Audit & Design Foundation — Phase 1

**الحالة: التدقيق والتأسيس التصميمي مكتملان؛ لا تغييرات على كود التطبيق.** تم فحص Workspace والـPreview بتاريخ 6 نوفمبر 2026. بعد إعادة بناء خدمة Web المصرح بها، تطابقت واجهة Preview على `http://127.0.0.1:3005` مع مصدر Workspace: ظهرت `Components` و`PC Builder` في Header، وبقيت حاويتا API وPostgreSQL دون تغيير. نتائج الصحة: Web `200`، وAPI Live/Ready `200`. طلب `/identity/me` كزائر يرجع `401` متوقعًا؛ لم يُسجل دخول أو تُعدّل بيانات. صور P0 محفوظة في `.backups/RIGX_V3_P0_Source_Parity_2026-11-06/screenshots/`.

### الاتجاه البصري المعتمد لهذه المرحلة

- الحفاظ على هوية RIGX الحالية وألوان Graphite/Orange وSemantic Tokens والخطين الحاليين؛ الديموهات مراجع مبادئ وليست مصادر ألوان أو محتوى أو بيانات.
- بناء خلفية محيطية مستمرة وإضاءة تفاعلية عامة بمؤشر الماوس على الأجهزة المناسبة، مع `pointer-events: none`، وتحديث منخفض التكلفة، وتعطيلها على اللمس وعند `prefers-reduced-motion`.
- استخدام Glass خفيف وانعكاسات حواف وعمق محدود، مع Hover وReveal وFloating وحركات متسقة تخدم التفاعل وتحافظ على القراءة.
- توحيد Micro-interactions الخاصة بالمشاركة والمقارنة والتنقل والأزرار، مع سلوك Touch بديل، Focus ظاهر، واحترام الأداء وRTL/LTR.
- عدم استخدام توهج مفرط أو استبدال دائم للمؤشر، وعدم إضافة ادعاءات عن المخزون أو الضمان أو الشحن أو الأسعار أو العروض ما لم توفرها بيانات وقواعد المشروع الفعلية.

### مراجعة الديموهات الأربعة

الملفات التي تمت مراجعتها كاملة من مجلد Downloads بتاريخ 6 نوفمبر 2026:

| الملف | مبادئ قابلة للاستفادة | ما لا يُنقل إلى RIGX |
|---|---|---|
| `deepseek_html_20261106_a379ea.html` | خلفية داكنة محيطية، Spotlight يتبع المؤشر، Reveal تدريجي، وبنية واضحة للفئات والمنتجات. | ألوان Cyan/Violet، المنتجات والمقاييس الوهمية، وأي وعود شحن أو ضمان. |
| `deepseek_html_20261106_fb93af.html` | معالجة Studio تحريرية، تقسيم الأقسام، Accent برتقالي، وحركة محدودة لحالات التفاعل. | Palette ونسخ العلامة الخاصة بالديمو، الادعاءات التسويقية، والخصم التجريبي. |
| `deepseek_html_20261106_9eb748.html` | تسلسل تصفح هندسي، شبكة/طبقات Blueprint، إبراز المواصفات، وحالات Reduced Motion. | الأزرق/الورق كهوية، خصم التجميعة، أعداد المنتجات والتقييمات والتوصيل وادعاءات الاختبار. |
| `deepseek_html_20261106_88f2fb.html` | تفاعل الفئات والمنتجات، انتقالات مرئية، ونموذج يوضح العلاقة بين واجهة المتجر وأدوات الإدارة. | ألوان Pink/Blue/Lime، القيم الوهمية، لوحة الإدارة المنفصلة، وأي منطق تجارة داخل ملف HTML مستقل. |

### حالة الواجهة ومصادر التنفيذ الحالية

- Header وMega Menu وMobile Drawer في `apps/web/src/components/site-header.tsx`؛ الصفحة المشتركة وطبقات العرض في `app-chrome.tsx` و`site-chrome.tsx`.
- عند Baseline Phase 1 كانت الإضاءة المرتبطة بالمؤشر محلية للبطاقات. أضافت Phase 2 طبقة Visual Engine عامة داخل `app-chrome.tsx`، مع الإبقاء على تفاعل البطاقة المحلي.
- Home والبطاقات والتفاصيل تستخدم `storefront-home.tsx`, `catalog-product-card.tsx`, `catalog-product-view.tsx`, `product-detail-gallery.tsx`، وإعدادات المتجر في `store-settings-provider.tsx`/`store-settings-admin.tsx`. تمت إزالة Compare ومكوناته ومساره بالكامل.
- Listing في `catalog-products-experience.tsx`: البحث والتصنيف والماركة والسعر والفرز في Toolbar؛ الـSidebar للفلاتر التقنية. يمكن تحسينها لاحقًا دون تغيير العقد الحالي، باستثناء أن Facets لا تستقبل Category حاليًا؛ أي إضافة API تحتاج مراجعة واعتماد مستقل.
- القيم من الديموهات مثل الأسعار، المخزون، الضمان، الشحن، الخصومات، أرقام الطلبات والتقييمات لا تُنقل. المرجع هو Catalog API والقرارات المعتمدة فقط.

### أدوار العمل

- عبدالملك صاحب المنتج وصاحب اعتماد النطاق والقرارات التجارية والتصميمية.
- ChatGPT في المحادثة المعمارية يحدد Acceptance Criteria ويراجع الاتجاه والتنفيذ باستقلال.
- DeepSeek هو المنفذ الأساسي داخل Codex بعد اعتماد المهمة. Codex يوفر Workspace الفعلي وأدوات التنفيذ والتحقق؛ لا يعمل منفذان على الملفات نفسها بالتوازي.
- ضمن Phase 1 لم يبدأ أي كود أو Dependency أو Backend/API/Database أو Docker.

### Phase 2 — RIGX Visual Engine Foundation

**الحالة: نُفذت طبقة Visual Engine وPilot على Home وبطاقات منتجاته في 6 نوفمبر 2026.** تم الحفاظ على ألوان Graphite/Orange والخطوط ومصدر بيانات الكتالوج، دون تغيير API أو Backend أو Database أو Dependencies.

1. تم مراجعة `app-chrome.tsx`, `globals.css`, و`catalog-product-card.tsx` ومواقع تركيبها قبل تحديد مكان الإضاءة المشتركة، مع الحفاظ على تأثيرات البطاقات وعدم تكرار listeners.
2. أُضيفت Ambient Background وGlobal Pointer Spotlight كطبقة مشتركة على Pointer دقيق، مع `requestAnimationFrame` وتنظيف listeners عند تغيير المسار أو إخفاء التبويب.
3. يُعطّل Spotlight على اللمس ومع `prefers-reduced-motion`. لا تعترض الطبقة النقر أو التركيز، ويستثني `AppChrome` مسارات `/admin`؛ لم أختبر صفحة Admin بحساب مصادق عليه.
4. أُضيف انعكاس حافة خفيف وFocus مرئي لبطاقات Home فقط عبر خيار `visualEngine` في `CatalogProductCard`؛ لم تتغير قيم الهوية أو الخطوط ولم تُضف Dependencies.
5. ظلت المشاركة وسلوك الميل الحالي كما هي؛ وأُزيلت Compare بالكامل لاحقًا في جولة 9 نوفمبر 2026 بدل تعطيلها فقط.
6. التحقق الفعلي: `npm test` نجح (`7` ملفات، `23` اختبارًا)، و`npm run build` نجح على Next.js `16.4.5`. Playwright على نسخة Next محلية متصلة بـPreview API للقراءة فقط فحص Home وProducts بالعربية/الوضع الداكن والإنجليزية/الوضع الفاتح، على Desktop `1440×1000` وMobile Touch `390×844`: الصفحات `200`، بلا Horizontal Overflow، وظهرت 8 بطاقات Home و9 بطاقات Products. فُحص استمرار الطبقة الثابتة بعد التمرير، وتعطيل Spotlight عند Reduced Motion وعلى Touch.
7. ظهر `401` متوقع لطلب الزائر `/api/v1/identity/me`، ولم يظهر خطأ JavaScript. بيئة Next المحلية منعت الاتصال بـGoogle Fonts (`EACCES`) واستخدمت خطوط fallback؛ لذلك الخطوط في صور After لا تثبت التطابق الطباعي مع Preview. لم تُنفذ اختبارات عبر متصفحات متعددة أو قياس أداء أجهزة منخفضة الإمكانيات.
8. صور Before من `.backups/RIGX_V3_P0_Source_Parity_2026-11-06/screenshots/`، وصور After من `.backups/RIGX_V3_Phase2_After_2026-11-06/screenshots/`.


### Phase 3 — المتجر الكامل والتحقق البصري الشامل (منفذ)

**الحالة: نُفذ في 7 نوفمبر 2026 ضمن جولة توحيد الهوية.** طُبقت أنماط Visual Engine وTokens على Product Details وCompare وCategories وAccount/Auth وAdmin، ووُحّدت Hover/Focus/Micro-interactions، مع الحفاظ على السلوك الوظيفي. التحقق: `tsc --noEmit` = 0، Vitest `28/28`، Next Build ناجح بـ22 Route، وPlaywright غير المتطفل `12/12` مع اختبار Login متجاوز بالتصميم.

### Phase 4 — RIGX Visual Identity Unification (7 نوفمبر 2026)

**الحالة: منفذة للمراجعة على 3006؛ لا تغيير في Backend/Database/API/Dependencies.**

- **الهوية:** Graphite/Obsidian + Ambient Orange. Light = ورق دافئ/Graphite، Dark = Obsidian، Accent برتقالي موحد وGrid تقني. ألوان الأزرق/Cyan أُعيد ربطها بالـAccent عبر Tokens.
- **الخلفية:** طبقة `.rigx-visual-engine` واحدة ثابتة بكامل الـViewport (Grid + 4 Radial Glows + Pointer Glow)، وأُزيل تكرار Gradient من `:root` و`[data-theme]`.
- **الفوتر:** أُعيد بناؤه كطبقة شفافة/متدرجة فوق نفس Canvas بدل `bg-slate-950` و`#0b0f15 !important`، مع Accent Rule وSystem Block وBack-to-top. السبب الجذري كان Gradient مكررًا + كتلة داكنة تحجب الطبقة.
- **المكونات:** Header/Mega Menu/Mobile Drawer، Products/Categories/Product Detail، Cart/Checkout، Login/Auth، Footer، وAdmin Shell/Dashboard موحدة على نفس الـTokens.
- **الخصوصية:** أُزيل مؤشر M4 من واجهة العميل؛ يظهر Badge "نسخة تجريبية" فقط عند `NEXT_PUBLIC_PREVIEW_MODE=true`.
- **التحقق:** `tsc --noEmit` = 0، Vitest `28/28`، Build ناجح (22 Route)، Playwright `12 passed / 1 skipped by design`، وVisual QA على 1440×1000 و390×844 في AR/EN وLight/Dark لكل الصفحات العامة. `footerBg = rgba(0,0,0,0)` في كل الحالات، بلا Horizontal Overflow، وReduced Motion يعطّل Pointer Effect ويمنع الـTransitions غير الضرورية.
- **الحدود:** Admin المحمي لم يُختبر بصريًا بجلسة مصادق عليها لعدم استخدام بيانات دخول/أسرار؛ تم التحقق من Login gate وShell code وBuild. صور المنتجات الحالية Placeholders من Seed وليست عيب واجهة.

