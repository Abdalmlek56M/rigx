# تصميم واجهة برمجة التطبيقات والأمان (API Design & Security)

تاريخ التحديث: نوفمبر 2026  
المنصة: **ASP.NET Core Web API (.NET 10)**  
الحالة: **M3.2a-e مكتملة وفق اختباراتها المسجلة. M4 Catalog & Media مكتملة على Local Preview وتشمل Public/Admin APIs والوسائط وSearch/Filters/Six Sorts/Technical Facets؛ Playwright نُفذ على Preview. التسجيل مغلق في Production.**

---

## 1. معايير واتفاقيات تصميم الـ API (RESTful Conventions)

1. **إصدارات الواجهة البرمجية (API Versioning):**
   - استخدام مسار URL صريح للإصدار: `/api/v1/[resource]`.
2. **تسمية المسارات وأفعال HTTP:**
   - استخدام الأسماء بصيغة الجمع والأحرف الصغيرة ذات الشرطات (kebab-case):
     - `GET /api/v1/catalog/products`: استعراض المنتجات مع التصفية والترقيم (Pagination).
     - `GET /api/v1/catalog/products/{id}`: جلب تفاصيل منتج محدد.
     - `POST /api/v1/catalog/products`: إنشاء منتج جديد (`201 Created` مع `Location` Header).
     - `PUT /api/v1/catalog/products/{id}`: استبدال أو تحديث شامل للمنتج (`200 OK` أو `204 No Content`).
     - `PATCH /api/v1/catalog/products/{id}/status`: تعديل جزئي لحالة المنتج.
     - `DELETE /api/v1/catalog/products/{id}`: حذف منطقي للصنف (`204 No Content`).
3. **نموذج الاستجابة الموحد (Unified Response Envelope):**
   - استجابات البيانات الناجحة:
     ```json
     {
       "success": true,
       "data": { ... },
       "meta": {
         "pageNumber": 1,
         "pageSize": 20,
         "totalCount": 150
       }
     }
     ```
4. **معالجة الأخطاء المعيارية (RFC 7807 Problem Details):**
   - جميع استجابات الأخطاء (4xx و 5xx) ترجع بصيغة `ProblemDetails`:
     ```json
     {
       "type": "https://hardwarestore.local/errors/validation-error",
       "title": "Validation Failed",
       "status": 400,
       "detail": "One or more validation errors occurred.",
       "instance": "/api/v1/sales/orders",
       "errors": {
         "CustomerPhone": ["The phone number format is invalid."],
         "Items[0].Quantity": ["Quantity must be greater than zero."]
       }
     }
     ```

---

## 2. الفصل الصارم لكائنات نقل البيانات (Public vs Internal DTOs)

لحماية أسرار المتجر والبيانات المالية وهوامش الربح:

1. **كائنات الواجهة العامة للمتجر (Storefront Public DTOs):**
   - تقتصر على البيانات التسويقية والتجارية العامة: السعر للجمهور، السعر بعد الخصم، المواصفات الفنية، توفر المخزون كحالة (`InStock`, `OutOfStock`, `LowStock`) دون كشف الرصيد الفعلي الدقيق في المستودعات إذا رغب المتجر في ذلك.
   - **يُمنع منعاً باتاً** تضمين حقول تكلفة الشراء (`purchase_cost`, `cost_price`, `cogs`)، هوامش الربح (`profit_margin`, `markup`)، هوية الموردين (`supplier_id`, `supplier_name`)، أو كميات الشراء الأولية في أي DTO عام أو مفتوح للعملاء.
2. **كائنات لوحة الإدارة المالية والمخزنية (Admin & Operations DTOs):**
   - تعرض تفاصيل التكلفة والأرباح وسجل الموردين حصراً للمستخدمين الموثقين والذين يحملون تصاريح صريحة (مثل `Reports.ViewProfit` و `Purchases.ViewCosts`).
   - يتم عزل هذه الـ DTOs في مسارات إدارية منفصلة تماماً: `/api/v1/admin/...`.

---

## 3. آليات المصادقة وحماية الجلسات (Authentication Architecture)

### 3.1 القرار المعماري والنشر

الاختيار المعتمد لـV1 هو **Server-side Cookie Sessions** عبر ASP.NET Core Cookie Authentication مع `ITicketStore` دائم في PostgreSQL. لا يصدر API `Access Token` أو `Refresh Token`، ولا يوجد JWT في هذا المسار. `ITicketStore` يعيد للمتصفح مفتاح جلسة مبهمًا فقط، بينما تبقى تذكرة المصادقة والصلاحيات على الخادم.

هذا الاختيار مناسب لأن المتصفح وواجهة Next.js والـAPI سيظهرون للعميل كمصدر واحد: `Next.js Rewrite` في التطوير و`Caddy Reverse Proxy` في الإنتاج. الـRewrite للتطوير موجود فعليًا، والواجهات المتصلة منفذة في `apps/web`، بينما Caddy غير موجود في `compose.yaml` ولا يوجد Rewrite إنتاجي بعد؛ يظل تجهيز النشر شرطًا قبل الإطلاق. لن نعتمد على CORS لتغطية اختلاف المصدر.

| البديل | الأثر | القرار |
|---|---|---|
| Cookie بجلسة خادمية و`ITicketStore` | إبطال مركزي وفوري، وحالة جلسة قابلة للتحقق من قاعدة البيانات؛ يحتاج قراءة/تحديث مخزن الجلسات | معتمد لـV1 |
| JWT مع Refresh Tokens | يزيد تعقيد التخزين والتدوير والاكتشاف المبكر للإبطال؛ لا يقدم فائدة لازمة مع مصدر واحد للمتصفح والـAPI | غير مستخدم في V1 |
| Identity Framework أو مزود خارجي | قد يضيف اعتمادًا/تدفقًا لا يلزم قبل تثبيت احتياج SSO أو مزود هوية | لا يُضاف الآن؛ يعاد تقييمه فقط عند ظهور احتياج موثق |

### 3.2 خصائص الـCookie وCSRF

- اسم Cookie بادئة `__Host-` في الإنتاج، وخصائص `HttpOnly=true`, `Secure=true`, `SameSite=Lax`, `Path=/`، دون `Domain`. التطوير المحلي عبر HTTP يحتاج إعدادًا خاصًا منفصلًا لا يضعف إعداد الإنتاج.
- كل عمليات التغيير المعتمدة على Cookie، بما فيها Login وRegister وLogout وطلبات البريد وكلمات المرور، تستخدم `IAntiforgery`: يصدر الخادم Token للواجهة من مسار مخصص، وترسله الواجهة في Header مثل `X-CSRF-TOKEN`. يضاف فحص `Origin` عند توفره، ولا يُعامل `SameSite=Lax` كبديل عن CSRF Token.
- `FallbackPolicy` تظل RequireAuthenticatedUser. تبقى `AllowAnonymous()` صريحة فقط على الصحة، والتصفح العام، وLogin/Register والتحقق/استعادة الحساب حيث يلزم؛ وتظل Checkout والبيانات الشخصية والإدارة محمية كما هو موضح في القسم 3.9.
- واجهة Next.js تستخدم Rewrite في التطوير وCaddy في الإنتاج لتوجيه `/api/*` إلى API تحت نفس المصدر. لا توجد حاليًا سياسة CORS مطلوبة لهذا المسار؛ لا تُفتح CORS كحل بديل.

### 3.3 تصميم مخزن الجلسة والجداول

جداول M3.1 الحالية هي `persons`, `user_accounts`, `roles`, `permissions`, `user_roles`, `role_permissions`, و`person_code_counters`. أضافت M3.2a-c الجداول الفعلية `auth_sessions`, `authorization_state`, `identity_tokens`, و`user_account_login_identifiers` في Identity، و`security_audit_events` في Auditing. لا يكرر التصميم `IsEmailVerified` الموجود بالفعل.

| الجدول | الحقول/القيود الحالية | الغرض |
|---|---|---|
| `user_accounts` (توسعة) | `password_hash` يقبل NULL للحساب المعلق؛ قيد DB يفرض Hash إذا `is_email_verified=true`; حقول username/email والتطبيع الحالية؛ `authorization_version`; حقول القفل | الحساب المعلق لا يملك كلمة مرور؛ فحص التصادمات قبل الفهارس، دون دمج حسابات تلقائيًا |
| `auth_sessions` (جديد) | `session_key_hash bytea` UNIQUE؛ `user_account_id NOT NULL` FK إلى `user_accounts(id)` بـRESTRICT؛ `protected_ticket bytea NOT NULL`; `created_at_utc`, `last_activity_at_utc`, `idle_expires_at_utc`, `absolute_expires_at_utc`; `revoked_at_utc`, `revoke_reason`; `authorization_version`; `rbac_revision`; `row_version bigint` للتزامن | مخزن `ITicketStore`، مدد الجلسة وإبطالها، ولقطة نسختي التفويض |
| `identity_tokens` (جديد) | `id`; `user_account_id` FK بـRESTRICT؛ `purpose` محصور في EmailVerification/PasswordReset؛ `token_hash` بطول 32 بايت وفريد؛ `created_at_utc`, `expires_at_utc`, `used_at_utc`, `revoked_at_utc`; فريد جزئي لرمز فعّال واحد لكل حساب وغرض | تخزين SHA-256 فقط؛ إصدار رمز جديد يبطل السابق ضمن معاملة |
| `security_audit_events` ضمن Auditing (جديد) | `id`, `event_type`, `occurred_at_utc`, `outcome`, `actor_kind`, `actor_user_account_id` nullable, `subject_user_account_id` nullable, `session_key_hash` nullable, `correlation_id`, `safe_metadata jsonb` | مصدر Append-only لـBootstrap وأحداث الهوية؛ لا يخزن أسرارًا أو كلمات مرور أو Tokens |
| `user_account_login_identifiers` (جديد) | `normalized_identifier` مفتاح أساسي وفريد؛ `user_account_id` FK بـRESTRICT؛ فهرس للحساب | يمنع تصادم Username مع Email لحساب آخر، ويسمح بسجل واحد إذا تطابقا للحساب نفسه |
| `authorization_state` (جديد) | صف singleton بمفتاح ثابت؛ `rbac_revision bigint NOT NULL`; `updated_at_utc` | نسخة عامة للتغييرات المشتركة على `role_permissions`؛ زيادتها ذرية داخل معاملة التغيير |

يُنشأ صف `authorization_state` الوحيد ضمن التهيئة/الـMigration. كل معاملة تغيّر `role_permissions` تحدّث `rbac_revision = rbac_revision + 1` وتكتب التعيين في نفس المعاملة؛ تحديث الصف يقفل نقطة الإصدار المشتركة ويمنع ضياع الزيادات المتزامنة. غياب الصف أو تعذر تحديثه يوقف تغيير الصلاحية ويفشل التحقق من الجلسات بشكل مغلق.

