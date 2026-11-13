# Storefront Settings

## الغرض

إدارة العناصر العامة للمتجر من Admin دون تعديل كود أو نشر جديد:

- رقم واتساب ورسالة البداية.
- روابط Facebook / Telegram / TikTok / YouTube / Instagram.
- عنوان المقر ومواعيده وتعليمات الاستلام.
- نصوص الثقة: الضمان، خدمة العملاء، الاستشارة المجانية.

القيم اختيارية بالكامل. أي حقل فارغ يُخفي العنصر من المتجر، ولا يوجد رقم أو عنوان أو وعد افتراضي مخترع.

## البيانات

جدول `store_settings` (Singleton `id=1`):

| العمود | ملاحظات |
|---|---|
| `whatsapp_number` | أرقام فقط 8–15، مع رمز الدولة |
| `whatsapp_message` | حتى 500 |
| `facebook_url` … `instagram_url` | http/https مطلق أو NULL |
| `pickup_address` / `pickup_hours` / `pickup_instructions` | نصوص اختيارية |
| `warranty_summary` / `service_summary` / `consultation_summary` | نصوص اختيارية |
| `created_at_utc` / `updated_at_utc` | timestamptz |
| `row_version` | إلزامي في Update |

## الـAPI

| Method | Route | الوصول |
|---|---|---|
| GET | `/api/v1/storefront/settings` | عام، `Cache-Control: public, max-age=60` |
| GET | `/api/v1/admin/storefront/settings` | `System.StoreSettings`، `no-store` |
| PUT | `/api/v1/admin/storefront/settings` | `System.StoreSettings` + CSRF + RowVersion |

- PUT يسجل `Storefront.SettingsUpdated` في Audit داخل نفس المعاملة.
- Audit metadata يحتوي حقولًا منطقية فقط (`whatsappConfigured`, `socialLinks`, `pickupConfigured`) ولا ينسخ الروابط.
- URL غير صالح أو رقم واتساب غير صالح → 400. RowVersion قديم → 409.

## الواجهة

- Admin: `/admin/storefront/settings`، ويظهر الرابط فقط لمن يملك `System.StoreSettings`.
- Storefront: زر WhatsApp عائم يظهر عند وجود رقم، وFooter Social links تظهر فقط للروابط المضبوطة.
- يوجد Fallback للقراءة من `NEXT_PUBLIC_STORE_WHATSAPP_NUMBER` إن كان مضبوطًا، لكن إعدادات Admin لها الأولوية.

## Migration

`src/Modules/Storefront/Persistence/Migrations/20261108214739_M5_StorefrontSettings.cs`

**لم تُطبق على Preview/Owner DB.** `PreviewInitializationService` يطبقها بعد Inventory. تم اختبارها على قاعدة QA المعزولة فقط.

## الاختبارات

`StoreSettingsPostgreSqlTests` — 2/2:

1. Defaults، Update، تطبيع رقم الواتساب، Audit.
2. رفض URL/رقم غير صالح، Stale 409، وتحديثان متزامنان (نجاح واحد و409 للآخر).

Playwright Admin M5 يغطي حفظ الإعدادات وظهور زر WhatsApp وروابط Footer.
