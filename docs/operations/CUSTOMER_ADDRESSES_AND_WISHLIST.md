# Customer Addresses + Wishlist

## Addresses (Identity)
- الجدول `customer_addresses` مملوك لـIdentity ومرتبط بـ`user_accounts`.
- Soft Delete (`is_deleted`) + Default واحد عبر Unique Filtered Index `WHERE is_default AND NOT is_deleted`.
- `RowVersion` للـOptimistic Concurrency، وLock على الحساب/العنوان داخل المعاملة لتجنّب سباق Default.
- Ownership مفروض في الاستعلام نفسه (IDOR → 404)، وAudit داخل نفس المعاملة.
- حقول: Label، Recipient، Phone، CountryCode (EG)، CallingCode (+20)، Governorate، City، District، Street، Building، Floor، Apartment، Landmark، PostalCode، Instructions.
- لا ربط بـOrders/Checkout في هذا النطاق؛ العقود جاهزة لـSales لاحقًا.
- API: `GET/POST /api/v1/identity/addresses`, `PUT/DELETE /{id}`.
- UI: `/account/addresses`.

## Wishlist (Storefront)
- `wishlist_items` Product-level فقط: `(user_account_id, product_id)` Unique.
- قراءة فقط من `products`, `product_images`, `categories` (ExcludeFromMigrations).
- Add idempotent؛ Remove؛ Merge بحد 200 ولا يكرر؛ Clear.
- المنتجات Deleted لا تُعرض، وDraft تظهر `isAvailable=false` ولا تُحذف صفوفها.
- Guest: `rigx-wishlist-v1` Snapshots في localStorage؛ Merge عند الدخول ثم مسح المحلي.
- UI: Heart على Card/Details، `/account/wishlist`.
- لا Variant افتراضي، ولا Stock/Restock، ولا Compare.
- API: `GET/POST/DELETE /api/v1/wishlist`, `POST /merge`.