ينشأ Session Key جديد من 32 بايتًا عشوائيًا تشفيريًا (`CSPRNG`) ويُرمز Base64Url للاستخدام كمعرّف مبهم داخل Cookie Authentication. لا يخزن المفتاح نفسه: يحسب Backend `SHA-256` للمفتاح ويخزن `session_key_hash` فقط، مع UNIQUE؛ وبما أن المفتاح عشوائي عالي الإنتروبيا فلا يعامل ككلمة مرور بشرية. لا يشتق من UserAccount ID أو الوقت أو عداد. عند تصادم نادر يعاد التوليد دون استبدال سجل. تُحمى Authentication Ticket بـASP.NET Core Data Protection قبل تخزينها في `protected_ticket bytea`؛ لا تخزن Claims أو serialization غير محمية. جميع حقول `*_at_utc` وحقول الانتهاء UTC.

`auth_sessions` يحتوي صفًا لكل Session Key؛ مفتاح الجدول هو Hash المفتاح. `StoreAsync` ينفذ INSERT؛ `RetrieveAsync` يعيد التذكرة فقط إذا كانت الجلسة غير ملغاة وغير منتهية والحساب فعالًا ونسختا التفويض مطابقتين؛ `RenewAsync` يحدث الصف الموجود فقط بشرط عدم الإلغاء والانتهاء ومطابقة `row_version`، ولا ينشئه من جديد؛ `RemoveAsync` يسجل إلغاءً منطقيًا ولا يحذف الصف. تحديث الخمول والإلغاء وتحديث تذكرة متزامنان عبر تحديث شرطي ذري/قفل صف؛ الإلغاء يفوز، ولا يستطيع `RenewAsync` إعادة إحياء جلسة ملغاة. الفشل أو تحديث صفر صفوف يؤدي لإعادة قراءة الحالة والرفض عند الشك.

قرار الوقت الحالي: `TimeProvider` هو المصدر الموحد لقرارات إنشاء الجلسات والرموز والتحقق من انتهائها، بالقيم UTC. لا تستخدم `CURRENT_TIMESTAMP` أو `clock_timestamp()` في منطق صلاحية الهوية، حتى لا تختلط ساعة التطبيق التي أنشأت الرمز بساعة أخرى عند استهلاكه. يتطلب ذلك مزامنة ساعة الخادم عبر NTP أو مصدر وقت موثوق. إذا انتقل V1 إلى أكثر من API replica مستقبلًا، يعاد تقييم استخدام وقت مركزي أو وقت قاعدة بيانات متسق. `idle_expires_at_utc` يمكن تمديده فقط حتى `absolute_expires_at_utc`، والأخير لا يتغير أبدًا. التنظيف يعمل على دفعات محدودة ويحذف فقط الجلسات المنتهية/الملغاة بعد مدة الاحتفاظ المعتمدة، مع بقاء سجل Security Audit مستقلًا. إلى أن تعتمد مدة الاحتفاظ، لا يوجد حذف آلي للسجلات التاريخية.

العلاقات `auth_sessions.user_account_id` و`identity_tokens.user_account_id` ومرجعي الحساب الاختياريين في `security_audit_events` تستخدم `ON DELETE RESTRICT`. لا ينشأ جدول مستقل لـ`auth_login_throttles` في V1: حد IP ينفذه Rate Limiter، والتدرج على الحساب يستخدم `access_failed_count` و`lockout_end_utc` بقفل مؤقت محدود؛ لا يوجد قفل دائم. لا تنشأ جداول Role/Permission إضافية؛ يستمر نموذج M3.1 مع `authorization_state`. قبل UNIQUE على حقول التطبيع، تفحص Migration أي تصادم وتتوقف بأمان عند وجوده. لا ينفذ `Database.Migrate()` عند تشغيل التطبيق.

### 3.4 إنشاء الجلسة والتحقق منها وإبطالها

1. يرسل المتصفح Login مع Username أو Email وكلمة المرور وCSRF Token. يبحث Backend باستخدام القيمة المطبّعة، ويستخدم استجابة عامة واحدة للحساب غير الموجود أو كلمة المرور الخاطئة أو الحساب غير النشط أو المقفول مؤقتًا.
2. لا يصدر Login جلسة لأي حساب غير مؤكد، حتى لو كان لديه PasswordHash قديم. تسجل الحسابات الجديدة دون كلمة مرور؛ بعد تأكيد البريد في M3.2d يختار صاحب البريد كلمة المرور. يظل الرد على الحساب غير المؤكد عامًا ومتوافقًا مع رد بيانات الدخول الخاطئة.
3. داخل معاملة مناسبة، ينشئ سجل جلسة جديدًا بمفتاح عشوائي وتذكرة محمية، ثم يرسل Cookie تحمل مفتاح مخزن التذكرة. يحدد الخادم سياق الجلسة المحدودة من حالة التحقق المخزنة، وتُلغى أي جلسة مجهولة/مؤقتة سابقة عند الانتقال لتسجيل الدخول لمنع Session Fixation.
4. عند كل طلب، يسترجع Cookie Authentication التذكرة من `ITicketStore`. يرفض الخادم الجلسة إذا لم يوجد الصف، أو انتهت مهلة الخمول/العمر الأقصى، أو ألغيت، أو عُطل الحساب، أو لم تطابق `authorization_version` للحساب أو `rbac_revision` العامة. أي اختلاف يبطل الـPrincipal القديم ويطلب تسجيل الدخول مجددًا؛ لا يسمح باستمرار الصلاحيات القديمة أو ترقيتها بصمت.
5. **مواعيد الصلاحية المعتمدة:** Customer: خمول 14 يومًا وحد أقصى 30 يومًا. Admin: خمول 30 دقيقة وحد أقصى 8 ساعات. يبدأ العمر الأقصى عند إنشاء الجلسة ولا يُمدد بالنشاط. إذا جمع الحساب دور Customer ودورًا غير Customer، يطبق الخيار الأكثر تشددًا (Admin)؛ وإذا تعذر تصنيف الدور، يطبق Admin حتى لا يتسع الوصول بصمت.
6. يحدث `last_activity_at_utc` و`idle_expires_at_utc` بحد أقصى مرة كل 5 دقائق لكل جلسة (قيمة تنفيذية مقترحة)، باستخدام تحديث شرطي ذري. لا يمدد ذلك `absolute_expires_at_utc`.
7. Logout الحالي يزيل/يلغي سجل الجلسة الحالية. Logout All يلغي كل الجلسات الفعالة للحساب. تغيير كلمة المرور أو Reset أو تعطيل الحساب يزيد `authorization_version` ويلغي الجلسات داخل المعاملة نفسها. إعادة تفعيل الحساب لا تعيد الجلسات القديمة؛ يحتاج المستخدم لتسجيل دخول جديد.
8. لا توجد Refresh Tokens. تجديد نشاط الجلسة هو تحديث تذكرة Cookie ضمن نفس الجلسة؛ التدوير الأمني لمعرف الجلسة يتم بتسجيل دخول ناجح أو إعادة إصدار مقصودة، مع حفظ الموعد النهائي المطلق وعدم تمديده. لا يعاد استخدام معرف جلسة سابق.

### 3.5 كلمات المرور والدخول والحماية من التخمين

- **قرار V1 الحالي:** استخدام `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>` بوضع `IdentityV3`، وخوارزمية **PBKDF2-HMAC-SHA512** وبعدد **220,000** iteration. يُبقي `IPasswordHashingService` حدًا معماريًا يسمح بترقية الخوارزمية مستقبلًا، ولا تُضاف مكتبة Argon2 أو أي مكتبة تجزئة خارجية في M3.2d.
- الكود الحالي يولد Salt عشوائيًا لكل Hash، ويستخدم صيغة Identity V3 ذاتية الوصف، ويعيد Hash القديم عند نجاح Login إذا طلب الإطار ترقية التكلفة. لا يُعاد استخدام Password Hash أو Salt ثابت.
- الحد الأدنى المعتمد 15 Unicode Scalar Value والحد الأقصى 128، مع رفض المدخل الأطول قبل تنفيذ PBKDF2. لا تُحذف المسافات من طرفي كلمة المرور ولا تُطبع Unicode أو تُحوّل حالتها بصمت. لا توجد قواعد تركيب إجبارية ولا تغيير دوري. يرفض Backend قائمة محلية للكلمات الشائعة؛ لا ترسل كلمة المرور إلى خدمة خارجية.
- يخزن `PasswordHash` فقط. تحديث القيمة وإبطال جلسات الحساب وزيادة `authorization_version` يتم ذريًا. لا يضاف `password_changed_at_utc` في M3.2d لأن `security_audit_events` والحقول الحالية تكفي للتدقيق، ولا توجد قاعدة عمل تحتاج هذا الحقل؛ استخدامه مستقبلًا يحتاج حاجة موثقة وMigration منفصلة.
- تبقى سياسة `5` محاولات لكل دقيقة لكل IP على نقاط الدخول الحساسة. يطبق Rate Limiter حد المصدر، وتستخدم حقول `access_failed_count` و`lockout_end_utc` في `user_accounts` لحظر حساب مؤقت ومتدرج، لا دائم، مع رسائل عامة؛ لا ينشأ جدول throttles مستقل في V1. عنوان IP يؤخذ من `RemoteIpAddress` بعد Forwarded Headers موثوقة.
- يحتفظ النظام بقيمتي `username` و`email` الأصليتين للعرض، ويستخدم حقلي `normalized_username` و`normalized_email` للمقارنة الفريدة وتسجيل الدخول دون حساسية لحالة الأحرف. تكون عملية التطبيع موحدة بين التسجيل وتسجيل الدخول؛ لا تحذف `+tags` ولا النقاط من البريد، ولا تطبق قواعد خاصة بمزود بريد. أي تصادم بعد التطبيع يوقف Migration/إنشاء الفهرس للمراجعة، ولا يدمج حسابات تلقائيًا.
- التطبيع التنفيذي موحد: إزالة المسافات المحيطة، Unicode NFC، ثم `ToUpperInvariant` لقيم البحث. يطبق على البريد كاملًا وفق قرار عدم حساسية الحالة، مع إبقاء قيمة العرض كما أدخلها المستخدم. لا يغيّر الجزء المحلي أو النطاق بقواعد مزود البريد، ولا يحذف `+tag` أو النقاط.
- يجري Login باسم المستخدم أو البريد المطبّع. لا تعرض الاستجابة وجود الحساب أو حالة التحقق أو التعطيل. منع التوقيت التفاضلي يشمل تنفيذ تحقق Hash وهمي عند عدم وجود الحساب، ضمن حدود الأداء.

### 3.6 التسجيل والتحقق من البريد واستعادة الحساب

