# دليل الأشخاص والحسابات والأدوار والصلاحيات (Party, Accounts & RBAC)

تاريخ التحديث: نوفمبر 2026  
الحالة: **M3.2a-d مكتملة وفق اختباراتها المسجلة، وM3.2e منفذة ومختبرة عبر Playwright، وM3.2f/M3.3 منفذتان مع اختبارات PostgreSQL مركزة. التسجيل مغلق في Production وتظل قرارات مزود البريد وCaddy وفتح الإنتاج منفصلة.**

---

## 1. نموذج فصل الهوية (Party Model: Person vs Account vs Roles)

تم بناء الهوية وفق معمارية الطرف (`Party Model`) لضمان عدم تكرار بيانات الأشخاص وفصل الكيان البشري عن حسابه الرقمي:

```
+---------------------------------------------------------------------------------+
|                                 PERSON / PARTY                                  |
|  - Id (UUID)                                                                    |
|  - PersonCode (Unique, Immutable, PER-YYYYMM-XXXXX؛ معتمد)                   |
|  - FullName (Non-unique, Arabic / English)                                      |
|  - PrimaryPhone, SecondaryPhone, NationalId / TaxId                             |
|  - CreatedAtUtc, Notes                                                          |
+---------------------------------------------------------------------------------+
           |                                       |
           | 0..1 (اختياري)                        | 1..* (علاقات وأدوار متعددة)
           v                                       v
+-------------------------------+       +-----------------------------------------+
|         USER ACCOUNT          |       |           ROLES & RELATIONSHIPS         |
|  - Id (UUID)                  |       |  - IsCustomer (Customer Profile)        |
|  - Username (Unique)          |       |  - IsSupplier (Supplier Representative) |
|  - Email (Unique)             |       |  - IsEmployee (Staff Member)            |
|  - PasswordHash (NULL قبل تأكيد البريد؛ PBKDF2-HMAC-SHA512 عبر PasswordHasher IdentityV3 و220,000 iteration في V1؛ قابلة للترقية خلف IPasswordHashingService) |
|  - IsEmailVerified, AuthorizationVersion                                      |
|  - Server-side Cookie Sessions؛ لا JWT/Refresh Tokens في V1                  |
|  - IsActive                  |       |  [الشخص نفسه يمكن أن يكون عميلاً        |
|                               |       |   ومورداً وموظفاً في نفس الوقت]         |
+-------------------------------+       +-----------------------------------------+
           |
           v
+-------------------------------+
|         SECURITY ROLES        |
|  - Owner, StoreManager        |
|  - Cashier, WarehouseKeeper   |
|  - Accountant, SupportAgent   |
+-------------------------------+
```

أسماء الأدوار في الرسم أمثلة تعريفية فقط؛ لا يمنح اسم `SuperAdmin` أي تجاوز. كل صلاحية تظل مفحوصة عبر `PermissionAuthorization` وفق PermissionCatalog.

### قواعد الهوية الصارمة:
1. **كود الشخص (`PersonCode`):** كود فريد وثابت بصيغة `PER-YYYYMM-XXXXX`، يولده عداد شهري ذري وفق `Africa/Cairo`، بينما UUID هو المفتاح الداخلي. القرار منفذ ومعتمد في M3.1؛ الإشارة القديمة إلى Q6.1 تاريخية.
2. **اسم المستخدم والبريد الإلكتروني:** فريدان تماماً لكل حساب مستخدم مسجل (`Unique`).
3. **الاسم الحقيقي:** يُسمح بتكراره لتفادي مشاكل تشابه الأسماء الرباعية والعائلية.
4. **تطابق البيانات والتشابه:**
   - التطابق التام في المعرفات الفريدة (الرقم القومي، السجل التجاري، كود الشخص، اسم المستخدم، البريد) يُمنع برمجياً.
   - التشابه في أرقام الهواتف أو الأسماء يُصدر **تحذيراً إدارياً للمراجعة (Warning for Review)** دون منع الحفظ تلقائياً.
5. **عدم إلزامية حساب الدخول:** لا يُشترط لكل مورد أو عميل امتلاك حساب دخول للنظام (`UserAccount`). حساب الدخول مطلوب فقط عند الحاجة لتسجيل الدخول إلى المتجر الإلكتروني أو لوحة الإدارة.

---

## 2. مصفوفة الصلاحيات التفصيلية (Granular Permissions Catalog)

