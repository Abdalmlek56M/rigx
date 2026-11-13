# RIGX — M3 Hardening + M5 Supplier Product Catalog + Warehouse Directory + Identity Split

**التاريخ:** 8 نوفمبر 2026  
**Workspace:** `C:\Users\Abdalmlek\Downloads\DEV - Copy\HARDWARE STORE`  
**الأصل المحمي:** `C:\Users\Abdalmlek\Downloads\DEV\HARDWARE STORE` — لم يُعدّل.

---

## 1. الإصلاحات الأمنية

### 1.1 فصل بيانات المورد المالية
- `SupplierView` العام لم يعد يحتوي `bankName` أو `bankAccountNumber` أو `bankIban`.
- أضيف `SupplierFinancialView` ومسارات:
  - `GET /api/v1/admin/purchasing/suppliers/{id}/financials` بصلاحية `Purchasing.ViewSupplierFinancials`.
  - `PUT /api/v1/admin/purchasing/suppliers/{id}/financials` بصلاحية `Purchasing.ManageSupplierFinancials`.
- القراءة والكتابة تُسجلان Audit داخل نفس المعاملة:
  - `Purchasing.SupplierFinancialsViewed`
  - `Purchasing.SupplierFinancialsUpdated`
- قراءة التفاصيل المالية **تفشل مغلقة** إذا تعذر كتابة Audit.
- لا تُكتب القيم البنكية في Audit metadata أو رسائل الخطأ أو الاستجابات العامة.
- صلاحيات Owner مُنحت عبر Migration `20261108143805_M3_SupplierFinancialPermissions`.
- **لا يوجد تشفير تطبيقي جديد**: الحقول ما زالت Plaintext في القاعدة. إضافة تشفير/Key Management قرار تشغيلي مستقل ولم يُنفذ لتجنب مفاتيح داخل الكود أو المشروع.

### 1.2 Supplier Concurrency
- `SupplierUpdateRequest` و`SupplierFinancialWriteRequest` يتطلبان `RowVersion > 0`.
- `SupplierCreateRequest` منفصل بلا RowVersion.
- اختبارات PostgreSQL تثبت:
  - RowVersion = 0 → 400.
  - RowVersion قديم → 409.
  - تحديثان متزامنان بنفس RowVersion → نجاح واحد و409 للآخر.
  - التغيير الناجح لا يُفقد.

### 1.3 Identity / RBAC
- `IdentityDelegationPolicy` يمنع:
  - منح صلاحية غير مملوكة.
  - تعديل حساب/دور أعلى صلاحية.
  - تعيين Owner من غير Owner.
  - تعطيل الذات.
- حماية آخر Owner و`System.ManageUsers` مطبقة داخل نفس معاملة القفل.
- اختبار مباشر: آخر Owner يحاول استبدال دوره بدور لا يملك Owner → 409، مع بقاء الأدوار والجلسات و`AuthorizationVersion` و`RbacRevision`.
- اختبار تزامن: عمليتان تحاولان إزالة آخر صلاحية إدارية → يبقى وصول Owner.
- كل رفض 403/409 يُسجل `Identity.AdministrationRejected`. فشل كتابة Audit يُسجل Warning ولا يخفي الرفض الأصلي.
- `OwnerBootstrapCommand` يرفض `stdin` المعاد توجيهه لكلمات المرور.
- وُثّق أن `IsEmailVerified=true` في Bootstrap تعني تأكيد المشغّل المحلي للملكية، وليست إثبات دورة بريد فعلية. لم تتغير سياسات البريد/Reset.

---

## 2. إعادة التنظيم المعماري

### 2.1 Identity Administration
قبل: ملف واحد `IdentityAdministration.cs` يجمع المستخدمين والأدوار والصلاحيات والتفويض والأقفال وAudit والتطبيع.

بعد:
- `IdentityAdministration.cs` — Facade رقيق يحافظ على العقود الحالية.
- `IdentityUserAdministrationService.cs` — استعلامات/أوامر المستخدمين وتعيين الأدوار.
- `IdentityRoleAdministrationService.cs` — استعلامات/أوامر الأدوار والصلاحيات.
- `IdentityAdministrationSupport.cs` — المعاملة، قفل `authorization_state`، Audit، التفويض، الرفض، والتطبيع.

**لماذا؟** حدود قائمة على نطاق الاستخدام، ومكان واضح لإضافة وظيفة أو إصلاح عيب، مع بقاء Endpoints والاختبارات كما هي. لم تُدخل Microservices/CQRS/MediatR/Generic Repositories.