- تسجيل M3.2c ينشئ `Person` و`UserAccount` ويربط دور `Customer` من الخادم داخل معاملة واحدة، دون كلمة مرور أو جلسة. لا يقبل DTO حقول Role/Permission، ولا يختار العميل دوره. تظل الصلاحيات فارغة لدور Customer وفق M3.1. بعد التحقق من البريد في M3.2d يختار صاحب البريد كلمة المرور. يستخدم التطوير والاختبارات In-Memory Email Provider، بينما تكامل SMTP الإنتاجي العام يحتاج إعدادًا وأسرارًا من المالك. Endpoint التسجيل غير متاح في Production. فتح التسجيل ينتظر اعتماد M3.2e وتجهيز النشر والتحقق التشغيلي والموافقة الصريحة.
- تنفيذ التسجيل يستخدم `PersonCodeGenerator.CreatePersonInCurrentTransactionAsync` داخل معاملة التسجيل التي تملكها الخدمة؛ لا يفتح المولد معاملة مستقلة ولا يعتمدها. الشخص والحساب وتعيين Customer وسجل Token تنجح أو تتراجع كوحدة واحدة. إرسال البريد يحدث بعد Commit، ويُستخدم Fake/Test Provider حاليًا دون مزود تجاري.
- لا ينشئ التسجيل جلسة. Email verification مطلوب قبل Checkout والطلبات الشخصية والوظائف التي تشترط بريدًا مؤكدًا. الحساب غير المؤكد لا يحصل على صلاحيات العميل الكاملة، ولا يمكنه تسجيل الدخول حتى بعد امتلاكه Hash قديم. بعد التأكيد في M3.2d يختار صاحب البريد كلمة مروره، ثم ينشئ Login جلسة كاملة من المصدر الموثوق.
- رمز تأكيد البريد صالح 24 ساعة؛ كلمة المرور لا تُنشأ في التسجيل. رموز الهوية عشوائية أحادية الاستخدام، ويُخزن SHA-256 فقط. لا يُستهلك الرمز عبر GET: يوضع في URL fragment وتستخرجه الواجهة ثم ترسله في POST محمي بـCSRF. يبقى تأكيد البريد وإعادة الإرسال واستهلاك الرمز ضمن M3.2d.
- حدود Rate Limiting المنفذة: Login 5/IP/دقيقة؛ التسجيل 10/IP/ساعة؛ إعادة إرسال التحقق 10/IP/ساعة مع 60 ثانية بين المحاولات و5 رسائل/24 ساعة للحساب؛ تأكيد البريد 20/IP/10 دقائق؛ طلب Password Reset 10/IP/ساعة مع حد حساب مستقل؛ واستهلاك Password Reset 20/IP/10 دقائق. ذاكرة النسخة لا تتشارك بين API instances، لذا تشغيل نسخة واحدة شرط V1. يظل فتح التسجيل في Production مشروطًا بمراجعة M3.2e وتجهيز النشر وفحص الحدود في بيئة الإنتاج وموافقة المالك الصريحة.
- استجابة التسجيل عند النجاح أو تصادم Username/Email موحدة في HTTP status والجسم لمنع تعداد الحسابات، مع أخطاء مستقلة لصحة/تنسيق المدخلات. إصدار الحساب والرمز ذري، والإرسال بعد Commit؛ فشل الإرسال لا يحذف السجل أو الرمز، وتتم الاستعادة عبر إعادة الإرسال في M3.2d. لا يسجل Audit أو Logs كلمة مرور أو Hash أو رمزًا أو رابط تأكيد.
- Forget Password وReset Password يعيدان استجابة عامة، وتكون رموز Reset منفصلة الغرض عن رموز التحقق. استهلاك الرمز وتغيير Hash وإبطال جميع الجلسات يحدث داخل معاملة واحدة.
- Checkout والطلبات الشخصية يرفضان الحساب غير المؤكد؛ لا يصدر Login جلسة قبل التأكيد، والتسجيل لا ينشئ كلمة مرور. لا يسمح للعميل بإرسال هوية حساب/شخص بديلة في الطلب.

### 3.7 مصدر الصلاحيات وتحديثها

تُحمل الصلاحيات من المسار الموثوق `UserAccount → UserRoles → Roles → RolePermissions → Permissions`. مصدر تعريف المفاتيح يظل `PermissionCatalog` الحالي فقط، وجدول `permissions` يعكسه. لا تقبل API أي Role أو Permission من العميل، ولا يوجد SuperAdmin bypass.

`authorization_version` للحساب **وحده غير كافٍ** لأن تعديل `role_permissions` يغير صلاحيات مستخدمين كثيرين دون تحديث صفوف حساباتهم. لذلك يوجد صف singleton في `authorization_state` يحمل `rbac_revision` عامة. أي تغيير في `role_permissions` يزيد هذه القيمة ذريًا داخل معاملة التغيير؛ وتزيد `user_accounts.authorization_version` عند تغيير `user_roles` أو تغيير أمني خاص بالحساب. تحفظ الجلسة الإصدارين اللذين بُني عليهما الـPrincipal. عند أي اختلاف، يُرفض الـPrincipal القديم وتُبطل الجلسة ويطلب تسجيل الدخول مجددًا؛ لا يعاد تحميل صلاحيات جديدة في الجلسة القديمة. إذا تعذر قراءة الإصدارين، يكون القرار fail-closed. لا يعتمد التصميم على `roles.permissions_version` لكل دور.

الحد المضمون هو أن أي Authorization يبدأ بعد Commit للتغيير لن يستخدم Claims أقدم. Request اجتاز التحقق قبل Commit قد يكتمل؛ العمليات الحساسة التي تحتاج إلغاءً خطيًا تعيد فحص `authorization_version` و`rbac_revision` داخل معاملة تنفيذ الأمر قبل الكتابة. عند تعذر قراءة الإصدار، القرار fail-closed.

### 3.8 Bootstrap أول مسؤول

ينشئ Bootstrap محلي لمرة واحدة Role باسم `Owner` وحسابًا مربوطًا بـPerson عبر أداة محلية، مع إدخال كلمة المرور تفاعليًا أو من مصدر أسرار مؤقت غير مسجل. يمنع تكرار الأداة إذا وجد Owner نشط أو سجل Bootstrap مكتمل. تسند صراحةً الصلاحيات الـ39 الحالية: `Catalog.View`, `Catalog.ManageProducts`, `Catalog.ManageTaxonomy`, `Catalog.ManageMedia` إضافة إلى صلاحيات Sales/Invoices/Inventory/Purchasing/Finance/System المذكورة في الكتالوج.

قبل تنفيذ M3.2a لم يكن المشروع يحتوي على Audit Log وظيفي. ينشئ M3.2a مصدرًا واحدًا `security_audit_events` داخل Auditing وواجهة Append-only؛ ويجب على M3.2b استخدامها لتسجيل نجاح وفشل الدخول من أول تنفيذ، بينما يسجل Bootstrap في M3.2f.

سجل الحدث Append-only بفرض صلاحيات PostgreSQL: دور التطبيق يملك INSERT وقراءة محددة فقط، ولا يملك UPDATE/DELETE على الجدول؛ لا تُخزن كلمة مرور أو Hash كلمة المرور أو Authentication Ticket أو Token خام أو محتوى حساس. الحقول تسجل النوع والنتيجة والفاعل/الهدف والوقت وCorrelation ID وبيانات آمنة محددة. يشمل Bootstrap وأحداث الهوية المذكورة أدناه. هذا يحمي من أخطاء حساب التطبيق ولا يدعي مقاومة DBA أعلى صلاحية أو tamper-proof cryptographic log.

**التنفيذ (M3.2f):** أضيف `FirstOwnerBootstrapService` و`--bootstrap-owner` كأداة محلية صريحة، بلا Endpoint عام. تستخدم PostgreSQL advisory transaction lock، وترفض التشغيل الثاني عبر وجود Role `Owner` أو حدث `Owner.Bootstrapped` ناجح، وتكتب حدث النجاح في نفس معاملة إنشاء Owner/Role/Permissions/`authorization_state`. تفاصيل التشغيل والاختبارات في [FIRST_OWNER_BOOTSTRAP.md](../operations/FIRST_OWNER_BOOTSTRAP.md).

حدث نجاح Bootstrap يكتب في نفس معاملة إنشاء Owner/Role/RolePermissions وتحديث `authorization_state.rbac_revision`؛ إذا تعذر إدراج الحدث أو Commit تتراجع العملية كلها. عند فشل Bootstrap يسجل الفشل في معاملة منفصلة بعد Rollback وبسبب عام. لا Endpoint عام ولا بيانات اعتماد ثابتة ولا bypass للصلاحيات. قيد Append-only بدور التطبيق لا يمنع DBA أعلى صلاحية من التلاعب.

### 3.9 نطاق Endpoints والواجهات

حالة المسارات: antiforgery وLogin وRegistration API موجودة، ومسارات M3.2d منفذة ومربوطة في Development/Testing فقط. `GET /identity/me` أُضيف في M3.2e لقراءة الحساب الحالي الآمن بواسطة واجهة المتصفح:

| المسار والحالة | الوصول | الغرض |
|---|---|---|
| `GET /api/v1/identity/antiforgery` | موجود: عام | إصدار CSRF token للواجهة |
| `POST /api/v1/identity/registrations` | موجود: عام + CSRF + Rate Limit؛ Development/Testing فقط حتى اكتمال M3.2e وبوابة فتح Production | إنشاء حساب Customer معلق بلا كلمة مرور أو جلسة |
| `POST /api/v1/identity/email-verifications` | M3.2d منفذ في Development/Testing: عام + CSRF + Rate Limit | استهلاك رمز البريد وتعيين كلمة المرور في طلب ذري واحد |
| `POST /api/v1/identity/email-verification-resends` | M3.2d منفذ في Development/Testing: عام + CSRF + Rate Limit | إعادة إرسال آمنة برسالة عامة |
| `POST /api/v1/identity/sessions` | موجود: عام + CSRF + Rate Limit | Login باسم مستخدم أو بريد |
| `DELETE /api/v1/identity/sessions/current` | M3.2d منفذ في Development/Testing: مصادق + CSRF | Logout الحالي |
| `DELETE /api/v1/identity/sessions` | M3.2d منفذ في Development/Testing: مصادق + CSRF | Logout All للحساب الحالي فقط |
| `GET /api/v1/identity/me` | موجود: مصادق + `no-store` | username/email/fullName/phoneNumber/roles/permissions/isEmailVerified فقط للحساب الحالي؛ `permissions` هي الصلاحيات الفعالة المكررة من قاعدة البيانات |
| `PATCH /api/v1/identity/profile` | 9 Nov: مصادق + CSRF + `no-store` | تعديل FullName/PhoneNumber فقط مع Audit |
| `GET /api/v1/identity/sessions` | 9 Nov: مصادق + `no-store` | Device/Browser/LastActivity/Created/Expiry/IsCurrent فقط؛ لا SessionKeyHash/ProtectedTicket |
| `DELETE /api/v1/identity/sessions/{id}` | 9 Nov: مصادق + CSRF | إبطال جلسة تخص الحساب فقط (IDOR-safe) + Audit |
| `DELETE /api/v1/identity/sessions/others` | 9 Nov: مصادق + CSRF | إبطال كل الجلسات الأخرى مع إبقاء الحالية + Audit |
| `POST /api/v1/identity/password-reset-requests` | M3.2d منفذ في Development/Testing: عام + CSRF + Rate Limit | بدء Reset برسالة عامة |
| `POST /api/v1/identity/password-resets` | M3.2d منفذ في Development/Testing: عام + CSRF + Rate Limit | استخدام رمز Reset وتغيير كلمة المرور |
| `POST /api/v1/identity/password-changes` | M3.2d منفذ في Development/Testing: مصادق + CSRF | تغيير كلمة المرور بعد التحقق من الحالية |
| `GET /api/v1/admin/identity/users` | M3.3: مصادق + `System.ManageUsers` | قائمة المستخدمين مع بحث/حالة/ترقيم؛ بلا Password/Hash/Token |
| `GET /api/v1/admin/identity/users/{id}` | M3.3: مصادق + `System.ManageUsers` | تفاصيل مستخدم وأدواره |
| `POST /api/v1/admin/identity/users` | M3.3+/9 Nov: مصادق + CSRF + `System.ManageUsers` | إنشاء حساب بدعوة بريد بدون كلمة مرور + Email Verification token |
| `POST /api/v1/admin/identity/users/{id}/password-reset` | 9 Nov: مصادق + CSRF + `System.ManageUsers` + Rate Limit | إرسال Reset Password آمن بدون كشف كلمة المرور |
| `PATCH /api/v1/admin/identity/users/{id}` | M3.3: مصادق + CSRF + `System.ManageUsers` | الاسم/الهاتف/التفعيل فقط، مع حماية آخر Owner |
| `PUT /api/v1/admin/identity/users/{id}/roles` | M3.3: مصادق + CSRF + `System.ManageUsers` | تعيين الأدوار، مع إبطال الجلسات وزيادة `authorization_version` |
| `GET /api/v1/admin/identity/roles` | M3.3: مصادق + `System.ManageUsers` | قائمة الأدوار وعدد المستخدمين والصلاحيات |
| `GET /api/v1/admin/identity/permissions` | M3.3: مصادق + `System.ManageUsers` | مفاتيح `PermissionCatalog` الحالية فقط |
| `POST /api/v1/admin/identity/roles` | M3.3: مصادق + CSRF + `System.ManageUsers` | إنشاء دور تشغيلي، مع حظر Owner/Customer والصلاحيات خارج الكتالوج |
| `PUT /api/v1/admin/identity/roles/{id}/permissions` | M3.3: مصادق + CSRF + `System.ManageUsers` | تعديل صلاحيات دور غير Owner/Customer وزيادة `rbac_revision` |
| `GET /api/v1/admin/purchasing/suppliers` | M5: مصادق + `Purchasing.View` | دليل الموردين مع بحث/حالة/ترقيم |
| `GET /api/v1/admin/purchasing/suppliers/{id}` | M5: مصادق + `Purchasing.View` | تفاصيل مورد |
| `POST /api/v1/admin/purchasing/suppliers` | M5: مصادق + CSRF + `Purchasing.ManageSuppliers` | إنشاء Person+SupplierProfile+Audit ذريًا |
| `PUT /api/v1/admin/purchasing/suppliers/{id}` | M5: مصادق + CSRF + `Purchasing.ManageSuppliers` | تعديل المورد مع RowVersion وتدقيق |
| `GET /api/v1/admin/purchasing/suppliers/{id}/financials` | M5: مصادق + `Purchasing.ViewSupplierFinancials` | قراءة Bank/Account/IBAN مع Audit fail-closed و`no-store` |
| `PUT /api/v1/admin/purchasing/suppliers/{id}/financials` | M5: مصادق + CSRF + `Purchasing.ManageSupplierFinancials` | تعديل البيانات المالية مع RowVersion وAudit |
| `GET /api/v1/admin/purchasing/suppliers/{supplierId}/products` | M5: مصادق + `Purchasing.View` | Supplier Product Catalog مع بحث/حالة/ترقيم |
| `GET /api/v1/admin/purchasing/suppliers/{supplierId}/products/{id}` | M5: مصادق + `Purchasing.View` | تفاصيل ربط مورد↔Variant |
| `POST /api/v1/admin/purchasing/suppliers/{supplierId}/products` | M5: مصادق + CSRF + `Purchasing.ManageSuppliers` | ربط Variant نشط بكود المورد + Notes + Activation |
| `PUT /api/v1/admin/purchasing/suppliers/{supplierId}/products/{id}` | M5: مصادق + CSRF + `Purchasing.ManageSuppliers` | تعديل الربط مع RowVersion وAudit |
| `GET /api/v1/admin/purchasing/suppliers/{supplierId}/available-variants` | M5: مصادق + `Purchasing.View` | بحث Variants النشطة للربط |
| `GET /api/v1/admin/inventory/warehouses` | M5: مصادق + `Inventory.View` | دليل المستودعات مع بحث/حالة/ترقيم |
| `GET /api/v1/admin/inventory/warehouses/{id}` | M5: مصادق + `Inventory.View` | تفاصيل مستودع |
| `POST /api/v1/admin/inventory/warehouses` | M5: مصادق + CSRF + `Inventory.ManageWarehouses` | إنشاء مستودع + Audit |
| `PUT /api/v1/admin/inventory/warehouses/{id}` | M5: مصادق + CSRF + `Inventory.ManageWarehouses` | تعديل مستودع مع RowVersion + Audit |
| `GET /api/v1/storefront/settings` | عام | WhatsApp/Social/Pickup/Trust settings؛ لا أسرار |
| `GET /api/v1/admin/storefront/settings` | مصادق + `System.StoreSettings` | إعدادات المتجر الحالية |
| `PUT /api/v1/admin/storefront/settings` | مصادق + CSRF + `System.StoreSettings` | تعديل الإعدادات مع RowVersion + Audit |

**Supplier financial protection:** استجابة `GET /suppliers` و`GET /suppliers/{id}` لا تحتوي `bankName` أو `bankAccountNumber` أو `bankIban`. البيانات المالية لا تُعاد إلا من المسار المخصص وبصلاحية `Purchasing.ViewSupplierFinancials`. هذه القراءة تُسجَّل في Audit داخل معاملة، وتفشل مغلقة إذا تعذر إنشاء سجل التدقيق. لا تُكتب القيم البنكية في Audit metadata أو رسائل الأخطاء. تخزين الحقول الحالي Plaintext؛ يلزم قرار Key Management/تشفير تشغيلي قبل الإنتاج.

المتجر يظل عامًا للتصفح. Checkout وبيانات الطلبات الشخصية تتطلب جلسة مصادق عليها، وCheckout يرفض `IsEmailVerified=false`، ويستخرج Person/Customer ID من المستخدم الحالي لا من Body. الإدارة تتطلب `RequirePermission` مناسبًا. تستخدم واجهة الويب `permissions` من `/identity/me` لإظهار روابط Catalog الإدارية فقط، مع بقاء فحص Backend هو الضمان النهائي؛ لا يُستنتج التصريح من اسم Role. واجهات M3.2e المنفذة هي Register/Verify/Resend/Login/Forgot/Reset/Account Security وLogout Current/All؛ لا واجهات إدارة مستخدمين أو Roles/Permissions (هذه M3.3).

### 3.9.1 Catalog & Media API المنفذة في M4

| المسار | الوصول | الغرض |
|---|---|---|
| `GET /api/v1/catalog/categories` | عام | شجرة التصنيفات النشطة |
| `GET /api/v1/catalog/brands` | عام | الماركات النشطة |
| `GET /api/v1/catalog/specification-filters` | عام | قيم Specifications الفعلية للمنتجات المنشورة، مجمعة حسب المجموعة والمواصفة |
| `GET /api/v1/catalog/products` | عام | المنتجات Published فقط مع Category/Brand/Search/Condition New-Used/Specification/Min-Max Price/Sort/Pagination |
| `GET /api/v1/catalog/products/{slug}` | عام | تفاصيل منتج Published مع Variants، ومواصفات Product/Variant، والصور |
| `GET /api/v1/catalog/media/{**storageKey}` | عام | قراءة Media مولدة تحت المسار الآمن فقط |
| `GET /api/v1/admin/catalog/categories` | `Catalog.View` | كل التصنيفات بما فيها غير النشط |
| `GET /api/v1/admin/catalog/brands` | `Catalog.View` | كل الماركات |
| `GET /api/v1/admin/catalog/products` | `Catalog.View` | قائمة الإدارة بكل الحالات |
| `POST/PUT /api/v1/admin/catalog/categories[/{id}]` | `Catalog.ManageTaxonomy` | إنشاء/تعديل التصنيف |
| `POST/PUT /api/v1/admin/catalog/brands[/{id}]` | `Catalog.ManageTaxonomy` | إنشاء/تعديل الماركة |
| `POST/PUT /api/v1/admin/catalog/products[/{id}]` | `Catalog.ManageProducts` | إنشاء/تعديل Product وVariants وSpecifications |
| `PATCH /api/v1/admin/catalog/products/{id}/status` | `Catalog.ManageProducts` | نشر/إخفاء مع Row Version |
| `POST/PUT/DELETE .../products/{id}/images[/{imageId}]` | `Catalog.ManageMedia` | رفع/تعديل/حذف Media |
| `POST/DELETE .../categories/{id}/image` | `Catalog.ManageMedia` | صورة التصنيف |
| `POST/DELETE .../brands/{id}/logo` | `Catalog.ManageMedia` | شعار الماركة |
| `GET .../media-audit` | `Catalog.ManageMedia` | تقرير الملفات المفقودة واليتيمة |
| `POST .../media-audit/cleanup` | `Catalog.ManageMedia` | حذف اليتيم الأقدم من مهلة الأمان بعد إعادة قراءة مراجع قاعدة البيانات لكل ملف قبل حذفه |

كل طلبات Admin التغييرية محمية بـCSRF. Public لا يعرض Draft ولا منتجًا مرتبطًا بـCategory/Brand غير نشط. التصنيف يعتبر فعالًا فقط إذا كان نشطًا وكانت كل سلسلة آبائه نشطة؛ الابن النشط تحت أب معطل يظل مرئيًا للإدارة ومخفيًا عن Storefront.