### 2.1 إدارة الكتالوج والمبيعات ونقاط البيع والفواتير (Catalog, Sales, Orders & Invoices)
* `Catalog.View`: استعراض بيانات الكتالوج الإدارية بما فيها المسودات.
* `Catalog.ManageTaxonomy`: إدارة التصنيفات والماركات فقط.
* `Catalog.ManageProducts`: إنشاء وتعديل المنتجات والمتغيرات والمواصفات والنشر.
* `Catalog.ManageMedia`: رفع وإدارة صور المنتجات.
* `Sales.ViewOrders`: استعراض قائمة وسجل الطلبات.
* `Sales.LogCommunication`: تسجيل نتائج الملاحظات والتواصل مع العميل عبر الواتساب أو الهاتف.
* `Sales.ApproveOrder`: مراجعة واعتماد الطلب بعد تأكيد العميل لبدء التجهيز.
* `Sales.CreatePOS`: إنشاء فواتير البيع المباشر في المعرض.
* `Sales.OverrideMinimumPrice`: صلاحية استثنائية لبيع صنف بأقل من سعر تكلفته بعد التحذير.
* `Sales.FulfillStorePickup`: اعتماد إجراءات تسليم الطلب للعميل في المعرض وتوثيق المستلم.
* `Sales.ProcessReturn`: اعتماد المرتجع وفحص السيريالات وإصدار إشعار دائن.
* `Invoices.ManageTemplates`: تعديل نصوص وتنسيق وقوالب الفواتير والمستندات (صلاحية مستقلة).
* `Invoices.ReviewDraft`: مراجعة وتعديل مسودة الفاتورة قبل اعتمادها وإصدارها نهائياً.
* `Invoices.IssueFinal`: اعتماد إصدار الفاتورة النهائي وتوليد رقمها الفريد وحفظ نسختها الثابتة.
* `Invoices.Reprint`: فتح الفواتير الصادرة وتنزيلها بصيغة PDF أو إعادة طباعتها.

### 2.2 إدارة المخزون والأرقام التسلسلية (Inventory)
* `Inventory.View`: استعراض الأرصدة والمخازن.
* `Inventory.Transfer`: تنفيذ تحويل بين المخازن.
* `Inventory.StockCount`: تسجيل جلسات الجرد الفعلي.
* `Inventory.StockAdjust`: اعتماد تسوية فروق الجرد والعجز/الزيادة.
* `Inventory.ManageSerials`: فحص وتعديل وتتبع الأرقام التسلسلية.
* `Inventory.ManageWarehouses`: إنشاء وتعديل دليل المستودعات (Code/Name/Address/Active) دون أرصدة أو حركات.

### 2.3 المشتريات والموردين (Purchasing)
* `Purchasing.View`: استعراض أوامر وفواتير الشراء وبيانات الموردين العامة (بدون Bank/IBAN).
* `Purchasing.ManageSuppliers`: إضافة وتعديل بيانات الموردين وربطهم بالأشخاص، وإدارة كتالوج أصناف المورد.
* `Purchasing.CreateOrder`: إنشاء أمر شراء مسودة.
* `Purchasing.ReceiveGoods`: استلام الشحنة وفحص السيريالات (`GRN`).
* `Purchasing.PostBill`: ترحيل فاتورة الشراء المالية.
* `Purchasing.ViewSupplierFinancials`: قراءة Bank/Account/IBAN مع Audit fail-closed.
* `Purchasing.ManageSupplierFinancials`: تعديل البيانات المالية للمورد مع RowVersion وAudit.

### 2.4 الحسابات المالية والخزائن (Financials)
* `Finance.ViewVaults`: استعراض أرصدة الخزائن والحسابات.
* `Finance.VerifyPayment`: التحقق من إيصالات التحويل البنكي/InstaPay واعتماد السداد الفعلي.
* `Finance.Disburse`: صرف نقدية أو تسوية مستحقات مورد.
* `Finance.ManageExpenses`: تسجيل واعتماد المصروفات التشغيلية.
* `Finance.ViewReports`: استعراض تقارير الأرباح والخسائر وحساب تكلفة البضاعة.

### 2.5 إدارة النظام والتشغيل المصرح به (Operations & Allowlisted Actions)
* `System.ManageUsers`: إنشاء المستخدمين وتعيين الأدوار وربطهم بالأشخاص.
* `System.ViewAuditLogs`: استعراض سجل التدقيق الشامل.
* `System.ViewErrorLogs`: استعراض سجلات الأخطاء البرمجية.
* `System.ExecuteAllowlistedAction`: صلاحية تشغيل إجراءات الصيانة المصرح بها (مسح كاش، فحص وسائط مفقودة).
* `System.ManageBackups`: طلب نسخ احتياطي فوري أو استعادة نسخة معتمدة.
* `System.StoreSettings`: ضبط إعدادات وسياسات المتجر (مهل الحجز، أيام المرتجعات، الضرائب).

---

## 3. الأمان وتوثيق الجلسات

