# مراجعة وتدقيق تصميم مراجع `inventory_movements` وتكامل القيود المركبة

الحالة: **مقترح مراجعة هندسية V3.3 — مسودة فنية متقدمة (غير معتمدة). لا تعدّل ERD أو تنفذ DDL أو Migrations أو كود تطبيق قبل المراجعة المعمارية والاعتماد الصريح من صاحب المشروع.**

---

## 1. ما تثبته الوثائق الحالية وتدقيق مصادر الحركات المخزنية

يعرض [ERD](DATABASE_DESIGN_ERD.md) سبعة أنواع لحركات المخزون في `inventory_movements.movement_type`:
`GoodsReceipt`, `SalesDispatch`, `ReturnRestock`, `StockCountAdjustment`, `TransferIn`, `TransferOut`, `ScrapDisposal`.
وفي المقابل، يعرض حقل نوع المستند المرجعي `reference_document_type` قائمة مختلفة جزئياً:
`GoodsReceipt`, `Invoice`, `SalesReturn`, `StockTransfer`, `StockTransferReceipt`, `StockCountAdjustment`.

### فحص المصادر التفصيلية والحالات القائمة في المخطط

1. **حركات الشراء والبيع والتحويل:**
   - `GoodsReceipt`: يقابله جدول `goods_receipts` وبنوده `goods_receipt_items`.
   - `SalesDispatch`: يقابله جدول `invoices` وبنوده `invoice_items`; يذكر ERD `invoices.order_id`، ولا يعرّف `invoice_items.order_item_id`.
   - `ReturnRestock`: يقابله جدول `sales_returns` وبنوده `return_items`.
   - `TransferOut`: يقابله رأس التحويل `stock_transfers` وبنوده `stock_transfer_items`.
   - `TransferIn`: يقابله رأس إيصال الاستلام `stock_transfer_receipts` وبنوده `stock_transfer_receipt_items`.

2. **تسويات عجز وفروق التحويلات (`transfer_discrepancy_settlements`):**
   - يعرّف ERD الجدول والحقول التالية: `stock_transfer_id`, `stock_transfer_item_id`, `discrepancy_quantity`, `discrepancy_type`, `approval_status`.
   - وجود الجدول في ERD لا يعني اعتماد مصفوفة الصلاحيات؛ Q2.4 ما زالت معلقة. الجدول مرجع مرشح لحركة مرتبطة بعجز تحويل، لكن ERD الحالي لا يعرّف typed reference من `inventory_movements` إليه.
   - لا يُعامل المصدر كسياسة اعتماد جديدة. عند ربطه مستقبلًا، يجب أن يثبت FK المركب أن البند يتبع أمر التحويل نفسه.

3. **فحص المرتجعات والتخريد (`return_inspections` و`DamagedScrap`):**
   - بالرجوع إلى [ERD](DATABASE_DESIGN_ERD.md)، يعرّف المخطط في سطر 1190 جدول `return_inspections` بالحقول:
     `return_item_id`, `serial_number_id`, `product_id`, `inspection_status`, `condition_assessed`, `disposition_decision`.
   - عند `condition_assessed = 'DamagedScrap'` و`disposition_decision = 'WriteOffScrap'`، تكون نتيجة الفحص مصدرًا مرشحًا لـ`ScrapDisposal`. ERD لا يربط الحركة بالفحص حاليًا، ولا يثبت أن كل تخريد منشؤه مرتجع.
   - قد يلزم مستند تخريد عام لمصدر داخلي غير المرتجعات، لكن هذا غير موثق حاليًا ولا يُعامل كجدول معتمد.

### جدول تحليل مصادر الحركات المخزنية

| نوع الحركة `movement_type` | رأس المستند المصدر | بند المستند المصدر | طبيعة المصدر في الوثائق الحالية | إمكانية فرض Typed FK |
|---|---|---|---|---|
| `GoodsReceipt` | `goods_receipts` | `goods_receipt_items` | كل حركة استلام لازم تتبع بندًا تجاريًا بعينه؛ قد ينتج عن البند أكثر من حدث/تخصيص صحيح | رأس وبند إلزاميان في التوصية؛ لا unique على البند وحده |
| `SalesDispatch` | `invoices` | `invoice_items` | كل حركة صرف بيع لازم تتبع بند فاتورة بعينه؛ تعدد الحركات الصحيحة للبند لا يُمنع بمفتاح مصدر عام | رأس وبند إلزاميان في التوصية؛ لا unique على البند وحده |
| `ReturnRestock` | `sales_returns` | `return_items` | كل حركة إعادة للمخزون لازم تتبع بند مرتجع بعينه؛ تفاصيل الفحص/التوزيع لا تُستنتج من الرأس وحده | رأس وبند إلزاميان في التوصية؛ لا unique على البند وحده |
| `TransferOut` | `stock_transfers` | `stock_transfer_items` | موثق في التحويلات | نعم (رأس وبند إلزامي) |
| `TransferIn` | `stock_transfer_receipts` | `stock_transfer_receipt_items` | موثق في إيصالات التحويل | نعم (رأس وبند إلزامي) |
| `StockCountAdjustment` | (أ) حدث `transfer_discrepancy_settlements` لتسوية تحويل؛ أو (ب) بند مستند جرد عام مقترح | مرجع التسوية حدث تفصيلي مرتبط ببند التحويل نفسه؛ مستند الجرد العام يحتاج مصدرًا تفصيليًا لا يحدده ERD بعد | لا يوجد typed reference حاليًا؛ Q2.4 تظل معلقة ولا تُستنتج منها صلاحيات | لا حركة بلا حدث تسوية محدد أو بند جرد؛ إنشاء المصدر العام يحتاج قرارًا مستقلًا |
| `ScrapDisposal` | بند مستند تخريد مقترح، ويمكنه حمل مرجع `return_inspections` عند التخريد الناتج عن مرتجع | الحركة تتبع بند التخريد؛ بند التخريد يثبت الفحص والمنتج عند انطباقه | ERD لا يربط الحركة بالفحص ولا يعرف مستند تخريد عام | بند المستند إلزامي؛ التخريد المرتبط بفحص يحتاج FK مركبًا وسلسلة `RESTRICT`، ومصدر التخريد العام يظل غير محسوم |

---

## 2. نطاق المفاتيح المركبة وحدود ما يثبته ERD

ERD يعرّف `inventory_movements.stock_item_id`, `warehouse_id`, `batch_id`, و`serial_number_id`، لكنه لا يعرّف `product_id` في الحركة ولا يصرح بكل nullability. البدائل: (أ) إضافة `product_id` مساعدًا وربطه بمفاتيح مركبة، وهو الاختيار الفني الموصى به لأنه يفرض تطابق المنتج داخل DB؛ (ب) عدم إضافة العمود والاعتماد على Backend validation، مع بقاء احتمالية سجلات متخالفة عند كتابة مباشرة أو خلل تحقق؛ (ج) فرض التطابق بـTriggers متعددة، وهو يزيد منطق DB وتعقيد التزامن والصيانة ولا يقدم هنا فائدة على FK صريح. خيار (أ) يكرر المعلومة عمدًا، ويحتاج تحديث ERD، ومفاتيح `UNIQUE` للأهداف، وأعمدة product مساعدة في مصادر لا تحملها حاليًا مثل receipt item وsettlement. لا يُعرض كأنه عمود حالي أو قرار معتمد، ومخاطره تتطلب مزامنة كل أعمدة المنتج وقيوده في أي كتابة.

القيود المركبة ذات الصلة الموثقة بالفعل تخص التحويلات: `stock_transfer_receipts UNIQUE (id, stock_transfer_id)`، و`stock_transfer_items UNIQUE (id, stock_transfer_id)`، ومفاتيح كروت المخزون الموثقة في ERD. تفاصيل الإيصال يجب أن تحمل `stock_transfer_receipt_id`, `stock_transfer_item_id`, و`stock_transfer_id`، وتستخدم FK مركبًا لكل من رأس الإيصال وبند التحويل (انظر ERD أسطر 286 و326–331). كذلك `transfer_discrepancy_settlements` يحمل الرأس والبند، ويلزمه FK مركب `(stock_transfer_item_id, stock_transfer_id)` إلى `(id, stock_transfer_id)` في `stock_transfer_items` (ERD أسطر 333–335).

مع `product_id` المساعد، يثبت FK المركب تطابق الحركة وكارت المخزون والدفعة والسيريال وبند المصدر. لا يُسمح بمصدر header-only لأي من أنواع الحركة السبعة: كل حركة لها بند أو حدث تفصيلي إلزامي. استثناء التسوية ليس رأسًا عامًا؛ `transfer_discrepancy_settlements` نفسها حدث تفصيلي مربوط ببند التحويل بمفتاح مركب. من دون إضافة العمود والأهداف المركبة، يبقى تطابق منتج الحركة مع المصدر تحققًا تطبيقيًا لا FK.

المفتاح المركب `(stock_item_id, warehouse_id)` في المثال يثبت التطابق إذا كانت القيمتان موجودتين معًا؛ لذلك توصية المذكرة أن يكونا `NOT NULL` لكل حركة مخزنية، إذ لا يوثق ERD أو Workflow حركة بلا كارت صنف أو مستودع. وبالمثل، توصى `quantity NOT NULL` وغير صفرية، لكن `quantity > 0` لا يصلح عالميًا: ERD يحدد الموجب للإضافة والسالب للصرف. يقترح قيد sign بحسب النوع: `GoodsReceipt`, `ReturnRestock`, `TransferIn` موجب؛ `SalesDispatch`, `TransferOut`, `ScrapDisposal` سالب؛ و`StockCountAdjustment` قد يكون موجبًا أو سالبًا. يجب مطابقة هذا القيد مع إشارة settlement التي تعتمد لاحقًا. لا توجد استثناءات موثقة لـ`stock_item_id` أو `warehouse_id`؛ أما `serial_number_id` فاختياري صراحةً للمنتجات غير المسلسلة.

**مستودع المرتجع:** لا يصح إنشاء FK مركب إلى `sales_returns.destination_warehouse_id` في المخطط الحالي؛ جدول `sales_returns` لا يحتوي هذا العمود (ERD أسطر 1165–1175). والقاعدة الموجودة تقول إن `ReturnRestock` يعيد الكمية إلى الدفعة الأصلية في المستودع المستلم (ERD سطر 199)، بينما لا يحدد Workflow مستودع وجهة مستقلًا للمرتجع. لذا يثبت `inventory_movements` المستودع الفعلي عبر `(stock_item_id, warehouse_id)`، وتبقى مطابقة موقعه للدفعة/وجهة المرتجع قرارًا يتطلب مصدرًا موثقًا أو تحقق Backend؛ إضافة `destination_warehouse_id` إلى المرتجع ستكون تغييرًا في ERD لا يُقترح كحقيقة حالية.

**Settlement approval:** FK إلى `transfer_discrepancy_settlements` يثبت وجود سجل التسوية فقط، ولا يثبت `approval_status='Approved'`. الضمان المقترح Backend transaction يقفل settlement بـ`SELECT ... FOR UPDATE`، يتحقق من `Approved`، ثم يدرج حركة التعديل ويحدث `L` والـrollups في المعاملة نفسها. إن كان مسموحًا بالكتابة خارج Backend، يلزم Trigger يقفل/يفحص صف التسوية نفسه؛ لا يكفي Trigger يقرأ الحالة دون قفل بسبب سباق تغيير الحالة.

