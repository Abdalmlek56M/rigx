# M5 — Supplier Directory Vertical Slice

## لماذا هذه الشريحة؟

بوابات M5 المعلقة:

| البوابة | الموضوع | هل تمنع Supplier Directory؟ |
|---|---|---|
| Q2.1 | سياسة تفعيل السيريال | لا — لا سيريالات ولا stock |
| Q2.2 / Q2.3 | حجز المخزون وتمديده | لا — لا reservations |
| Q2.4 | تسويات عجز التحويل | لا — لا transfers |
| Q2.5 | Partial Dispatch | لا — لا transfers/receipts |
| Q6.3 | نطاق SKU/Barcode | لا — لا Stock، والربط بـProductVariant ID فقط |
| Q8.7 | Condition/Warranty/Inspection | لا — لا receiving/inspection |

لذلك اختير **Supplier Directory** كأول شريحة M5 آمنة ومستقلة، لأنها:
- بيانات تجارية حقيقية (شركة، ضرائب، سجل تجاري، اتصال، شروط سداد).
- أساس لازم لاحقًا لأوامر الشراء والاستلام دون تجاوز بوابات المخزون.
- لا تنشئ أو تعدل أي كمية/تكلفة/سيريال/SKU.
- قابلة للاختبار على PostgreSQL مستقلة.

أُضيفت بعدها شريحتان مستقلتان: [Supplier Product Catalog](M5_SUPPLIER_PRODUCT_CATALOG.md) و[Warehouse Directory](M5_WAREHOUSE_DIRECTORY.md).

## النطاق المنفذ

- جدول `supplier_profiles` في Purchasing.
- `Person` في Identity مع `IsSupplier=true` و`PersonCode`.
- Create/Update/List/Get فقط؛ لا Delete.
- بحث وفلترة وترقيم، وتفعيل/تعطيل.
- منع تكرار Tax ID / Commercial Registration (Unique Partial Indexes).
- Concurrency عبر `row_version` إلزامي في Update.
- Audit في نفس معاملة الإنشاء/التعديل.
- **فصل البيانات المالية:** العرض العام لا يحتوي Bank/Account/IBAN؛ القراءة من `/financials` بصلاحية مستقلة، وتُدقق وتفشل مغلقة عند تعذر Audit.

## القرارات

1. **SupplierProfile في Purchasing** و**Person في Identity**، مع FK إلى `persons` (ADR-011).
2. **معاملة واحدة** باستخدام اتصال Purchasing وربط Identity به، لضمان ذرية Person+Profile+Audit.
3. `SupplierPersonReference` قراءة فقط ومستبعد من Purchasing migrations؛ لا يملك Purchasing مخطط persons.
4. `is_active` و`row_version` حقول تشغيلية في SupplierProfile.
5. **فصل مالي:** `SupplierView` و`SupplierFinancialView` عقدان مستقلان، وصلاحيتان `Purchasing.ViewSupplierFinancials` و`Purchasing.ManageSupplierFinancials` (ADR-012).
6. لا Purchase Orders أو Stock في هذه الشريحة. Warehouse Directory وSupplier Product Catalog شرائح منفصلة.

## الـAPI

| Method | Route | الصلاحية |
|---|---|---|
| GET | `/api/v1/admin/purchasing/suppliers` | `Purchasing.View` |
| GET | `/api/v1/admin/purchasing/suppliers/{id}` | `Purchasing.View` |
| POST | `/api/v1/admin/purchasing/suppliers` | `Purchasing.ManageSuppliers` |
| PUT | `/api/v1/admin/purchasing/suppliers/{id}` | `Purchasing.ManageSuppliers` |
| GET | `/api/v1/admin/purchasing/suppliers/{id}/financials` | `Purchasing.ViewSupplierFinancials` |
| PUT | `/api/v1/admin/purchasing/suppliers/{id}/financials` | `Purchasing.ManageSupplierFinancials` |

- GET: `no-store`.
- POST/PUT: CSRF عبر Middleware الحالي.
- Actor ID من Claims؛ Audit يسجل Actor دون أسرار.
- لا تُعاد بيانات بنكية في `GET /suppliers` أو `GET /suppliers/{id}`.
- `SupplierUpdateRequest` و`SupplierFinancialWriteRequest` يتطلبان `RowVersion > 0`؛ `SupplierCreateRequest` منفصل بلا RowVersion.
- `GET /financials` يسجل `Purchasing.SupplierFinancialsViewed`، و`PUT /financials` يسجل `Purchasing.SupplierFinancialsUpdated`، وكلاهما داخل المعاملة ويفشل مغلقًا إذا تعذر Audit.

## Migration

- `src/Modules/Identity/Persistence/Migrations/20261108143805_M3_SupplierFinancialPermissions.cs`
- `src/Modules/Purchasing/Persistence/Migrations/20261108092628_M5_SupplierDirectory.cs`

**لم تُطبق على Preview/Owner DB.** التطبيق يتطلب اعتمادًا منفصلًا ونسخة احتياطية:

```powershell
dotnet ef database update --project src/Modules/Identity `
  --startup-project src/Host/HardwareStore.Api --context IdentityDbContext
dotnet ef database update --project src/Modules/Purchasing `
  --startup-project src/Host/HardwareStore.Api --context PurchasingDbContext
```

قبل التطبيق:
- نسخة احتياطية.
- التأكد من وجود `persons` و`product_variants` (Identity/Catalog migrations) قبل Purchasing.
- مراجعة SQL المولد و`has-pending-model-changes`.
- تطبيق على قاعدة اختبارية أولًا.

## الاختبارات

`SupplierDirectoryPostgreSqlTests` على PostgreSQL 17/Testcontainers:

1. إنشاء Person+Profile+Audit، بحث، تحديث، تعطيل، منع تكرار Tax.
2. تحديثان متزامنان بنفس `row_version`: نجاح واحد و409 للآخر.
3. فشل Audit → Rollback كامل للـPerson والـProfile.
4. رفض Email/Payment Terms غير الصالحة.
5. فصل المالي: `GET` العام لا يحتوي Bank/IBAN، وقراءة/كتابة Financials تُدقق.
6. فشل Audit في قراءة Financials → فشل مغلق.
7. RowVersion مفقود/قديم → 400/409 دون فقدان التغيير الناجح.

**النتيجة الفعلية:** 7/7.

## القيود

- لا Purchase Orders بعد؛ تحتاج Warehouses/ProductVariant وقرار Q6.3.
- لا Receiving/Stock/Costing.
- لا حذف مورد؛ التعطيل فقط للحفاظ على التاريخ.
- بيانات البنك محفوظة Plaintext حاليًا؛ لا تظهر للعملاء ولا في Audit. يلزم قرار Key Management/تشفير تشغيلي قبل الإنتاج.
- Migration غير مطبقة على Preview/Owner DB.
- لا M5 موسع (Reservations/Transfers/Serials) قبل حسم Q2.1–Q2.5 وQ8.7.