1. **المصادقة (Server-side Cookie Sessions):**
   - اتجاه M3.2 المعتمد تصميميًا هو ASP.NET Core Cookie Authentication مع `ITicketStore` مدعوم بقاعدة البيانات؛ يحمل المتصفح مفتاح جلسة مبهمًا، وتظل التذكرة والصلاحيات على الخادم. لا JWT أو Refresh Tokens في V1.
   - Cookie: `HttpOnly`, `Secure` في الإنتاج، `SameSite=Lax`، مع CSRF Token فعلي للطلبات المعتمدة على Cookie. ربط Same-origin عبر Next.js Rewrite محليًا وCaddy في الإنتاج؛ CORS ليس بديلًا. تفاصيل الجداول والأعمار والإبطال في [تصميم API والأمان، قسم Authentication](../api/API_DESIGN_SECURITY.md#3-آليات-المصادقة-وحماية-الجلسات-authentication-architecture).
   - Customer: خمول 14 يومًا/حد أقصى 30 يومًا. Admin: خمول 30 دقيقة/حد أقصى 8 ساعات. تعطيل الحساب أو تغيير/إعادة تعيين كلمة المرور يلغي الجلسات؛ صلاحيات الجلسة تستمد من قاعدة البيانات ويُرفض الإصدار القديم. يستخدم `TimeProvider` UTC كمرجع موحد لصلاحية الجلسات والرموز، مع مزامنة ساعة الخادم.
2. **كلمات المرور والتحقق:** V1 يستخدم PBKDF2-HMAC-SHA512 عبر `PasswordHasher<TUser>` بوضع IdentityV3 و220,000 iteration، مع الإبقاء على `IPasswordHashingService` للترقية. لا تُضاف مكتبة Argon2 في M3.2d. الحد الأدنى 15 Unicode Scalar Value والحد الأقصى 128، دون Trim أو Normalization أو قواعد تركيب إجبارية، مع رفض الكلمات الشائعة محليًا. الحساب غير المؤكد لا يحصل على جلسة، والتسجيل لا ينشئ كلمة مرور؛ يختارها صاحب البريد بعد التحقق في M3.2d.
3. **مصدر الأدوار والصلاحيات:** قاعدة البيانات عبر `UserAccount → UserRoles → Roles → RolePermissions → Permissions`، مع `PermissionCatalog` مرجعًا لتعريف الصلاحيات. لا يثق النظام بما يرسله العميل ولا يوجد SuperAdmin bypass. Owner الأول يحصل صراحةً على معرفات الكتالوج الـ39 الحالية عبر Bootstrap محلي لمرة واحدة؛ التفصيل في وثيقة API.
4. **إبطال تغير الصلاحيات المشتركة:** `authorization_version` للحساب يكشف تغيّر UserRoles، و`authorization_state.rbac_revision` العام يكشف تعديل `RolePermissions` المشترك. أي اختلاف يبطل الجلسة والـPrincipal القديم ويطلب تسجيل الدخول مجددًا؛ لا يكفي إصدار الحساب وحده.
5. **الجلسات ومخزن المفاتيح:** تحفظ تذكرة Cookie في `ITicketStore` وتكون Data Protection keys دائمة ومشتركة بين API replicas في الإنتاج. إعداد Key Ring وCaddy وForwarded Headers ومصادر IP الموثوقة تفصيليًا في [تصميم API والأمان](../api/API_DESIGN_SECURITY.md#3-آليات-المصادقة-وحماية-الجلسات-authentication-architecture).
6. **حالة القرار:** M3.2a-d مكتملة تقنيًا بعد نجاح PostgreSQL lifecycle `14/14` ومجموعة PostgreSQL الكاملة `47/47`، وM3.2e منفذة ومختبرة عبر Playwright `2/2`. Username/Email مطبعان وغير حساسين لحالة الأحرف مع حفظ الأصل ودون حذف `+tags` أو النقاط. تستخدم واجهات العميل API الحقيقي و`GET /identity/me`، التي تعيد الصلاحيات الفعالة من قاعدة البيانات ليستخدمها العرض في إظهار روابط الإدارة؛ فحص الـEndpoint نفسه يظل المرجع النهائي. البريد عبر `IEmailProvider` مع In-Memory Provider للتطوير وSMTP عام في Production ينتظر إعداد المزود والأسرار. التسجيل لا ينشئ جلسة أو كلمة مرور؛ تأكيد البريد 24 ساعة، وإعادة الإرسال تخضع للحدود المعتمدة. لا توجد APIs لإدارة Roles/Permissions ضمن M3.2.
7. **فصل الصلاحيات في المعالج (.NET 10):**   - ربط كل Endpoint بسياسة الصلاحية المحددة:
     ```csharp
     [Authorize]
     [HasPermission(Permissions.Sales.ApproveOrder)]
     [HttpPost("api/v1/sales/orders/{id}/approve")]
     public async Task<IActionResult> ApproveOrder(Guid id) { ... }
     ```