### 2.1 سلامة `variant_id` عبر كروت المخزون والدفعات والبنود والحركة

ERD يعرّف `variant_id` nullable في `stock_items`, `inventory_batches`, `inventory_batch_balances`, `serial_numbers`, `invoice_items`, و`stock_transfer_items`؛ وتوجد كذلك في `goods_receipt_items` و`return_items`. لكنه لا يعرّف `variant_id` في `inventory_movements`. لذلك إضافة `inventory_movements.variant_id` عمود مساعد مقترح فقط، وليست حقيقة قائمة.

تطابق `product_id` لا يكفي: يمكن أن تتطابق سجلات كلها على المنتج نفسه بينما يختلف الـvariant. كذلك FK مركب يضم `variant_id` nullable يعمل افتراضيًا بـ`MATCH SIMPLE`؛ إذا كان variant في صف الحركة أو المصدر `NULL` فقد يتجاوز PostgreSQL مقارنة الأعمدة الأخرى. `MATCH FULL` لا يحل ذلك مع `product_id` غير nullable لأنه يرفض ترك variant فارغًا. وERD يذكر `product_variants` كجدول، لكنه لا يعرض تعريف مفاتيحه أو `UNIQUE (id, product_id)`؛ يلزم استكمال هذا المفتاح في التصميم قبل إمكان FK يثبت ملكية الـvariant للمنتج.

**نموذج الربط المقترح، وليس DDL أو اعتمادًا:**

| العلاقة | ما يجب إثباته |
|---|---|
| `stock_items` ↔ `product_variants` | `stock_items.product_id` يساوي المنتج المالك للـ`variant_id` عند عدم NULL؛ والـvariant فارغ فقط إذا كان سجل الصنف منتجًا بلا variant. FK إلى `(variant_id, product_id)` يناسب حالة UUID، ويُضاف Trigger عند الكتابة لمقارنة حالة NULL بدقة. |
| `inventory_batches` ↔ `goods_receipt_items` و`product_variants` | الدفعة تحتفظ بنفس `(product_id, variant_id)` لبند الاستلام الذي أنشأها؛ الـproduct FK وحده لا يثبت variant. يلزم مفتاح مصدر مركب مع مقارنة variant؛ وERD لا يحدد حاليًا مفتاح بند إنشاء الدفعة أو قيد variant المركب. |
| `inventory_batch_balances` ↔ `inventory_batches` و`stock_items` | ERD يوثق بالفعل `trg_batch_balance_variant_integrity` لمقارنة `variant_id` للرصيد والدفعة وكارت الصنف باستخدام مقارنة NULL آمنة، إضافة إلى علاقات المنتج/المستودع. يلزم أيضًا ضمان أن كل variant غير NULL يخص `product_id` نفسه؛ تعريف هدف `product_variants` غير موجود في ERD المعروض. |
| `serial_numbers` ↔ `inventory_batches` و`stock_items` | يثبت السيريال نفس المنتج والـvariant اللذين في دفعته؛ وعند وجود موقع/كارت مخزون، يجب تطابق variant مع كارت الموقع أيضًا. FK الحالي إلى batch وvariant كلٌ على حدة لا يثبت ذلك. يلزم FK لملكية variant + Trigger يفرض تساوي القيم بما فيها NULL. |
| `invoice_items` و`goods_receipt_items` و`return_items` ↔ المنتج/السيريال | يحفظ كل بند variant نفسه الذي اختاره المستند، ويرفض سيريالًا من variant آخر حتى لو product متطابق. ERD يعرض variant وserial على بعض هذه البنود لكنه لا يصف مركبًا يثبت التطابق. |
| `stock_transfer_items` ↔ كارت المصدر وكارت الوجهة والدفعة | يجب تطابق `product_id` و`variant_id` للبند مع كروت المصدر والوجهة والدفعة المحددة. المركبات الحالية الموصوفة في ERD تتضمن المنتج ولا تتضمن variant؛ إضافة variant إلى FK وحده لا تضمن المطابقة إذا كان nullable، فيلزم Trigger يقارن القيم الثلاث بـ`IS NOT DISTINCT FROM` أو مفتاح variant غير nullable معتمد. |
| `inventory_movements` ↔ كارت المخزون والدفعة والسيريال والبند المصدر | يضاف `variant_id` مساعدًا في المقترح، ثم يقارن مع variant كارت المخزون والدفعة والسيريال وبند المصدر المناسب لنوع الحركة. مفاتيح المنتج الحالية/المقترحة تحمي product فقط؛ يلزم FK لملكية variant وTrigger لفرض التطابق الدقيق بما فيه NULL. |

الحاجز المقترح داخل قاعدة البيانات هو مفاتيح مركبة غير nullable حيث تنطبق، مع Trigger تحقق على صفوف المصدر/الحركة يستخدم `IS DISTINCT FROM` لمقارنة variants، ويُشغّل على insert/update في كل طرف قابل للتغيير. يجب أن يمنع التنفيذ تغييرات variant المتزامنة التي تكسر تاريخًا قائمًا، مثلًا بقفل صفوف الأطراف في المعاملة أو منع تعديل تعريف variant بعد استخدامه. Backend validation يكرر الفحص برسالة مفهومة، لكنه لا يحل محل القيد/Trigger إذا كانت الكتابة المباشرة إلى PostgreSQL ممكنة. لا توجد حاليًا ضمانات variant كاملة على جميع هذه العلاقات، ولم يُعدّل ERD في هذه المهمة.

### 2.2 ربط حركات التحويل بتخصيصات السيريال وسجلات استلامه

ERD يعرّف `stock_transfer_item_serials` كسجل تخصيص سيريال لبند التحويل، ويعرّف `stock_transfer_receipt_item_serials` بسجل استلام يحمل `transfer_item_serial_id`, `serial_number_id`, `stock_transfer_receipt_item_id`, و`stock_transfer_item_id`. يوضح ERD أن سجل الاستلام يجب أن يشير لنفس تخصيص الصرف والسيريال، لكن `inventory_movements` لا يحتوي حاليًا مرجعًا إلى أي من هذين السجلين؛ تطابق المنتج والدفعة لا يثبت هوية الحدث أو البند.

**الربط المقترح على مستوى المفاتيح، وليس DDL تنفيذيًا:**

| الحركة | مراجع الحركة المقترحة | سلسلة الإثبات |
|---|---|---|
| `TransferOut` لسيريال | إضافة `transfer_item_serial_id` إلى `inventory_movements`؛ يلزم مع `stock_transfer_id`, `stock_transfer_item_id`, و`serial_number_id` | FK مركب من (معرف التخصيص، بند التحويل، السيريال) إلى سجل `stock_transfer_item_serials` نفسه، مع FK مركب يثبت أن بند التحويل يتبع `stock_transfer_id`. بذلك لا يقبل سيريالًا من تخصيص أو أمر تحويل آخر. |
| `TransferIn` لسيريال | إضافة `stock_transfer_receipt_item_serial_id` إلى `inventory_movements`؛ يلزم مع `stock_transfer_receipt_id`, `stock_transfer_receipt_item_id`, `stock_transfer_id`, `stock_transfer_item_id`, و`serial_number_id` | FK مركب من معرف سجل استلام السيريال وبند الإيصال وبند التحويل والسيريال إلى الصف نفسه في `stock_transfer_receipt_item_serials`. ذلك الصف بدوره يرتبط مركبًا بـ`transfer_item_serial_id` و`serial_number_id` في تخصيص الصرف، وببند الإيصال/التحويل نفسه؛ فتكتمل السلسلة: الحركة ← سجل استلام السيريال ← تخصيص الصرف الأصلي ← بند التحويل. |

في هذا التصميم، `TransferOut` يحتاج معرف تخصيص الصرف، و`TransferIn` يحتاج معرف سجل استلام السيريال؛ لا يلزم تكرار `transfer_item_serial_id` داخل حركة الاستلام إذا كان سجل الاستلام نفسه يملك FK مركبًا إلزاميًا إلى تخصيص الصرف ويحفظ السيريال والبند معًا. يمكن تكراره فقط إذا كان مطلوبًا لتسهيل الاستعلامات، وليس بديلًا عن FK المركب. يجب أن تتضمن المفاتيح المرجعية على جدولي الربط معرفات البنود/السيريال المطلوبة كـ`UNIQUE` targets، وأن تضيف العلاقة المقترحة التحقق من `product_id` و`variant_id` للسيريال والبند. وبسبب nullable `variant_id`، يظل Trigger ذو مقارنة NULL آمنة ضروريًا.

للمنتجات المسلسلة، يتحقق Backend/Trigger من `requires_serial` ويطلب سيريالًا وتخصيصًا/سجل استلام للحركة المناسبة، ويكون مقدار حركة السيريال وحدة واحدة. للمنتجات غير المسلسلة، يظل `serial_number_id` ومعرفا الربط التسلسلي فارغين، وتستمر الحركة على مصدر البند/الحدث غير التسلسلي. FK لا يستطيع تقرير إلزام السيريال من دون ربط موثوق بصفة المنتج. لا يوضع `UNIQUE` على `(stock_transfer_id, stock_transfer_item_id, movement_type)`؛ يسمح ببنود متعددة أو أحداث صحيحة متعددة للبند. الفرادة عند اللزوم تخص مرجع حدث السيريال نفسه (تخصيص صرف بعينه أو سجل استلام بعينه) لمنع تكرار الحدث ذاته، دون منع أحداث أخرى صحيحة للبند أو التوزيع التشغيلي الذي لم يُحسم في `Q2.5`.

---

## 3. حصرية مصدر الحركة وإلزامية البند/الحدث التفصيلي

قيد `ck_movement_one_source` يعد **مصدر الحركة الرئيسي** وفق نوعها، ولا يعد `stock_transfer_id` رأس مصدر إضافيًا عندما يكون مرجع الحركة الرئيسي هو إيصال `TransferIn`. وقيد `ck_movement_source_matches_type` يطابق نوع الحركة مع الرأس والبند/الحدث التفصيلي الصحيحين، ويرفض أي مرجع بديل زائد. كما تمنع مفاتيح FK المركبة بندًا تابعًا لرأس آخر.

كون `reference_item_id` nullable في ERD الحالي لا يثبت أن الحركة يجوز أن تكون بلا مصدر تفصيلي. التوصية لهذا النموذج أن كل نوع من الأنواع السبعة يتطلب بندًا أو حدثًا محددًا: بند استلام للبضاعة، بند فاتورة للصرف، بند مرتجع للإعادة، بند تحويل للصرف، بند إيصال مع تخصيص للاستلام، بند جرد عام مقترح أو سجل تسوية تحويل محدد لتعديل المخزون، وبند تخريد للتخلص. `transfer_discrepancy_settlements` استثناء شكلي فقط من وجود item column منفصل؛ هي حدث تسوية مستقل، وتثبت علاقتها ببند التحويل بمفتاح مركب. لا يسمح لأي نوع برأس مستند وحده. مصدر الجرد العام ومستند التخريد العام إضافتان مقترحتان لا يثبتهما ERD الحالي.

لا يكفي `MATCH FULL` وحده: فهو يعالج NULL الجزئي داخل FK مركب، لكنه لا يمنع مراجع البنود الزائدة أو المراجع من نوع آخر. لذلك يجمع DDL أدناه بين مفاتيح FK مركبة، `ck_movement_one_source`, `ck_movement_item_pairs`, و`ck_movement_source_matches_type`.

