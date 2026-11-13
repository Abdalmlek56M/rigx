# First Owner Bootstrap (M3.2f)

## الهدف

إنشاء أول حساب `Owner` مرة واحدة بطريقة محلية صريحة، بلا Endpoint عام، وبلا بيانات
اعتماد افتراضية، وبلا كلمة مرور في Command Line أو Environment أو Logs.

## ما تم تنفيذه

- `FirstOwnerBootstrapService` داخل `src/Modules/Identity/OwnerBootstrap.cs`:
  - يحصر التشغيل المتزامن باستخدام PostgreSQL advisory transaction lock.
  - يرفض التشغيل إذا وُجد Role باسم `Owner` أو حدث Audit ناجح باسم `Owner.Bootstrapped`.
  - يرفض تصادم Username/Email مع حساب آخر.
  - يتحقق من Password Policy الحالية قبل أي كتابة.
  - ينشئ `Person` + `UserAccount` + `Owner` Role + كل صلاحيات `PermissionCatalog` الـ39.
  - يزيد `authorization_state.rbac_revision` داخل نفس المعاملة.
  - يكتب `Owner.Bootstrapped` في نفس المعاملة؛ فشل Audit يلغي كل التغييرات.
  - عند الفشل يحاول كتابة `Owner.BootstrapFailed` في معاملة منفصلة، بدون أسرار.
- `OwnerBootstrapCommand` في API:
  - لا يعمل إلا عند تمرير `--bootstrap-owner`.
  - لا يعمل إلا عند `OwnerBootstrap:Enabled=true`.
  - يقرأ Username/Email/FullName/Password من طرفية تفاعلية فقط.
  - يرفض `stdin` المعاد توجيهها حتى لا تدخل كلمة المرور في Logs/CI/Pipes.
  - كلمة المرور لا تُعرض أثناء الكتابة ولا تُسجل.
- لا توجد Route أو Endpoint عامة لهذه العملية.

## التشغيل

### 1) المتطلبات

- PostgreSQL متاح.
- Migrations مطبقة مسبقًا (Identity + Auditing).
- أدوار PostgreSQL الخاصة بالتدقيق موجودة وفق Migration:
  `hardware_store_audit_owner`, `hardware_store_app`, `hardware_store_audit_writer`.
- `ConnectionStrings:Default` مضبوط للـAPI.
- لا تشغّل العملية على Preview/Permanent DB دون خطة نسخ احتياطي واعتماد.

### 2) الأمر

من جذر المستودع:

```powershell
$env:OwnerBootstrap__Enabled = "true"
dotnet run --project src/Host/HardwareStore.Api -- --bootstrap-owner
```

ثم أدخل:

- Username
- Email
- Full name
- Password (يُكتب مخفيًا)
- تأكيد Password

بعد النجاح:

```powershell
Remove-Item Env:\OwnerBootstrap__Enabled
```

### 3) النتيجة المتوقعة

- Role: `Owner`
- 39 `RolePermission` من `PermissionCatalog`
- `Person` واحد + `UserAccount` واحد
- `authorization_state.rbac_revision` +1
- Audit: `Owner.Bootstrapped` / `Succeeded` / `System`
- لا كلمة مرور أو Hash أو Token في الـAudit أو الـLogs.

## منع التشغيل الثاني

- أي وجود لـ`Owner` Role يمنع التشغيل.
- أي حدث `Owner.Bootstrapped` ناجح يمنع التشغيل حتى لو حُذف الدور يدويًا.
- المحاولات المتزامنة تُسلسل بـAdvisory Lock؛ واحد فقط ينجح.
- عند رفض المحاولة تُكتب `Owner.BootstrapFailed` مع رمز الفشل فقط.

## الأمان

- لا تُمرر كلمة المرور في Args أو Env أو URL.
- لا تُقبل كلمة المرور من stdin المعاد توجيهه؛ يلزم TTY تفاعلي.
- لا تُطبع كلمة المرور أو Hash.
- لا تُعدّل `.env.preview.local` أو `service.json`.
- لا تستخدم Preview Bootstrap كبديل إنتاجي.
- **ملكية البريد:** `IsEmailVerified=true` في Bootstrap تعني أن المشغّل المحلي يملك الخادم وأدخل الحساب، وليست إثباتًا لدورة تحقق بريد فعلية. لا تغيّر هذه الأداة سياسة التحقق/إعادة التعيين؛ أي تغيير في دورة البريد يحتاج قرارًا مستقلًا.
- بعد إنشاء Owner، غيّر كلمة المرور عبر مسار تغيير كلمة المرور الآمن إذا لزم.
- لا تفعّل `OwnerBootstrap:Enabled` بشكل دائم في الإنتاج.

## الاختبارات

```powershell
dotnet test tests/HardwareStore.Api.Tests/HardwareStore.Api.Tests.csproj `
  --filter "FullyQualifiedName~OwnerBootstrapPostgreSqlTests"
```

تغطي:

- نجاح إنشاء Owner و39 صلاحية.
- منع التشغيل الثاني.
- تشغيل متزامن: نجاح واحد فقط.
- Audit ناجح داخل المعاملة.
- فشل Audit → Rollback كامل ولا Owner.
- رفض مدخلات وكلمة مرور ضعيفة قبل أي كتابة.
- عدم وجود Username/Email/Password في SafeMetadata.

## قيود

- الأداة تفترض أن Migrations مطبقة مسبقًا؛ لا تشغّل Migration تلقائيًا.
- التشغيل على Production يتطلب مراجعة مستقلة لآلية النشر والأسرار قبل التنفيذ.
- لا توجد إدارة لاحقة لكلمة مرور Owner من هذه الأداة؛ استخدم مسار تغيير كلمة المرور الحالي.

