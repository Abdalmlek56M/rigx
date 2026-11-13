# RIGX — Expanded Storefront Wave 0–1 Delivery Report

**التاريخ:** 9 نوفمبر 2026  
**Workspace:** `C:\Users\Abdalmlek\Downloads\DEV - Copy\HARDWARE STORE`  
**الأصل المحمي:** `C:\Users\Abdalmlek\Downloads\DEV\HARDWARE STORE` — لم يُعدّل.

> **ملاحظة مرجعية:** لم تُعثر على ملفات الصور الأربع المرفقة داخل `Downloads` أو `Temp` في بيئة التنفيذ. تم الاعتماد على الأوصاف النصية الدقيقة في ملف التكليف للمناطق الأربع (Navbar، Product Card، Mega Menu، Category Rail).

---

## 1. ما اكتمل فعليًا end-to-end

### Wave 0 — Stabilization
- **Preview DB target guard:** `PreviewInitializationService` يتحقق قبل أي DDL من تطابق Host/Port/Database بين `Default` و`Migrations`، ويسمح باختلاف User/Password فقط.
- **Preview regression test:** اختبار PostgreSQL يستدعي `InitializeAsync` كاملًا، يتحقق من جداول Identity/Auditing/Catalog/Purchasing/Inventory/Storefront، ثم يعيد التهيئة ويتأكد من عدم تكرار Owner/Products/Categories.
- **Supplier projection:** `SupplierDirectoryService.GetAsync/GetByIdAsync` تستخدمان Projection إلى `SupplierView` ولا تحملان أعمدة البنك في المسار العام.
- **Supplier financial protection:** بقيت كما هي؛ لم يُوسّع التشفير/Key Management بهذه الجولة بناءً على توجيه المالك.

### Wave 1 — Storefront Visual + Settings
- **إزالة Compare بالكامل:** الأزرار من البطاقات/التفاصيل، روابط Desktop/Mobile، Provider/Layer، مسار `/compare`، المكونات والاختبارات المرتبطة.
- **الخط والاتساق:** `button/input/select/textarea/option` ترث خط الصفحة والـletter-spacing؛ لا Browser Default في النماذج.
- **بطاقة المنتج:** السطح أصبح `var(--ds-surface)` بلا Gradient رمادي غير مقصود، ومنطقة الصورة محايدة وموحدة النسبة.
- **Store Settings Module:** جدول `store_settings` وPublic/Admin API وAdmin UI وصلاحية `System.StoreSettings` وRowVersion/Audit.
- **WhatsApp Floating:** يظهر فقط عند وجود رقم من الإعدادات أو env، مع رسالة قابلة للإدارة وReduced Motion.
- **Footer Social:** Facebook/Telegram/TikTok/YouTube/Instagram تظهر فقط للروابط المضبوطة، بأيقونات RIGX متسقة.
- **New/Used:** `CatalogProductCondition` + Migration + Admin select + Badge + `condition` filter في API/UI.
- **Permission UX:** 39 مفتاحًا بأسماء/أوصاف/مجموعات AR/EN وبحث؛ المفاتيح المخزنة/المرسلة لم تتغير.

### Wave 2 — Account/Identity (جزئي)
- **Permission Arabic UX** مكتمل داخل Roles screen.
- Admin user creation/email change/reset password، Sessions/Devices، وCustomer Account Settings **لم تُنفذ بعد**.

---

## 2. التقسيم المعماري ولماذا

- **Module `Storefront` جديد:** يملك `StoreSettings` و`StorefrontDbContext` و`StoreSettingsService` و`StorefrontEndpoints`. هذا يفصل الإعدادات العامة عن Catalog/Purchasing/Identity، ويترك مساحة لـCMS/FAQ/Policies لاحقًا.
- **StoreSettingsService:** يملك التطبيع/التحقق/RowVersion/الإدارة الذرية/Audit؛ الـEndpoint رقيق.
- **Catalog Condition:** أُضيف كحقل Domain صريح مع Check Constraint بدل استنتاجه من السعر/Naming.
- **Preview guard:** منطق تحقق مستقل قبل migration runner، مع اختبار PostgreSQL حقيقي.
- **Supplier projection:** يقلل تحميل البيانات الحساسة من طبقة القراءة نفسها، وليس فقط من الـDTO.