---

## 4. هوية الأمر والحركة وإعادة المحاولة (Idempotency)

قيد `uq_movement_event_idempotency` الحالي في ERD على `(reference_document_type, reference_document_id, reference_item_id, movement_type)` لا يصلح كقيد عام: يرفض أكثر من حركة للبند حتى لو كانت حركات صحيحة منفصلة، مثل صرف بند فاتورة من دفعتين أو توزيع المقبول في حدث استلام واحد على دفعتين مخزونيتين. لا يجوز استبداله بمفتاح مبني فقط على `(InvoiceId, ItemId, BatchId)`؛ فقد تتكرر نفس الثلاثية في عمليتين مشروعَتين منفصلتين.

### هوية الأمر وهوية الصف الناتج

1. عند قبول API command، يستخدم العميل `Idempotency-Key` ثابتًا للعملية المنطقية، ولا ينشئ مفتاحًا جديدًا عند retry. تحفظ طبقة `Command Inbox` أو `Idempotency Store` المفتاح مع نطاق المستخدم/العملية، وبصمة الطلب، وحالة التنفيذ والنتيجة. إعادة المفتاح نفسه مع payload مختلف تُرفض. عند التخزين في PostgreSQL، يلزم تثبيت نتيجة الأمر وحركاته في transaction واحدة (أو بروتوكول استعادة موثق إذا لم تكن في قاعدة واحدة) حتى لا تبقى علامة نجاح بلا حركة أو حركة بلا نتيجة قابلة للاسترجاع.
2. ينشأ `command_id` مرة واحدة ويُحفظ مع الأمر المقبول. لكل حركة ناتجة عن الأمر يُنشأ `movement_event_id` فرعي ثابت ويُحفظ قبل/ضمن transaction التنفيذ. مفتاح الحركة الفرعي يكون مرتبطًا بـ`command_id` ومعرّف تخصيص/سطر ثابت أو sequence محفوظ؛ لا يُشتق من invoice/item/batch وحدها. `UNIQUE (movement_event_id)` يمنع إدخال نفس صف الحدث مرتين، لكنه لا يجعل الطلب نفسه idempotent إذا أرسل العميل مفتاحًا جديدًا عند retry.
3. `Outbox` يفيد لنشر آثار خارجية بعد commit، لكنه ليس بديلًا عن `Command Inbox` في منع إعادة تنفيذ أمر HTTP.
4. إذا انقطع الاتصال قبل commit، تعيد المحاولة بنفس `Idempotency-Key` ويمكن للأمر أن ينفذ إذا لم يكن قد ثُبت. إذا فُقد رد commit بعد نجاحه، يقرأ الخادم سجل المفتاح/النتيجة الملتزمة ويرجعها دون إعادة الحركات. لا يكفي الاعتماد على collision في `UNIQUE (movement_event_id)`؛ تعارض الفهرس وحده يمنع صفًا مكررًا لكنه لا يضمن إعادة response أو منع إعادة خطوات أخرى.

### Mapping الهوية لكل نوع حركة

| النوع | مصدر Typed FK الحالي/المقترح | متى يكون بند المصدر إلزاميًا؟ | مفتاح الحركة الفرعي المقترح وحماية التكرار |
|---|---|---|---|
| `GoodsReceipt` | `goods_receipts` + `goods_receipt_items` | إلزامي لكل حركة؛ البند هو المصدر التجاري التفصيلي | `command_id + receipt allocation/event id` محفوظ. يسمح بأحداث استلام منفصلة للبند/الدفعة نفسها؛ لا unique على الرأس والبند أو الدفعة وحدهما. |
| `SalesDispatch` | `invoices` + `invoice_items` | إلزامي لكل حركة؛ لا يمنع ذلك أكثر من حدث صرف مشروع للبند | `command_id + dispatch/allocation event id` محفوظ. يسمح بتقسيم بند الفاتورة على دفعات أو عمليات مستقلة؛ لا unique عام على البند. |
| `ReturnRestock` | `sales_returns` + `return_items` | إلزامي لكل حركة؛ الفحص/التصرف يظل تفصيلاً إضافيًا عند انطباقه | `command_id + inspection/disposition allocation event id` محفوظ؛ لا قيد عام على return item. |
| `TransferOut` | `stock_transfers` + `stock_transfer_items` | إلزامي حسب Workflow وERD | `command_id + dispatch line id` عند اعتماد نموذج الإرساليات؛ أو هوية محفوظة لأمر الصرف في السيناريو A. لا unique على transfer item قبل قرار A/B. |
| `TransferIn` | `stock_transfer_receipts` + `stock_transfer_receipt_items` + تخصيص قبول بالدفعة | بند الإيصال إلزامي؛ تخصيص الدفعة لازم إذا نتج أكثر من movement row | لكل allocation ثابت `allocation_id` يُعاد استخدامه عند retry، ويطابق `movement_event_id` للحركة الناتجة. يسمح بعدة allocations للبند نفسه ودفعات مختلفة. |
| `StockCountAdjustment` | `transfer_discrepancy_settlements` كحدث تسوية محدد، أو بند مستند جرد عام مقترح | إلزامي: settlement event المرتبط ببند تحويل، أو adjustment item للجرد العام؛ لا header-only | `command_id + adjustment event line id` محفوظ. لا unique على التسوية/البند وحدهما؛ Q2.4 تظل معلقة. |
| `ScrapDisposal` | بند مستند تخريد مقترح؛ ويرتبط بفحص `return_inspections` إذا كان منشؤه مرتجعًا | إلزامي: بند التخريد، مع فحص مرتبط عند مسار المرتجع؛ لا header-only | `command_id + scrap event line id` محفوظ. لا unique على الفحص أو الصنف وحدهما؛ كل عملية مشروعة لها مفتاح أمر جديد. |

هوية allocation/line يجب أن تكون مُنشأة مرة واحدة ومخزنة؛ إذا كانت تفاصيل الأمر تتكون من أكثر من حركة (مثل batch allocations)، تُسجل هويات الأبناء ضمن inbox أو مستند المصدر في المعاملة نفسها. لا يعتمد هذا التصميم على UUIDv5؛ يجوز استخدام أي مولد UUID بعد اعتماد المعماري طالما الهوية محفوظة وتُعاد كما هي عند retry.

---

## 5. حسم وتحليل التباس `PartiallyDispatched` في التحويلات المخزنية

تتضمن حالات `stock_transfers.status` قيمة `PartiallyDispatched` (ERD، سطر 838)، ويعرض Workflow صرفًا جزئيًا (سطر 187) لكنه لا يحسم هل المقصود بنود مختلفة أم إرساليات متكررة للبند نفسه. لذلك لا نثبت `uq_transfer_out_per_transfer_item` قبل القرار.

### السيناريوهات المحتملة لطبيعة الصرف الجزئي

#### السيناريو (أ): الصرف الجزئي لبنود مختلفة، وكل بند يُصرف مرة واحدة
- **المفهوم:** يحتوي أمر التحويل على 3 بنود مختلفة (شاشات، كروت شاشة، رامات). يتم صرف بند الشاشات وبند الكروت اليوم، بينما يتأخر بند الرامات للغد لعدم جاهزيته. يتحول أمر التحويل إلى `PartiallyDispatched`.
- **سلوك البند الفردي:** كل بند تحويل يتم صرف كميته المطلوبة دفعة واحدة في حركة `TransferOut` واحدة.
- **الأثر:** يمكن إبقاء `TransferOut` مرتبطًا مباشرةً بـ`stock_transfers` و`stock_transfer_items`. عند اعتماد أن كل بند يصرف مرة واحدة، يمنع قيد جزئي فريد على `(stock_transfer_id, stock_transfer_item_id)` تكرار الصرف للبند. يجب كذلك أن يساوي صرف البند كمية الصرف المقصودة وفق قرار العمل؛ القيد الفريد لا يثبت الكمية ولا يحدّث rollups.

#### السيناريو (ب): صرف البند نفسه على سيارات أو أوقات متعددة
- **المفهوم:** يطلب بند التحويل 50 قطعة من صنف معين. سيارة النقل الأولى تستوعب 30 قطعة فقط ويتم شحنها، بينما تُشحن الـ 20 قطعة المتبقية في سيارة ثانية بعد ساعات.
- **سلوك البند الفردي:** يحتاج البند الواحد إلى حركتي `TransferOut` منفصلتين في توقيتين مختلفين.
- **أثر القيد:** `uq_transfer_out_per_transfer_item` سيرفض الصرف الثاني، حتى لو كان كمية صحيحة متبقية.
- **تقييم `stock_transfer_dispatches` و`stock_transfer_dispatch_items`:** هذا النموذج يضيف حدث صرف مستقلًا وبنوده، ويربط كل صرف جزئي بوقت/شحنة وبند التحويل الأصلي؛ وبذلك يكون لكل حركة `TransferOut` مصدر حدث ثابت. يجب أن تحتوي dispatch item على `stock_transfer_id`, `stock_transfer_item_id`, وكمية الصرف، مع FK مركب يثبت أن بند التحويل يتبع الأمر نفسه؛ ويمكن أن تسجل الدفعة/السيريالات حسب التصميم المعتمد. يضيف هذا جداول ومراجع وrollups/اختبارات مزامنة، لكنه يحفظ سجل كل شحنة. إزالة القيد وحدها مع الاعتماد على `movement_event_id` تسمح بالتكرار الصحيح لكنها لا تسجل هوية الشحنة، ويظل حد مجموع الصرف تحققًا تجميعيًا بمعاملة وقفل/تزامن، لا `CHECK` صفّيًا.

> [!IMPORTANT]
> **قرار المالك مطلوب:** هل تعتمد العمليات السيناريو (أ) أم (ب)؟ لا يقرر هذا المقترح عدد الشحنات أو توقيتها.

---

## 6. DDL توضيحي متكامل — نموذج تصميم فقط، وليس Migration

المقطع التالي سلسلة إنشاء واحدة بترتيب الاعتماد، ومراجعها الذاتية وأهداف الـFK ومفاتيحها الفريدة معرفة داخله. جداول المستندات القائمة مختصرة إلى الأعمدة اللازمة لإظهار العلاقات، وليست بديلًا لتعريفاتها الكاملة في ERD. يتضمن `stock_adjustment_documents` و`stock_transfer_receipt_item_allocations` إضافتين مقترحتين غير موجودتين في ERD؛ لا تعتبران معتمدتين. المخطط يعرض علاقة `TransferOut` الحالية؛ لا يحسم سيناريو B ولا يتضمن جداول الإرساليات قبل قرار المالك. لا يُشغّل هذا SQL ولا يطبّق على قاعدة البيانات.

**نطاق هذا المقطع:** لا يحتوي المثال الحالي على `inventory_movements.variant_id` أو معرف تخصيص السيريال/سجل استلامه داخل الحركة؛ هذان الامتدادان موضحان كنموذج علاقات غير تنفيذي في القسمين 2.1 و2.2، ولم يُضاف لهما DDL في هذه الجولة. لذلك لا يُعد هذا المقطع دليلًا على فرض تطابق variant أو السيريال المخصص للتحويل حتى يُراجع المعماري امتدادات العلاقات ويعتمدها.