### 2.2 الوحدات
- `SupplierDirectoryService` بقي متماسكًا ولم يُقسّم قسرًا.
- `SupplierProductCatalogService` جديد داخل Purchasing، منفصل عن Supplier Directory.
- `Inventory` Module جديد لـ`WarehouseDirectoryService` و`InventoryDbContext`.
- `PreviewInitializationService` يطبق Migrations بالترتيب: Identity → Auditing → Catalog → Purchasing → Inventory.

---

## 3. جودة OOP وقابلية الصيانة

- عقود Create/Update منفصلة لكل من Supplier وSupplier Product وWarehouse.
- RowVersion إلزامي في كل Update؛ لا تحقق اختياري.
- `IdentityDelegationPolicy` يبقى منطقًا خالصًا قابلًا للاختبار منفصلًا عن EF.
- `SupplierProductCatalogService` يملك التحقق من التكرار والتفعيل والأقفال والAudit.
- `WarehouseDirectoryService` يملك قواعد Code/Name/Address/Activation فقط.
- كل Endpoint رقيق ويحوّل استثناءات النطاق إلى Problem Details متسق.
- لا تقديم أسعار/MOQ/Lead Times/Stock في نماذج Supplier Product.
- لا يعيد `SupplierView` أي حقل مالي.
- عولجت ثغرة Theme: Bootstrap أصبح يحترم Cookie `hardware-store-theme` عند غياب localStorage بدل تجاوزها بـOS preference.

---

## 4. ميزات M5 المنفذة

### 4.1 Supplier Directory (موجود، مُحصّن)
- Person + SupplierProfile + بحث/تعديل/تعطيل/RowVersion/Audit.
- فصل مالي كامل بصلاحيتين مستقلتين.

### 4.2 Supplier Product Catalog
- `supplier_products`: SupplierProfile ↔ CatalogProductVariant.
- `SupplierSku` + Notes + Activation.
- Unique: (Supplier, Variant) و(Supplier, SupplierSku).
- Create/Update/List/Get/Search/Pagination.
- يمنع Variant غير النشط والتكرار.
- `available-variants` يستبعد Variants المرتبطة بالفعل.
- Admin UI: `/admin/purchasing/suppliers/[id]/products` مع درج ربط وصنف.
- Audit: Linked/Updated/Deactivated.
- **لا أسعار/MOQ/Lead Times/PO/Stock.**

### 4.3 Warehouse Directory
- Module `Inventory` + جدول `warehouses`.
- Code فريد Uppercase، Name، Address، IsActive، RowVersion.
- Create/Update/List/Get/Search/Pagination + Audit.
- Admin UI: `/admin/inventory/warehouses` مع مجموعة Inventory في Shell.
- **لا Stock Items/Balances/Movements/Transfers.**

---

## 5. الاختبارات المنفذة ونتائجها الفعلية

| الاختبار | النتيجة |
|---|---|
| `dotnet build HARDWARE_STORE.slnx` | 0 أخطاء / 0 تحذيرات |
| `IdentityAdministrationPostgreSqlTests` | 7/7 |
| `SupplierDirectoryPostgreSqlTests` | 7/7 |
| `SupplierProductCatalogPostgreSqlTests` | 3/3 |
| `WarehouseDirectoryPostgreSqlTests` | 2/2 |
| `PermissionAuthorizationTests` | 11/11 |
| إجمالي المجموعة المركزة | **30/30** |
| `npx tsc --noEmit` | 0 |
| `npx vitest run` | 40/40 |
| `npm run build` | ناجح |
| Playwright `admin-m5.spec.ts` | 1/1 |
| `has-pending-model-changes` (Identity/Purchasing/Inventory) | لا تغييرات معلقة |

### Visual QA
- Desktop 1440×1000 وMobile 390×844.
- AR/EN وLight/Dark وRTL/LTR.
- Console/Network: لا أخطاء غير متوقعة؛ فقط 401 المتوقعة من `/identity/me` قبل تسجيل الدخول.
- اللقطات في `docs/review/m3-hardening-m5-2026-11-08/`.

### قاعدة الاختبار
- استُخدمت قاعدة معزولة `rigx_m5_qa_20261108` على PostgreSQL Preview `5435`، API على `8091`، وWeb على `3006`.
- تم التحقق من 13 Migration مطبقة في القاعدة المعزولة.
- فحص Audit metadata: 0 سجلات تحتوي Bank/Account/IBAN.
- **Preview/Owner DB لم تُمس.**

---

## 6. الأعمال المؤجلة وأسبابها

