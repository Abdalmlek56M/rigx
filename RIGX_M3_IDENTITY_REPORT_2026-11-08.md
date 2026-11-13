# RIGX — M3.2f / M3.3 Identity Report (8 نوفمبر 2026)

## 1. النطاق

إكمال الجزء المتبقي من M3 بعد Phase A/B:

- **M3.2f — First Owner Bootstrap:** أداة محلية لمرة واحدة، ذرّية مع Audit، بلا Endpoint عام.
- **M3.3 — Operational Identity & RBAC:** إدارة المستخدمين، الأدوار، الصلاحيات، والتدقيق،
  مع حماية آخر Owner وفصل Customer/Staff.

**لم تبدأ M5. لم تُعدّل قاعدة Preview الدائمة. لم تُضف Migration جديدة.**

---

## 2. M3.2f — First Owner Bootstrap

### التنفيذ
- `src/Modules/Identity/OwnerBootstrap.cs`
  - `FirstOwnerBootstrapService`
  - `FirstOwnerBootstrapRequest/Result/Exception`
- `src/Host/HardwareStore.Api/OwnerBootstrapCommand.cs`
  - يعمل فقط مع `--bootstrap-owner` و`OwnerBootstrap:Enabled=true`.
  - يقرأ البيانات وكلمة المرور من الطرفية؛ كلمة المرور مخفية وغير مسجلة.
- `Program.cs` يشغّل الأمر قبل بدء الـWeb Host ثم يخرج.

### الضمانات
| المتطلب | التنفيذ |
|---|---|
| بلا Endpoint عام | لا Route؛ CLI فقط |
| بلا Credentials افتراضية | Password يُدخل تفاعليًا |
| بلا أسرار في Logs/URLs/Args | لا تُطبع ولا تُمرر |
| منع التشغيل الثاني | وجود Role `Owner` أو حدث `Owner.Bootstrapped` ناجح |
| التزامن | `pg_advisory_xact_lock` داخل معاملة |
| Audit ذرّي | إدراج `Owner.Bootstrapped` في نفس اتصال/معاملة Identity |
| فشل Audit | Rollback كامل، ثم محاولة `Owner.BootstrapFailed` منفصلة |
| الصلاحيات | كل `PermissionCatalog` الـ36 |
| إبطال/تصادم | رفض التصادم مع Username/Email آخر |

### الاختبارات
`OwnerBootstrapPostgreSqlTests` (Testcontainers/PostgreSQL 17):
1. إنشاء Owner + 36 صلاحية + Audit ناجح + منع التكرار.
2. تشغيل متزامن: نجاح واحد فقط.
3. فشل Audit (حذف الجدول) → لا Owner ولا RolePermissions.
4. رفض مدخلات وكلمة مرور ضعيفة قبل أي كتابة.
5. عدم وجود Username/Email/Password في SafeMetadata.

---

## 3. M3.3 — Operational Identity & RBAC

### Backend
- `src/Modules/Identity/IdentityAdministration.cs`
  - `IdentityAdministrationService`
  - DTOs: `AdminIdentityUser`, `AdminIdentityUserPage`, `AdminIdentityRole`
  - طلبات: تعديل مستخدم، تعيين أدوار، إنشاء دور، تعديل صلاحيات.
- `src/Host/HardwareStore.Api/IdentityAdministrationEndpoints.cs`
  - كل المسارات تحت `/api/v1/admin/identity`
  - كلها تتطلب `System.ManageUsers`.
  - GET = `no-store`; POST/PATCH/PUT تمر عبر CSRF.

#### المسارات
| Method | Route | الغرض |
|---|---|---|
| GET | `/users` | بحث/حالة/ترقيم |
| GET | `/users/{id}` | تفاصيل مستخدم |
| PATCH | `/users/{id}` | الاسم/الهاتف/التفعيل |
| PUT | `/users/{id}/roles` | تعيين الأدوار |
| GET | `/roles` | الأدوار + الصلاحيات + عدد المستخدمين |
| GET | `/permissions` | `PermissionCatalog` فقط |
| POST | `/roles` | إنشاء دور تشغيلي |
| PUT | `/roles/{id}/permissions` | تعديل صلاحيات دور |

### القواعد الأمنية
- لا تُعاد كلمات المرور أو Hashes أو Tokens أو الجلسات.
- لا يمكن تعديل صلاحيات `Owner` أو `Customer`.
- لا يمكن إنشاء دور بالكود `Owner` أو `Customer`.
- لا يمكن استخدام صلاحية خارج `PermissionCatalog`.
- حماية آخر Owner نشط عند:
  - تعطيل الحساب.
  - إزالة دور Owner.
- تعيين الأدوار/التعطيل:
  - يزيد `authorization_version`.
  - يبطل الجلسات النشطة.
- تعديل صلاحيات دور:
  - يزيد `authorization_state.rbac_revision`.