Nullability في ERD غير محددة لبعض أعمدة `inventory_movements`. التوصية هنا أن يكون `warehouse_id`, `stock_item_id`, `product_id`, `quantity`, `movement_number`, `movement_type`, `movement_event_id` و`created_at_utc` بقيد `NOT NULL`. تبقى `batch_id` و`serial_number_id` اختيارية؛ لا يوجد استثناء موثق يجعل الحركة بلا كارت مخزون أو مستودع. ERD الحالي لا يحتوي `product_id` في الحركة؛ إضافته عمود مساعد مقترح وليست حقيقة قائمة. تبقى `unit_cost_at_movement` و`operator_person_id` nullable وفق التعريف الحالي حتى يحسم التصميم خلاف ذلك. `quantity` تكون غير صفرية، مع الإشارة حسب نوع الحركة: موجبة لـ`GoodsReceipt`, `ReturnRestock`, `TransferIn` وسالبة لـ`SalesDispatch`, `TransferOut`, `ScrapDisposal`؛ ويسمح نوع `StockCountAdjustment` بالإشارة المناسبة لتسوية الزيادة أو النقص دون افتراض سياسة مالية. قيمة التكلفة لا تحسم طريقة التقييم Q1.1.

```sql
-- 1) أهداف العلاقات المشتركة
CREATE TABLE warehouses (id uuid PRIMARY KEY);
CREATE TABLE products (id uuid PRIMARY KEY);
CREATE TABLE persons (id uuid PRIMARY KEY);

CREATE TABLE stock_items (
    id uuid PRIMARY KEY,
    warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    UNIQUE (id, warehouse_id),
    UNIQUE (id, product_id),
    UNIQUE (id, warehouse_id, product_id)
);

CREATE TABLE inventory_batches (
    id uuid PRIMARY KEY,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    UNIQUE (id, product_id)
);

-- رصيد الدفعة في كارت مخزون ومستودع بعينهما؛ وجود المنتج وحده لا يكفي.
CREATE TABLE inventory_batch_balances (
    id uuid PRIMARY KEY,
    batch_id uuid NOT NULL,
    stock_item_id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    product_id uuid NOT NULL,
    remaining_quantity numeric(12,3) NOT NULL DEFAULT 0 CHECK (remaining_quantity >= 0),
    quarantine_quantity numeric(12,3) NOT NULL DEFAULT 0 CHECK (quarantine_quantity >= 0),
    UNIQUE (batch_id, stock_item_id, warehouse_id, product_id),
    FOREIGN KEY (batch_id, product_id)
        REFERENCES inventory_batches(id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_item_id, warehouse_id, product_id)
        REFERENCES stock_items(id, warehouse_id, product_id) ON DELETE RESTRICT
);

CREATE TABLE serial_numbers (
    id uuid PRIMARY KEY,
    batch_id uuid NOT NULL REFERENCES inventory_batches(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    UNIQUE (id, product_id),
    UNIQUE (id, batch_id, product_id),
    FOREIGN KEY (batch_id, product_id)
        REFERENCES inventory_batches(id, product_id) ON DELETE RESTRICT
);

-- 2) مصادر الشراء والبيع والمرتجعات
CREATE TABLE goods_receipts (
    id uuid PRIMARY KEY,
    warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    UNIQUE (id, warehouse_id)
);
CREATE TABLE goods_receipt_items (
    id uuid PRIMARY KEY,
    goods_receipt_id uuid NOT NULL REFERENCES goods_receipts(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    UNIQUE (goods_receipt_id, id),
    UNIQUE (goods_receipt_id, id, product_id)
);

CREATE TABLE invoices (
    id uuid PRIMARY KEY,
    warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    UNIQUE (id, warehouse_id)
);
CREATE TABLE invoice_items (
    id uuid PRIMARY KEY,
    invoice_id uuid NOT NULL REFERENCES invoices(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    UNIQUE (invoice_id, id),
    UNIQUE (invoice_id, id, product_id)
);

CREATE TABLE sales_returns (
    id uuid PRIMARY KEY
);
CREATE TABLE return_items (
    id uuid PRIMARY KEY,
    sales_return_id uuid NOT NULL REFERENCES sales_returns(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    UNIQUE (sales_return_id, id),
    UNIQUE (sales_return_id, id, product_id),
    UNIQUE (id, product_id)
);

-- نفس هوية المنتج في بند المرتجع والفحص، لا FK مستقلان قابلان للتعارض.
CREATE TABLE return_inspections (
    id uuid PRIMARY KEY,
    return_item_id uuid NOT NULL,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    serial_number_id uuid NULL REFERENCES serial_numbers(id) ON DELETE RESTRICT,
    inspection_status varchar(32) NOT NULL,
    condition_assessed varchar(32) NULL,
    disposition_decision varchar(32) NULL,
    UNIQUE (id, product_id),
    FOREIGN KEY (return_item_id, product_id)
        REFERENCES return_items(id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (serial_number_id, product_id)
        REFERENCES serial_numbers(id, product_id) ON DELETE RESTRICT
);

-- 3) التحويلات وبنودها وإيصالاتها وتسوياتها
CREATE TABLE stock_transfers (
    id uuid PRIMARY KEY,
    source_warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    destination_warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    status varchar(32) NOT NULL CHECK (status IN (
        'Draft', 'Approved', 'PartiallyDispatched', 'InTransit',
        'PartiallyReceived', 'Completed', 'ClosedWithDiscrepancy', 'Cancelled'
    )),
    CHECK (source_warehouse_id <> destination_warehouse_id),
    UNIQUE (id, source_warehouse_id, destination_warehouse_id),
    UNIQUE (id, source_warehouse_id),
    UNIQUE (id, destination_warehouse_id)
);

CREATE TABLE stock_transfer_items (
    id uuid PRIMARY KEY,
    stock_transfer_id uuid NOT NULL,
    source_warehouse_id uuid NOT NULL,
    destination_warehouse_id uuid NOT NULL,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    stock_item_source_id uuid NOT NULL,
    stock_item_dest_id uuid NOT NULL,
    batch_id uuid NULL REFERENCES inventory_batches(id) ON DELETE RESTRICT,
    requested_quantity numeric(12,3) NOT NULL CHECK (requested_quantity > 0),
    dispatched_quantity numeric(12,3) NOT NULL DEFAULT 0
        CHECK (dispatched_quantity >= 0 AND dispatched_quantity <= requested_quantity),
    UNIQUE (id, stock_transfer_id),
    UNIQUE (stock_transfer_id, id),
    UNIQUE (stock_transfer_id, id, product_id),
    UNIQUE (stock_transfer_id, id, stock_item_source_id, source_warehouse_id, product_id),
    UNIQUE (stock_transfer_id, id, stock_item_dest_id, destination_warehouse_id, product_id),
    FOREIGN KEY (stock_transfer_id, source_warehouse_id, destination_warehouse_id)
        REFERENCES stock_transfers(id, source_warehouse_id, destination_warehouse_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_item_source_id, source_warehouse_id, product_id)
        REFERENCES stock_items(id, warehouse_id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_item_dest_id, destination_warehouse_id, product_id)
        REFERENCES stock_items(id, warehouse_id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (batch_id, product_id)
        REFERENCES inventory_batches(id, product_id) ON DELETE RESTRICT
);

CREATE TABLE stock_transfer_receipts (
    id uuid PRIMARY KEY,
    stock_transfer_id uuid NOT NULL REFERENCES stock_transfers(id) ON DELETE RESTRICT,
    destination_warehouse_id uuid NOT NULL,
    UNIQUE (id, stock_transfer_id),
    UNIQUE (id, destination_warehouse_id),
    FOREIGN KEY (stock_transfer_id, destination_warehouse_id)
        REFERENCES stock_transfers(id, destination_warehouse_id) ON DELETE RESTRICT
);

CREATE TABLE stock_transfer_receipt_items (
    id uuid PRIMARY KEY,
    stock_transfer_receipt_id uuid NOT NULL,
    stock_transfer_item_id uuid NOT NULL,
    stock_transfer_id uuid NOT NULL,
    product_id uuid NOT NULL,
    accepted_quantity numeric(12,3) NOT NULL DEFAULT 0 CHECK (accepted_quantity >= 0),
    rejected_damaged_quantity numeric(12,3) NOT NULL DEFAULT 0 CHECK (rejected_damaged_quantity >= 0),
    rejected_disposition varchar(32) NULL,
    notes text NULL,
    UNIQUE (id, stock_transfer_item_id),
    UNIQUE (id, stock_transfer_item_id, stock_transfer_id),
    UNIQUE (stock_transfer_receipt_id, id),
    UNIQUE (stock_transfer_receipt_id, id, product_id),
    UNIQUE (stock_transfer_receipt_id, id, stock_transfer_item_id, stock_transfer_id, product_id),
    FOREIGN KEY (stock_transfer_receipt_id, stock_transfer_id)
        REFERENCES stock_transfer_receipts(id, stock_transfer_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_item_id, stock_transfer_id)
        REFERENCES stock_transfer_items(id, stock_transfer_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_id, stock_transfer_item_id, product_id)
        REFERENCES stock_transfer_items(stock_transfer_id, id, product_id) ON DELETE RESTRICT
);

CREATE TABLE transfer_discrepancy_settlements (
    id uuid PRIMARY KEY,
    settlement_number varchar(64) NOT NULL UNIQUE,
    stock_transfer_id uuid NOT NULL REFERENCES stock_transfers(id) ON DELETE RESTRICT,
    stock_transfer_item_id uuid NOT NULL,
    product_id uuid NOT NULL,
    discrepancy_type varchar(32) NOT NULL,
    discrepancy_quantity numeric(12,3) NOT NULL CHECK (discrepancy_quantity > 0),
    approval_status varchar(32) NOT NULL CHECK (approval_status IN ('PendingApproval', 'Approved', 'Rejected')),
    UNIQUE (id, product_id),
    FOREIGN KEY (stock_transfer_item_id, stock_transfer_id)
        REFERENCES stock_transfer_items(id, stock_transfer_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_id, stock_transfer_item_id, product_id)
        REFERENCES stock_transfer_items(stock_transfer_id, id, product_id) ON DELETE RESTRICT
);

-- تخصيصات المقبول إلى دفعات متعددة: إضافة مقترحة لمعالجة TransferIn متعدد الدفعات.
CREATE TABLE stock_transfer_receipt_item_allocations (
    id uuid PRIMARY KEY,
    stock_transfer_receipt_item_id uuid NOT NULL,
    stock_transfer_item_id uuid NOT NULL,
    stock_transfer_id uuid NOT NULL,
    product_id uuid NOT NULL,
    batch_id uuid NOT NULL REFERENCES inventory_batches(id) ON DELETE RESTRICT,
    stock_item_id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    allocation_key uuid NOT NULL,
    quantity numeric(12,3) NOT NULL CHECK (quantity > 0),
    UNIQUE (stock_transfer_receipt_item_id, allocation_key),
    UNIQUE (id, stock_transfer_receipt_item_id, batch_id, quantity, product_id),
    UNIQUE (id, stock_transfer_receipt_item_id, stock_transfer_item_id, stock_transfer_id,
            stock_item_id, warehouse_id, batch_id, quantity, product_id),
    FOREIGN KEY (stock_transfer_receipt_item_id, stock_transfer_item_id, stock_transfer_id)
        REFERENCES stock_transfer_receipt_items(id, stock_transfer_item_id, stock_transfer_id)
        ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_id, stock_transfer_item_id, product_id)
        REFERENCES stock_transfer_items(stock_transfer_id, id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_id, stock_transfer_item_id, stock_item_id, warehouse_id, product_id)
        REFERENCES stock_transfer_items(stock_transfer_id, id, stock_item_dest_id,
                                        destination_warehouse_id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_item_id, warehouse_id, product_id)
        REFERENCES stock_items(id, warehouse_id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (batch_id, stock_item_id, warehouse_id, product_id)
        REFERENCES inventory_batch_balances(batch_id, stock_item_id, warehouse_id, product_id)
        ON DELETE RESTRICT,
    FOREIGN KEY (batch_id, product_id)
        REFERENCES inventory_batches(id, product_id) ON DELETE RESTRICT
);

-- 4) مستندات مصدر الجرد العام والتخريد المقترحة (ومنها التخريد المرتبط بفحص مرتجع)
-- ربط return_inspection_id هنا يجعل المسار الفعلي:
-- inventory_movements -> stock_adjustment_documents -> return_inspections -> return_items.
CREATE TABLE stock_adjustment_documents (
    id uuid PRIMARY KEY,
    warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    adjustment_kind varchar(32) NOT NULL CHECK (
        adjustment_kind IN ('StockCountAdjustment', 'ScrapDisposal')
    ),
    return_inspection_id uuid NULL,
    return_inspection_product_id uuid NULL,
    UNIQUE (id, adjustment_kind),
    UNIQUE (id, warehouse_id),
    UNIQUE (id, return_inspection_product_id),
    UNIQUE (id, return_inspection_id, return_inspection_product_id),
    CHECK ((return_inspection_id IS NULL) = (return_inspection_product_id IS NULL)),
    CHECK (return_inspection_id IS NULL OR adjustment_kind = 'ScrapDisposal'),
    FOREIGN KEY (return_inspection_id, return_inspection_product_id)
        REFERENCES return_inspections(id, product_id) MATCH FULL ON DELETE RESTRICT
);
CREATE TABLE stock_adjustment_items (
    id uuid PRIMARY KEY,
    adjustment_document_id uuid NOT NULL REFERENCES stock_adjustment_documents(id) ON DELETE RESTRICT,
    return_inspection_id uuid NULL,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    UNIQUE (adjustment_document_id, id),
    UNIQUE (adjustment_document_id, id, product_id),
    FOREIGN KEY (adjustment_document_id, return_inspection_id, product_id)
        REFERENCES stock_adjustment_documents(id, return_inspection_id, return_inspection_product_id)
        ON DELETE RESTRICT
);
-- يلزم Trigger موصوف في النص: عند وجود return_inspection_id على المستند
-- يفرض نفس inspection id في كل بند، ويلزم حركة ScrapDisposal أن تشير لبند مطابق.

-- 5) سجل الحركات: الأعمدة الجوهرية في ERD + مراجع Typed FK.
CREATE TABLE inventory_movements (
    id uuid PRIMARY KEY,
    movement_number varchar(64) NOT NULL UNIQUE,
    movement_type varchar(32) NOT NULL CHECK (movement_type IN (
        'GoodsReceipt', 'SalesDispatch', 'ReturnRestock', 'StockCountAdjustment',
        'TransferIn', 'TransferOut', 'ScrapDisposal'
    )),
    warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    stock_item_id uuid NOT NULL REFERENCES stock_items(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    batch_id uuid NULL REFERENCES inventory_batches(id) ON DELETE RESTRICT,
    serial_number_id uuid NULL REFERENCES serial_numbers(id) ON DELETE RESTRICT,
    quantity numeric(12,3) NOT NULL CHECK (quantity <> 0),
    unit_cost_at_movement numeric(18,4) NULL,
    operator_person_id uuid NULL REFERENCES persons(id) ON DELETE RESTRICT,
    notes text NULL,
    created_at_utc timestamptz NOT NULL,
    movement_event_id uuid NOT NULL UNIQUE,

    goods_receipt_id uuid NULL,
    goods_receipt_item_id uuid NULL,
    invoice_id uuid NULL,
    invoice_item_id uuid NULL,
    sales_return_id uuid NULL,
    return_item_id uuid NULL,
    stock_transfer_id uuid NULL,
    stock_transfer_item_id uuid NULL,
    transfer_out_stock_item_id uuid NULL,
    transfer_out_warehouse_id uuid NULL,
    stock_transfer_receipt_id uuid NULL,
    stock_transfer_receipt_item_id uuid NULL,
    transfer_receipt_allocation_id uuid NULL,
    transfer_discrepancy_settlement_id uuid NULL,
    adjustment_document_id uuid NULL,
    adjustment_kind varchar(32) NULL,
    adjustment_item_id uuid NULL,

    -- مراجع typed مركبة؛ حقول transfer_out_* مساعدة لتمييز كارت/مستودع المصدر عن الوجهة.
    FOREIGN KEY (stock_item_id, warehouse_id)
        REFERENCES stock_items(id, warehouse_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_item_id, product_id)
        REFERENCES stock_items(id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (batch_id, stock_item_id, warehouse_id, product_id)
        REFERENCES inventory_batch_balances(batch_id, stock_item_id, warehouse_id, product_id)
        ON DELETE RESTRICT,
    FOREIGN KEY (serial_number_id, batch_id, product_id)
        REFERENCES serial_numbers(id, batch_id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (goods_receipt_id) REFERENCES goods_receipts(id) ON DELETE RESTRICT,
    FOREIGN KEY (goods_receipt_id, warehouse_id)
        REFERENCES goods_receipts(id, warehouse_id) ON DELETE RESTRICT,
    FOREIGN KEY (goods_receipt_id, goods_receipt_item_id)
        REFERENCES goods_receipt_items(goods_receipt_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (goods_receipt_id, goods_receipt_item_id, product_id)
        REFERENCES goods_receipt_items(goods_receipt_id, id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE RESTRICT,
    FOREIGN KEY (invoice_id, warehouse_id)
        REFERENCES invoices(id, warehouse_id) ON DELETE RESTRICT,
    FOREIGN KEY (invoice_id, invoice_item_id)
        REFERENCES invoice_items(invoice_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (invoice_id, invoice_item_id, product_id)
        REFERENCES invoice_items(invoice_id, id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (sales_return_id) REFERENCES sales_returns(id) ON DELETE RESTRICT,
    FOREIGN KEY (sales_return_id, return_item_id)
        REFERENCES return_items(sales_return_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (sales_return_id, return_item_id, product_id)
        REFERENCES return_items(sales_return_id, id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_id) REFERENCES stock_transfers(id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_item_id, stock_transfer_id)
        REFERENCES stock_transfer_items(id, stock_transfer_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_id, stock_transfer_item_id, transfer_out_stock_item_id,
                 transfer_out_warehouse_id, product_id)
        REFERENCES stock_transfer_items(stock_transfer_id, id, stock_item_source_id,
                                        source_warehouse_id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_receipt_id) REFERENCES stock_transfer_receipts(id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_receipt_id, warehouse_id)
        REFERENCES stock_transfer_receipts(id, destination_warehouse_id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_receipt_id, stock_transfer_receipt_item_id)
        REFERENCES stock_transfer_receipt_items(stock_transfer_receipt_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (stock_transfer_receipt_id, stock_transfer_receipt_item_id,
                 stock_transfer_item_id, stock_transfer_id, product_id)
        REFERENCES stock_transfer_receipt_items(stock_transfer_receipt_id, id,
                                                stock_transfer_item_id, stock_transfer_id,
                                                product_id) ON DELETE RESTRICT,
    FOREIGN KEY (transfer_receipt_allocation_id, stock_transfer_receipt_item_id,
                 stock_transfer_item_id, stock_transfer_id, stock_item_id, warehouse_id,
                 batch_id, quantity, product_id)
        REFERENCES stock_transfer_receipt_item_allocations(id, stock_transfer_receipt_item_id,
                   stock_transfer_item_id, stock_transfer_id, stock_item_id, warehouse_id,
                   batch_id, quantity, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (transfer_discrepancy_settlement_id)
        REFERENCES transfer_discrepancy_settlements(id) ON DELETE RESTRICT,
    FOREIGN KEY (transfer_discrepancy_settlement_id, product_id)
        REFERENCES transfer_discrepancy_settlements(id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (adjustment_document_id) REFERENCES stock_adjustment_documents(id) ON DELETE RESTRICT,
    FOREIGN KEY (adjustment_document_id, adjustment_kind)
        REFERENCES stock_adjustment_documents(id, adjustment_kind) MATCH FULL ON DELETE RESTRICT,
    FOREIGN KEY (adjustment_document_id, adjustment_item_id)
        REFERENCES stock_adjustment_items(adjustment_document_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (adjustment_document_id, adjustment_item_id, product_id)
        REFERENCES stock_adjustment_items(adjustment_document_id, id, product_id) ON DELETE RESTRICT,
    FOREIGN KEY (adjustment_document_id, warehouse_id)
        REFERENCES stock_adjustment_documents(id, warehouse_id) ON DELETE RESTRICT,
    CONSTRAINT ck_movement_one_source CHECK (
        (movement_type = 'TransferIn' AND
         num_nonnulls(goods_receipt_id, invoice_id, sales_return_id,
                      stock_transfer_receipt_id, transfer_discrepancy_settlement_id,
                      adjustment_document_id) = 1)
        OR
        (movement_type <> 'TransferIn' AND
         num_nonnulls(goods_receipt_id, invoice_id, sales_return_id, stock_transfer_id,
                      stock_transfer_receipt_id, transfer_discrepancy_settlement_id,
                      adjustment_document_id) = 1)
    ),
    CONSTRAINT ck_movement_item_pairs CHECK (
        (goods_receipt_item_id IS NULL OR goods_receipt_id IS NOT NULL) AND
        (invoice_item_id IS NULL OR invoice_id IS NOT NULL) AND
        (return_item_id IS NULL OR sales_return_id IS NOT NULL) AND
        (stock_transfer_item_id IS NULL OR stock_transfer_id IS NOT NULL) AND
        (stock_transfer_receipt_item_id IS NULL OR stock_transfer_receipt_id IS NOT NULL) AND
        (transfer_receipt_allocation_id IS NULL OR
         (stock_transfer_receipt_id IS NOT NULL AND stock_transfer_receipt_item_id IS NOT NULL
          AND stock_transfer_item_id IS NOT NULL AND stock_transfer_id IS NOT NULL
          AND batch_id IS NOT NULL)) AND
        (adjustment_kind IS NULL OR adjustment_document_id IS NOT NULL) AND
        (adjustment_item_id IS NULL OR adjustment_document_id IS NOT NULL)
    ),
    CONSTRAINT ck_movement_source_matches_type CHECK (
        CASE movement_type
          WHEN 'GoodsReceipt' THEN goods_receipt_id IS NOT NULL AND goods_receipt_item_id IS NOT NULL
              AND invoice_id IS NULL AND sales_return_id IS NULL AND stock_transfer_id IS NULL
              AND stock_transfer_receipt_id IS NULL AND transfer_discrepancy_settlement_id IS NULL
              AND adjustment_document_id IS NULL AND invoice_item_id IS NULL AND return_item_id IS NULL
              AND stock_transfer_item_id IS NULL AND stock_transfer_receipt_item_id IS NULL
              AND transfer_receipt_allocation_id IS NULL AND adjustment_item_id IS NULL
          WHEN 'SalesDispatch' THEN invoice_id IS NOT NULL AND invoice_item_id IS NOT NULL
              AND goods_receipt_id IS NULL AND sales_return_id IS NULL AND stock_transfer_id IS NULL
              AND stock_transfer_receipt_id IS NULL AND transfer_discrepancy_settlement_id IS NULL
              AND adjustment_document_id IS NULL AND goods_receipt_item_id IS NULL AND return_item_id IS NULL
              AND stock_transfer_item_id IS NULL AND stock_transfer_receipt_item_id IS NULL
              AND transfer_receipt_allocation_id IS NULL AND adjustment_item_id IS NULL
          WHEN 'ReturnRestock' THEN sales_return_id IS NOT NULL AND return_item_id IS NOT NULL
              AND goods_receipt_id IS NULL AND invoice_id IS NULL AND stock_transfer_id IS NULL
              AND stock_transfer_receipt_id IS NULL AND transfer_discrepancy_settlement_id IS NULL
              AND adjustment_document_id IS NULL AND goods_receipt_item_id IS NULL
              AND invoice_item_id IS NULL AND stock_transfer_item_id IS NULL
              AND stock_transfer_receipt_item_id IS NULL AND transfer_receipt_allocation_id IS NULL
              AND adjustment_item_id IS NULL
          WHEN 'TransferOut' THEN stock_transfer_id IS NOT NULL AND stock_transfer_item_id IS NOT NULL
              AND goods_receipt_id IS NULL AND invoice_id IS NULL AND sales_return_id IS NULL
              AND stock_transfer_receipt_id IS NULL AND transfer_discrepancy_settlement_id IS NULL
              AND adjustment_document_id IS NULL AND goods_receipt_item_id IS NULL
              AND invoice_item_id IS NULL AND return_item_id IS NULL
              AND stock_transfer_receipt_item_id IS NULL AND transfer_receipt_allocation_id IS NULL
              AND adjustment_item_id IS NULL
          WHEN 'TransferIn' THEN stock_transfer_receipt_id IS NOT NULL
              AND stock_transfer_id IS NOT NULL AND stock_transfer_item_id IS NOT NULL
              AND stock_transfer_receipt_item_id IS NOT NULL AND transfer_receipt_allocation_id IS NOT NULL
              AND batch_id IS NOT NULL
              AND goods_receipt_id IS NULL AND invoice_id IS NULL AND sales_return_id IS NULL
              AND transfer_discrepancy_settlement_id IS NULL
              AND adjustment_document_id IS NULL
              AND goods_receipt_item_id IS NULL AND invoice_item_id IS NULL
              AND return_item_id IS NULL AND adjustment_item_id IS NULL
          WHEN 'StockCountAdjustment' THEN (
              (transfer_discrepancy_settlement_id IS NOT NULL AND adjustment_document_id IS NULL)
              OR (transfer_discrepancy_settlement_id IS NULL AND adjustment_document_id IS NOT NULL
                  AND adjustment_kind = 'StockCountAdjustment' AND adjustment_item_id IS NOT NULL)
          ) AND goods_receipt_id IS NULL AND invoice_id IS NULL AND sales_return_id IS NULL
              AND stock_transfer_id IS NULL AND stock_transfer_receipt_id IS NULL
              AND goods_receipt_item_id IS NULL AND invoice_item_id IS NULL AND return_item_id IS NULL
              AND stock_transfer_item_id IS NULL AND stock_transfer_receipt_item_id IS NULL
              AND transfer_receipt_allocation_id IS NULL
          WHEN 'ScrapDisposal' THEN adjustment_document_id IS NOT NULL
              AND adjustment_kind = 'ScrapDisposal'
              AND adjustment_item_id IS NOT NULL
              AND goods_receipt_id IS NULL AND invoice_id IS NULL AND sales_return_id IS NULL
              AND stock_transfer_id IS NULL AND stock_transfer_receipt_id IS NULL
              AND transfer_discrepancy_settlement_id IS NULL
              AND goods_receipt_item_id IS NULL AND invoice_item_id IS NULL AND return_item_id IS NULL
              AND stock_transfer_item_id IS NULL AND stock_transfer_receipt_item_id IS NULL
              AND transfer_receipt_allocation_id IS NULL
          ELSE FALSE
        END IS TRUE
    ),
    CONSTRAINT ck_movement_transfer_in_event_id CHECK (
        movement_type <> 'TransferIn' OR movement_event_id = transfer_receipt_allocation_id
    ),
    CONSTRAINT ck_movement_transfer_stock_item_role CHECK (
        (movement_type = 'TransferOut' AND transfer_out_stock_item_id = stock_item_id
         AND transfer_out_warehouse_id = warehouse_id) OR
        (movement_type <> 'TransferOut' AND transfer_out_stock_item_id IS NULL
         AND transfer_out_warehouse_id IS NULL)
    ),
    CONSTRAINT ck_movement_serial_batch_quantity CHECK (
        (serial_number_id IS NULL OR (batch_id IS NOT NULL AND abs(quantity) = 1))
    ),
    CONSTRAINT ck_movement_adjustment_reason CHECK (
        movement_type NOT IN ('StockCountAdjustment', 'ScrapDisposal') OR notes IS NOT NULL
    ),
    CONSTRAINT ck_movement_quantity_direction CHECK (
        (movement_type IN ('GoodsReceipt', 'ReturnRestock', 'TransferIn') AND quantity > 0) OR
        (movement_type IN ('SalesDispatch', 'TransferOut', 'ScrapDisposal') AND quantity < 0) OR
        movement_type = 'StockCountAdjustment'
    )
);
```

