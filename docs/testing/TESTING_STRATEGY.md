# استراتيجية الاختبارات وضمان الجودة (Testing Strategy)

تاريخ التحديث: نوفمبر 2026  
الحالة: **استراتيجية حالية؛ M3.2d/M3.2e مكتملتان، وM4 Catalog & Media مكتملة على Local Preview. Vitest وPlaywright والتحقق المباشر من Backend/PostgreSQL Preview نُفذت؛ تبقى اختبارات تزامن Media Cleanup وبوابات Production.**  

> [!IMPORTANT]
> **طبيعة الوثيقة وحدود المرحلة الحالية:**  
> يحدد هذا المستند استراتيجية الاختبارات والسيناريوهات المستهدفة. سُجلت اختبارات Foundation وM3.1-M3.2e في [PROGRESS.md](../../PROGRESS.md). في RIGX V2 نجح Next production build وTypeScript، وVitest `17/17`، وPlaywright للأسطح المتأثرة `11/11`: Foundation `2/2`، SEO `2/2`، Storefront `3/3`، Comparison `3/3`، وAdmin Catalog/Media/Publication `1/1`. Backend non-Docker `51/51` وCatalog PostgreSQL `4/4` محفوظان من بوابة M4. Customer Identity `1/1` مسجل سابقًا ولم يُعد لأن منطق الهوية لم يتغير.

> **RIGX V2:** أضيفت اختبارات Storefront helpers للـShare/WhatsApp، Comparison selection/limits/category/storage، Carousel keyboard، Product SEO raw HTML، Robots/Sitemap، Admin taxonomy Drawer/CRUD، وComparison E2E على 390/1440. التفاصيل الفعلية والنتائج النهائية في [PROGRESS.md](../../PROGRESS.md).

---

## 1. هرم الاختبارات والنهج المتبع (Testing Pyramid)

يعتمد المشروع على هرم اختبارات عملي ومتوازن للتحقق من سلامة قواعد العمل والمعاملات المالية وحركات المخزون:

```
          / \
         / E2E \       <-- Playwright (Critical Journeys: Registration, Orders, POS, Idempotency)
        /-------\
       / Integr. \     <-- WebApplicationFactory + Testcontainers (PostgreSQL Concurrency)
      /-----------\
     / Architecture\   <-- NetArchTest (عزل حدود الموديولات ومنع التسريب المعماري)
    /---------------\
   /   Unit Tests    \ <-- xUnit + FluentAssertions (Domain Logic, Formulas, Quarantine, Snapshots)
  /-------------------\
```

---

## 2. اختبارات الوحدة وقواعد النطاق (Unit Testing)

