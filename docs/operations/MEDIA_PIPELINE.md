# Catalog Media Pipeline

## ما ينفذ

- **Original preservation:** يتم حفظ البايتات الأصلية المرفوعة كما هي في `products/{productId}/{imageId}.original` دون تعديل أو قص أو إعادة ترميز.
- **Display derivative:** WebP مضغوط في `products/{productId}/{imageId}.webp`، بحد أقصى `StoredMaxDimension` (2000px).
- **Thumbnail:** WebP صغير في `products/{productId}/{imageId}-thumb.webp` بحد أقصى 480px.
- **DB:** `product_images.storage_key`, `thumbnail_storage_key`, `original_storage_key`.
- **Atomic writes:** الكتابة عبر ملف مؤقت ثم Move؛ فشل أي مشتق يحذف الملفات الناتجة ولا يترك صفًا يتيمًا.
- **Validation:** MIME/Magic/Decode، أبعاد، Pixel limit، MaxUploadBytes، MaxStoredBytes، بلا EXIF توجيه خاطئ (Orientation يطبق قبل WebP).
- **Public access:** مسار `/api/v1/catalog/media/{**storageKey}` يخدم `.webp` فقط ويرفض `.original` و`nosniff` وCache immutable.
- **Maintenance:** Media Audit/Cleanup يعتبر StorageKey + Thumbnail + Original مراجع، وينظف `.webp`/`.original` غير المرجعية بعد Grace Period.

## Legacy Media

الصور القديمة قد لا تملك `original_storage_key`؛ لا ندّعي أن الأصل محفوظ لها. تظل `.webp` وThumbnail تعمل كما هي، ولا تُعاد كتابة المفاتيح أو الروابط.

## Watermark

لم يوجد ملف شعار RIGX رسمي معتمد في `apps/web/public` أو Workspace. لذلك **Overlay غير مفعل** ولم يُخترع Wordmark نصي بديل. عند توفير الشعار:

- يوضع على `.webp`/Thumbnail فقط، أعلى اليسار، داخل Safe Margin.
- لا يُمس `.original`.
- يلزم اختبار وضوح/تباين وأبعاد صغيرة، وإعادة توليد قابلة للتكرار.

## القيود

- لا يتم قبول فيديو في هذه الدفعة.
- لا يتم تقديم `.original` عبر API عام.
- لا يتم تحويل الصور القديمة إلى أصل غير موجود.