**ضمانات وحدود المثال:**
- `stock_transfer_receipt_items` يثبت أن `stock_transfer_receipt_id` و`stock_transfer_item_id` مرتبطان بـ`stock_transfer_id` نفسه باستخدام قيدي `(receipt_id, stock_transfer_id)` و`(transfer_item_id, stock_transfer_id)`. و`transfer_discrepancy_settlements` يثبت تطابق البند والرأس بقيد `(stock_transfer_item_id, stock_transfer_id)`.
- `TransferIn.stock_transfer_id` مفتاح ربط تابع لبند التحويل، وليس رأس المصدر الذي يُحسب في حصرية المصادر؛ مصدر الحركة هو إيصال الاستلام وبنده وتخصيصه. هذا التفريق يمنع قيد «مصدر واحد» من رفض حركة استلام صحيحة تحمل معرف أمر التحويل لإثبات علاقة البند.
- `return_inspections(return_item_id, product_id)` يشير إلى `(return_items.id, return_items.product_id)`، و`(serial_number_id, product_id)` إلى السيريال للمنتجات المسلسلة؛ يظل السيريال nullable. عند تخريد مرتجع، مستند التخريد يحمل inspection id وinspection product مع FK مركب `ON DELETE RESTRICT`. بند المستند يحمل inspection id الاختياري، وFK ثلاثي `(adjustment_document_id, return_inspection_id, product_id)` يطابق المنتج بهوية الفحص المسجلة على المستند عند ملء المعرفين. لأن المستند العام يسمح بـNULL، FK وحده لا يمنع ترك inspection id فارغًا في بند تابع لمستند مرتجع؛ يلزم Trigger يتحقق عند كتابة/تعديل المستند أو بند التخريد من أن كل بند في مستند مرتبط بفحص يحمل الفحص نفسه، وأن الحركة `ScrapDisposal` لهذا المستند تشير لبند مطابق. في المستند العام تظل حقول الفحص NULL ويسمح التخريد العام. الحركة تربط المستند والبند والمنتج. فحص أن قرار الفحص هو `WriteOffScrap` يظل تحققًا تطبيقيًا/معاملة؛ FK لا يفحص الحالة.
- كل حركة لها مصدر تفصيلي إلزامي. `GoodsReceipt` يحتاج `goods_receipt_item_id`، و`SalesDispatch` يحتاج `invoice_item_id`، و`ReturnRestock` يحتاج `return_item_id`، و`TransferOut` يحتاج `stock_transfer_item_id`، و`TransferIn` يحتاج `stock_transfer_receipt_item_id` وتخصيصًا محددًا، و`ScrapDisposal` يحتاج `adjustment_item_id`. أما `StockCountAdjustment` فيحتاج إما حدث `transfer_discrepancy_settlements` المرتبط ببند تحويل، أو بندًا في مستند جرد عام مقترح. التسوية حدث تفصيلي، وليست رأس مستند عام. `reference_item_id` nullable في ERD لا يسمح بحركة بلا مصدر تفصيلي.
- في `TransferOut` يربط FK مركب `(stock_transfer_id, stock_transfer_item_id, transfer_out_stock_item_id, transfer_out_warehouse_id, product_id)` ببند التحويل من جهة المصدر، ويفرض CHECK مساواة حقلي المساعدة بـ`inventory_movements.stock_item_id` و`warehouse_id`. في `TransferIn` يربط FK التخصيص الحركة بكارت الوجهة والدفعة والمستودع والكمية والمنتج، والتخصيص نفسه مربوط بجهة الوجهة في بند التحويل. بذلك لا يُستخدم FK المصدر على حركة الوجهة.
- إذا كان `batch_id` موجودًا، FK `(batch_id, stock_item_id, warehouse_id, product_id)` يفرض وجود رصيد لهذه الدفعة في كارت المخزون والمستودع نفسيهما؛ لا يكتفي بتطابق المنتج. وFK `(serial_number_id, batch_id, product_id)` يربط السيريال بدفعته الأصلية وبمنتجه، فيما يمنع CHECK سيريالًا بلا دفعة ويفرض `abs(quantity)=1` عند وجود سيريال. السيريال يظل اختياريًا للمنتجات غير المسلسلة. المفاتيح لا تثبت كفاية الرصيد أو تحديثه؛ ذلك يحتاج transaction وقفل/تحقق رصيد.
- يظل `batch_id` nullable في الحركة العامة مثل ERD الحالي؛ لذلك لا يثبت المثال وجود رصيد دفعة عندما تكون الحركة بلا batch. فرض وجود batch لمنتج بعينه يتطلب أن تكون خاصية تتبع الدفعات متاحة على المنتج/الصنف في تصميم المصدر، ثم قيد Trigger أو تحقق Backend؛ لا يضيف هذا المثال سياسة تتبع جديدة. وبالمثل، يثبت المثال صحة السيريال إذا أُرسل، لكنه لا يفرض وجوده لكل منتج يتطلب serial ما لم يُربط ذلك بقيد إضافي أو تحقق تطبيقي معتمد.
- في `TransferIn`، `batch_id` و`transfer_receipt_allocation_id` ومرجع بند الإيصال غير NULL حسب CHECKs النوعية؛ FK متعدد الأعمدة يطابق allocation والدفعة والكارت والمستودع والكمية والمنتج. لذلك لا يستطيع PostgreSQL `MATCH SIMPLE` تخطي القيد بسبب `batch_id=NULL`. يظل قيد مجموع التخصيصات مقابل `accepted_quantity` بحاجة إلى معاملة تقفل بند الإيصال أو Trigger متزامن.
- تخصيصات `TransferIn` تتيح عدة صفوف allocations لنفس `stock_transfer_receipt_item_id` على دفعات متعددة. كل allocation key فريد داخل بند الإيصال، و`movement_event_id` للحركة يطابق allocation `id`، كما يطابق FK قيمة الدفعة والكمية والمنتج. قيد التجميع `SUM(allocation.quantity) <= accepted_quantity` لا يمكن أن يفرضه `CHECK` عادي؛ يتطلب معاملة تقفل بند الإيصال أو trigger مصممًا ومختبرًا.
- لا يضع المثال `UNIQUE` على `(header,item)` للشراء/البيع/المرتجع/التسويات، ولا على بند إيصال التحويل، كي لا يمنع أحداثًا صحيحة متكررة. `UNIQUE(movement_event_id)` يمنع إعادة نفس صف الحدث فقط؛ `Command Inbox/Idempotency Store` هو ما يربط طلب العميل المتكرر بنتيجة الأمر الملتزم.
- `product_id` غير موجود في ERD الحالي. إضافته إلى `inventory_movements` عمود مساعد مكرر عمدًا من `stock_items` ومصدر الحركة، ثم حمايته بمركبات FK إلى `(stock_item_id, product_id)`، والدفعة والسيريال وبند المصدر؛ هذا أقوى من تحقق التطبيق وحده. مخاطره: تكرار البيانات، ضرورة تمرير القيمة نفسها في كل كتابة، وتحديث ERD والـEF mapping والـconstraints معًا. البديل عدم إضافة العمود وقبول تحقق Backend للمنتج، مع بقاء فجوة سلامة مرجعية في قاعدة البيانات. التوصية الفنية هي العمود المساعد بعد المراجعة المعمارية، لا اعتماد التعديل الآن. كذلك لا يفرض `UNIQUE` على `TransferOut` قبل قرار سيناريو الصرف.
- `sales_returns.destination_warehouse_id` غير موجود في ERD الحالي، لذلك لا يربط المثال `ReturnRestock` به. القاعدة الحالية تشير إلى إرجاع الكمية للدفعة الأصلية في المستودع المستلم، لكن لا يوجد عمود وجهة على رأس المرتجع ولا تفاصيل Workflow تحدد علاقة مرجعية بديلة؛ يثبت `stock_item_id` و`warehouse_id` موقع الحركة الفعلي فقط. أي تحقق من الموقع المقصود يظل Backend validation حتى يعتمد مصدر موقع واضح ويضاف إلى ERD.
- FK settlement يثبت وجود المصدر وتطابق المنتج/البند، ولا يثبت `approval_status`. المسار الموصى به هو Backend transaction تقفل صف settlement، تتحقق من `Approved` وتدرج الحركة وتحدّث الأثر في المعاملة ذاتها. إذا كانت الكتابة المباشرة من خارج Backend ممكنة، يلزم Trigger مع قفل صف settlement. هذه طبقة غير ممثلة كضمان داخل DDL التوضيحي.
- `ck_movement_quantity_direction` يفرض أن تكون الكمية غير صفرية وبالإشارة المناسبة لنوع الحركة؛ لا يفرض `quantity > 0` عالميًا لأن ERD يعرّف الصرف بالسالب. `StockCountAdjustment` يسمح بالإشارة للزيادة أو النقص.
- `stock_adjustment_documents` مجرد غلاف مصدر مقترح مع `adjustment_kind` ومعرفات منفصلة؛ لا يدمج دورة موافقة أو صلاحيات الجرد والتخريد، ولا يضيف حالة/اعتماد. إذا اختلفت دورة العمل فعليًا، يلزم فصل النوعين بعد قرار المالك.