- كل تغيير ناجح يكتب Audit Event داخل نفس المعاملة:
  - `Identity.UserUpdated`
  - `Identity.UserDeactivated`
  - `Identity.UserRolesChanged`
  - `Identity.RoleCreated`
  - `Identity.RolePermissionsChanged`
- القفل: `authorization_state` Row Lock + معاملة واحدة لكل تغيير.

### الواجهة
- `/admin/identity/users`
  - بحث بالاسم/Username/Email، فلتر نشط/غير نشط، ترقيم.
  - جدول كثيف: الاسم/البريد، Username، الأدوار، الحالة، تأكيد البريد.
  - Drawer تعديل: الاسم، الهاتف، التفعيل، الأدوار.
- `/admin/identity/roles`
  - جدول الأدوار وعدد المستخدمين والصلاحيات.
  - Drawer تعديل صلاحيات مجمعة حسب `Catalog/Sales/...`.
  - Drawer إنشاء دور جديد.
  - Owner/Customer للقراءة فقط.
- Sidebar: مجموعة `الهوية والصلاحيات` تظهر فقط لمن يملك `System.ManageUsers`.
- لا أزرار وهمية، ولا SuperAdmin bypass، والـBackend هو الضمان.

### الاختبارات
`IdentityAdministrationPostgreSqlTests` (Testcontainers/PostgreSQL 17):
1. إنشاء دور Staff وتعيينه لمستخدم وتعديل صلاحياته، مع Audit.
2. حماية آخر Owner عند التعطيل، وتشغيل متزامن يترك Owner نشطًا واحدًا.
3. رفض الصلاحيات غير المعروفة، الأدوار المحجوزة، وتعديل صلاحيات Owner.

`PermissionAuthorizationTests`: 9 اختبارات قائمة، وكلها ناجحة.
`npx vitest run`: 10 ملفات / 40 اختبارًا.
`npx tsc --noEmit`: نجح.
`npm run build`: نجح — 25 Route.
Visual QA: صفحات المستخدمين والأدوار وDrawers في Dark/AR، 0 Console Errors، عبر بيانات API مُحاكاة وآلية قراءة فقط.

---

## 4. الملفات الرئيسية

### Backend
- `src/Modules/Identity/OwnerBootstrap.cs`
- `src/Modules/Identity/IdentityAdministration.cs`
- `src/Host/HardwareStore.Api/OwnerBootstrapCommand.cs`
- `src/Host/HardwareStore.Api/IdentityAdministrationEndpoints.cs`
- `src/Host/HardwareStore.Api/Program.cs`
- `src/Modules/Identity/IdentityPersistenceRegistration.cs`

### Tests
- `tests/HardwareStore.Api.Tests/OwnerBootstrapTests.cs`
- `tests/HardwareStore.Api.Tests/IdentityAdministrationTests.cs`

### Frontend
- `apps/web/src/components/identity-users-admin.tsx`
- `apps/web/src/components/identity-roles-admin.tsx`
- `apps/web/src/app/admin/identity/users/page.tsx`
- `apps/web/src/app/admin/identity/roles/page.tsx`
- `apps/web/src/components/admin-shell.tsx`
- `apps/web/src/lib/api-client.ts`
- `apps/web/src/lib/i18n.tsx`
- `apps/web/src/app/globals.css`

### Docs
- `docs/operations/FIRST_OWNER_BOOTSTRAP.md`
- `docs/api/API_DESIGN_SECURITY.md`
- `docs/architecture/USERS_ROLES_PERMISSIONS.md`
- `README.md`, `PROGRESS.md`, `NEXT_TASK.md`, `MODEL_HANDOFF.md`

---

## 5. القيود

- لا حذف أدوار من الواجهة (لتجنب كسر مراجع مستقبلية)؛ يمكن تعطيل/تعديل الصلاحيات.
- لا إعادة تعيين كلمة مرور من شاشة الأدمن؛ تُستخدم مسارات Password Reset/Change الحالية.
- لا تعديل لـEmail/Username من شاشة الأدمن حاليًا (لتجنب تعقيدات الهوية والتصادم).
- Owner/Customer محميان من تعديل الصلاحيات.
- لا Migration جديدة؛ كل الجداول موجودة من M3.1/M3.2.
- التشغيل الإنتاجي لـOwner Bootstrap يحتاج مراجعة مستقلة لآلية النشر والأسرار.
- M5 لم تبدأ، وبوابة M5 ما زالت معلقة على قرارات المالك.

---

## 6. الخطوة التالية

1. مراجعة بشرية للـM3.2f/M3.3 diff واختبارات PostgreSQL.
2. عند الحاجة: إضافة حذف/تعطيل أدوار، وإدارة Email/Username بمسار آمن، وإعادة تعيين كلمة المرور من الأدمن.
3. فتح بوابة M5 فقط بعد حسم Q2.1–Q2.5 وQ6.3 وQ8.7.
4. عدم بدء M5 تلقائيًا.