رفع الصور يتطلب Backend فكًا فعليًا وإعادة ترميز WebP؛ لا يثق في MIME أو الامتداد. الحدود الافتراضية: 12 MiB للرفع، 8192px لكل ضلع، 40MP، 2000px للنسخة المخزنة، 480px للـThumbnail، جودة 82، و8 MiB للـWebP الناتج. إعادة الترميز تزيل Metadata، ويُرفض الملف غير القابل للفك أو خارج الحدود. تنظيف اليتامى لا يحذف مرجعًا حيًا، ولا يمس ملفًا أحدث من 30 دقيقة. قائمة Audit الأولية مرشحات فقط؛ قبل حذف كل ملف يعيد الخادم الاستعلام من `product_images` و`categories` و`brands`، ويتخطى الملف إذا صار مرجعًا. تظل الفجوة الدقيقة بين آخر استعلام وحذف الملف على القرص غير ذرية بطبيعتها، لذلك تبقى مهلة الأمان ومنع تشغيل أكثر من عملية Cleanup متزامنة ضوابط تشغيلية مطلوبة.

`GET /products` يقبل `category`, `brand`, `search`, `minimumPrice`, `maximumPrice`, وإعادة `specification` بصيغة URL-encoded `groupName|name|value`، و`sort` بقيم `newest`, `oldest`, `price-asc`, `price-desc`, `name-asc`, `name-desc`، و`page`, `pageSize` بحد أقصى 100. القيم المتعددة لنفس المواصفة تُطبق بـOR، والمواصفات المختلفة تُطبق بـAND. `FromPrice` هو أقل سعر متغير نشط له سعر، وإلا `BasePrice`؛ وترتيب السعر يعتمد `FromPrice`.

Product Summary View يعيد `specifications` بحد أقصى ثلاث مواصفات Product-level مرتبة للعرض المختصر. مواصفات Variant لا تُدمج في Summary ولا تُنسب إلى المنتج كله؛ تعود كاملة في Product Detail بعد اختيار Variant. هذا عقد عرض ولا يغيّر حدود الوحدات أو التخزين.

`/admin` مسار واجهة Next.js وليس API endpoint جديدًا. يستخدم نفس Admin Catalog APIs المحمية، ويبني ظهور الوحدات من `permissions` التي يعيدها `GET /api/v1/identity/me`. إخفاء رابط الواجهة ليس تفويضًا؛ كل Admin API يستمر في فرض `Catalog.*` على الخادم.

RIGX Product Comparison V1 لا يضيف API: يحفظ IDs/Slugs بصورة Local Versioned، ثم يعيد جلب `GET /api/v1/catalog/products/{slug}` لكل اختيار، ويتحقق من ID وCategory والحالة العامة. الأسعار والمواصفات لا تُخزن في Preference. WhatsApp Inquiry يستخدم `NEXT_PUBLIC_STORE_WHATSAPP_NUMBER` المملوك ويُخفى عند غيابه، بينما SEO Server Fetch يستخدم `API_ORIGIN` العام فقط ولا يصل إلى Admin APIs.

شاشة Login في M4 لا تضمن `name` لحقول الاعتماد، ولا تستخدم Query Parameters؛ المصادقة تظل `POST /api/v1/identity/sessions` عبر API Client مع Cookies وCSRF. PasswordField يبدل الظهور محليًا فقط، ويظهر رسائل 401 و429 و503 داخل النموذج دون Reload صامت.

`POST /api/v1/development/preview/initialize` لا يُسجّل إلا عندما `ASPNETCORE_ENVIRONMENT=Development` و`Preview:Enabled=true`. يحتاج CSRF و`X-Preview-Bootstrap-Key` من `.env.preview.local`؛ يطبق Migrations على قاعدة Preview المعزولة ثم ينشئ Owner من إعدادات البيئة ويضيف 14 منتجًا تجريبيًا وصورًا مولدة. لا توجد Credentials افتراضية، والـEndpoint غير موجود في Production.

### 3.10 ملفات واعتمادات متوقعة، وسجل Data Protection

الملفات المتوقعة مع قصر التغييرات على المسؤوليات القائمة: تحديث `src/Modules/Identity/IdentityPersistence.cs` و`IdentityPersistenceRegistration.cs`؛ إضافة خدمات Password Lifecycle/Session Revocation وواجهة إرسال بريد Password Reset داخل `src/Modules/Identity/`؛ توسيع تسجيل DI وMiddleware في `src/Host/HardwareStore.Api/Program.cs` فقط عند الحاجة؛ توسيع كتابة Audit تحت `src/Modules/Auditing/` إذا لزم دعم Transaction مشتركة؛ اختبارات Identity/API داخل `tests/HardwareStore.Api.Tests/`. لا يحتاج M3.2d Migration أو Model Snapshot جديدًا. لا تعدل صفحات `apps/web` أو `next.config.ts` أو Caddy/Compose ضمن هذه الشريحة؛ تنتقل إلى M3.2e أو تجهيز النشر.

| الاعتماد | الموجود/المقترح | قرار التصميم |
|---|---|---|
| `Microsoft.AspNetCore.App` | موجود ضمن API Host | يوفر Cookie Authentication وData Protection وAntiforgery وForwarded Headers وRate Limiting و`PasswordHasher<TUser>` المستخدم حاليًا؛ لا حزمة Authentication خارجية |
| EF Core/Npgsql وTestcontainers | موجودة من M3.1/الاختبارات | تعاد للاستخدام في Persistence والاختبارات، دون تحديث بسبب M3.2 |
| `TopSecret.Cryptography.Argon2` | غير مثبت وغير مستخدم | لا يضاف في M3.2d؛ أي اعتماد مستقبلي يحتاج تغييرًا أمنيًا منفصلًا وتقييمًا وقياسًا |
| `Microsoft.AspNetCore.Identity` PasswordHasher | مستخدم عبر ASP.NET Core Shared Framework | PBKDF2-HMAC-SHA512 بوضع IdentityV3 و220,000 iteration؛ لا حزمة خارجية |
| مزود إرسال البريد | `IEmailProvider`، In-Memory Provider للتطوير والاختبار، و`SmtpEmailProvider` إنتاجي عام مبني على مكتبة .NET القياسية | لا Dependency لمزود تجاري محدد. أسرار SMTP تُحقن من environment، وProduction يفشل بوضوح عند نقص الإعدادات |
| Data Protection provider | إعداد ASP.NET Core | filesystem دائم محلي/مركزي ممكن بلا حزمة إضافية؛ أي Key Vault أو خدمة تخزين خارجية قد تتطلب Provider بعد اختيارها |

**Data Protection Key Ring:** التطوير والإنتاج يستخدمان Key Rings منفصلة، لكل بيئة `ApplicationName` ثابتًا ومختلفًا؛ لا تحفظ مفاتيح التطوير في ملفات المصدر أو الأرشيف. الإنتاج يستخدم Ring دائمًا ومشتركًا خارج Container filesystem وعلى كل API replicas، مع تشفير at-rest عبر `ProtectKeysWithCertificate` أو مزود تشفير مكافئ بمفتاح/شهادة منفصلة عن صورة التطبيق وصلاحيات قراءة/كتابة محدودة لهوية الخدمة. عند اختيار مخزن دائم صريح، لا يُفترض أن تشفير التخزين مفعّل تلقائيًا؛ يجب إعداد التشفير والتحقق منه. Backup يشمل Ring المشفر ومادة فك تشفيره في قناة/صلاحية منفصلة، وتجرى تجربة Restore دورية قبل النشر. تستعاد المفاتيح والقاعدة كنسخة متوافقة؛ وقبل فتح المرور تُبطل جلسات ورموز النسخة المستعادة لتجنب إعادة جلسات ألغيت بعد Snapshot. فقد المفتاح أو مادة فك التشفير يؤدي لرفض البيانات المحمية وإعادة تسجيل الدخول/إصدار Tokens؛ لا يوجد fallback بمفتاح جديد. تبقى المفاتيح القديمة ما دامت بيانات محمية بها ضمن الاحتفاظ.

### 3.10.1 Forwarded Headers وRate Limiting

في الإنتاج، Caddy هو أول Reverse Proxy؛ إعداد Caddy الموثق غير موجود حاليًا في Compose. Caddy يرفض افتراضيًا قيم `X-Forwarded-*` الواردة من العميل ويكتب قيمًا موثوقة. لا يضبط `trusted_proxies` في Caddy إلا عند وجود Proxy أمامه مثل CDN، وتقتصر القائمة على عناوينه الموثوقة مع ترتيب سلسلة صحيح.

في API، يشغّل `UseForwardedHeaders` قبل Rate Limiting وAuthentication، ويقبل `X-Forwarded-For` و`X-Forwarded-Proto` فقط من Caddy المحدد في `KnownProxies` أو من CIDR ضيقة مخصصة لشبكة Proxy في `KnownNetworks`، مع `ForwardLimit=1` لاتصال Caddy مباشر. شبكة KnownNetworks يجب ألا تضم API أو عملاء آخرين، ويجب عزل منفذ API وعدم نشره للإنترنت في الإنتاج. لا يستخدم `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` لأنه قد يزيل قيود KnownProxies/Networks. إذا أضيف CDN أو Proxy ثانٍ لاحقًا، يراجع طول السلسلة والمصادر الموثوقة وForwardLimit قبل تشغيله.

Rate Limiter يأخذ `HttpContext.Connection.RemoteIpAddress` بعد معالجة Forwarded Headers، ولا يقرأ `X-Forwarded-For` يدويًا ولا يثق به مباشرة. Header مزور من اتصال غير موثوق لا يغير IP المستخدم للحصة. حد `5` محاولات/دقيقة/IP يبقى كما اعتمد، ومفتاح الحساب/IP للـbackoff يخزن بصيغة مشتقة. إذا زادت API replicas، يجب أن تكون حالة limiter مشتركة؛ ذاكرة نسخة واحدة لا تحقق حدًا عامًا عبر النسخ.

### 3.11 تقسيم M3.2 واختبارات القبول