---
## 7. سيناريوهات تحقق سلبية وإيجابية مقترحة

هذه السيناريوهات مواصفات اختبار تصميمية فقط. لم تُنفذ على PostgreSQL؛ لا تدعي هذه المذكرة نجاح اختبار أو Migration.

| السيناريو | الإجراء | النتيجة المتوقعة والضمان |
|---|---|---|
| رأس إيصال وبند تحويل من أمرين مختلفين | أدخل `stock_transfer_receipt_items` برأس إيصال تابع للتحويل T1 وبند تابع لـT2 | رفض composite FK بين `(receipt_id, transfer_id)` أو `(transfer_item_id, transfer_id)`؛ يثبت `stock_transfer_id` الواحد |
| كارت مخزون ومستودع غير متطابقين | `stock_item_id` يعود لمستودع W1 بينما `warehouse_id=W2` | رفض composite FK `(stock_item_id, warehouse_id)`؛ العمودان `NOT NULL` في التوصية |
| مستودع حركة التحويل لا يطابق المصدر/الوجهة | `TransferOut` على غير مستودع المصدر أو `TransferIn` على غير وجهة الإيصال | رفض composite FK للرأس والمستودع؛ حقول المصدر والوجهة موجودة في تعريفات التحويل والإيصال بالمثال |
| تسوية برأس وبند غير متطابقين | أدخل `transfer_discrepancy_settlements` مع transfer T1 وبند تابع لـT2 | رفض FK `(stock_transfer_item_id, stock_transfer_id)` |
| فحص بمنتج مخالف لبند المرتجع | `return_inspections.return_item_id` لبند منتجه P1 مع `product_id=P2` | رفض FK المركب `(return_item_id, product_id)` |
| سيريال بمنتج مختلف عن فحص المرتجع | اجعل `serial_number_id` لسيريال P2 و`product_id=P1` | رفض FK `(serial_number_id, product_id)`؛ ومع `serial_number_id=NULL` يقبل الفحص للمنتج غير المسلسل |
| حركة تحويل بكارت مخزون غير كارت بند التحويل | `TransferOut.stock_item_id` ليس `stock_item_source_id`، أو allocation الاستلام ليس على `stock_item_dest_id` | رفض FK المصدر مع CHECK حقول `transfer_out_*` في الصرف؛ وFK التخصيص/الوجهة في الاستلام |
| دفعة بلا رصيد في كارت الحركة ومستودعها | استخدم دفعة صحيحة للمنتج لكن لا يوجد balance مطابق لـ`stock_item_id` و`warehouse_id` | رفض FK الرباعي إلى `inventory_batch_balances`؛ تطابق المنتج وحده غير كافٍ |
| سيريال ودفعة غير متطابقين أو كمية سيريال متعددة | اربط `serial_number_id` بسيريال من batch أخرى، أو اجعل `abs(quantity) <> 1` | رفض FK الثلاثي `(serial_number_id,batch_id,product_id)` أو `ck_movement_serial_batch_quantity` |
| نفس المنتج لكن variant مختلف بين كارت المخزون والدفعة | اجعل `product_id` متطابقًا و`stock_items.variant_id=V1` مقابل `inventory_batches.variant_id=V2` | يجب رفضه بمقارنة Trigger/`IS NOT DISTINCT FROM`؛ FK المنتج وحده لا يكفي، وFK المركب مع variant nullable قد يتخطى الفحص بـ`MATCH SIMPLE` |
| variant مختلف بين بند الفاتورة والحركة | `invoice_items` على V1 والحركة على V2 مع نفس `product_id` | يرفضه FK مركب عند وجود variantين، أو Trigger variant المقترح؛ غياب `inventory_movements.variant_id` في ERD الحالي يعني أن DB لا يفرضه الآن |
| variant مختلف بين بند التحويل وكارت المصدر/الوجهة أو الدفعة | نفس المنتج لكن بند التحويل V1، وكارت أو batch V2 | يرفضه Trigger مقارنة variant المقترح؛ المركبات الحالية التي تثبت المنتج فقط لا تمنع الاختلاف |
| حالة NULL تخفي variant مخالفًا | صف المصدر variant=null والصف التابع variant=V1، أو العكس، مع تطابق product | يجب أن ترفضه المقارنة NULL الآمنة؛ FK مركب عادي `MATCH SIMPLE` قد يتجاوزه. لا يُقبل null إلا إذا كانت كل الأطراف المعنية null |
| variant تابع لمنتج مختلف | `variant_id` يعود إلى منتج P2 بينما صف المخزون/المصدر يحمل P1 | يرفضه FK مقترح إلى مفتاح `(variant_id, product_id)` بعد تعريف `UNIQUE` الهدف في `product_variants`؛ ERD الحالي لا يعرض تعريف الهدف المركب |
| حركة صرف بسيريال غير المخصص لبند التحويل | الحركة تحمل transfer item T1 وسيريالًا مخصصًا في `stock_transfer_item_serials` لبند T2 | يرفضه FK مركب من `transfer_item_serial_id`, بند التحويل والسيريال إلى صف التخصيص نفسه |
| حركة تحويل مسلسلة بلا مرجع سجل السيريال التفصيلي | `TransferOut` يحمل `serial_number_id` بلا `transfer_item_serial_id`، أو `TransferIn` يحمل السيريال بلا `stock_transfer_receipt_item_serial_id` | يرفضه قيد نوع الحركة/Trigger المقترح؛ السيريال وحده لا يثبت تخصيص الصرف ولا سجل الاستلام |
| حركة `TransferOut` تشير لسيريال من تحويل آخر | استعمل السيريال نفسه/منتجه ودفعتُه، لكن مرجع التخصيص تابع لتحويل مختلف | يرفضه ربط الحركة بتخصيص الصرف المركب ثم ببند التحويل/الأمر؛ تطابق المنتج والدفعة لا يكفي |
| حركة `TransferIn` تشير إلى سجل استلام من إيصال آخر | `stock_transfer_receipt_item_serial_id` تابع لبند إيصال آخر، ولو كان السيريال والمنتج متطابقين | يرفضه FK المركب إلى سجل استلام السيريال مع إيصال وبند التحويل، ثم FK السجل إلى تخصيص الصرف الأصلي |
| سجل استلام سيريال مربوط بتخصيص صرف غير مطابق | اجعل `transfer_item_serial_id` يخص سيريالًا أو بند تحويل آخر عن الموجود في صف استلام السيريال | يرفضه FK المركب `(transfer_item_serial_id, serial_number_id, stock_transfer_item_id)` المقترح؛ يمنع ربط إيصال/تحويل مختلف |
| منتج يتطلب سيريال وحركة بلا سيريال | نفذ حركة تحويل لمنتج `requires_serial=true` مع `serial_number_id=NULL` | يجب أن يرفضه تحقق Backend أو Trigger يقرأ خاصية المنتج؛ FK وحده لا يعرف الإلزام من ERD الحالي |
| حركة غير مسلسلة مع مراجع تخصيص/استلام سيريال | منتج لا يتطلب سيريال مع معرفات تخصيص أو receipt-serial موجودة | يرفضها قيد نوع/تحقق Trigger المقترح؛ المنتج غير المسلسل يظل مقبولًا عندما تكون جميع مراجع السيريال NULL |
| حركتان صحيحتان لبند تحويل واحد وسيريالين مختلفين | سجل حركتين منفصلتين لتخصيصَي سيريال مختلفين على بند التحويل نفسه | يقبلهما التصميم؛ يمنع فقط تكرار مرجع تخصيص الصرف أو سجل استلام السيريال نفسه، ولا يفرض حركة واحدة على بند التحويل |
| `TransferIn` بــ`batch_id=NULL` أو دفعة غير دفعة allocation | أدخل حركة بلا batch أو بقيمة مختلفة عن allocation | يرفض CHECK إلزام batch في TransferIn أو FK الحدث المركب؛ لا يمكن لـ`MATCH SIMPLE` تخطي القيد بسبب NULL |
| سيريال أو بند مصدر بمنتج مختلف عن الحركة | `inventory_movements.product_id=P1` مع stock item أو batch أو serial أو source item بمنتج P2 | رفض composite FK الموافق؛ في بديل عدم إضافة `product_id` لا يوفر DB هذا الضمان ويظل Backend validation فقط |
| بند مستند التخريد بمنتج مختلف عن فحص المرتجع | اربط adjustment document بفحص منتجه P1، واملأ `return_inspection_id` في البند، ثم أدخل adjustment item بمنتج P2 | رفض FK الثلاثي `(adjustment_document_id, return_inspection_id, product_id)`؛ البند العام بلا فحص يبقى مسموحًا |
| إغفال فحص المرتجع في بند تخريد تابع لمستند مرتجع | مستند التخريد مرتبط بفحص، لكن بند التخريد يترك `return_inspection_id=NULL`، أو الحركة لا تشير إلى بند مطابق | يرفض Trigger التحقق المقترح؛ FK وحده قد يتجاوز الحالة بسبب NULL |
| محاولة ربط ReturnRestock بمستودع مستند غير موجود | افترض `sales_returns.destination_warehouse_id` رغم غيابه عن ERD | لا يوجد FK صالح لهذا العمود في التصميم الحالي؛ يجب رفض إضافته ضمن التنفيذ دون تحديث ERD وقرار معماري. التحقق المتاح يثبت location في الحركة فقط |
| تسوية غير معتمدة تنتج StockCountAdjustment | حالة settlement هي `PendingApproval` أو `Rejected` وقت الأمر | Backend transaction المقترحة ترفض بعد قفل الصف؛ وإذا فُرضت الكتابة المباشرة، Trigger بقفل الصف يرفض. FK وحده لا يرفضها |
| حذف فحص مصدر تخريد مرتجع | اربط مستند `ScrapDisposal` بالفحص ثم حاول حذف `return_inspections` | رفض `ON DELETE RESTRICT` على سلسلة المستند؛ الحركة تمنع حذف مستندها أيضًا |
| أكثر من رأس مصدر للحركة | أرسل `GoodsReceipt` مع `goods_receipt_id` و`invoice_id` معًا | رفض `ck_movement_one_source` |
| رأس لا يطابق نوع الحركة | `movement_type=SalesDispatch` مع `goods_receipt_id` فقط | رفض `ck_movement_one_source` أو `ck_movement_source_matches_type` |
| مرجع بند من نوع آخر أو بلا رأسه | حركة فاتورة تحمل `goods_receipt_item_id`، أو تحمل item دون header الموافق | رفض `ck_movement_item_pairs` أو composite FK |
| NULL في مصدر تفصيلي إلزامي أو حقول الحركة الأساسية | جرّب كل نوع من السبعة بلا بند/حدثه التفصيلي، أو `TransferIn` بلا batch/allocation، أو حركة بلا `stock_item_id` أو `warehouse_id` أو `quantity` | رفض `ck_movement_source_matches_type` أو `NOT NULL`؛ لا يسمح أي نوع بمصدر header-only |
| صفر أو إشارة كمية مخالفة | كمية صفر أو `SalesDispatch` موجب أو `GoodsReceipt` سالب | رفض `CHECK(quantity <> 0)` أو `ck_movement_quantity_direction`; `StockCountAdjustment` يقبل الإشارتين |
| نوع تعديل مع discriminator فارغ/خاطئ | `StockCountAdjustment` بمستند `ScrapDisposal` أو kind فارغ | رفض `ck_movement_source_matches_type` وFK المركب `MATCH FULL` |
| تكرار event ID للصف نفسه | أعِد إدخال `movement_event_id` ذاته | رفض قيد `UNIQUE`; على HTTP يجب أن تعيد `Command Inbox` النتيجة السابقة، لا اعتبار خطأ uniqueness هو replay response |
| Retry بعد فقد رد Commit | أعد إرسال الطلب بنفس `Idempotency-Key` ونفس payload | يعيد الخادم نتيجة الأمر الملتزم من inbox ولا يكرر الحركة؛ نفس المفتاح مع payload مختلف يُرفض. إرسال مفتاح جديد يعني أمرًا جديدًا ولا يمنعه `movement_event_id` القديم وحده |
| حركة مشروعة متكررة لنفس البند والدفعة | أرسل command جديدًا ومفتاحًا جديدًا مع نفس source item/batch لكن حدث عمل مستقل | يقبلها التصميم ما لم تتعارض مع حد كمية/حالة عمل؛ لا يوجد unique على item/batch وحدهما |
| استلام بند واحد موزع على دفعتين | أنشئ allocation مستقلًا للدفعة B1 وآخر لـB2 داخل نفس receipt item | يقبل صفا الحركة بمفتاحي allocation/event مختلفين؛ يمنع retry لكل allocation تكرار أثره |
| تجاوز المقبول عند جمع allocations | مجموع allocations يتجاوز `accepted_quantity` | يجب أن ترفضه معاملة تقفل receipt item أو Trigger محدد؛ FK/UNIQUE/CHECK الصف لا يجمع allocations |
| الصرف حسب السيناريو A | اصرف بنود تحويل مختلفة مرة لكل بند | ينجح. إذا اعتمد A لاحقًا، يضاف unique جزئي على transfer/item بعد التأكد من توافقه مع الكميات |
| الصرف حسب السيناريو B | اصرف البند نفسه في dispatchين مختلفين | ينجح فقط مع dispatch events مستقلة؛ يرفضه unique القديم على transfer/item. يتطلب مصدر dispatch مفصلًا أو قرارًا صريحًا بقبول ضعف تتبع الشحنات |

