# M5 — Warehouse Directory Vertical Slice

## الهدف والحدود

تعريف المستودعات كبيانات رئيسية (Code/Name/Address/Active) لإتاحة استخدامها لاحقًا في Stock/Transfers، دون إدخال كميات أو أرصدة أو حركات أو سيريالات أو تحويلات.

## القرار

- Module `Inventory` جديد وحدود واضحة عن Purchasing وCatalog.
- جدول `warehouses` فقط.
- `Code` فريد Uppercase (حتى 32 حرفًا).
- `Name` إلزامي (حتى 128).
- `Address` اختياري (حتى 255).
- `IsActive` و`RowVersion` إلزامي في Update.
- Audit في نفس المعاملة.

## الـAPI

| Method | Route | الصلاحية |
|---|---|---|
| GET | `/api/v1/admin/inventory/warehouses` | `Inventory.View` |
| GET | `/api/v1/admin/inventory/warehouses/{id}` | `Inventory.View` |
| POST | `/api/v1/admin/inventory/warehouses` | `Inventory.ManageWarehouses` |
| PUT | `/api/v1/admin/inventory/warehouses/{id}` | `Inventory.ManageWarehouses` |

- GET: `no-store`، بحث/حالة/ترقيم.
- POST/PUT: CSRF عبر Middleware الحالي.
- Audit: `Inventory.WarehouseCreated`, `Inventory.WarehouseUpdated`, `Inventory.WarehouseDeactivated` مع Warehouse ID والحالة فقط.
- RowVersion مفقود/قديم → 400/409.

## الواجهة

- `/admin/inventory/warehouses` داخل مجموعة Inventory في Admin Shell.
- جدول Desktop + بطاقات Mobile.
- بحث/حالة/Pagination.
- درج إنشاء/تعديل مع Code/Name/Address/Active.
- زر الإدارة يظهر فقط لـ`Inventory.ManageWarehouses`؛ العرض لـ`Inventory.View`.

## Migration

- `src/Modules/Identity/Persistence/Migrations/20261108165119_M5_WarehousePermission.cs`
- `src/Modules/Inventory/Persistence/Migrations/*_M5_WarehouseDirectory.cs`

**لم تُطبق على Preview/Owner DB.** لا تعتمد على Catalog/Purchasing.

## الاختبارات

`WarehouseDirectoryPostgreSqlTests` — 2/2:

1. Create/Update/List/Search/Deactivate/Audit + رفض تكرار Code.
2. RowVersion مفقود 400، Stale 409، وتحديثان متزامنان: نجاح واحد و409 للآخر.

Playwright Admin M5 يغطي إنشاء مستودع وتعديله على Desktop، ولقطة Mobile.

## القيود

- لا Stock Items أو Balances أو Movements.
- لا Transfers أو Receipts أو Reservations أو Serials.
- لا حذف مستودع؛ التعطيل فقط.
- لا تحديد Default Warehouse أو قواعد تخصيص قبل قرارات المخزون.