* **النطاق ومجموعات الاختبار الأساسية:**
  1. **قواعد توافق مجمّع الحواسيب (V1 Compatibility):**
     - اختبار تطابق مقبس المعالج واللوحة الأم (`Socket Match`).
     - اختبار تطابق جيل الرام (`DDR4` vs `DDR5`) وتجاوز عدد المنافذ المتاحة.
     - اختبار ظهور *"التوافق غير مؤكد"* عند غياب المواصفات.
     - تأكيد خلو اختبارات V1 من قيود أبعاد الكيس وارتفاع المبرد والمقاسات الفيزيائية.
  2. **قواعد المخزون والسيريالات والحجر:**
     - اختبار حتمية منع الرصيد السالب (`AvailableQuantity >= 0`).
     - **اختبار دورة حياة السيريال المعيارية:** اختبار انتقالات الحالات المشروعة حصراً (`InStock` -> `Reserved` -> `InTransit` -> `Sold` -> `InQuarantine` -> `ReturnedToSupplier` -> `Scrapped`) ورفض أي انتقال غير مشروع.
     - **اختبار الفصل بين السيريال والخصائص المادية:** اختبار استقلال حالة السيريال (`status`) عن حالة القطعة (`condition: NEW, OPEN_BOX, USED`) وقرارات الفحص (`inspection_disposition`).
     - اختبار كون السيريال اختيارياً حسب إعداد كارت الصنف (`RequiresSerialNumber`).
     - **اختبار التسوية الفورية للحجوزات (Inline Expiration):** التحقق من تحرير الحجز المنتهي وإعادة كمياته وسيرياله للرصيد المتاح آنياً عند تنفيذ استعلام الفحص (`SELECT ... FOR UPDATE`) قبل تقييم الكفاية لمنع الرفض الزائف.
     - **اختبار حماية الحجز أثناء التجهيز والشحن (FulfillmentHold Protection Test):** التحقق من أن تحول الطلب إلى `InFulfillment` ينقل الحجز إلى `FulfillmentHold`، واختبار أن المجدول الخلفي والتسوية الفورية يتجاهلان تماماً حجوزات `FulfillmentHold` حتى مع انقضاء أي موعد زمني، لمنع البيع المزدوج أثناء وجود البضاعة مع شركة الشحن.
     - **اختبار أرصدة الدفعات في المستودعات (`inventory_batch_balances`):** التحقق من توزيع الدفعة الواحدة على أكثر من مستودع، واختبار مطابقة رصيد المستودع لمجموع دفعاته، ومطابقة رصيد الدفعة التجميعي في `inventory_batches` لمجموع أرصدتها بالفروع.
     - **اختبار دورة الحجر الفني للمرتجعات:** التحقق من دخول الصنف المرتجع تلقائياً في حالة الحجر (`InQuarantine`)، ومنع انتقاله للمخزون الصالح للبيع إلا بعد تقرير فحص فني معتمد في `return_inspections`.
     - **المرتجع الجزئي للباقات (`Q3.2`):** لا يثبت الاختبار التوزيع التناسبي أو أي نتيجة مالية قبل قرار المالك. بعد حسم `Q3.2`، يُضبط الاختبار على الخيار المعتمد وحده (التوزيع التناسبي، أو إعادة احتساب الخصم بالكامل على المتبقي، أو منع الإرجاع الجزئي)، ويتحقق من مبلغ إشعار الدائن والتكلفة الناتجين وفق ذلك القرار. تظل النتائج المتوقعة لهذه الحالة معلقة حتى الاعتماد.
  3. **نظام الفواتير والحسابات المالية واللقطات (Snapshots):**
     - اختبار عدم تكرار أرقام الفواتير الصادرة نهائياً.
     - **اختبار تجميد بنود الطلب والفاتورة:** التحقق من حفظ سعر البيع للجمهور لحظة الطلب، ومنع تخزين تكلفة الشراء التقديرية داخل `order_items` (تسجيل COGS يقتصر على الصرف الفعلي).
     - اختبار حفظ النسخة الثابتة الكاملة (`InvoiceSnapshot`) وتضمين `template_version` و `rendered_pdf_storage_key` ومصاريف الشحن الصريحة.
     - اختبار معادلة الفاتورة الإلزامية: `Total = Subtotal - Discount + Tax + Shipping`.
     - اختبار منع تعديل الإجماليات أو الضرائب يدوياً بعيداً عن بنود الفاتورة.
     - اختبار التحقق اليدوي من سداد InstaPay أو توقيع Webhook البوابة قبل تحويل الطلب لمدفوع.

---

## 3. اختبارات التكامل والتزامن (Integration & Concurrency Testing)