---

## 8. التوصية الفنية والقرارات التي ما زالت مفتوحة

### التوصية الفنية للمراجعة، وليست اعتمادًا

استكمال Typed FKs مع قيود المركبات التي يثبتها ERD فعليًا: انتماء receipt item إلى رأس الإيصال والتحويل نفسيهما، وانتماء settlement item إلى transfer نفسه، وربط return inspection بمنتج return item وسيرياله بمنتجه. التوصية بإضافة `product_id` المساعد إلى الحركة وربطه بكارت المخزون والدفعة والسيريال وبند المصدر. لحركة التخريد الناتجة عن فحص مرتجع، يكون FK الحامي عبر `stock_adjustment_documents.return_inspection_id` و`return_inspection_product_id` إلى الفحص، وبند التخريد إلى منتج الفحص، ثم FK الحركة إلى المستند والبند؛ هذا مسار Referential Integrity حقيقي بـ`ON DELETE RESTRICT`، مع Trigger لسد حالة NULL الاختيارية ومنع بند/حركة تخريد بلا inspection المرتبط. يظل إنشاء المستند المشترك ومصدره اقتراحًا غير موجود في ERD.

الجدول المشترك `stock_adjustment_documents` يحفظ التمييز التقني بـ`adjustment_kind` ومعرف مستقل لكل مستند، لكنه لا يوحد أو ينشئ دورة اعتماد أو صلاحيات؛ DDL لا يضيف `status` أو `approver`. إذا تطلب الجرد والتخريد دورة مستقلة، فاختيار جدول مشترك أو subtype/جدولين يظل قرارًا للمالك والمعماري.