---

## 3. الاختبارات التي شُغلت ونتائجها

| الاختبار | النتيجة |
|---|---|
| `dotnet build HARDWARE_STORE.slnx` | 0 أخطاء / 0 تحذيرات |
| 43/43 focused backend | Catalog 4، Identity 7، Supplier 7، SupplierProduct 3، Warehouse 2، StoreSettings 2، Preview 6، Permission 12 |
| `npx tsc --noEmit` | 0 |
| `npx vitest run` | 36/36 (9 ملفات؛ انخفض العدد بعد إزالة Compare tests) |
| `npm run build` | ناجح |
| Playwright `admin-m5.spec.ts` | 1/1 (Suppliers/Products/Warehouses/Store Settings/WhatsApp/Footer) |
| `has-pending-model-changes` | نظيف لـCatalog/Storefront/Purchasing/Inventory |
| QA DB migrations | 15 Migration مطبقة على `rigx_m5_qa_20261108` فقط |

### Visual QA
- Store Settings Admin في Dark Mode.
- Storefront Footer Social + WhatsApp Floating.
- Light Product Card: `backgroundColor = rgb(250, 248, 244)` و`backgroundImage = none`.
- Condition filter: `?condition=used` أعاد منتجًا واحدًا مع Badge "Used".
- لا أخطاء Console/Network غير متوقعة في Playwright؛ 401 المتوقعة من `/identity/me` فقط.

---

## 4. ما لم يكتمل ولماذا

| العنصر | السبب |
|---|---|
| RIGX logo overlay على Image derivatives + حفظ الأصل | يحتاج تغيير Media pipeline/Storage ومراجعة أثر إعادة التوليد؛ لم يُنفذ في هذه الجولة |
| Admin user creation/email change/reset + sessions/devices | Wave 2 كبيرة وتحتاج قرارات بريد/OTP/Reset وسياسة Account Takeover |
| Real Orders/Checkout/Addresses/Shipping | Wave 3 تحتاج قرارات Stock/Shipping/Deposit/Order lifecycle |
| CMS/FAQ/Policies/Warranty Lookup | Wave 4، تحتاج محتوى/سياسات معتمدة من المالك |
| PC Builder | Wave 5، يحتاج بيانات Compatibility موثوقة |
| Partners/Finance/Returns | Wave 6، تحتاج معادلات مالية واعتماد المالك |
| Payment Gateway | Wave 7، لم يُختر المزود/الحساب |
| Compare | أُزيل بطلب المالك من الواجهة؛ الكود/Route/الاختبارات أُزيلت |

---

## 5. قرارات المالك المطلوبة

1. **Image derivatives:** هل نضيف شعار RIGX إلى الصورة المشتقة فقط مع حفظ الأصل في Storage؟ وما موقعه/حجمه ومصدر الشعار الرسمي؟
2. **Admin Email Change:** هل يتطلب تأكيد البريد الجديد (رابط/OTP) أم اعتماد Owner فقط مع Audit؟
3. **Temporary Password / First-login change:** هل نفعّل `MustChangePassword` كسياسة دائمة؟
4. **Checkout/Shipping:** من يحدد تكلفة الشحن والعربون؟ وما الحالات النهائية للطلب؟
5. **Stock reservation:** هل يُحجز عند الطلب غير المؤكد أم بعد المراجعة؟
6. **Warranty/Returns:** المدد والسياسات النهائية.
7. **Partner profit formula:** إيراد أم صافي ربح؟ وكيف تُعالج المرتجعات/المصروفات؟
8. **Payment provider:** Paymob أو غيره، وحساب Sandbox/Production.