* **الأداة:** `WebApplicationFactory<Program>` مع `Testcontainers.PostgreSql`.
* **سيناريوهات الاختبار الحرجة والتزامنية المخططة (Planned Testing Scenarios):**
  1. **التزامن والتسابق على آخر قطعة مخزون (Last-Piece Concurrency Race):**
     - إطلاق 10 طلبات متزامنة لحجز صنف لا يتوفر منه سوى قطعة واحدة فقط (`AvailableQuantity = 1`).
     - **النتيجة المتوقعة:** ينجح طلب واحد فقط في حجز القطعة، وتفشل الطلبات التسعة الأخرى برسالة نفاد المخزون، مع بقاء الرصيد المتاح 0 دون أي رصيد سالب أو حجز مزدوج.
  2. **التبديل الذري لمكونات التجميعة (Atomic Component Swapping):**
     - تنفيذ عملية تبديل قطعة بأخرى داخل جهاز مجمع قيد التجهيز داخل معاملة قاعدة بيانات ذرية واحدة.
     - التحقق من عودة القطعة القديمة إلى المخزن بحالتها وسيريالها، وسحب القطعة الجديدة، وتحديث فرق السعر وهامش الربح، وتسجيل سبب التبديل في سجل التدقيق غير القابل للتعديل.
     - محاكاة حدوث خطأ أثناء السحب والتحقق من ارتداد المعاملة بالكامل (Rollback) وعدم ضياع بيانات أي من القطعتين.
  3. **اختبار تكرار الطلبات الآمن (Idempotency Key Testing):**
     - إرسال طلب إنشاء عملية أو سداد برأس `Idempotency-Key` مكرر مرتين.
     - التحقق من تنفيذ العملية مرة واحدة فقط، وأن الطلب الثاني يُرجع نفس الاستجابة المخزنة السابقة مع كود `200 OK` دون تكرار الحجز أو تكرار الخصم المالي.
  4. **اختبارات حماية الصلاحيات و BOLA / IDOR:**
     - محاولة استعلام العميل (أ) عن تفاصيل طلب أو فاتورة تخص العميل (ب) عبر الـ UUID.
     - التحقق من إرجاع كود `404 Not Found` أو `403 Forbidden` وفشل محاولة الوصول غير المصرح بها.
  5. **اختبار الاستبعاد المتبادل لمسارات التسليم (Fulfillment Mutual Exclusivity Test):**
     - محاولة إدراج سجل في `order_deliveries` لطلب مسجل كـ `StorePickup`، أو إدراج سجل في `store_pickups` لطلب مسجل كـ `Delivery`.
     - **النتيجة المتوقعة:** رفض العملية فوراً على مستوى محرك PostgreSQL بكسر القيد الخارجي المركب (`(order_id, fulfillment_type)`) وتريجر الاستبعاد المتبادل، مما يمنع نهائياً تداخل مساري التسليم.
  6. **اختبار دورة أوامر التحويل والاستلام المتكرر والجزئي (Stock Transfer & Multi-Receipt Idempotency):**
     - إنشاء أمر تحويل `stock_transfers` وصرف الشحنة عبر `TransferOut` -> التأكد من خصم الرصيد الفعلي ورصيد الدفعة `inventory_batch_balances.remaining_quantity` للمصدر، وتسجيل السيريالات في `stock_transfer_item_serials` وتحول حالتها المعيارية في `serial_numbers.status` إلى `InTransit` مع تفريغ `warehouse_id = NULL`.
     - اختبار تعدد أحداث الاستلام (`stock_transfer_receipts`): تسجيل استلام دفعة أولى جزئية (3 وحدات مقبولة) -> التأكد من نجاح حركة `TransferIn` المرتبطة بـ `stock_transfer_receipt_items.id`.
      - إعادة إرسال نفس أمر الاستلام بنفس `Idempotency-Key` بعد فقد الاستجابة -> يعيد Backend النتيجة المسجلة في `Command Inbox/Idempotency Store` ولا يكرر الحركة؛ اختبار API لا يفترض أن UNIQUE polymorphic الحالي يحقق ذلك.
      - تسجيل حدث استلام ثانٍ مستقل للشحنة المتبقية (وحدة مقبولة ووحدة تالفة للحجر) -> التأكد من قبول الحركة الصحيحة الثانية لنفس بند التحويل بهوية حدث مستقلة.
      - توزيع الكمية المقبولة من بند إيصال واحد على أكثر من دفعة: لا يرفض التصميم الحركة الصحيحة بسبب UNIQUE على بند الإيصال؛ اختبار ربط كل كمية بهوية allocation/حدث ثابتة ينتظر اعتماد التصميم.
      - صرف نفس بند التحويل على شحنتين: لا يُختبر كحالة مسموحة أو مرفوضة قبل حسم `Q2.5`؛ بعد القرار يضاف اختبار الكمية التراكمية ومنع تكرار نفس حدث الصرف.
     - عند `requires_serial = true`، التحقق من أن عدد سيريالات `Restocked` يساوي `accepted_quantity` وعدد سيريالات `Quarantined` يساوي `rejected_damaged_quantity`، وأن أنواعها لا تختلط؛ هذا تحقق تطبيقي داخل نفس المعاملة لأن العدد تجميعي عبر الصفوف.
     - التحقق من `uq_receipt_transfer_item_serial` وFK المركب `(transfer_item_serial_id, serial_number_id)`: إعادة استلام التخصيص نفسه أو تمرير سيريال يختلف عن سيريال الصرف -> رفض PostgreSQL.
     - اختبار تنافسي لاتصالين يخصصان السيريال نفسه لتحويلين: قفل `SELECT ... FOR UPDATE` والمعاملة يحميان انتقال الحالة، والفهرس الجزئي `uq_active_serial_transfer` حاجز الذرية؛ يفشل تخصيص واحد فقط.
  7. **اختبار انتقالات آلة الحالات المعيارية (Canonical State Machine Transitions):**
     - اختبار سلامة الانتقالات المسموحة لـ Order Status و Payment Status و Delivery Status و Pickup Status و Stock Transfer Status وفق القوائم المعيارية المعتمدة (بما فيها `Completed` و `ClosedWithDiscrepancy`).
     - التحقق من رفض أي انتقال قفزي غير مصرح به مع إرجاع كود الخطأ المناسب.
      - لمسار Delivery في V1: الموظف المخول يحدث يدويًا `Processing`, `ReadyForDispatch`, `Dispatched`, `Delivered`، وحالات `DeliveryFailed`/`ReturnedToStore` عند الحاجة؛ يسجل كل تغيير `changed_by_person_id` و`changed_at_utc` في سجل الحالة.
      - التحقق من قبول رقم التتبع ورابطه اختياريًا، وعدم اشتراط تكامل API أو Webhook مع شركة الشحن؛ ورفض التحديث من مستخدم غير مخول. لا يغيّر التحديث اليدوي تلقائيًا توقيت `SalesDispatch` أو الفاتورة قبل حسم `Q7.2`.
      - التحقق من استقلال سجل Delivery عن `store_pickups` وعن `stock_transfers`.
      - استلام مرتجع طلب واحد في مستودع واحد أو أكثر يظل اختبارًا مؤجلًا حتى حسم `Q3.3`؛ بعدها تُختبر مطابقة كل بند/كمية/سيريال مع موقعه من دون افتراض سياسة قبل القرار.
  8. **اختبار سلامة عهدة وموقع السيريال عبر كامل دورة حياته (Serial Custody Lifecycle Invariant Test):**
     - فحص قيد قاعدة البيانات `chk_serial_custody_by_status`:
       - التحقق من صحة حالة `InStock` و `Reserved` (وجود `warehouse_id` و `stock_item_id`).
       - التحقق من صحة حالة `InTransit` (تفريغ `warehouse_id` و `stock_item_id`).
       - التحقق من صحة حالة `InQuarantine` (وجود `warehouse_id` وتفريغ `stock_item_id`).
       - التحقق من صحة حالة `Sold` (تفريغ `warehouse_id` و `stock_item_id` ووجود `invoice_id`).
       - التحقق من صحة حالتي `ReturnedToSupplier` و `Scrapped` (تفريغ كامل للمستودعات والفواتير).
       - محاولة إدخال حالة غير متسقة (مثال: سيريال `Sold` مع تحديد `warehouse_id`) -> التحقق من رفض PostgreSQL فوراً.
  9. **اختبار سلامة أرصدة الدفعات وتفادي ثغرة PostgreSQL `MATCH SIMPLE` (Batch Balances Composite Integrity Test):**
     - التحقق من قيود المفاتيح الأجنبية بأعمدة NOT NULL المركبة (`fk_batch_balance_stock_warehouse`, `fk_batch_balance_stock_product`, `fk_batch_balance_batch_product`): محاولة ربط رصيد دفعة بمستودع (ب) مع الإشارة لـ `stock_item` يخص مستودع (أ) -> رفض فوري لكسر المفتاح الأجنبي المركب.
     - محاولة ربط رصيد دفعة لصنف مختلف عن منتج الدفعة الأصلية -> رفض فوري.
     - اختبار التريجر الدفاعي `trg_batch_balance_variant_integrity`: محاولة ربط دفعة ذات variant_id بصنف ذي variant_id مختلف (أو أحدهما NULL والآخر UUID) -> التحقق من إطلاق استثناء ورفض العملية.
     - التحقق من منع الرصيد السالب `chk_batch_balance_positive`: محاولة صرف كمية أكبر من المتاح في `remaining_quantity` أو `quarantine_quantity` -> رفض العملية.
     - التحقق من القفل الحصري `SELECT ... FOR UPDATE` أثناء حركات الصرف المتزامنة لمنع الصرف المزدوج.
  10. **اختبار تسوية العجز مع بقاء Q2.4 معلقة:**
       - قبل اعتماد المالك للسياسة، اختبر فقط حظر الإغلاق عند `D-A-R-L > 0`، ورفض إدخال تسوية غير معتمدة ضمن `L`، وذرية تحديث التسوية والحركة والـ rollup.
       - لا تختبر مبلغاً أو دوراً بعينه ولا تتوقع صلاحية تجارية؛ تضاف اختبارات مصفوفة الصلاحيات بعد توثيق قرار Q2.4.
  11. **اختبار حماية السجلات التاريخية وحظر الحذف التعاقبي (Historical Audit Protection Test):**
      - اختبار رفض الحذف بواسطة PostgreSQL لكل FK فعلي إلى التحويلات/الاستلامات/السيريالات/التسويات والمراجع المخزنية بعد وجوده.
      - لاختبار `reference_document_id/reference_item_id` polymorphic، يلزم أولاً اعتماد وتنفيذ FK typed أو Trigger حذف لكل نوع؛ لا يصح اعتبار هذا السيناريو ناجحاً على مستوى PostgreSQL قبل ذلك، ومصدر `StockCountAdjustment` ما زال غير محسوم توثيقياً.