| العمل | السبب |
|---|---|
| تطبيق Migrations على Preview/Owner DB | يتطلب اعتمادًا صريحًا ونسخة احتياطية |
| Stock Items/Balances/Movements | Q2.1–Q2.5 غير محسومة |
| Transfers/Reservations/Serials | Q2.1–Q2.5 وQ8.7 غير محسومة |
| Purchase Orders/Receiving | تحتاج قرارات المخزون والتكلفة وQ6.3 |
| أسعار شراء/MOQ/Lead Times | قرارات تجارية/مالية غير معتمدة |
| تشفير بيانات البنك/Key Management | يحتاج قرارًا تشغيليًا ومخزن مفاتيح آمن |
| 21st MCP verification | يحتاج Restart لتحميل `TWENTYFIRST_TOKEN` |
| Context7 | DNS غير متاح |

---

## 7. المخاطر والقرارات التجارية المتبقية

1. **تخزين البيانات البنكية Plaintext:** الحماية الحالية على مستوى الصلاحيات والAudit فقط. قبل الإنتاج يلزم قرار تشفير/Key Management (KMS/Secrets Manager) وعدم وضع مفاتيح في الكود.
2. **Supplier SKU case sensitivity:** Unique index حالي حساس لحالة الأحرف. إذا أراد المالك توحيد حالة الأحرف، يلزم قرار وتعديل Index/تطبيع.
3. **Warehouse master data:** لا Default Warehouse ولا قواعد تخصيص. تحتاج قرارات مخزون.
4. **Supplier Product variant level:** تم اختيار ProductVariant لتجنب Q6.3. إذا تغير نطاق SKU/Barcode، قد يلزم مراجعة العلاقة.
5. **Preview migration order:** Purchasing يعتمد على Catalog Variants. أي Migration Runner مستقبلي يجب أن يحفظ الترتيب Identity → Auditing → Catalog → Purchasing → Inventory.
6. **Theme cookie behavior:** أُصلح لاحترام Cookie عند غياب localStorage؛ SLA/UI regression مغطى بـPlaywright الأساسي.

---

## 8. حالة البوابات المعلقة

| البوابة | الحالة | الأثر |
|---|---|---|
| Q2.1 Serial activation | مفتوحة | تمنع Serials/Stock |
| Q2.2/Q2.3 Reservation/hold extension | مفتوحة | تمنع Reservations/Orders |
| Q2.4 Transfer discrepancy | مفتوحة | تمنع Transfers |
| Q2.5 Partial dispatch | مفتوحة | تمنع Transfers/Receipts |
| Q6.3 SKU/Barcode scope | مفتوحة | لم تمنع Supplier Product Catalog لأنه Variant ID فقط |
| Q8.7 Condition/Warranty/Inspection | مفتوحة | تمنع Receiving/Inspection |
| Production registration | مغلق | لا فتح دون اعتماد |
| Migrations on Preview/Owner DB | غير مطبقة | تحتاج موافقة |

---

## 9. نقطة الاستكمال التالية

1. مراجعة المالك لهذا التقرير واللقطات.
2. نسخة احتياطية، ثم تطبيق Migrations على قاعدة اختبار، ثم Preview بموافقة صريحة.
3. قرار تشفير/Key Management للبيانات البنكية قبل الإنتاج.
4. بعد حسم Q2.1–Q2.5 وQ6.3 وQ8.7: بدء Stock/Transfers/Receipts/PO كشرائح رأسية.
5. Restart OpenCode للتحقق من 21st، وإعادة محاولة Context7 عند توفر DNS.

---

## الملفات الرئيسية المتغيرة

### Backend
- `src/Modules/Identity/IdentityAdministration.cs`
- `src/Modules/Identity/IdentityUserAdministrationService.cs`
- `src/Modules/Identity/IdentityRoleAdministrationService.cs`
- `src/Modules/Identity/IdentityAdministrationSupport.cs`
- `src/Modules/Identity/PermissionCatalog.cs`
- `src/Modules/Identity/OwnerBootstrap.cs`
- `src/Modules/Purchasing/SupplierDirectoryModels.cs`
- `src/Modules/Purchasing/SupplierDirectoryService.cs`
- `src/Modules/Purchasing/SupplierProduct.cs`
- `src/Modules/Purchasing/SupplierProductModels.cs`
- `src/Modules/Purchasing/SupplierProductCatalogService.cs`
- `src/Modules/Purchasing/PurchasingPersistenceRegistration.cs`
- `src/Modules/Inventory/Warehouse.cs`
- `src/Modules/Inventory/WarehouseModels.cs`
- `src/Modules/Inventory/WarehouseDirectoryService.cs`
- `src/Modules/Inventory/InventoryPersistenceRegistration.cs`
- `src/Modules/Inventory/HardwareStore.Modules.Inventory.csproj`
- `src/Host/HardwareStore.Api/PurchasingEndpoints.cs`
- `src/Host/HardwareStore.Api/InventoryEndpoints.cs`
- `src/Host/HardwareStore.Api/OwnerBootstrapCommand.cs`
- `src/Host/HardwareStore.Api/PreviewInitializationService.cs`
- `src/Host/HardwareStore.Api/Program.cs`

