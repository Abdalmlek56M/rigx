# M5 Inventory Readiness Audit — 9 نوفمبر 2026

> **Read-only audit.** لم يُنشأ Stock أو Purchase Order أو Movement في هذه الجولة. القرارات Q2.1–Q2.5 وQ6.3 وQ8.7 ما زالت مفتوحة.

## 1. ما هو منفذ فعليًا

| المجال | المنفذ | القيود |
|---|---|---|
| Supplier Directory | `supplier_profiles` + Person + بحث/RowVersion/Audit + financial permissions | لا أسعار شراء أو PO |
| Supplier Product Catalog | `supplier_products` يربط SupplierProfile بـ`product_variants` + SupplierSku/Notes/Active | لا Cost/MOQ/Lead time/Stock |
| Warehouse Directory | `warehouses` (Code/Name/Address/Active/RowVersion) | لا Stock Items/Balances/Movements |
| Catalog | Product/Variant/Spec/Image + Condition New/Used | لا Stock quantity |
| Identity/RBAC | Permissions + Audit + sessions | Sessions/Devices سيُستكملان في هذه الجولة |

## 2. ما ينقص لاستقبال مشتريات حقيقي

### 2.1 Purchase Order
- `purchase_orders`, `purchase_order_items` مع:
  - SupplierProfileId, WarehouseId, Status, Currency, ExpectedAtUtc, Notes, totals.
  - Unit cost snapshot, tax/discount policy (Q1.2/Q1.5).
- قواعد: لا إرسال PO لمورد غير نشط، ولا بند Variant غير نشط، ولا تكرار Variant في نفس PO بدون قرار.
- Permissions: `Purchasing.CreateOrder`, `Purchasing.ApproveOrder` (غير موجودة حاليًا، تحتاج إضافة).
- Audit + RowVersion + Idempotency.

### 2.2 Goods Receipt (GRN)
- `goods_receipts`, `goods_receipt_items` مع POItemId, received qty, rejected qty, batch/serial data.
- **موانع:** Q2.1 (Serial policy)، Q2.4 (تكلفة/تسوية)، Q8.7 (Condition/Warranty/Inspection).
- لا يمكن ترحيل Stock/Batch قبل حسم Costing (Q1.1) وسياسة السيريال.

### 2.3 Stock Ledger
- `stock_items`, `inventory_batches`, `inventory_batch_balances`, `inventory_movements`.
- قواعد: No Negative Stock، Batch custody، Warehouse balance، Row locks، `FOR UPDATE`، idempotent movement.
- **موانع:** Q2.1، Q2.4، Q1.1 (Cost)، Q2.5 (Partial Dispatch).

### 2.4 Serial Registration
- `serial_numbers` مع Custody lifecycle (`InStock/Reserved/InTransit/Sold/InQuarantine/...`).
- يحتاج قرار Q2.1: هل التتبع لكل Variant أم حسب `RequiresSerialNumber`؟
- يحتاج تعريف حالة Condition (`NEW/OPEN_BOX/USED`) وربطها بالمنتج/الاستلام.

### 2.5 Transfer / Reservation
- `stock_transfers`, `stock_transfer_items`, `stock_transfer_receipts`, `stock_reservations`.
- موانع: Q2.2/Q2.3/Q2.4/Q2.5.

## 3. ما يؤثر على تكلفة الوحدة

- **Q1.1 Costing:** Specific Identification / FIFO / WAC — لم يُحسم.
- **Q2.4 Transfer discrepancy:** تكلفة الفروق واعتماد التسوية.
- **Q1.5 Shipping VAT:** هل يضاف على الشحن؟
- **Q1.2 VAT:** هل الأسعار شاملة أم غير شاملة؟

لا يجوز بناء `unit_cost` أو `COGS` قبل اعتماد هذه القواعد.

## 4. ما يمكن تنفيذه لاحقًا دون تغيير سياسات تجارية

1. **Purchase Order Draft بدون Stock/Cost posting:**
   - إنشاء PO مسودة + بنود من SupplierProduct + RowVersion/Audit.
   - لا استلام ولا ترحيل مخزون.
2. **Warehouse contact/operational fields:**
   - إضافة Phone/Manager/Notes للـWarehouse إذا احتاج التشغيل، دون Stock.
3. **Supplier Product enhancements:**
   - `LeadTimeDays` و`MOQ` **لا** تُضاف قبل اعتماد سياسة الشراء.
4. **Read-only inventory reporting shell:**
   - صفحات تعرض "غير متاح حتى اعتماد سياسة المخزون" بدل أرقام وهمية — غير ضرورية الآن.

## 5. الخطة المقترحة — M5.4 Purchase Order Draft Slice

**الهدف:** أول دورة شراء حقيقية بلا ترحيل مخزون أو تكلفة نهائية.

- جدول `purchase_orders` + `purchase_order_items`.
- إنشاء/تعديل/إلغاء PO مسودة، بحث/ترقيم/حالات.
- ربط بند بـSupplierProduct (Variant + SupplierSku) وكمية مطلوبة وسعر شراء **اختياري** لا يدخل COGS.
- Permissions: `Purchasing.CreateOrder` (موجودة)، `Purchasing.ApproveOrder` (جديدة).
- Audit + RowVersion + منع تعديل PO مؤكدة/ملغاة.
- Admin UI + API + PostgreSQL tests.

**معايير القبول:**
- لا PO لمورد/Variant غير نشط.
- لا تعديل بعد الإرسال/الاعتماد.
- إجمالي الكميات صحيح، ولا Stock Movement.
- Audit لكل إنشاء/تعديل/إلغاء.
- لا Migration على Preview/Owner DB.

**الاختبارات:**
- PostgreSQL CRUD + uniqueness + stale/concurrent.
- Permission tests.
- E2E Admin Draft PO.
- لا E2E Stock لأنها غير منفذة.

## 6. الخطوة التالية

اعتماد Q1.1 وQ2.1 وQ2.4 أولًا، ثم M5.4 PO Draft، ثم Goods Receipt + Stock Ledger في شريحة منفصلة. لا تُنشأ Stock Movements قبل تعريف Costing والسياسة.