### 3.1 ربط سيناريوهات المخزون بضمان محدد

| السيناريو | الضمان الذي يختبره | جهة الفرض |
|---|---|---|
| 1. سباق آخر قطعة | قفل صف الرصيد وتحديث شرطي داخل معاملة؛ `CHECK` يمنع السالب كخط دفاع | التطبيق/المعاملة + PostgreSQL |
| 2. تبديل التجميعة | معاملة ذرية وسجل حركات غير قابل للحذف؛ قواعد التجميعة في التطبيق | التطبيق/المعاملة + FK/RESTRICT |
| 3. Idempotency | هوية حدث ثابتة مخزنة، وإعادة نتيجة الأمر من Command Inbox عند المفتاح نفسه، وآثار العملية في معاملة واحدة؛ تصميم القيد النهائي لم يعتمد | التطبيق/المعاملة + DB بعد اعتماد التصميم |
| 4. BOLA/IDOR | تفويض نطاق السجل وملكيته قبل الإرجاع | تحقق التطبيق؛ لا يكفي FK |
| 5. مسار التسليم | FK/`CHECK` لنوع fulfillment كما هو موثق، وتحديث حالة يدوي مع سجل موظف/وقت؛ لا تكامل ناقل خارجي | PostgreSQL + التطبيق/المعاملة |
| 6. التحويلات والاستلام | هوية حدث ثابتة؛ علاقات FK typed المقترحة لم تعتمد بعد؛ قفل ومعاملة للكميات وحالة السيريال | التطبيق/المعاملة + DB بعد اعتماد التصميم |
| 7. آلة الحالات | `CHECK` لقيم الحالة، والتحقق من الحالة السابقة/التالية في خدمة المجال | PostgreSQL + التطبيق/المعاملة |
| 8. عهدة السيريال | `chk_serial_custody_by_status` وFKs؛ انتقال الحالة والموقع داخل معاملة مقفولة | PostgreSQL + التطبيق |
| 9. أرصدة الدفعات | FKs مركبة بأعمدة غير NULL، `trg_batch_balance_variant_integrity`، `chk_batch_balance_positive` وقفل الرصيد | PostgreSQL + التطبيق |
| 10. تسوية الفروق | فحص `D-A-R-L` بعد قفل بند التحويل؛ لا مبلغ أو دور قبل حسم Q2.4 | PostgreSQL + التطبيق؛ القرار معلق |
| 11. حماية التاريخ | `ON DELETE RESTRICT` للعلاقات ذات FK فعلي؛ مراجع الحركة polymorphic معلقة حتى اعتماد قيد typed/Trigger ومصدر التسوية | PostgreSQL؛ فجوة تصميم مفتوحة |

