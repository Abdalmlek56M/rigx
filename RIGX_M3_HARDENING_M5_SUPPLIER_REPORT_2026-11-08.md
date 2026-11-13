# RIGX — M3 Hardening + M5 Supplier Directory Report (8 نوفمبر 2026)

## 1. ما تم إصلاحه في M3

### 1.1 منع تصعيد الصلاحيات
أضيف `IdentityDelegationPolicy` كقواعد نقية مستقلة عن EF:

- لا يمنح المدير صلاحية لا يملكها.
- لا يعدل حسابًا يملك صلاحيات أعلى منه.
- لا يعدل دورًا يحتوي صلاحيات لا يملكها.
- لا يعيّن أو يزيل دور `Owner` إلا من يملك دور `Owner`.
- لا يعدل حساب `Owner` آخر من غير Owner.
- لا يعطّل المستخدم حسابه بنفسه.
- لا يمكن أن يبقى النظام بلا Owner نشط.
- لا يمكن أن يبقى النظام بلا مستخدم نشط يملك `System.ManageUsers`.
- كل محاولة رفض 403/409 تُسجل `Identity.AdministrationRejected` بعد Rollback.

### 1.2 تفاصيل التنفيذ
- `IdentityActor` و`IdentityRoleGrant` و`IdentityDelegationPolicy`.
- كل عملية إدارة تحمّل صلاحيات الفاعل من قاعدة البيانات داخل معاملة `authorization_state` المقفلة، لا من Claims العميل.
- `EnsureAdministrativeAccessAsync` بعد الحفظ؛ أي فقدان وصول إداري يسبب Rollback.
- تعطيل الحساب/تغيير الأدوار يبطل الجلسات ويزيد `authorization_version`.
- تعديل صلاحيات الدور يزيد `rbac_revision`.
- Audit النجاح داخل نفس المعاملة؛ Audit الرفض في معاملة منفصلة.

### 1.3 التحقق
- `Manager_cannot_escalate_beyond_own_permissions_or_touch_owners`.
- حماية آخر Owner + تعطيل متزامن.
- منع تعديل Owner/Customer وصلاحيات خارج الكتالوج.
- منع تعطيل الذات.
- 4 اختبارات إدارة هوية + 3 OwnerBootstrap + 10 PermissionAuthorization = **21/21 focused backend**.

### 1.4 مراجعة First Owner Bootstrap ودورة الهوية
- Bootstrap لا يملك Endpoint عامًا، ولا Credentials افتراضية، ويكتب Audit ذريًا، ويرفض التشغيل الثاني، ويعمل تحت Advisory Lock.
- Email Verification / Password Reset / Password Change / Logout:
  - الرموز أحادية الاستخدام، hash فقط، وروابط fragment.
  - نجاح Verify/Reset/Change يزيد `authorization_version` ويبطل الجلسات.
  - فشل Audit في العمليات الحساسة fail-closed.
- لم تُعدّل سياسات كلمة المرور أو الجلسات المتفق عليها.

---

## 2. تحسينات OOP والتنظيم وقابلية الصيانة

- فصل `IdentityAdministrationModels.cs` عن `IdentityAdministrationService`.
- إضافة `IdentityDelegationPolicy.cs` كمسؤولية مستقلة قابلة للاختبار.
- فصل `SupplierDirectoryModels.cs` عن `SupplierDirectoryService`.
- إضافة ADR-010 (Identity Delegation) وADR-011 (Supplier Party/Purchasing).
- Admin Shell:
  - لم يعد يفترض `Catalog.View` لكل Admin.
  - يعرض فقط مجموعات المسارات المتاحة للصلاحيات الفعلية.
  - Dashboard يعرض حالة محدودة وروابط متاحة لمن لا يملك Catalog.View.
- Mobile Supplier Cards بدل جدول أفقي ضيق.
- الحفاظ على Modular Monolith وعدم إدخال Microservices/CQRS/Generic Repositories.

---

## 3. ميزة M5 — Supplier Directory

### 3.1 اختيار الشريحة
بوابات M5 الحالية: Q2.1–Q2.5 (Serials/Reservations/Transfers)، Q6.3 (SKU scope)، Q8.7 (Condition/Warranty).
كلها تخص Stock/Receiving/Transfers/Products. لذلك اختيرت **Supplier Directory** لأنها بيانات تجارية مستقلة ولا تمس كمية أو تكلفة أو SKU.

### 3.2 النطاق
- `supplier_profiles` في Purchasing.
- `Person` في Identity مع `IsSupplier=true`.
- `CompanyName`, Tax ID, Commercial Registration, Contact, Bank, Payment Terms, Notes, Active.
- Create/Edit/List/Get؛ لا Delete.
- بحث/فلترة/ترقيم.
- منع تكرار Tax/Commercial Registration عبر Unique Partial Indexes.
- `RowVersion` Concurrency.
- Audit في نفس معاملة Person+Profile.
- لا Stock/Transfers/PO/Receipts.

### 3.3 API
- `GET /api/v1/admin/purchasing/suppliers` — `Purchasing.View`
- `GET /api/v1/admin/purchasing/suppliers/{id}` — `Purchasing.View`
- `POST /api/v1/admin/purchasing/suppliers` — `Purchasing.ManageSuppliers`
- `PUT /api/v1/admin/purchasing/suppliers/{id}` — `Purchasing.ManageSuppliers`

