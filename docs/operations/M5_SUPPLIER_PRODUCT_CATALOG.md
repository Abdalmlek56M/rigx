# M5 — Supplier Product Catalog Vertical Slice

## الهدف والحدود

تسجيل الأصناف التي يستطيع المورد توريدها، مع كود المورد وملاحظات وحالة تفعيل، دون إدخال أسعار شراء أو تكاليف أو MOQ أو Lead Times أو Purchase Orders أو Stock.

الربط يتم على مستوى **`CatalogProductVariant`** وليس Product، لأن سعر/مخزون/باركود/تكوين الشراء يميز الـVariant. هذا يتجنب حسم Q6.3 (نطاق SKU/Barcode) ولا ينشئ هوية صنف جديدة.

## البيانات

`supplier_products`:

| العمود | النوع/القيد |
|---|---|
| `id` | UUIDv7، PK |
| `supplier_profile_id` | FK `supplier_profiles`, Restrict |
| `product_variant_id` | FK `product_variants`, Restrict |
| `supplier_sku` | `varchar(128)` إلزامي |
| `notes` | `varchar(2000)` اختياري |
| `is_active` | boolean، default true |
| `created_at_utc` / `updated_at_utc` | timestamptz |
| `row_version` | bigint، concurrency token |
| Unique | `(supplier_profile_id, product_variant_id)` |
| Unique | `(supplier_profile_id, supplier_sku)` |

- `CatalogProductVariant` يُقرأ من Purchasing كمرجع Read-only (`SupplierVariantReference`) مستبعد من Purchasing migrations.
- يمكن ربط أكثر من Variant لنفس المنتج، وأكثر من Supplier لنفس Variant.
- لا يُسمح بربط Variant غير نشط.
- لا يمكن تكرار نفس Variant أو نفس Supplier SKU لنفس المورد.

## الـAPI

| Method | Route | الصلاحية |
|---|---|---|
| GET | `/api/v1/admin/purchasing/suppliers/{supplierId}/products` | `Purchasing.View` |
| GET | `/api/v1/admin/purchasing/suppliers/{supplierId}/products/{id}` | `Purchasing.View` |
| POST | `/api/v1/admin/purchasing/suppliers/{supplierId}/products` | `Purchasing.ManageSuppliers` |
| PUT | `/api/v1/admin/purchasing/suppliers/{supplierId}/products/{id}` | `Purchasing.ManageSuppliers` |
| GET | `/api/v1/admin/purchasing/suppliers/{supplierId}/available-variants` | `Purchasing.View` |

- GET: `no-store`، بحث/حالة/ترقيم.
- POST: `ProductVariantId`, `SupplierSku`, `Notes`, `IsActive`.
- PUT: `SupplierSku`, `Notes`, `IsActive`, `RowVersion` إلزامي > 0.
- Audit: `Purchasing.SupplierProductLinked`, `Purchasing.SupplierProductUpdated`, `Purchasing.SupplierProductDeactivated` مع Supplier/Product IDs فقط.
- CSRF عبر Middleware الحالي.

## الواجهة

- `/admin/purchasing/suppliers/[id]/products` — جدول Product/Edition/Catalog SKU/Supplier code/Status/Actions.
- رابط "Supplier products" من صف المورد في دليل الموردين.
- درج ربط صنف: بحث Variants، اختيار Variant، Supplier code، Notes، Activation.
- بحث/فلترة/Pagination، وحالات Loading/Empty/Error.
- يظهر زر الربط فقط لـ`Purchasing.ManageSuppliers`؛ العرض لـ`Purchasing.View`.

## Migration

`src/Modules/Purchasing/Persistence/Migrations/20261108162615_M5_SupplierProductCatalog.cs`

**لم تُطبق على Preview/Owner DB.** تتطلب Catalog migrations أولًا لوجود `products` و`product_variants`.

## الاختبارات

`SupplierProductCatalogPostgreSqlTests` — 3/3:

1. ربط Variant + Supplier SKU + Audit + بحث/Options + Update/Deactivate.
2. رفض تكرار العلاقة، تكرار Supplier SKU، وVariant غير نشط.
3. تحديثان متزامنان بنفس RowVersion: نجاح واحد و409 للآخر.

Playwright Admin M5 يغطي ربط أول Variant من الواجهة وحفظ Supplier SKU.

## القيود

- لا أسعار شراء/عملات/MOQ/Lead Times/Costing.
- لا Purchase Orders/Receiving/Stock.
- لا تغيير في SKU/Barcode أو Catalog identity.
- لا حذف للربط؛ التعطيل فقط.
