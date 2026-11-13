# Local Development Preview

## الغرض

هذه البيئة مخصصة للمعاينة المحلية فقط. تستخدم مشروع Docker Compose منفصلًا باسم `hardware-store-preview`، ومنافذ وVolumes مستقلة، ولا تلمس `compose.yaml` أو قاعدة التطوير القديمة.

## المتطلبات

- Docker Desktop يعمل.
- يمكن أن يكون Docker CLI داخل `PATH` أو في مسار Docker Desktop الافتراضي؛ سكربتات Preview تكتشف المسار المحلي تلقائيًا.
- PowerShell 7.
- Node.js فقط لتشغيل اختبارات Playwright من المضيف؛ واجهة Preview نفسها تعمل داخل Docker.
- موافقة صاحب المشروع على إنشاء PostgreSQL مؤقتة وتطبيق Migrations في هذه البيئة.

## الإعداد

```powershell
Copy-Item .env.preview.example .env.preview.local
notepad .env.preview.local
```

استبدل كل قيمة تبدأ بـ`replace-`. لا توجد كلمة مرور افتراضية. `.env.preview.local` مستبعد من Git والأرشيف.

## التشغيل

```powershell
.\scripts\preview.ps1 -Action Start
```

المسار المتوقع افتراضيًا:

- الموقع: `http://127.0.0.1:3005`
- المنتجات: `http://127.0.0.1:3005/products`
- الإدارة: `http://127.0.0.1:3005/admin`
- API: `http://127.0.0.1:8085`
- PostgreSQL: `127.0.0.1:5435`

اسم الدخول هو `PREVIEW_OWNER_USERNAME`، وكلمة المرور توجد فقط في `.env.preview.local` تحت `PREVIEW_OWNER_PASSWORD`.

لروابط المشاركة وSEO المحلي، القيمة الافتراضية لـ`NEXT_PUBLIC_SITE_URL` هي `http://127.0.0.1:3005`. لا تضبط `STORE_WHATSAPP_NUMBER` إلا برقم المتجر الرسمي؛ الزر يظل مخفيًا عندما تكون القيمة فارغة.

### استعادة دخول Preview Owner دون حذف البيانات

إذا رفض Preview Owner الدخول، راجع أولًا أن Docker/API/Web/PostgreSQL في حالة سليمة وأن Username/Password الحاليين مطابقان لما في `.env.preview.local`. لتحديث الحساب نفسه:

1. عدّل `PREVIEW_OWNER_USERNAME` و/أو `PREVIEW_OWNER_PASSWORD` داخل `.env.preview.local` فقط.
2. شغّل `.\scripts\preview.ps1 -Action Start` مجددًا.
3. Bootstrap الحالي يعيد مزامنة Username/Email وLogin Identifiers، يصفّر `AccessFailedCount` وLockout، يعيد تعيين Password Hash من الإعدادات، يزيد `AuthorizationVersion`، ويلغي الجلسات القديمة مع إعادة `Catalog.*`.

لا تمرر Password في Command Line، ولا تحذف الحساب أو قاعدة البيانات. إذا كان Username/Email المطلوب مملوكًا لحساب غير Owner فسيفشل Bootstrap بأمان ولا يحول ذلك الحساب إلى Owner؛ يلزم قرار مالك/معالجة إدارية صريحة لهذا التصادم.

## Launcher واختصار سطح المكتب

```powershell
.\scripts\open-preview.ps1
.\scripts\create-preview-shortcut.ps1
```

ينشئ الأمر الثاني اختصار `HARDWARE STORE Preview` على Desktop. عند الضغط عليه:

- إن كانت خدمات Preview سليمة، يفتح `http://127.0.0.1:3005` مباشرة.
- إن لم تكن جاهزة، يسأل قبل استدعاء `preview.ps1 -Action Start`.
- عند الفشل يعرض Logs منقحة ولا يفتح صفحة معطلة.
- لا يحتوي الاختصار على Username أو Password أو Bootstrap Key ولا يبدأ مع Windows.

الإزالة:

```powershell
.\scripts\create-preview-shortcut.ps1 -Remove
```

السكربت:

1. يشغّل خدمات Preview المستقلة.
2. ينتظر API وWeb.
3. يحصل على CSRF Cookie/Token.
4. يستدعي Bootstrap مرة واحدة باستخدام `PREVIEW_BOOTSTRAP_KEY`.
5. يطبق Migrations على قاعدة Preview فقط: Identity، Auditing، Catalog، Purchasing، Inventory، ثم Storefront (بهذا الترتيب لاعتماد Purchasing على Catalog Variants).
6. ينشئ/يحدّث حساب Owner وصلاحيات `Catalog.*` و`Purchasing.*` و`Inventory.*` بحسب `PermissionCatalog` الحالي، بما فيها `Purchasing.ViewSupplierFinancials` و`Purchasing.ManageSupplierFinancials` و`Inventory.ManageWarehouses`.
7. يضيف/يستأنف 14 منتجًا وصور WebP مولدة محليًا دون إنشاء صفوف مكررة، ويعيد حالة Preview Owner إلى حالة دخول سليمة عند الحاجة.

تم إصلاح سبب HTTP 500 السابق: Seed كان يمرر اسم التصنيف المعروض بدل Slug إلى قاموس التصنيفات. المحاولة السابقة كانت قد حفظت 11 تصنيفًا و10 ماركات بالفعل، لذلك أُعيد تشغيل Bootstrap على القاعدة نفسها واستأنف المنتجات بدل إعادة إنشاء الصفوف.

## الإيقاف

```powershell
.\scripts\preview.ps1 -Action Stop
```

هذا يوقف الحاويات ويحافظ على Preview Volumes. لا تستخدم `docker compose down -v` مع أي مشروع غير معروف. لإعادة بناء بيانات Preview من الصفر لاحقًا، يجب تحديد الحاويات الثلاثة والVolumes الثلاثة ذات بادئة `hardware-store-preview` صراحةً والحصول على موافقة منفصلة.

## الحالة

```powershell
.\scripts\preview.ps1 -Action Status
```

## اختبارات المتصفح

بعد تشغيل Preview:

```powershell
$env:E2E_DB_CONTAINER='hardware-store-preview-postgres-1'
$env:E2E_DB_NAME='hardware_store_preview'
$env:E2E_DB_USER='hardware_store_preview_owner'
$env:E2E_WEB_PORT='3210'
$env:API_ORIGIN='http://127.0.0.1:8085'
$env:E2E_PREVIEW_OWNER_USERNAME='preview-owner'
$env:E2E_PREVIEW_OWNER_PASSWORD=(Get-Content .env.preview.local | Where-Object { $_ -like 'PREVIEW_OWNER_PASSWORD=*' }).Split('=',2)[1]
Set-Location apps/web
npm run test:e2e
```

Playwright يشغّل نسخة Web اختبارية منفصلة على `3210` تستخدم `API_ORIGIN`، ولا يعيد استخدام Web Preview الإنتاجي الشكل على `3005`.

## اختبار Admin M5 على بيئة معزولة

عند الحاجة لاختبار شاشات M5 الجديدة دون تطبيق Migrations على Preview:

1. أنشئ قاعدة مستقلة باسم مؤقت على PostgreSQL التطوير/Preview.
2. شغّل API من المصدر الحالي على منفذ مستقل (مثل `8091`) مع `ConnectionStrings__Default` و`ConnectionStrings__Migrations` إلى القاعدة الجديدة، و`Preview__Enabled=true` وإعدادات Owner/Bootstrap من Environment.
3. استدعِ `POST /api/v1/development/preview/initialize` مع CSRF و`X-Preview-Bootstrap-Key`؛ يطبق Migrations الخمسة ثم Seed/Owner.
4. شغّل Web من `apps/web` على `3006` مع `API_ORIGIN=http://127.0.0.1:8091`.
5. شغّل `E2E_M5_QA=1 npx playwright test tests/e2e/admin-m5.spec.ts` مع Credentials من Environment. اللقطات تُكتب إلى `docs/review/m3-hardening-m5-2026-11-08/`.

هذه البيئة اختبار فقط. لا تنقل القاعدة أو بياناتها إلى Preview/Owner DB ولا تستخدم كلمات مرور حقيقية في الملفات.

شغّل سيناريو الهوية منفصلًا عن سيناريوهات Admin عند تشغيل المجموعة كاملة، لأن حد Login المعتمد `5/IP/minute` مقصود ويُطبق على الاختبارات أيضًا. لا ترفع الحد لتسهيل الاختبار.

## البريد

التطوير الحالي يستخدم In-Memory Email Provider ولا يوفر Inbox مرئية. إضافة Mailpit اقتراح غير مطبق في [MAILPIT_PREVIEW_PROPOSAL.md](MAILPIT_PREVIEW_PROPOSAL.md) وتحتاج موافقة منفصلة قبل تغيير Compose أو المنافذ. لا توجد Credentials SMTP حقيقية ضمن Preview.