> يلزم تثبيت أسماء القيود والفهارس وTriggers في migrations عند التنفيذ. المجاميع عبر عدة صفوف مثل `A+R+L <= D` لا يفرضها `CHECK` عادي؛ تنفذ ذريًا بقفل بند التحويل ومعاملة أو Trigger مضبوط، ثم تختبر ضد PostgreSQL. جميع الاختبارات هنا مستقبلية ولم تُشغّل في هذه الجولة.
## 4. اختبارات واجهات المستخدم والمسار الشامل (E2E Testing)

* **الأداة:** `Playwright`.
* **المنفذ حاليًا في M3.2e:** مسار عميل كامل باستخدام API وتطبيق حقيقيين: Register → Verify Email/Set Password → Reject Token Reuse → Reject Wrong Password → Login → Password Change → Reject Stale Second Session → Logout Current → Forgot/Reset → Login with New Password → Logout All. نجح `2/2`; يستخدم اختبار التكامل قاعدة PostgreSQL مؤقتة لاستخراج الرمز المحلي، ولا يحاكي واجهة Backend.
* **M4 المنفذة:** Storefront E2E يغطي Home ببيانات فعلية، Carousel Buttons/Keyboard في RTL/LTR، البحث بالاسم/SKU، Category/Brand/Min-Max، الخيارات الستة للترتيب، Specification Facets، Pagination مع بقاء الفلتر، Variant Price/SKU/Specs، إخفاء Draft، تبديل AR/EN وLight/Dark مع الاستمرار بعد Reload، وLogin POST دون Credentials في URL. Admin E2E يغطي إنشاء Category/Brand/Product، Variant/Specification، Draft Preview، رفع صورة ومعالجتها، النشر، وظهور المنتج المنشور. Customer Identity E2E يغطي التسجيل والتأكيد وإعادة الاستخدام وكلمة المرور الخاطئة وLogin وPassword Change وإبطال الجلسة الثانية وLogout Current/Reset/Logout All. Visual QA يغطي `360/390/430/768/1024/1280/1440/1920` وLight/Dark وAR/EN وReduced Motion، ولم يجد Horizontal Overflow. يظل اختبار PostgreSQL المتزامن لعملية Media Cleanup مطلوبًا قبل اعتماد تشغيلها في Production.
* **RIGX Comparison V1:** Unit Tests تثبت حد 3، Same-category، Remove، Stale preference sanitization، WhatsApp URL، وCurrency. Playwright يثبت Add/Reload persistence/Cross-category rejection/Live API values/Differences/Mobile contained scrolling/Stale removal. لا يظهر Draft أو WhatsApp بدون إعداد رقم المالك.
* **M5 Admin E2E (8 نوفمبر 2026):** `apps/web/tests/e2e/admin-m5.spec.ts` يعمل فقط عند `E2E_M5_QA=1` مع Credentials من Environment. يثبت تسجيل دخول Owner، إنشاء مورد، فتح/حفظ البيانات المالية، غياب Bank/IBAN من API العام، ربط Variant بكود مورد، إنشاء/تعديل مستودع، Desktop/Mobile، AR/EN، Light/Dark، وConsole/Network بلا أخطاء غير متوقعة (يُسمح فقط بـ401 المتوقعة من `/identity/me`). شُغّل 1/1 على بيئة QA معزولة.
* **Wave 0–1 (9 نوفمبر 2026):** `PreviewInitializationPostgreSqlTests` تستدعي التهيئة الكاملة مرتين وتتحقق من جداول Identity/Auditing/Catalog/Purchasing/Inventory/Storefront. `StoreSettingsPostgreSqlTests` تغطي defaults/update/audit/URL/RowVersion/concurrency. `CatalogPostgreSqlTests` تغطي New/Used persistence. Playwright يغطي Store Settings + WhatsApp + Footer social + Supplier/Warehouse. لم يُنفذ Image derivative بالشعار أو Image Pipeline الأصلي بعد.
* **Overnight Batch (9 نوفمبر 2026):** `PostgreSqlIdentitySessionManagementTests` تغطي list/current/IDOR/RevokeOthers. `IdentityAdministrationPostgreSqlTests` تغطي Admin invite + duplicate email. `CatalogTests` تغطي حفظ `.original` ومطابقته. `cart.test.ts` تغطي Added/Incremented/MaxLines/MaxQuantity/Invalid وlocalStorage parsing. Playwright Account Management يغطي Profile/Sessions/Admin invite + Mobile.
* **مسارات الاختبار الحرجة:**
  1. **مسار الطلب والمراجعة (Order & Approval Flow):**
     - التحقق من منع الـ Guest Checkout وإلزام العميل بتسجيل حساب أولاً.
     - إرسال طلب جديد -> إشعار قيد المراجعة -> تسجيل موظف المبيعات لملاحظات تواصل WhatsApp -> تأكيد العميل -> اعتماد الإدارة للطلب -> بدء التجهيز.
  2. **مسار الاستلام من المحل (Store Pickup Flow):**
     - اختيار الاستلام من الفرع -> إخطار العميل بالجاهزية -> حضور العميل وتوثيق الموظف لإجراءات التحقق من الهوية والتسليم.
  3. **مسار نقطة البيع في المعرض (POS Direct Sale):**
     - مسح باركود/سيريال -> إنشاء فاتورة فورية -> سداد نقدي -> طباعة إشعار الفاتورة.
  4. **مسار فحص وقبول المرتجع (Return Inspection Flow):**
     - تقديم طلب مرتجع -> استلام فني في الحجر -> فحص الحالة الفنية -> اعتماد التقرير وتحويل الصنف لحالته النهائية -> إصدار إشعار الدائن (Credit Note).

---

## 5. المستهدفات المقترحة للجودة والأداء (Proposed Targets)

تُعتبر هذه المعدلات **مستهدفات مبدئية مقترحة** قابلة للقياس والتحسين بعد بناء النواة البرمجية:
* **تغطية الكود (Target Code Coverage):** استهداف تغطية مرتفعة لمنطق الـ Domain والـ Application الأساسي (كمؤشر إرشادي مستهدف ~80%).
* **زمن استجابة الـ API:** استهداف زمن استجابة سريع للعمليات القياسية في بيئة التطوير والإنتاج (< 100ms للعمليات العادية و < 250ms لعمليات التجميع المعقدة).
