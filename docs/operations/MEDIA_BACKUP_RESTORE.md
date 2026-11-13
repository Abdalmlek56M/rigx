# MEDIA — التخزين الدائم والنسخ الاحتياطي والاستعادة

آخر تحديث: 9 نوفمبر 2026  
القرار المعتمد: تبقى الصور والفيديوهات محفوظة حتى حذف إداري صريح، دون انتهاء صلاحية تلقائي، مع Backup منفصل واستعادة متحققة.

## 1. التخزين الحالي
- Docker Named Volumes: `preview_catalog_media` و`api_data_catalog_media` (حسب ملف Compose).
- المجلد داخل الحاوية: `/app/.catalog-media` (أو `Catalog__MediaRoot`).
- تطوير QA الحالي يستخدم `.dotnet-home/catalog-media-qa` (غير مشمول في ZIP).
- لا حذف تلقائي. `CatalogMediaMaintenanceService` يدعم Audit/Cleanup يدويًا فقط، مع Grace Period وإعادة فحص المراجع قبل كل حذف.
- منع حذف الأصل/المشتقات المرتبطة بـProduct/Category/Brand أو محتوى منشور؛ يجب إزالة المرجع أو إلغاء النشر أولًا.

## 2. النسخ الاحتياطي
```powershell
.\scripts\backup-media.ps1 `
  -Source ".dotnet-home\catalog-media-qa" `
  -BackupRoot ".backups\media-qa" `
  -Verify
```
- ينشئ `media-<timestamp>` داخل BackupRoot (خارج Source)، وينسخ الشجرة كاملة.
- يحسب SHA-256 لكل ملف في `source-manifest.sha256` ثم يتحقق من النسخة في `backup-manifest.sha256`.
- لا يحذف المصدر ولا يستبدل نسخة سابقة.
- `-Verify` يستعيد النسخة إلى `restore-<timestamp>` ويعيد حساب البصمات ويقارنها.

## 3. الاستعادة
1. شغّل السكربت مع `-Verify` على نسخة الاختبار أولًا.
2. أوقف الـAPI المصرح به فقط (لا توقف Preview/Owner).
3. خذ نسخة من الحالة الحالية (DB + Media) قبل الاستعادة.
4. استعد ملفات Media إلى Volume جديد/مجلد مؤقت، ثم بدّل `Catalog__MediaRoot` أو انسخ إلى الـVolume بعد التحقق.
5. أعد تشغيل API واختبر صورة/فيديو منشورًا، ثم Audit من Admin.
6. سجّل النتيجة والـChecksum في سجل التشغيل.

## 4. القيود الحالية
- النسخ الاحتياطي محلي (Off-host غير مفعّل)؛ مطلوب Storage دائم/Off-host قبل Production، ويحتاج قرار المالك وربما تكلفة.
- لا حذف تلقائي ولا Retention تلقائي.
- الحد الأقصى للرفع قابل للضبط من الإعدادات، ولم يُعتمد حد نهائي.
- لا يوجد Watermark لعدم وجود شعار رسمي.

## 5. دليل التحقق
تم تنفيذ `backup-media.ps1 -Verify` على:
- مجلد اختبار صناعي (2 ملفات) → `BACKUP_OK` و`RESTORE_VERIFIED`.
- مجلد QA الفعلي (6 ملفات) → `BACKUP_OK` و`RESTORE_VERIFIED` بتاريخ 9 نوفمبر 2026.