---

## 6. المخاطر المتبقية ونقطة الاستكمال

- **Media pipeline:** لا يزال لا يحفظ الأصل ولا يضع الشعار؛ هذا أهم فجوة بصرية متبقية في Wave 1.
- **Permissions label map:** يحتاج تحديثًا آليًا عند إضافة مفتاح جديد (حاليًا 39).
- **Store Settings public cache:** 60 ثانية؛ تغيير الإعدادات قد يتأخر دقيقة في المتجر.
- **New/Used filter:** لا يوجد بعد فلتر داخل Drawer/شريحة Chip، لكنه موجود في شريط الأدوات وAPI.
- **No migrations on Preview/Owner DB:** لا بد من نسخة احتياطية واعتماد قبل التطبيق.

**النقطة التالية:** Wave 2 (Admin user creation/email/reset + Sessions/Devices + Customer Account) بالتوازي مع إكمال Image derivative pipeline، ثم Wave 3 Orders/Checkout بعد تثبيت قرارات الشحن/المخزون.

---

## 7. الملفات الرئيسية

### Backend
- `src/Modules/Storefront/*` (Module/Entity/DbContext/Service/Registration/Migration)
- `src/Host/HardwareStore.Api/StorefrontEndpoints.cs`
- `src/Host/HardwareStore.Api/PreviewInitializationService.cs`
- `src/Modules/Catalog/CatalogPersistence.cs`, `CatalogApplication.cs`
- `src/Host/HardwareStore.Api/CatalogEndpoints.cs`
- `src/Modules/Purchasing/SupplierDirectoryService.cs`
- `tests/HardwareStore.Api.Tests/PreviewInitializationTests.cs`, `StoreSettingsTests.cs`, `CatalogPostgreSqlTests.cs`, `PermissionAuthorizationTests.cs`

### Frontend
- `apps/web/src/components/store-settings-provider.tsx`, `store-settings-admin.tsx`
- `apps/web/src/app/admin/storefront/settings/page.tsx`
- `apps/web/src/components/whatsapp-floating-button.tsx`, `site-chrome.tsx`, `app-chrome.tsx`, `app-providers.tsx`
- `apps/web/src/components/catalog-product-card.tsx`, `catalog-product-view.tsx`, `catalog-products-experience.tsx`, `catalog-product-editor.tsx`
- `apps/web/src/lib/permission-labels.ts`, `identity-roles-admin.tsx`
- `apps/web/src/lib/api-client.ts`, `i18n.tsx`, `storefront.ts`, `icons.tsx`, `globals.css`
- `apps/web/tests/e2e/admin-m5.spec.ts`

### Docs
- `README.md`, `PROGRESS.md`, `NEXT_TASK.md`, `ROADMAP.md`, `MODEL_HANDOFF.md`
- `docs/adr/ARCHITECTURE_DECISION_RECORDS.md` (ADR-016–018)
- `docs/operations/STOREFRONT_SETTINGS.md`
- `docs/operations/LOCAL_PREVIEW.md`
- `docs/api/API_DESIGN_SECURITY.md`
- `docs/database/DATABASE_DESIGN_ERD.md`
- `docs/architecture/MODULAR_MONOLITH_ARCHITECTURE.md`
- `docs/testing/TESTING_STRATEGY.md`
- `docs/review/m3-hardening-m5-2026-11-08/`

---

## 8. أرشيف التسليم

- **الملف:** `HARDWARE_STORE_RIGX_EXPANDED_STOREFRONT_WAVE_2026-11-09.zip`
- **SHA256:** محسوب في الملف المجاور `HARDWARE_STORE_RIGX_EXPANDED_STOREFRONT_WAVE_2026-11-09.zip.sha256`.
- **التحقق:** فك الأرشيف وعدد الملفات وخلوّه من `.env` الفعلي و`node_modules`/`.next`/`bin`/`obj`/`.backups`/Logs/Test Outputs/Old ZIPs.
