# Reviews + Offers

## Product & Store Reviews (Catalog)
- `product_reviews`: مراجعة واحدة لكل (UserAccount, Product)، Rating 1..5، Title اختياري، Body نص عادي 10..4000، ShowAvatar، Status Pending/Published/Rejected/Hidden، ModerationReason/Actor/Time، RowVersion.
- `store_reviews`: مراجعة واحدة لكل حساب، نفس الحالات.
- لا Verified Purchase: لا توجد Orders؛ لا يُعرض أي شارة شراء مؤكد.
- أي إنشاء/تعديل يعيد الحالة إلى Pending. الحذف متاح لصاحب المراجعة.
- Public يعرض Published فقط مع Aggregate (Average/Count/Distribution).
- Avatar يظهر فقط عند `show_avatar=true` وبوجود Avatar للحساب.
- Admin moderation بصلاحية `Catalog.ManageProducts`، مع سبب اختياري.
- Rate Limiting/Spam: لا يوجد Rate Limiter مخصص بعد؛ الحماية الحالية حساب واحد/منتج + Auth + نص محدود + Moderation. يُوسّع عند فتح Reviews للإنتاج.

## Offers (Storefront)
- `offers` + `offer_items`: عنوان AR/EN، وصف، Start/End، Status Draft/Published/Cancelled، Priority، RowVersion.
- OfferItem: Product + OfferPrice صريح يحدده Admin. يُرفض السعر ≤0 أو ≥ BasePrice.
- Publish يتحقق أن كل المنتجات Published. Public يعرض فقط Published وداخل النافذة الزمنية.
- لا Coupons أو قواعد خصم تلقائية أو أسعار وهمية.
- Admin `/admin/content/offers` بصلاحية `System.StoreSettings`.
- Public `/offers` + قسم Offers في Home.

## حدود
- لا مراجعات أو عروض Demo تُنشأ في الكود.
- لا تُحسب التقييمات من بيانات غير منشورة.