| Slice | النطاق | اختبارات قبول مركزة |
|---|---|---|
| M3.2a — مخزن الجلسات وأساس سجل الأمن | `auth_sessions`, `ITicketStore`, Data Protection، Cookie/CSRF، `security_audit_events` وكتابة Append-only مقيدة | 32-byte random key؛ DB تحفظ Hash فقط؛ تذكرة محمية وغير مقروءة؛ رفض المفقودة والمنتهية/الملغاة؛ Idle وAbsolute؛ خصائص Cookie وCSRF؛ ثبات Key Ring بعد restart وبين النسخ؛ Logout مقابل Renew متزامن لا يعيد الجلسة؛ تحديث ذري عند السباق؛ دور التطبيق يستطيع INSERT/SELECT ولا يستطيع UPDATE/DELETE لسجل الأمن |
| M3.2b — كلمات المرور وLogin | PBKDF2-HMAC-SHA512 عبر PasswordHasher IdentityV3 بعدد 220,000 iteration، مع `IPasswordHashingService` للترقية، التطبيع المعتمد، رسائل موحدة، Rate Limit/lock fields؛ محاولات الدخول الناجحة والفاشلة عبر أساس Audit المنفذ في M3.2a | Salt فريد ورفض Hash malformed/مدخلات مستنزفة؛ رسالة واحدة لحساب مجهول/كلمة خطأ/حساب معطل؛ كل محاولة صحيحة/فاشلة تنتج حدث Audit آمنًا؛ كلمات شائعة مرفوضة؛ ترقية Hash؛ Benchmark محلي؛ 5/IP؛ XFF المزور لا يتجاوز الحد؛ Lock مؤقت بلا قفل دائم؛ القيم المختلفة بالحالة تتطابق في lookup مع حفظ الأصل؛ `+tag` يظل متميزًا عن العنوان من دونه، والنقاط لا تُحذف ولا تدمج عنوانين مختلفين |
| M3.2c — التسجيل وإصدار الرمز | معاملة Person/Account/Customer/Token؛ معرفات دخول موحدة؛ Email abstraction وFake Provider؛ التسجيل دون كلمة مرور أو جلسة | نجاح/تصادم بمعرّفات عامة موحدة؛ Role من الخادم؛ لا تقبل Password/Role/Permission من العميل؛ تصادم قديم يوقف Migration بلا دمج؛ رمز 32-byte وتخزين SHA-256؛ إرسال بعد Commit ورابط fragment؛ فشل الإرسال يحتفظ بالحساب والرمز؛ Endpoint غير متاح في Production؛ حد 10/IP/ساعة فعال قبل الفتح؛ Rate limits داخل الذاكرة ضمن حد نسخة API واحدة |
| M3.2d — تأكيد البريد وكلمة المرور والجلسات | استهلاك الرمز عبر POST؛ اختيار كلمة مرور بعد التحقق؛ إعادة الإرسال/Reset/Password Change/Logout وإبطال الجلسة؛ لا Migration | رمز صالح 24 ساعة أحادي الاستخدام وCSRF؛ لا GET للاستهلاك؛ Login فقط بعد التأكيد وإعداد Hash؛ حد الحساب 60 ثانية و5/24 ساعة وحدود IP؛ الاستجابات العامة؛ تعديل UserRoles يزيد `authorization_version`؛ تغييرات `role_permissions` تزيد `rbac_revision`؛ إبطال الجلسات وزيادة إصدار الحساب ذريًا؛ فشل Audit في العمليات الحساسة fail-closed |
| M3.2e — واجهة المتصفح والتكامل | Next.js pages، same-origin rewrite/proxy، رحلة العميل | E2E CSRF وCookie لا تقرأها JavaScript وSession Fixation؛ anonymous browse مقابل Checkout؛ Customer لا يصل لبيانات/إدارة آخر؛ X-Forwarded-For مزور من مصدر غير موثوق لا يغير IP rate limit؛ Caddy موثوق يعطي IP العميل الصحيح |
| M3.2f — Owner Bootstrap | أداة محلية لمرة واحدة؛ يستعمل أساس Audit المنفذ في M3.2a | Bootstrap وAudit atomically؛ فشل Audit يمنع Owner؛ منع التشغيل الثاني؛ لا أسرار؛ Owner يحصل على 39 صلاحية الكتالوج صراحةً؛ دور التطبيق لا يستطيع UPDATE/DELETE؛ Rollback عند فشل الحدث |
| اختبار Restore/Deployment | ليس شرطًا لبناء M3.2 محليًا؛ يجرى قبل النشر | استعادة DB + Key Ring ومادة فك التشفير؛ إبطال sessions/tokens قبل فتح المرور؛ API ترفض Proxy غير موثوق؛ `ApplicationName` الإنتاج لا يطابق Development؛ اختبار صلاحيات ملفات Ring |

لا تُعتبر اختبارات PostgreSQL أو E2E منفذة إلا بعد تشغيلها فعليًا على قاعدة/بيئة اختبار معزولة. لا تُشغل اختبارات M3.1/RBAC الناجحة مجددًا ما لم يتغير الكود المشترك.

### 3.12 التوصيات الفنية النهائية

- التصميم المفضل هو Server-side Cookie و`ITicketStore` ومفتاح جلسة CSPRNG وتشفير التذكرة بـData Protection.
- تغييرات تعيين الدور ترفع `user_accounts.authorization_version`؛ أي تغيير مشترك على `role_permissions` يرفع `authorization_state.rbac_revision` ذريًا. اختلاف أي منهما يبطل الجلسة والـPrincipal القديم ويطلب Login جديدًا؛ الفشل fail-closed.
- Caddy هو Proxy موثوق فقط بعد ضبط KnownProxies/Networks، وRate Limiter يستخدم `RemoteIpAddress` بعد Forwarded Headers.
- `auth_sessions` يخزن Hash Session Key فقط وتذكرة Data Protection محمية؛ تحديث الخمول والإبطال شرطيان ذريًا بحيث لا يعيد Renew جلسة ملغاة.
- `security_audit_events` وواجهة Append-only الأساسية تسبق Login في M3.2a؛ يجب أن يسجل M3.2b أحداث الدخول منذ أول تنفيذ. ربط Bootstrap بالحدث ضمن معاملة واحدة يظل في M3.2f.
- الخيار الحالي لـV1 هو PBKDF2-HMAC-SHA512 عبر PasswordHasher IdentityV3 بعدد 220,000 iteration. لا تُضاف مكتبة Argon2 أو أي مكتبة تجزئة خارجية الآن؛ يظل `IPasswordHashingService` نقطة الترقية المستقبلية.

### 3.13 قرارات المالك المعتمدة لتصميم M3.2

- Username وEmail يخضعان لتطبيع موحد ومقارنة غير حساسة لحالة الأحرف، مع حفظ القيمة الأصلية للعرض؛ لا تُحذف `+tags` أو نقاط البريد ولا تطبق قواعد خاصة بمزود.
- يستخدم الإرسال Abstraction مستقلة؛ In-Memory Provider للتطوير والاختبارات، وتكامل SMTP عام قابل للتهيئة في Production. اختيار المزود التجاري الحقيقي وأسراره وبيانات المرسل يظل قرارًا تشغيليًا للمالك.
- لا تصدر جلسة Login لحساب غير مؤكد. التسجيل لا ينشئ PasswordHash؛ يختار صاحب البريد كلمة المرور بعد التحقق. Checkout والطلبات والوظائف التي تتطلب بريدًا مؤكدًا تظل ممنوعة حتى نجاح التحقق وتسجيل الدخول.
- صلاحية رمز تأكيد البريد 24 ساعة، وReset 30 دقيقة، ومهلة إعادة الإرسال 60 ثانية لكل حساب؛ الاستخدام مرة واحدة، Hash فقط، وإبطال السابق عند إصدار بديل، مع حدود إرسال للحساب وIP.

### 3.14 تفاصيل تؤجل لمرحلة إعداد النشر

- مكان Key Ring الإنتاجي ومفتاح/شهادة الحماية وصلاحياته وBackup/Restore الفعلي. يظل التصميم ملزمًا بدوام المفتاح ومشاركته وتشفيره؛ اختيار الخدمة/الشهادة عند إعداد التشغيل.
- عناوين/CIDR الفعلية لـCaddy وأي CDN قبله، وشبكة API وForwardLimit لسلسلة البروكسي. الثقة الافتراضية تظل مغلقة حتى تحديد بيئة النشر.
- اختيار مزود البريد وحساب الإرسال وبيانات SMTP والحقول الفعلية `Identity:Email:Smtp:*`؛ لا توجد Credentials في المستودع، وProduction لا يبدأ بمزود ناقص.
- مدة الاحتفاظ والتنظيف لـ`auth_sessions` و`security_audit_events` بعد قرار الخصوصية/التشغيل؛ لا تنظيف تلقائي قبل ذلك.
- أي ترقية مستقبلية إلى Argon2id أو تغيير لمعاملات PBKDF2 تُعامل كتغيير أمني منفصل بعد قياسها على بيئة مماثلة للإنتاج.

تصميم M3.2a-d مكتمل وفق سجله، وM3.2e منفذة أدناه على مستوى المتصفح. لا يشمل النطاق APIs إدارة Roles/Permissions أو M3.3. التسجيل غير متاح في Production حتى استيفاء بوابة الفتح وموافقة المالك.

### 3.15 النسخة النهائية لتصميم M3.2d — 5 نوفمبر 2026

هذه النسخة تثبت النطاق المنفذ. اكتمل كود M3.2d واجتاز اختبارات PostgreSQL المخصصة `14/14`، كما نجحت مجموعة PostgreSQL الكاملة `47/47`.

#### 3.15.1 نطاق الشريحة

يشمل M3.2d:

- تأكيد البريد واختيار كلمة المرور في POST واحد.
- إعادة إرسال رمز تأكيد البريد.
- طلب Password Reset واستهلاكه.
- تغيير كلمة المرور من جلسة مصادق عليها.
- Logout للجلسة الحالية وLogout All للحساب.
- إبطال الجلسات وزيادة `authorization_version` عند تغيير كلمة المرور.
- CSRF وRate Limiting وAudit وحماية التزامن ومنع تعداد الحسابات.
- اختبارات الوحدة والتكامل وPostgreSQL.

لا يشمل M3.2d أي صفحة Next.js أو اختبار متصفح E2E. هذه ضمن M3.2e.

#### 3.15.2 DTOs

| DTO | الحقول | قواعد التحقق |
|---|---|---|
| `EmailVerificationRequest` | `token`, `newPassword` | Token مطلوب؛ كلمة المرور من 15 إلى 128 حرفًا Unicode، محليًا فقط، وليست في قائمة الكلمات الشائعة |
| `EmailVerificationResendRequest` | `email` | بريد مطلوب ويُطبع فقط للبحث؛ لا يُعاد في الاستجابة |
| `PasswordResetRequest` | `email` | بريد مطلوب ويُطبع فقط للبحث؛ لا يُعاد في الاستجابة |
| `PasswordResetCompletionRequest` | `token`, `newPassword` | Token مطلوب؛ نفس سياسة كلمة المرور |
| `PasswordChangeRequest` | `currentPassword`, `newPassword` | كلتاهما مطلوبة؛ التحقق من الحالية قبل تغيير Hash |

لا يرسل العميل User ID أو Account ID أو Role أو Permission أو Customer ID في أي DTO. هوية العميل في مسارات Logout وPassword Change تُستخرج من الـPrincipal المصادق فقط.

#### 3.15.3 الاستجابات