### 3.4 Migration
`20261108092628_M5_SupplierDirectory` أضيفت إلى Purchasing.
- **لم تُطبق على Preview الدائم.**
- `has-pending-model-changes` = No changes.
- التطبيق يتطلب مراجعة/موافقة ونسخة احتياطية.

### 3.5 الاختبارات
`SupplierDirectoryPostgreSqlTests` على PostgreSQL 17/Testcontainers:
1. Person + Profile + Audit + Search + Update/Deactivate + duplicate Tax.
2. تحديثان متزامنان بنفس RowVersion → نجاح واحد و409.
3. فشل Audit → Rollback كامل.
4. Email/Payment Terms غير صالحة → 400.

---

## 4. التغييرات البصرية

- Admin Sidebar يعرض:
  - الكتالوج (حسب الصلاحية)
  - المتجر/المحتوى (Catalog.View)
  - الهوية والصلاحيات (ManageUsers)
  - المشتريات → الموردون (Purchasing.View)
- Dashboard للحسابات غير الكتالوجية: رسالة واضحة + روابط متاحة، بلا طلب API محروم.
- Supplier Directory:
  - Desktop جدول كثيف.
  - Mobile Cards تعرض الشركة/التواصل/الهاتف/الضريبة/الشروط/الحالة.
  - Drawer إنشاء/تعديل مع حقول تجارية وبنكية.
  - Dark/Light/RTL/LTR Tokens.
- Footer Admin يعرض الآن `الكتالوج والمشتريات` و`Admin APIs` بدل ادعاء Catalog فقط.
- Visual QA:
  - Users/Roles/Drawers.
  - Suppliers Desktop/Mobile.
  - Limited Dashboard.
  - 0 Console Errors.

---

## 5. الاختبارات والتحقق

| الاختبار | النتيجة |
|---|---|
| `dotnet build HARDWARE_STORE.slnx` | ✅ 0 Warning / 0 Error |
| Focused backend (Owner/Identity/Permissions/Supplier) | ✅ 21/21 |
| PostgreSQL/Testcontainers | ✅ 4 Supplier + 4 Identity + 3 Owner |
| `npx tsc --noEmit` | ✅ 0 |
| `npx vitest run` | ✅ 10 ملفات / 40 اختبارًا |
| `npm run build` | ✅ 26 Route |
| `dotnet ef migrations has-pending-model-changes` | ✅ No changes |
| Visual QA (Desktop/Mobile, AR, Dark) | ✅ 0 Console Errors |
| Preview DB migration | ⛔ لم تُطبق — بانتظار اعتماد |

### لقطات الجولة

- [Identity Users](docs/review/m3-hardening-m5-2026-11-08/identity-users-dark.png)
- [Identity User Drawer](docs/review/m3-hardening-m5-2026-11-08/identity-user-drawer-dark.png)
- [Identity Roles](docs/review/m3-hardening-m5-2026-11-08/identity-roles-dark.png)
- [Identity Role Permissions](docs/review/m3-hardening-m5-2026-11-08/identity-role-permissions-dark.png)
- [Identity Role Create](docs/review/m3-hardening-m5-2026-11-08/identity-role-create-dark.png)
- [Suppliers Desktop](docs/review/m3-hardening-m5-2026-11-08/suppliers-dark.png)
- [Supplier Create](docs/review/m3-hardening-m5-2026-11-08/supplier-create-dark.png)
- [Supplier Edit](docs/review/m3-hardening-m5-2026-11-08/supplier-edit-dark.png)
- [Suppliers Mobile](docs/review/m3-hardening-m5-2026-11-08/suppliers-mobile-dark.png)
- [Limited Admin Dashboard](docs/review/m3-hardening-m5-2026-11-08/dashboard-limited-dark.png)

---

## 6. المخاطر والقرارات المطلوبة

1. **Q2.1–Q2.5 / Q6.3 / Q8.7** ما زالت معلقة؛ لا Stock/Transfers/Serials/Reservations/Purchase Orders.
2. **Supplier migration** لم تُطبق على Preview الدائم؛ التطبيق يحتاج موافقة ونسخة احتياطية.
3. **Bank data** محفوظة Plaintext في DB ومتاحة لمن يملك Purchasing.View؛ يلزم قرار تشفير/تقييد لاحق.
4. **Purchasing → Identity** اعتماد مباشر على Party persistence داخل معاملة واحدة؛ موثق في ADR-011، ويُستخرج إلى Party Contract عند توسع المشتريات.
5. لا حذف أدوار/موردين، لا تعديل Email/Username من Admin، لا Admin Password Reset.
6. Phase B: 21st MCP يحتاج Restart لتحميل `TWENTYFIRST_TOKEN`; Context7 DNS ما زال معطلًا.
7. Production registration مغلق.

**Rollback baseline:** `HARDWARE_STORE_RIGX_PHASE_A_M3_2026-11-08.zip` (قبل جولة hardening/M5 الحالية).

---

## 7. نقطة الاستكمال التالية

1. مراجعة بشرية للـM3 delegation وM5 Supplier migration.
2. تطبيق Supplier migration على قاعدة اختبار/Preview منفصلة بموافقة.
3. تصميم Warehouse/Purchase Order بعد حسم Q6.3 وQ2.x، دون Stock حركة.
4. M5 Stock/Transfers/Serials فقط بعد حسم Q2.1–Q2.5 وQ8.7.
5. إعادة تشغيل OpenCode لتحميل 21st، وإعادة فحص Context7.
6. لا تبدأ M6/M5 موسعًا تلقائيًا.
