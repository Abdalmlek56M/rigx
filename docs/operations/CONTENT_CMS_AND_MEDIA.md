# Content CMS + Media Library

## Homepage
- الجدول `homepage_layouts` يحتفظ بصف `Draft` وصف `Published`، كل منهما `layout_json` (jsonb) و`row_version`.
- Block Types ثابتة: Sections (`hero, brands, categories, discovery, guide, latest, closing`) وRails (`brands`, `categories`) بحقول `title/lead/contentType/limit/itemOrder/hiddenItemIds`.
- `Publish` يتحقق من وجود كل Brand/Category/Product مشار إليه، ويرفض أي JSON غير صالح أو HTML.
- Public: `GET /api/v1/content/homepage` يعيد Published أو Default آمن.
- Admin: `GET/PUT /api/v1/admin/content/homepage` + `POST /homepage/publish` بصلاحية `System.StoreSettings`.

## Articles
- `content_articles` + `content_article_revisions`.
- Draft/Published/Unpublish، Slug فريد، AR/EN، Summary/Body نص عادي فقط، Tags، Image Storage Key، SEO/Canonical، SortOrder.
- كل Create/Update/Publish/Unpublish يضيف Revision (`Created/Saved/Published/Unpublished`).
- Public: `GET /content/articles` و`/content/articles/{slug}` — Published فقط.
- الصفحات SSR: `/articles`, `/articles/[slug]` مع Metadata/OG/Canonical/Sitemap وNoindex عند الغياب.

## FAQ
- `content_faqs` AR/EN + Category + SortOrder + Draft/Published.
- Public: `GET /content/faqs`، صفحة `/faq` حسب اللغة.

## Policies
- `content_policies` بنسخ `version_number` فريدة لكل `kind` (Shipping/Returns/Exchanges/Warranty/Terms).
- Draft → Review → Published؛ تعديل المنشور ممنوع (409)، والنشر يؤرشف السابق.
- لا نصوص قانونية افتراضية؛ Public 404 حتى ينشر المالك نصًا معتمدًا.

## Media Library
- `content_media_assets` + `content_media_asset_usages` (Catalog).
- Storage: نفس `Catalog:MediaRoot` تحت `content/{assetId}` مع `.webp` + `-thumb.webp` + `.original` على Volume.
- Admin `/admin/media`: Upload/Search/Filter/Metadata AR-EN/Alt/Caption/Tags/Dimensions/Size/Replace/Usages.
- Replace يعيد استخدام نفس Storage Keys، لذلك لا تنكسر مراجع المقالات.
- Delete منطقي ويُرفض 409 عند وجود Usage `is_published=true`.
- Storefront يسجّل الاستخدام عبر `IContentMediaUsageSink` بعد حفظ المقال.
- Watermark معطّل حتى توفر شعار RIGX معتمد.
- لا Video Library قبل Storage دائم معتمد.

## الأمان
- كل النصوص تُفحص ضد HTML وControl Characters؛ لا `dangerouslySetInnerHTML`.
- Admin endpoints تحت `System.StoreSettings` أو `Catalog.ManageMedia`.