| الحالة | الاستجابة |
|---|---|
| Email Verification ناجح | `204 No Content`؛ لا Cookie مصادقة ولا Session |
| Resend أو Reset Request | `202 Accepted` بنفس الرسالة والجسم دائمًا: `If the account is eligible, instructions will be sent.` |
| Password Reset أو Password Change ناجح | `204 No Content` |
| Logout أو Logout All ناجح | `204 No Content` مع حذف Cookie الحالية حيث ينطبق |
| Token غير صالح أو منتهٍ أو مستخدم أو غرضه خاطئ | `400 ProblemDetails` برسالة عامة واحدة دون أي تفاصيل عن الحساب |
| كلمة مرور لا تحقق السياسة | `400 ValidationProblem` على `newPassword` فقط |
| Rate Limit | `429 Too Many Requests` مع `Retry-After` الصحيح للسياسة |
| فشل قاعدة البيانات أو Audit الحرج | `503 Service Unavailable` عام، مع Rollback |

كل استجابات الهوية ترسل `Cache-Control: no-store` و`Pragma: no-cache`، ولا تحتوي Email أو Account ID أو حالة التحقق أو سببًا يكشف الحساب.

#### 3.15.4 التحقق من البريد وتعيين كلمة المرور

1. يتحقق middleware من CSRF وحد `20/IP/10 minutes`.
2. يطبع Token ويتم فك Base64Url والتأكد من أن طوله 32 بايت.
3. يتحقق مبدئيًا من وجود سجل `identity_tokens` بالغرض `EmailVerification`، وأنه غير مستخدم أو ملغى أو منتهٍ. هذا فحص خفيف قبل PBKDF2، وليس دليل نجاح.
4. تتحقق سياسة كلمة المرور، ثم يحسب PBKDF2 خارج نطاق القفل الطويل.
5. تبدأ معاملة قصيرة:
   - يقفل `user_accounts` أولًا.
   - يقفل سجل الرمز ثانيًا.
   - يعيد كل التحقق: الحساب فعال وغير مؤكد، والرمز ما زال فعالًا وغير منتهٍ وغير مستخدم.
6. يحدث الرمز و`user_accounts` ذريًا:
   - `used_at_utc = now`.
   - `is_email_verified = true`.
   - `password_hash = newHash`.
   - `authorization_version += 1`.
7. يلغي جلسات الحساب الفعالة إن وجدت، ويكتب Audit في نفس المعاملة.
8. Commit، ثم `204` دون `SignInAsync` أو Cookie مصادقة.

إذا فشل فحص كلمة المرور أو Audit فلا يستهلك الرمز. لا يعتمد النجاح على الفحص المبدئي وحده.

#### 3.15.5 إعادة إرسال رمز التأكيد

1. يطبع البريد ويحصل مقدم الطلب على استجابة `202` عامة قبل كشف أي نتيجة.
2. حد IP: `10/hour`.
3. للحساب الموجود والفعّال وغير المؤكد فقط:
   - قفل صف الحساب.
   - رفض الإصدار إذا مر أقل من 60 ثانية من آخر رمز.
   - رفض الإصدار عند بلوغ 5 رموز خلال 24 ساعة.
   - إلغاء الرمز الفعال السابق لنفس الغرض.
   - إنشاء رمز جديد صالح 24 ساعة وتخزين SHA-256 فقط.
4. الرمز وحدث Audit يُحفظان في معاملة واحدة.
5. الإرسال يحدث بعد Commit. فشل المزود لا يلغي الرمز ولا يغيّر الاستجابة العامة، ويُنشئ حدث فشل Audit منفصلًا.

الحساب غير الموجود أو المؤكد أو غير الفعال لا يوقف المسار قبل الحصول على استجابة موحدة ولا يستلم بريدًا.

#### 3.15.6 Password Reset Request

- يقبل `email` فقط ويطبع باستخدام نفس قواعد Username/Email المعتمدة.
- حد IP: `10/hour`.
- حد الحساب المعتمد: 60 ثانية بين الطلبات و5 رموز/24 ساعة لكل حساب.
- يرسل فقط للحساب الفعال والمؤكد.
- يلغي أي رمز Reset فعال سابق وينشئ رمزًا جديدًا صالحًا 30 دقيقة.
- الحساب غير الموجود وغير المؤكد وغير الفعال يستقبل نفس `202` دون بريد.
- تقارب الاستجابة: نفس التحقق والتنظيم وتوليد مرشح الرمز واستعلام البريد وكتابة Audit؛ لا توجد فروع استجابة مبكرة تعتمد على وجود الحساب. لا يضاف تأخير ثابت اعتباطي قبل أن يثبت القياس أنه ضروري.

#### 3.15.7 استهلاك Password Reset

1. CSRF ثم حد `20/IP/10 minutes`.
2. فحص مبدئي للرمز بالغرض `PasswordReset`.
3. تحقق سياسة كلمة المرور وحساب PBKDF2 قبل القفل الطويل.
4. معاملة:
   - قفل `user_accounts` ثم رمز Reset.
   - إعادة التحقق النهائي من الحساب والرمز.
   - استهلاك الرمز وتحديث PasswordHash.
   - `access_failed_count = 0`.
   - `access_failed_window_started_utc = NULL`.
   - `lockout_end_utc = NULL`.
   - `authorization_version += 1`.
   - إلغاء كل `auth_sessions` الفعالة للحساب مع `revoke_reason` ورفع `row_version`.
   - كتابة Audit.
5. Commit ثم `204`. لا تنشأ Session تلقائية من Reset.

أي Token غير صالح أو منتهٍ أو مستخدم أو لغرض آخر يرجع نفس `400` العام. فشل سياسة كلمة المرور أو التحقق النهائي أو Audit لا يستهلك الرمز ولا يغيّر الحساب أو الجلسات.

#### 3.15.8 Password Change

- يتطلب جلسة مصادق عليها وCSRF.
- يتحقق من كلمة المرور الحالية باستخدام `IPasswordHashingService`.
- عند النجاح: تحديث Hash، تصفير حقول الفشل والقفل، زيادة `authorization_version`، وإلغاء كل الجلسات بما فيها الحالية، ثم حذف Cookie الحالية.
- عند فشل كلمة المرور الحالية: لا يتغير الحساب ولا الجلسات.
- لا تُقبل Account ID أو Person ID من الطلب.

#### 3.15.9 Logout

- `DELETE /sessions/current`: يلغي الجلسة الحالية فقط عبر `ITicketStore` و`SignOutAsync`، ويحذف Cookie.
- `DELETE /sessions`: يستخرج Account ID من الـPrincipal، يلغي جميع الجلسات الفعالة للحساب، ثم يمسح Cookie الحالية.
- لا يسمح لأي منهما بإلغاء جلسات حساب آخر.
- تُستخدم الجلسة الملغاة مرفوضة دائمًا حتى لو فشل حذف Cookie من المتصفح.

#### 3.15.10 Rate Limits وTrusted IP

| العملية | حد IP | حد الحساب |
|---|---|---|
| Login | 5/دقيقة | Lockout الحالي للحساب |
| Registration | 10/ساعة | لا يوجد إضافي في M3.2d |
| Email Verification | 20/10 دقائق | Single-use Token |
| Email Verification Resend | 10/ساعة | 60 ثانية و5/24 ساعة |
| Password Reset Request | 10/ساعة | 60 ثانية و5/24 ساعة |
| Password Reset Consumption | 20/10 دقائق | Single-use Token |

ترتيب الـMiddleware الحالي يستخدم `ForwardedHeaders` قبل `RateLimiter`؛ المفتاح هو `HttpContext.Connection.RemoteIpAddress` بعد معالجة Forwarded Headers الموثوقة. لا يُقرأ `X-Forwarded-For` مباشرة داخل الـPolicy. في V1 يعمل الـlimiter داخل API instance واحدة.

#### 3.15.11 المعاملات وترتيب الأقفال

- ترتيب ثابت: `user_accounts` أولًا، ثم `identity_tokens`، ثم `auth_sessions`.
- البحث الأولي عن Token/Account يكون بلا قفل لتحديد الهدف، ثم يعاد التحقق بعد القفل.
- لا يُحسب PBKDF2 أثناء حجز أقفال الصفوف.
- استهلاك Token وإبطال Sessions وتحديث Account و`authorization_version` وAudit في المعاملة نفسها.
- أي RowsAffected = 0 في التحقق النهائي يعني سباقًا ويُرجع response عامًا، ولا يعيد المحاولة بصمت.
- Email يُرسل بعد Commit فقط.

#### 3.15.12 Audit

تكتب أحداث الأمان دون Email أو Password أو Hash أو Token أو رابط كامل:

- `Identity.EmailVerification.Succeeded`
- `Identity.EmailVerification.Rejected`
- `Identity.EmailVerification.ResendRequested`
- `Identity.EmailVerification.DeliveryFailed`
- `Identity.PasswordReset.Requested`
- `Identity.PasswordReset.Succeeded`
- `Identity.PasswordReset.Rejected`
- `Identity.Password.Changed`
- `Identity.Session.LoggedOut`
- `Identity.Session.LoggedOutAll`

يُفضل أن يستخدم الصف الأمني الحرج نفس Connection وTransaction عبر واجهة كتابة محدودة في Auditing، دون إنشاء Outbox أو Abstraction عام. تُستخدم `SafeMetadata` للسبب المختصر فقط، و`CorrelationId` من `HttpContext.TraceIdentifier`. إذا لم تنجح كتابة Audit في عملية تغيّر كلمة المرور أو تستهلك Token، تُلغى المعاملة ولا تُعاد استجابة نجاح.

#### 3.15.13 Migration

**لا توجد Migration مطلوبة لـM3.2d.** الجداول الحالية `identity_tokens` و`auth_sessions` تحتوي على الحقول والفهارس اللازمة، و`authorization_version` موجود. لا يُضاف `password_changed_at_utc` لأن Audit والحقول الحالية تكفي، ولا توجد قاعدة عمل تحتاج هذا الحقل. أي حاجة مستقبلية له تحتاج Migration منفصلة ومبررًا.

#### 3.15.14 الاختبارات المطلوبة

**Unit:**

- توليد Token من 32 بايت عشوائي وتخزين SHA-256 فقط.
- صلاحية التحقق 24 ساعة، وReset 30 دقيقة، والحد عند `expires_at_utc == now`.
- رفض Purpose الخاطئ وToken malformed والمستخدم والملغى.
- سياسة كلمة المرور: 15 و128 والكلمات الشائعة، وعدم تعديل كلمة المرور عند الفشل.
- ثبات الاستجابات العامة وإنتاج روابط Fragment صحيحة.

**Integration وAPI:**

- Verification صالح يؤكد الحساب ويضع Hash ويستهلك Token دون Cookie أو Session.
- Verification غير صالح أو منتهٍ أو مستخدم لا يغيّر أي حالة.
- Resend يلغي السابق، ويطبق 60 ثانية و5/24 ساعة.
- Resend وReset Request لا يكشفان وجود الحساب أو حالة التحقق.
- Reset صالح يغيّر Hash ويعيد عدادات الفشل والقفل إلى الصفر.
- Reset وPassword Change يزيدان `authorization_version` ويلغيان كل الجلسات.
- Old Session مرفوضة عند الطلب التالي، والكلمة القديمة لا تعود تعمل.
- Logout Current لا يلغي جلسة أخرى، وLogout All يلغي كل جلسات الحساب.
- CSRF مطلوب على كل POST/DELETE، والـIP Limit يعمل بعد Forwarded Headers الموثوقة.
- Audit لا يحتوي Email أو Password أو Hash أو Token أو Link.