### Migrations
- `src/Modules/Identity/Persistence/Migrations/20261108143805_M3_SupplierFinancialPermissions.cs`
- `src/Modules/Purchasing/Persistence/Migrations/20261108162615_M5_SupplierProductCatalog.cs`
- `src/Modules/Identity/Persistence/Migrations/20261108165119_M5_WarehousePermission.cs`
- `src/Modules/Inventory/Persistence/Migrations/20261108170212_M5_WarehouseDirectory.cs`

### Frontend
- `apps/web/src/lib/api-client.ts`
- `apps/web/src/lib/i18n.tsx`
- `apps/web/src/app/layout.tsx`
- `apps/web/src/components/suppliers-admin.tsx`
- `apps/web/src/components/supplier-products-admin.tsx`
- `apps/web/src/components/warehouses-admin.tsx`
- `apps/web/src/components/admin-shell.tsx`
- `apps/web/src/app/admin/purchasing/suppliers/[id]/products/page.tsx`
- `apps/web/src/app/admin/inventory/warehouses/page.tsx`
- `apps/web/tests/e2e/admin-m5.spec.ts`

### Tests
- `tests/HardwareStore.Api.Tests/SupplierDirectoryTests.cs` (7)
- `tests/HardwareStore.Api.Tests/SupplierProductCatalogTests.cs` (3)
- `tests/HardwareStore.Api.Tests/WarehouseDirectoryTests.cs` (2)
- `tests/HardwareStore.Api.Tests/IdentityAdministrationTests.cs` (6)
- `tests/HardwareStore.Api.Tests/PermissionAuthorizationTests.cs` (11)

### Docs
- `README.md`, `PROGRESS.md`, `NEXT_TASK.md`, `ROADMAP.md`, `MODEL_HANDOFF.md`
- `docs/adr/ARCHITECTURE_DECISION_RECORDS.md` (ADR-012–015)
- `docs/api/API_DESIGN_SECURITY.md`
- `docs/architecture/MODULAR_MONOLITH_ARCHITECTURE.md`
- `docs/database/DATABASE_DESIGN_ERD.md`
- `docs/testing/TESTING_STRATEGY.md`
- `docs/operations/FIRST_OWNER_BOOTSTRAP.md`
- `docs/operations/LOCAL_PREVIEW.md`
- `docs/operations/M5_SUPPLIER_DIRECTORY.md`
- `docs/operations/M5_SUPPLIER_PRODUCT_CATALOG.md`
- `docs/operations/M5_WAREHOUSE_DIRECTORY.md`
- `docs/review/m3-hardening-m5-2026-11-08/`

---

## القيود

- لم يُنفذ أي Migration على Preview/Owner DB.
- 3005 Preview و8085 API لم يُعاد بناؤهما ولم تُطبق عليهما الشرائح الجديدة.
- لم تُنفذ Stock/Transfers/Reservations/Serials/Purchase Orders.
- لا Git repo؛ لا Diff.
- الاختبارات الكاملة للـBackend لم تُشغّل كمجموعة واحدة بسبب اعتماد بعضها على Docker/بيئة؛ شُغلت المجموعات المركزة المتأثرة.

---

## 10. أرشيف التسليم

- **الملف:** `HARDWARE_STORE_RIGX_M3_M5_CATALOG_WAREHOUSE_2026-11-08.zip`
- **SHA256:** محسوب في الملف المجاور `HARDWARE_STORE_RIGX_M3_M5_CATALOG_WAREHOUSE_2026-11-08.zip.sha256` (لا تُكتب القيمة داخل الأرشيف نفسه لتجنب التضمين الذاتي).
- **التحقق:** 380 ملفًا، وفُك الأرشيف إلى مجلد مؤقت وتأكد وجود الملفات الجديدة واللقطات والتقرير. لا `.env` فعلي، ولا `node_modules`/`.next`/`bin`/`obj`/`.backups`/Logs/Test Outputs/Old ZIPs.

