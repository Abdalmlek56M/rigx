# سجل القرارات والتوصيات المعمارية (Architecture Decision Records - ADR)

تاريخ التحديث: نوفمبر 2026  
الحالة: **مسودة للمراجعة والاعتماد**  

---

## فهرس القرارات والتوصيات

* [ADR-001: اعتماد منصة .NET 10 للـ Backend](#adr-001-اعتماد-منصة-net-10-للـ-backend) — **معتمد مبدئياً وفق التوجه التقني**
* [ADR-002: اختيار معمارية المونوليث النمطي (Modular Monolith)](#adr-002-اختيار-معمارية-المونوليث-النمطي-modular-monolith) — **معتمد مبدئياً وفق التوجه التقني**
* [ADR-003: قاعدة بيانات PostgreSQL مع EF Core ومقترح UUIDv7](#adr-003-قاعدة-بيانات-postgresql-مع-ef-core-ومقترح-uuidv7) — **معتمد مبدئياً (UUIDv7 مقترح)**
* [ADR-004: اعتماد Next.js و TypeScript لتطبيقات الواجهة الثلاثة](#adr-004-اعتماد-nextjs-و-typescript-لتطبيقات-الواجهة-الثلاثة) — **معتمد مبدئياً وفق التوجه التقني**
* [ADR-005: نموذج التواصل بين الوحدات عبر العقود والأحداث الداخلية](#adr-005-نموذج-التواصل-بين-الوحدات-عبر-العقود-والأحداث-الداخلية) — **مقترح / بانتظار الاعتماد**
* [ADR-006: تتبع الأرقام التسلسلية ككيان اختياري لكل منتج](#adr-006-تتبع-الأرقام-التسلسلية-ككيان-اختياري-لكل-منتج) — **مقترح / بانتظار الاعتماد**
* [ADR-007: نشر المتاجر المستقلة بنموذج النسخ المعزولة (Isolated Instances)](#adr-007-نشر-المتاجر-المستقلة-بنموذج-النسخ-المعزولة-isolated-instances) — **معتمد مبدئياً وفق التوجه التقني**
* [ADR-008: معالجة صور الكتالوج على الخادم باستخدام SkiaSharp](#adr-008-معالجة-صور-الكتالوج-على-الخادم-باستخدام-skiasharp) — **منفذ في M4**
* [ADR-009: عزل Local Preview عن Compose وبيانات التطوير](#adr-009-عزل-local-preview-عن-compose-وبيانات-التطوير) - **منفذ في M4**
* [ADR-010: تفويض إدارة الهوية دون تصعيد صلاحيات](#adr-010-تفويض-إدارة-الهوية-دون-تصعيد-صلاحيات) - **منفذ في M3.3**
* [ADR-011: Supplier Profile في Purchasing مع Party من Identity](#adr-011-supplier-profile-في-purchasing-مع-party-من-identity) - **منفذ في M5**
* [ADR-012: فصل بيانات المورد المالية عن العرض العام](#adr-012-فصل-بيانات-المورد-المالية-عن-العرض-العام) - **منفذ في M5**
* [ADR-013: Supplier Product Catalog على مستوى ProductVariant](#adr-013-supplier-product-catalog-على-مستوى-productvariant) - **منفذ في M5**
* [ADR-014: Warehouse Directory كبيانات رئيسية مستقلة](#adr-014-warehouse-directory-كبيانات-رئيسية-مستقلة) - **منفذ في M5**
* [ADR-015: تقسيم إدارة الهوية إلى Facade وخدمات نطاق](#adr-015-تقسيم-إدارة-الهوية-إلى-facade-وخدمات-نطاق) - **منفذ في M3 hardening**
* [ADR-016: Store Settings كمصدر عام واحد لبيانات المتجر](#adr-016-store-settings-كمصدر-عام-واحد-لبيانات-المتجر) - **منفذ في Wave 1**
* [ADR-017: حالة المنتج New/Used كبيانات صريحة](#adr-017-حالة-المنتج-newused-كبيانات-صريحة) - **منفذ في Wave 1**
* [ADR-018: حماية Preview من اختلاف وجهة Migrations](#adr-018-حماية-preview-من-اختلاف-وجهة-migrations) - **منفذ في Wave 0**
* [ADR-019: معرف جلسة عام + بيانات جهاز منفصلة عن التذكرة](#adr-019-معرف-جلسة-عام-بيانات-جهاز-منفصلة-عن-التذكرة) - **منفذ 9 نوفمبر 2026**
* [ADR-020: حفظ أصل الصورة مع مشتقات العرض](#adr-020-حفظ-أصل-الصورة-مع-مشتقات-العرض) - **منفذ 9 نوفمبر 2026**
* [ADR-021: سلامة إضافة السلة كقيمة إرجاع صريحة](#adr-021-سلامة-إضافة-السلة-كقيمة-إرجاع-صريحة) - **منفذ 9 نوفمبر 2026**
* [ADR-022: دعوة مستخدم Admin بدون كلمة مرور](#adr-022-دعوة-مستخدم-admin-بدون-كلمة-مرور) - **منفذ 9 نوفمبر 2026**
* [ADR-023: Outbox دائم لرسائل الهوية مع Payload مشفّر](#adr-023-outbox-دائم-لرسائل-الهوية-مع-payload-مشفّر) - **منفذ 9 نوفمبر 2026**
* [ADR-024: CMS محتوى آمن داخل Storefront مع فصل Draft/Published](#adr-024-cms-محتوى-آمن-داخل-storefront-مع-فصل-draftpublished) - **منفذ 9 نوفمبر 2026**
* [ADR-025: مكتبة صور قابلة لإعادة الاستخدام مع Usage Guard](#adr-025-مكتبة-صور-قابلة-لإعادة-الاستخدام-مع-usage-guard) - **منفذ 9 نوفمبر 2026**
* [ADR-026: بوابة قرار M5 للمشتريات — Read-only](#adr-026-بوابة-قرار-m5-للمشتريات--read-only) - **مفتوح**
* [ADR-027: مسودات شراء متعددة المصادر (Draft-only)](#adr-027-مسودات-شراء-متعددة-المصادر-draft-only) - **منفذ 9 نوفمبر 2026**

---

### ADR-001: اعتماد منصة .NET 10 للـ Backend
* **الحالة:** معتمد مبدئياً (وفق المادة 6 في `AGENTS.md`).
* **السياق:** الحاجة لمنصة قوية وسريعة وموثوقة تدعم العمليات المعقدة للمخزون والتوافق.
* **القرار:** استخدام C# / ASP.NET Core على .NET 10.

---

### ADR-002: اختيار معمارية المونوليث النمطي (Modular Monolith)
* **الحالة:** معتمد مبدئياً (وفق المادة 6 في `AGENTS.md`).
* **السياق:** تجنب تعقيد الخدمات المصغرة (Microservices) مع الحفاظ على عزل الوحدات وتنظيم النطاقات.
* **القرار:** بناء تطبيق موحد مقسم إلى وحدات (Modules) معزولة منطقياً.

---

### ADR-003: قاعدة بيانات PostgreSQL مع EF Core ومقترح UUIDv7
* **الحالة:** معتمد مبدئياً للـ PostgreSQL؛ مقترح لاستخدام معيار UUIDv7.
* **السياق:** محرك علائقي قوي يدعم العمليات المالية المعقدة والبيانات غير المنظمة (JSONB).
* **المقترح:** استخدام PostgreSQL 17 عبر Entity Framework Core مع مفاتيح `UUIDv7` المتسلسلة زمنياً.

---

### ADR-004: اعتماد Next.js و TypeScript لتطبيقات الواجهة الثلاثة
* **الحالة:** معتمد مبدئياً (وفق المادة 6 في `AGENTS.md`).
* **السياق:** واجهة متجر عامة سريعة تدعم SEO (SSR) مع لوحات إدارة وتشغيل متناسقة وتدعم RTL.
* **القرار:** استخدام Next.js و React و TypeScript. (تفاصيل المكتبات الفرعية تظل خيارات تصميمية قيد التجربة).

---

### ADR-005: نموذج التواصل بين الوحدات عبر العقود والأحداث الداخلية
* **الحالة:** مقترح / بانتظار الاعتماد.
* **السياق:** عزل بيانات كل وحدة ومنع التبعيات الدائرية.
* **المقترح:** استخدام واجهات عقود عامة (`Contracts`) للاستعلامات المباشرة، وأحداث داخل الذاكرة للتنسيق غير المتزامن.

---

### ADR-006: تتبع الأرقام التسلسلية ككيان اختياري لكل منتج
* **الحالة:** مقترح / بانتظار الاعتماد.
* **السياق:** تتبع الضمان ومنع استبدال القطع، مع جعل السيريال اختيارياً بحسب كارت الصنف (`RequiresSerialNumber`) وليس إجبارياً على فئات بأكملها.
* **المقترح:** كيان `SerialNumber` كعنصر مستقل في المخزون يُفعل عند الحاجة.

---

### ADR-007: نشر المتاجر المستقلة بنموذج النسخ المعزولة (Isolated Instances)
* **الحالة:** معتمد مبدئياً.
* **السياق:** عدم بناء منصة SaaS متعددة المستأجرين (Multi-tenant shared DB) في V1، مع إتاحة إعادة نشر النظام كنسخ مستقلة لكل عميل.
* **القرار:** نشر نسخة مستقلة تماماً بحاوياتها وقاعدة بياناتها الخاصة عبر Docker Compose لكل متجر جديد.

---

### ADR-008: معالجة صور الكتالوج على الخادم باستخدام SkiaSharp
* **الحالة:** منفذ في M4.
* **السياق:** فحص MIME/Magic Bytes وحده لا يثبت أن الصورة قابلة للفك، ولا يزيل Metadata أو يضمن حدود الأبعاد/الحجم.
* **القرار:** استخدام SkiaSharp Stable `4.153.2` لفك الصور، فحص الأبعاد قبل الترميز الكامل، إعادة ترميز WebP وتوليد Thumbnail. يبقى مرجع SkiaSharp المُدار داخل Catalog Module، بينما `SkiaSharp.NativeAssets.Linux.NoDependencies` مرجع مباشر في مشروع API التنفيذي لضمان نشر `libSkiaSharp.so` معه. لا يُحفظ الملف الأصلي.
* **تصحيح لاحق:** كان السجل السابق يذكر `4.154.0` ووجود Native Assets داخل Catalog Module. صُحح ذلك بعد فحص توافق النشر إلى `4.153.2` مع فصل أصل Linux التنفيذي إلى API.
* **البدائل المرفوضة:** ImageSharp بسبب نموذج الترخيص، والاعتماد على تحويل المتصفح لأنه لا يقدم تحققًا موثوقًا على الخادم.

---

### ADR-009: عزل Local Preview عن Compose وبيانات التطوير
* **الحالة:** منفذ في M4.
* **السياق:** `compose.yaml` يبني Web في Production mode، بينما API rewrite في Next.js يعمل في Development فقط. استخدامه للمعاينة يخلق تجربة مضللة وقد يلمس بيانات قديمة.
* **القرار:** `compose.preview.yaml` مستقل باسم مشروع `hardware-store-preview`، ومنافذ `5435/8085/3005` وVolumes خاصة. Web يعمل من `Dockerfile.preview` في Development مع `API_ORIGIN`, وBootstrap/Seed on-demand فقط ومن إعدادات `.env.preview.local` غير المحفوظة في Git.

---

### ADR-010: تفويض إدارة الهوية دون تصعيد صلاحيات
* **الحالة:** منفذ في M3.3 hardening (8 نوفمبر 2026).
* **السياق:** `System.ManageUsers` كان يسمح نظريًا بإنشاء دور بكل الصلاحيات أو تعيين `Owner` للنفس، أي تصعيد غير محدود.
* **القرار:** كل عملية إدارة هوية تحمّل صلاحيات الفاعل من قاعدة البيانات داخل نفس معاملة القفل. لا يمنح المدير إلا صلاحيات يملكها، ولا يعدّل حسابًا أو دورًا أعلى منه صلاحية، ولا يعيّن/يزيل `Owner` إلا Owner. يُمنع تعطيل الذات، ويجب أن يبقى Owner نشط واحد على الأقل و`System.ManageUsers` عند مستخدم نشط. القواعد في `IdentityDelegationPolicy` قابلة للاختبار منفصلة عن EF.
* **البدائل المرفوضة:** الاعتماد على Claims العميل، أو إخفاء الأزرار فقط، أو SuperAdmin bypass.
* **الأثر:** رفض 403/409 مع Rollback، واختبارات PostgreSQL للتصعيد ولآخر Owner.

### ADR-011: Supplier Profile في Purchasing مع Party من Identity
* **الحالة:** منفذ كأول شريحة M5 (8 نوفمبر 2026).
* **السياق:** SupplierProfile يملك بيانات تجارية/بنكية، بينما `Person` يملك الهوية و`PersonCode`. أي فصل كامل يفرض معاملة موزعة، وأي دمج كامل يكسر حدود Purchasing.
* **القرار:** `supplier_profiles` في Purchasing؛ `persons` تبقى ملك Identity ومربوطة بـFK. إنشاء/تعديل المورد يستخدم اتصالًا ومعاملة واحدة مع `IdentityDbContext`، و`SupplierPersonReference` للقراءة فقط ومستبعد من Purchasing migrations. Audit في نفس المعاملة.
* **البدائل المرفوضة:** جدول Supplier مستقل بلا Person، أو معاملة موزعة، أو وضع SupplierProfile داخل Identity.
* **الأثر:** معاملة ذرية وحدود بيانات واضحة، مع اعتماد Purchasing على Identity persistence؛ يُستخرج إلى Party Contract عند توسع Purchasing (Purchase Orders/Receipts).

### ADR-012: فصل بيانات المورد المالية عن العرض العام
* **الحالة:** منفذ في M5 (8 نوفمبر 2026).
* **السياق:** `Purchasing.View` كان يعيد بيانات البنك/IBAN مع قائمة الموردين. هذا يوسع نطاق من يحتاج البيانات الحساسة، ويجعل حماية الواجهة وحدها غير كافية.
* **القرار:** عقدان منفصلان: `SupplierView` للعرض العام بلا Bank/Account/IBAN، و`SupplierFinancialView` عبر `/suppliers/{id}/financials`. صلاحيتان مستقلتان: `Purchasing.ViewSupplierFinancials` و`Purchasing.ManageSupplierFinancials`. القراءة الحساسة تُسجل Audit داخل معاملة وتفشل مغلقة إذا تعذر التسجيل. لا تُكتب البيانات البنكية في Audit metadata أو رسائل الخطأ.
* **البدائل المرفوضة:** إخفاء الحقول في الواجهة فقط، أو إعادة البيانات كاملة لمن يملك `Purchasing.View`، أو تشفير تطبيقي بمفاتيح داخل المشروع دون Key Management معتمد.
* **الأثر:** اختبارات PostgreSQL تثبت غياب الحقول من الاستجابة العامة، ووجود Audit للقراءة/الكتابة، وRollback عند فشل Audit. تخزين الحقول الحالي Plaintext ويحتاج قرار Key Management/تشفير تشغيلي قبل الإنتاج.

### ADR-013: Supplier Product Catalog على مستوى ProductVariant
* **الحالة:** منفذ في M5 (8 نوفمبر 2026).
* **السياق:** احتياج المشتريات لمعرفة الأصناف التي يستطيع المورد توريدها دون فتح أسعار أو كميات أو أوامر شراء، ودون حسم Q6.3 (نطاق SKU/Barcode) أو إنشاء Stock.
* **القرار:** جدول `supplier_products` يربط `SupplierProfile` بـ`CatalogProductVariant` بواسطة ID داخلي، مع `SupplierSku` وNotes و`IsActive`. Unique على (Supplier, Variant) و(Supplier, SupplierSku). لا أسعار/MOQ/Lead time/Stock/PO. فعّل العلاقة على Variant النشط فقط، ويُسمح لأكثر من Variant لنفس المنتج وأكثر من Supplier لنفس Variant. Create/Update/List/Get/Search/Pagination + Audit.
* **البدائل المرفوضة:** الربط بـProduct فقط (يفقد التمييز بين الإصدارات)، أو إضافة أسعار شراء الآن (تحتاج قواعد عمل واعتماد مالي)، أو إنشاء SKU جديد داخل Purchasing.
* **الأثر:** شريحة قابلة للاختبار بلا تجاوز بوابات M5؛ Purchasing يقرأ Catalog Variants كمرجع Read-only مستبعد من migrations.

### ADR-014: Warehouse Directory كبيانات رئيسية مستقلة
* **الحالة:** منفذ في M5 (8 نوفمبر 2026).
* **السياق:** `warehouses` مطلوب كأساس لاحق للمخزون والتحويلات، لكن Q2.1–Q2.5 لم تُحسم. إدخال Stock/Balances الآن سيتجاوز البوابة.
* **القرار:** Module `Inventory` جديد، جدول `warehouses` بالحقول Code/Name/Address/IsActive/RowVersion فقط. Code فريد Uppercase، RowVersion إلزامي في Update، Audit داخل المعاملة. لا Stock Items/Balances/Movements.
* **البدائل المرفوضة:** انتظار حسم كل قرارات المخزون، أو إنشاء Stock Items مبدئية، أو وضع Warehouses داخل Purchasing.
* **الأثر:** بيانات رئيسية قابلة للاستخدام لاحقًا مع حد واضح: لا كميات ولا حركات.

### ADR-015: تقسيم إدارة الهوية إلى Facade وخدمات نطاق
* **الحالة:** منفذ في M3 hardening (8 نوفمبر 2026).
* **السياق:** `IdentityAdministrationService` جمع استعلامات وأوامر المستخدمين والأدوار والصلاحيات والتفويض والتطبيع وAudit في ملف واحد، ما يجعل إضافة وظيفة أو إصلاح عيب عالي المخاطر.
* **القرار:** Facade رقيق `IdentityAdministrationService` يحافظ على العقود الحالية؛ `IdentityUserAdministrationService` لاستعلامات/أوامر المستخدمين وتعيين الأدوار؛ `IdentityRoleAdministrationService` للأدوار والصلاحيات؛ `IdentityAdministrationSupport` للمعاملة/القفل/Audit/التفويض/التطبيع. السلوك والـAPI contracts لا تتغير.
* **البدائل المرفوضة:** Microservices أو CQRS/MediatR أو Generic Repositories، أو تقسيم إلى ملفات صغيرة بلا حدود نطاق.
* **الأثر:** مكان واضح لإضافة وظيفة إدارة هوية، واختبارات PostgreSQL الحالية بقيت صالحة.

### ADR-016: Store Settings كمصدر عام واحد لبيانات المتجر
* **الحالة:** منفذ في Wave 1 (9 نوفمبر 2026).
* **السياق:** رقم WhatsApp وروابط Social وعنوان الاستلام ونصوص الثقة كانت موزعة بين env/كود/محتوى ثابت، ما يجعل كل تحديث يحتاج نشرًا.
* **القرار:** Module `Storefront` وجدول Singleton `store_settings`، مع Public GET وAdmin GET/PUT بصلاحية `System.StoreSettings` وCSRF/RowVersion/Audit. الحقول اختيارية، وأي حقل فارغ يُخفي العنصر. لا قيم افتراضية أو أرقام مخترعة.
* **البدائل المرفوضة:** الاعتماد على env فقط، أو تخزين الإعدادات في جدول إعدادات عام بلا صلاحية مستقلة، أو عرض روابط وهمية.
* **الأثر:** إدارة محتوى عام بدون نشر؛ لا أسرار أو بيانات دفع.

### ADR-017: حالة المنتج New/Used كبيانات صريحة
* **الحالة:** منفذ في Wave 1 (9 نوفمبر 2026).
* **السياق:** المتجر يحتاج تمييز الجديد/المستعمل ولا يجوز استنتاجه من السعر أو النص.
* **القرار:** `CatalogProductCondition` على `CatalogProduct` بقيم New/Used، Default New، Check Constraint، Migration. تظهر Badge للمستعمل، ويوجد فلتر `condition` في Catalog API/UI، ويُدار من Admin.
* **البدائل المرفوضة:** استنتاج الحالة من Price/SKU، أو إضافتها كـTag حر بلا قيد.
* **الأثر:** بيانات صريحة قابلة للفلترة؛ لا تغيير في Stock أو Pricing.

### ADR-018: حماية Preview من اختلاف وجهة Migrations
* **الحالة:** منفذ في Wave 0 (9 نوفمبر 2026).
* **السياق:** Preview initialization كان يطبق DDL عبر `Migrations` بينما runtime يقرأ من `Default`؛ اختلاف Host/Port/Database قد يهيئ قاعدة غير التي يخدمها التطبيق.
* **القرار:** قبل أي DDL، يُقارن Host/Port/Database بين الاتصالين ويُسمح باختلاف User/Password فقط. يُرفض الاختلاف برسالة واضحة. اختبار PostgreSQL يستدعي التهيئة كاملة ويتحقق من الجداول والتكرار الآمن.
* **البدائل المرفوضة:** الاعتماد على مراجعة بشرية فقط، أو مقارنة Connection String كاملة (تفشل مع اختلاف credentials المشروع).
* **الأثر:** يمنع توجيه DDL لقاعدة خاطئة قبل حدوثه.

### ADR-019: معرف جلسة عام + بيانات جهاز منفصلة عن التذكرة
* **الحالة:** منفذ 9 نوفمبر 2026.
* **السياق:** `auth_sessions` كان مفتاحه Hash فقط ولا يوجد معرف عام آمن لإدارة جلسة بعينها، ولا بيانات جهاز للعرض.
* **القرار:** `PublicId` عشوائي فريد، `UserAgent` و`IpAddress` داخليان، ولا يُعاد `SessionKeyHash` أو `ProtectedTicket`. List/Revoke يمران عبر PublicId مقيّد بـ`UserAccountId` لمنع IDOR، مع Audit.
* **البدائل المرفوضة:** عرض Hash/Mask من المفتاح، أو استخدام مفتاح الجلسة نفسه كمعرف عام.
* **الأثر:** يمكن إدارة Sessions/Devices دون كشف أسرار، مع الحفاظ على DatabaseTicketStore كمسار الجلسات الوحيد.

### ADR-020: حفظ أصل الصورة مع مشتقات العرض
* **الحالة:** منفذ 9 نوفمبر 2026.
* **السياق:** Pipeline السابق كان ينتج WebP/Thumbnail فقط، ولا يحفظ الأصل، ما يمنع إعادة المعالجة أو Watermark لاحقًا.
* **القرار:** حفظ البايتات الأصلية في `.original` مستقلة، مع `.webp` وThumbnail، وعمود `original_storage_key`. Public endpoint يخدم `.webp` فقط، وMedia Audit/Cleanup يتعامل مع الأصل. Watermark غير مفعل حتى توفير شعار رسمي.
* **البدائل المرفوضة:** حفظ الأصل داخل DB، أو Overlay على المصدر، أو اختراع Wordmark نصي.
* **الأثر:** إمكانية إعادة التوليد وحماية الأصل؛ Legacy يبقى بدون أصل مزعوم.

### ADR-021: سلامة إضافة السلة كقيمة إرجاع صريحة
* **الحالة:** منفذ 9 نوفمبر 2026.
* **السياق:** `addCartItem` كان يعيد مصفوفة فقط، والـProvider يعرض Toast نجاح حتى عند بلوغ الحد أو رفض الإضافة.
* **القرار:** `tryAddCartItem` يعيد `{items,status}` بحالات Added/Incremented/MaxLines/MaxQuantity/Invalid، والـProvider لا يعرض Toast نجاح إلا للنجاح، ويعرض Notice خطأ للرفض. تُفرض حدود 99/50، وتُلتقط أخطاء localStorage. لا Quick Add لبطاقة سعرها يبدأ من Variant مختلف؛ يوجّه للتفاصيل.
* **البدائل المرفوضة:** الاعتماد على مقارنة طول المصفوفة بعد `setState`، أو استخدام Variant افتراضي عشوائي.
* **الأثر:** لا رسالة نجاح كاذبة، ولا إضافة صامتة، وسعر/اختيار Variant متسقان.

### ADR-022: دعوة مستخدم Admin بدون كلمة مرور
* **الحالة:** منفذ 9 نوفمبر 2026.
* **السياق:** إنشاء مستخدم بكلمة مرور مؤقتة يعرّض الأسرار للنقل/العرض، ويخالف مبدأ عدم كشف كلمات المرور.
* **القرار:** Admin ينشئ الحساب بدون PasswordHash، ويُصدر Email Verification token أحادي الاستخدام (24h)، ويُرسل رابط التفعيل. لا يُعاد Token في API. Roles تُفحص بـDelegation، وكل شيء داخل معاملة + Audit. Password Reset الإداري يرسل رابط Reset بدل كلمة مؤقتة.
* **البدائل المرفوضة:** كلمة مرور افتراضية/مؤقتة تُعرض للمدير، أو إرسال Password في البريد.
* **الأثر:** لا كلمات مرور في API/Logs، ويظل الحساب غير قابل للدخول حتى التفعيل.

### ADR-023: Outbox دائم لرسائل الهوية مع Payload مشفّر
* **الحالة:** منفذ 9 نوفمبر 2026.
* **السياق:** `IdentityEmailDispatcher` كان Channel في الذاكرة؛ توقف التطبيق قبل الإرسال يفقد الرسالة، ولا يوجد Retry أو Dead Letter.
* **القرار:** جدول `identity_email_outbox` داخل Identity يحفظ Payload مشفّرًا عبر ASP.NET Data Protection (`HardwareStore.IdentityEmailOutbox.v1`). الحالات Pending/Processing/Sent/Failed/DeadLetter، مع Attempts وNextAttemptAt وBackoff أُسّي وDead Letter بعد 5 محاولات، وClaim عبر `FOR UPDATE SKIP LOCKED`. يُمسح الـPayload بعد الإرسال، ولا تُسجّل الروابط أو Tokens في Logs/API. مفتاح الحماية يُثبّت على Volume عبر `DataProtection:KeyRingPath`.
* **البدائل المرفوضة:** Channel في الذاكرة، أو حفظ URL/Token كنص صريح، أو قائمة أنواع رسائل مفتوحة (يُسمح فقط بـVerification/PasswordReset/SecurityNotification).
* **القيد المعروف:** التسجيل/الدعوة يضعان الرسالة في Outbox بعد Commit مباشرة عبر Context مستقل؛ إذا سقطت العملية في تلك الفجوة يبقى الحساب معلّقًا ويمكن استخدام Resend. الرسالة نفسها durable بعد الإدراج.
* **الأثر:** اختبارات PostgreSQL للتشفير، الفشل، Backoff، Dead Letter، وإعادة التشغيل مع Key Ring دائم.

### ADR-024: CMS محتوى آمن داخل Storefront مع فصل Draft/Published
* **الحالة:** منفذ 9 نوفمبر 2026.
* **السياق:** `/admin/home-content` كان Prototype في `localStorage`، والمقالات/FAQ/السياسات غير موجودة.
* **القرار:** محتوى الواجهة مملوك لـStorefront (`homepage_layouts`, `content_articles`, `content_article_revisions`, `content_faqs`, `content_policies`). الصفحة الرئيسية تحفظ Layout JSON بأنواع Sections/Rails معروفة فقط، ولها صف Draft وصف Published؛ النشر يتحقق من وجود Product/Category/Brand المشار إليها. المقالات تُعرض كنص عادي (لا HTML/JavaScript)، وكل حفظ/نشر يضيف Revision. السياسات نسخ مرقمة غير قابلة للتعديل بعد النشر. الشعار/Rich Text/HTML غير مسموح.
* **البدائل المرفوضة:** localStorage، أو قبول HTML خام، أو Block Types عشوائية، أو تحرير نسخة سياسة منشورة.
* **الأثر:** Public pages تقرأ Published فقط، وFallback آمن للـLayout الافتراضي، وSEO/Sitemap للمقالات.

### ADR-025: مكتبة صور قابلة لإعادة الاستخدام مع Usage Guard
* **الحالة:** منفذ 9 نوفمبر 2026.
* **السياق:** Media Pipeline يحفظ الأصل والـDerivatives لصور المنتجات فقط، ولا يوجد Asset محتوى قابل لإعادة الاستخدام أو منع حذف مستخدم.
* **القرار:** Catalog يملك `content_media_assets` و`content_media_asset_usages`، ويستخدم نفس `ICatalogMediaStorage` تحت `content/{assetId}`. Replace يكتب فوق نفس المفاتيح فلا تنكسر المراجع. Delete حذف منطقي ويُرفض (409) إذا كان الأصل مستخدمًا في محتوى منشور. Storefront يسجّل الاستخدام عبر `IContentMediaUsageSink`. Watermark معطّل لعدم وجود شعار معتمد.
* **البدائل المرفوضة:** تخزين جديد منفصل عن Volume الحالي، أو حذف فيزيائي للملفات، أو Watermark نصي.
* **القيد المعروف:** تسجيل الاستخدام يحدث بعد حفظ المقال؛ فشله لا يُفشل حفظ المقال ويُوثّق كفجوة مراقبة.

### ADR-026: بوابة قرار M5 للمشتريات — Read-only
* **الحالة:** مفتوح، لم يُحسم تجاريًا.
* **السياق:** `docs/operations/M5_INVENTORY_READINESS.md` يقترح Purchase Order Draft، بينما `OPEN_QUESTIONS.md`/`ROADMAP.md` يمنعان البدء قبل Q2.1–Q2.5 وQ6.3 وQ8.7.
* **القرار:** في هذه الدفعة لا تُنشأ جداول أو APIs أو Migrations لـPurchase Orders/Goods Receipt/Stock Ledger/Serials/Transfers/Reservations. الفرق بين Draft Request وPO Draft وApproved/Sent PO وGoods Receipt وStock Posting موثّق كقرار مالك مطلوب.
* **المطلوب من المالك:** اعتماد تعريف البوابة: هل يُسمح بـDraft-only Purchase Request لا يمس Stock/Cost، أم يبقى كل Purchasing معطّلًا حتى اعتماد Q2.1–Q2.5/Q6.3/Q8.7.
* **الأثر:** بقية الدفعة تستمر، والمخزون يظل Read-only.

### ADR-027: مسودات شراء متعددة المصادر (Draft-only)
* **الحالة:** منفذ 9 نوفمبر 2026 بقرار مالك صريح في تكليف Main Engineering.
* **السياق:** ليس كل شراء من Supplier مسجل؛ قد يكون من Marketplace أو بائع فردي أو مصدر آخر. فرض Supplier FK كان سيمنع تسجيل مسودات شراء مشروعة.
* **القرار:** جدول `purchase_drafts` + `purchase_draft_items` في Purchasing، مع `SourceType` (RegisteredSupplier/Marketplace/IndividualSeller/Other)، `SupplierId` اختياري (إلزامي فقط للمورد المسجل)، وبيانات البائع الخارجي مستقلة. الحالة Draft/Cancelled فقط، والمبالغ تخطيطية ولا تُرحّل. لا إنشاء SupplierProfile تلقائيًا، ولا Stock/Cost/Ledger/PO/GRN.
* **الصلاحيات:** القراءة بـ`Purchasing.View` والكتابة بـ`Purchasing.CreateOrder` مؤقتًا كصلاحية إنشاء قائمة معتمدة، دون إنشاء Purchase Order.
* **البدائل المرفوضة:** فرض Supplier FK، أو إنشاء مورد لكل بائع فردي، أو تحويل Draft إلى PO/Stock.
* **الأثر:** مرونة تشغيلية حقيقية مع فصل واضح عن المشتريات الرسمية والمخزون. transitions إلى Approved/Received تحتاج بوابة المالك.