**PostgreSQL/Testcontainers، منفذة ضمن التصريح الحالي:**

- طلبان متزامنان لاستهلاك Token نفسه: واحد فقط ينجح.
- طلبات Resend متزامنة لا تترك أكثر من Token فعّال.
- تحديث الحساب وToken وSessions وAudit إما ينجح كاملًا أو يتراجع.
- فشل Audit المحقون لا يترك Password أو Token أو Sessions في حالة جزئية.
- فشل Email بعد Commit لا يلغي Token أو الحساب.
- الحساب غير الفعال ومنتهي الصلاحية والحدود الزمنية تُرفض عند PostgreSQL.

#### 3.15.15 معايير قبول M3.2d

- Build ناجح عند التنفيذ، مع معالجة أو توثيق كل تحذير.
- نجاح اختبارات Unit وAPI التكاملية المخططة أعلاه.
- نجاح اختبارات PostgreSQL المخصصة على Testcontainers: `14/14`، ونجاح مجموعة PostgreSQL الكاملة `47/47`.
- تطبيق Migration M3.2c على قاعدة اختبار مؤقتة ناجح، ولا توجد Migration جديدة لـM3.2d.
- لا يوجد أي مسار ينشئ Session قبل اكتمال التأكيد واختيار كلمة المرور.
- فشل التزامن لا يسمح باستهلاك الرمز مرتين أو بإعادة جلسة ملغاة.
- Audit والاستجابات العامة والـCSRF والـRate Limits تستوفي التصميم.
- التسجيل يظل مغلقًا في Production، ولا يبدأ M3.2e ضمن هذه الشريحة.

#### 3.15.16 M3.2e المنفذ في هذه الجولة

- Next.js pages لLogin/Register/Verify/Forgot/Reset.
- استخراج Fragment، وعرض النماذج، والحالة البصرية، وAccessibility وRTL.
- Same-origin browser integration وPlaywright journeys.
- مزود البريد العام وتكوين Base URLs وإعداد SMTP من environment؛ اختيار مزود نهائي وأسراره ينتظر المالك.
- Caddy وتجهيز الإنتاج وفتح Registration العام ما زالت خارج التنفيذ المكتمل وتنتظر اعتمادًا وإعدادًا تشغيليًا.

### 3.16 حالة تنفيذ M3.2d — 5 نوفمبر 2026

- نُفذت خدمات دورة حياة الحساب وإبطال الجلسات وناقل بريد مؤجل داخل الذاكرة العملية، وربطت مسارات M3.2d في Development/Testing فقط. لا يوجد Outbox دائم؛ أي مزود إنتاجي يحتاج معالجة الاستمرارية قبل الإطلاق.
- يستخدم Audit المشترك Connection وTransaction نفسه في تأكيد البريد وReset Password وPassword Change وإلغاء الجلسات وتجديد الرموز.
- لا توجد Migration جديدة؛ بقي مخطط M3.2c كما هو.
- `dotnet build HARDWARE_STORE.slnx --no-restore -m:1 -t:Rebuild` نجح: 0 أخطاء، مع تحذير `xUnit2031` القائم في اختبار RBAC. ظهر `NU1900` في Builds جزئية سابقة بسبب تعذر بيانات فحص NuGet، ولم يظهر في Rebuild النهائي.
- الاختبارات المحلية غير الموسومة `RequiresDocker` نجحت `30/30` في جولة M3.2e، شاملة الإعداد الإنتاجي Fail-closed لمزود SMTP.
- حالة PostgreSQL lifecycle لبريد/استعادة/جلسات نجحت `14/14`، ومجموعة PostgreSQL/Testcontainers الكاملة `47/47`. لا Migration جديدة.
- M3.2d مكتملة تقنيًا. التسجيل يظل مغلقًا في Production لحين استكمال تجهيز النشر واعتماد المالك.

### 3.17 حالة تنفيذ M3.2e — 5 نوفمبر 2026

- الصفحات المطلوبة منفذة في `apps/web` وتستخدم عميل API same-origin مع Cookies وCSRF، وSession Provider، و`GET /identity/me`.
- رمز التحقق والاستعادة يقرأ من Fragment ثم يزال من شريط العنوان قبل POST؛ الرسائل العامة وحالات الرمز غير الصالح وحالة الجلسة المنتهية معروضة للمستخدم.
- Playwright نجح `2/2` على API وقاعدة مؤقتة حقيقية، وغطى إعادة استخدام الرمز، إبطال جلسة ثانية بعد Password Change، Logout Current/All، Reset، وLogin بكلمة المرور الجديدة. Vitest نجح `6/6`، وNext production build نجح.
- لا Caddy أو Production same-origin rewrite منفذ. التسجيل لا يُفتح في Production قبل إعداد مزود SMTP وProxy الموثوق، واختبار الحدود في بيئة النشر، وموافقة المالك.

**مراجع تقنية:** [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)؛ [Microsoft `ITicketStore`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.cookies.iticketstore?view=aspnetcore-10.0)؛ [Microsoft Data Protection key storage](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0)؛ [Microsoft password hashing guidance](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-10.0)؛ [Microsoft Identity configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0)؛ [Microsoft Forwarded Headers security](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/security-considerations)؛ [Caddy `reverse_proxy`](https://caddyserver.com/docs/caddyfile/directives/reverse_proxy).

---

## 4. التحقق، التفويض، وحماية ملكية السجلات (Validation & Authorization)

1. **التحقق من المدخلات (FluentValidation):**
   - كل Command أو Request DTO له كلاس `AbstractValidator<T>` مستقل.
   - يتم التحقق تلقائياً قبل وصول الطلب إلى Handler عبر الـ Pipeline Behavior في MediatR.
2. **التفويض المعتمد على الصلاحيات (Permission-Based Authorization):**
   - استخدام Handler الحالي يفحص Permission claims في هوية المصادقة الموثوقة؛ مع Cookie Sessions تحفظ التذكرة في `ITicketStore` ولا يرسل المتصفح Permission claims قابلة للتحرير:
     ```csharp
     [Authorize]
     [HasPermission("Inventory.StockAdjust")]
     [HttpPost("api/v1/inventory/adjustments")]
     public async Task<IActionResult> AdjustStock([FromBody] AdjustStockCommand command) { ... }
     ```
3. **حماية ملكية السجلات ومنع ثغرات BOLA / IDOR:**
   - استخدام الـ `UUID` كمعرف للسجلات ليس بديلاً عن التفويض والتحقق من الملكية.
   - في واجهات المتجر العامة والعملاء (`/api/v1/storefront/...` و `/api/v1/customer/...`)، يجب أن تفرض استعلامات قاعدة البيانات التحقق الصارم من ملكية السجل:
     ```sql
     -- جلب الطلب مشروط بملكية العميل الموثق
     SELECT * FROM orders 
     WHERE id = @orderId AND customer_person_id = @currentAuthenticatedPersonId;
     ```
   - في حال محاولة الوصول لمعرف `orderId` يخص عميلاً آخر، ترجع الواجهة `404 Not Found` (أو `403 Forbidden`) لمنع اكتشاف وجود المعرفات.
   - لا يُعفى من هذا الفحص إلا موظفو لوحة الإدارة الذين يملكون صراحةً صلاحية `Sales.ViewAllOrders` أو `Support.ManageTickets`.

---

## 5. سياسات الحماية وموثوقية المعاملات (Security & Reliability Policies)

1. **دعم تكرار المعاملات الآمن (Idempotency Key):**
   - تدعم الواجهة البرمجية في عمليات إنشاء الطلبات، حجز المخزون، وتأكيد السداد والـ Webhooks رأس HTTP مخصص:
     `Idempotency-Key: <UUIDv4>`
   - يقوم وسيط برمجي (Idempotency Middleware) بفحص المفتاح في قاعدة البيانات أو الذاكرة المؤقتة (Distributed Cache):
     - إذا كان الطلب جديداً، ينفذ العملية ويخزن النتيجة المقترنة بالمفتاح لمدة 24 ساعة.
     - إذا تكرر نفس المفتاح لنفس العميل، يُرجع الخادم الاستجابة المخزنة السابقة مباشرة مع كود `200 OK` دون تكرار خصم الرصيد أو حجز المخزون أو توليد طلبات مكررة.
2. **تقييد معدل الطلبات (Rate Limiting):**
   - استخدام الميزة المدمجة في .NET 10 (`Microsoft.AspNetCore.RateLimiting`):
 - Login: 5 محاولات/IP/دقيقة. التسجيل: 10 طلبات/IP/ساعة، ولا يفتح في Production حتى اكتمال M3.2d وM3.2e، ونجاح اختبارات التدفق المتكامل، وموافقة المالك الصريحة، وتفعيل حد التسجيل قبل الفتح. إعادة إرسال البريد: 10/IP/ساعة مع حد حساب مرة كل 60 ثانية وبحد أقصى 5/24 ساعة؛ تأكيد الرمز: 20/IP/10 دقائق؛ طلب Reset: 10/IP/ساعة مع حد حساب مستقل؛ استهلاك Reset: 20/IP/10 دقائق. Rate Limiter داخل الذاكرة لا يوزع الحد بين نسخ API؛ V1 يعمل على نسخة واحدة.
     - نقاط إنشاء الطلبات وحجز المخزون (Checkout & Stock Reservation): حد أقصى 5 طلبات لكل دقيقة لكل مستخدم أو IP لمنع هجمات حرمان المخزون (Denial-of-Inventory / Hoarding).
     - نقاط الـ Storefront العامة (تصفح، بحث): 100 طلب لكل دقيقة لكل IP.
     - نقاط لوحة الإدارة: 300 طلب لكل دقيقة لكل مستخدم موثق.
3. **رؤوس الأمان (Security Headers):**
   - تفعيل `Content-Security-Policy (CSP)`.
   - `X-Content-Type-Options: nosniff`.
   - `X-Frame-Options: DENY` (لمنع هجمات Clickjacking).
   - `Strict-Transport-Security (HSTS)` على الإنتاج.
4. **أمان رفع الملفات والصور (File Upload Security):**
   - فحص الامتداد ونوع الـ MIME الصريح، وفحص البايتات السحرية (Magic Bytes) للملف لمنع رفع ملفات تنفيذية متنكرة كصور.
   - إعادة تشفير وضغط الصور على الخادم وتحويلها إلى امتداد `WebP` بمقاسات محددة مع مسح أي بيانات تعريفية غير ضرورية (EXIF Metadata).
