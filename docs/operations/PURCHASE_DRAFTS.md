# Purchase Drafts (Draft-only)

## الغرض
تسجيل مسودات شراء تخطيطية من:
- RegisteredSupplier
- Marketplace
- IndividualSeller
- Other

بدون فرض Supplier Profile على كل عملية، وبدون أي ترحيل مخزني أو مالي.

## الجداول
- `purchase_drafts`: SourceType، SupplierId اختياري، MarketplaceName، ExternalSellerName/Contact، ListingUrl، PurchaseDate، Notes، Status (Draft/Cancelled)، TotalAmount، CreatedBy، RowVersion.
- `purchase_draft_items`: ProductId، VariantId اختياري، ItemName/SKU snapshot، Quantity، UnitPrice، LineTotal، SortOrder.

## القواعد
- RegisteredSupplier يتطلب SupplierId نشطًا، ويمنع تمرير SupplierId لأي مصدر آخر.
- Marketplace يتطلب اسم السوق؛ IndividualSeller يتطلب اسم البائع.
- Product يجب أن يوجد، والVariant يجب أن يخص نفس المنتج وأن يكون Active.
- Quantity 1..100000، UnitPrice > 0، والإجمالي يُحسب على الخادم.
- RowVersion إلزامي للتعديل، والمسودة الملغاة نهائية.
- Audit: `Purchasing.DraftCreated/Updated/Cancelled`.

## ما لا يحدث
- لا PO Approved/Sent، ولا Goods Receipt، ولا Stock/Cost/Ledger، ولا Supplier Debt.
- لا إرسال خارجي لأي بائع.

## API
- `GET /api/v1/admin/purchasing/drafts` (`Purchasing.View`)
- `GET /{id}` (`Purchasing.View`)
- `POST`, `PUT /{id}`, `POST /{id}/cancel` (`Purchasing.CreateOrder` مؤقتًا)
- UI: `/admin/purchasing/drafts`
