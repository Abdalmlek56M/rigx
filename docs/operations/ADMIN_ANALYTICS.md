# Admin Analytics

## Endpoint
`GET /api/v1/admin/analytics/dashboard` — صلاحية `Catalog.View`.

## المصادر
يقرأ كل Module عبر DbContext Factory منفصل، دون JOINs عابرة للوحدات:
- Catalog: المنتجات/التصنيفات/الماركات/Condition/Media/Reviews.
- Purchasing: Suppliers/Supplier Products/Purchase Drafts per Source.
- Identity: العملاء/الحسابات النشطة/غير المؤكدة/الجديدة.
- Inventory: Warehouses النشطة.
- Storefront: Articles/FAQ/Policies/Offers/Homepage configuration.
- Auditing: آخر 8 أحداث آمنة عبر `SecurityAuditQueryService`.

## العرض
- Metric groups.
- Time series لآخر 14 يومًا (Accounts/Reviews).
- Rating distribution.
- Draft purchase sources.
- Recent safe audit events.
- UI: أعلى `/admin` عبر `AdminAnalytics`.

## القيود
- لا Sales/Profit/Conversion/Stock turnover قبل وجود بيانات حقيقية.
- لا أرقام Demo أو Hardcoded.
- الاستعلامات Aggregate مباشرة؛ Caching مؤجل حتى تثبت الحاجة بعد القياس.