لا يُعتمد `uq_transfer_in_per_receipt_item`: قد يمنع أكثر من حركة صحيحة عندما يُوزع المقبول من receipt item إلى batches مختلفة، كما يصف Workflow تسجيل حركة لكل دفعة مقبولة. يقترح DDL `stock_transfer_receipt_item_allocations` بهوية allocation ثابتة وFK يطابق receipt item والدفعة والكمية. حد مجموع allocations إلى المقبول يحتاج معاملة/قفل صف أو Trigger، وليس `CHECK` عاديًا.

لا يُعتمد `uq_transfer_out_per_transfer_item` قبل اختيار صاحب المشروع بين السيناريو (أ) والسيناريو (ب). إذا اختير B، فـ`stock_transfer_dispatches` و`stock_transfer_dispatch_items` يضيفان هوية شحنة وسجلًا تفصيليًا؛ دون ذلك تظل الحركات قابلة للتكرار لكن لا تحفظ السيارة/الوقت ككيان مصدر. إنشاءهما تغيير تصميم مقترح فقط.

### فجوات فنية وقرارات المالك/المعماري

1. **سياسة الصرف الجزئي:** اختيار A أو B؛ لا تفرض المذكرة قيدًا أو workflow تشغيليًا بدل المالك.
2. **مصدر التعديلات:** هل `StockCountAdjustment` في عجز التحويل يشير مباشرةً إلى `transfer_discrepancy_settlements`؟ ما مصدر الجرد العام والتخريد الداخلي؟ Q2.4 تبقى Pending Owner Approval، ولا تُستنتج منها صلاحية أو مبلغ.
3. **مصدر التخريد المرتبط بالفحص:** تأكيد أن `ScrapDisposal` لفحص `DamagedScrap/WriteOffScrap` يمر بمستند تخريد مرتبط بالفحص. لا تثبت الوثائق أن كل ScrapDisposal ناشئ عن return inspection.
4. **TransferIn multi-batch:** اعتماد سجل allocations، وهوية allocation، ومكان تنفيذ/قفل حد المجموع؛ ERD يثبت أحداث استلام متعددة، لكن لا يعرف جدول توزيع البند المقبول على batch.
5. **Idempotency:** تحديد نطاق ومدة الاحتفاظ بسجل الأمر ومكان حفظ `command_id` وchild event IDs، وربطها بسياسة `Idempotency-Key` في API. `Outbox` وحده لا يمنع تكرار command، و`UNIQUE(movement_event_id)` لا يحمي عند توليد مفتاح جديد لكل retry.
6. **Nullability ومطابقة المنتج:** اعتماد توصية `NOT NULL` لـ`stock_item_id`, `warehouse_id`, `quantity`، وإضافة `product_id` المساعد وتحديث ERD لاحقًا أو اختيار تحقق Backend مع فجوة DB معلنة. يلزم أيضًا حسم إن كان `unit_cost_at_movement` و`operator_person_id` يظلان nullable كما عُرّفا هنا.
7. **مستودع المرتجع:** لا يوجد حاليًا `sales_returns.destination_warehouse_id`. يلزم قرار معماري بشأن مصدر الموقع المقصود إذا كان مطلوبًا FK يتجاوز location الفعلي على الحركة؛ لا يضيفه هذا التصحيح.
8. **تطبيق ضمان الموافقة:** تحديد أن إنشاء حركة التسوية يمر حصرًا عبر Backend transaction، أو أن كل مسارات الكتابة تتطلب Trigger؛ مع اختبار التزامن حول تغيير حالة الاعتماد.
9. **اختبارات قاعدة البيانات:** جميع السيناريوهات أعلاه مخططة ولم تُنفذ. يلزم تنفيذها فقط ضمن مرحلة تطبيق/قاعدة بيانات معتمدة لاحقًا.

في جولة التوحيد الوثائقي اللاحقة، أُضيفت إلى ERD وسير المخزون والاختبارات خريطة مصادر وهوية حدث وحواجز التكرار المقترحة، مع إبقائها صراحةً غير معتمدة. كما وُثّق نطاق التوصيل اليدوي لـV1 منفصلًا عن قرار `Q7.2`. لا تمثل هذه المزامنة اعتمادًا معماريًا أو تجاريًا ولا إغلاقًا لـV3.3؛ تظل النقاط الواردة في قسم القرارات مفتوحة للمراجعة.
