# RIGX Interaction, Shopping UX & Global UI Polish — تقرير التسليم

**التاريخ:** 7 نوفمبر 2026  
**مصدر التكليف:** `RIGX_INTERACTION_SHOPPING_UX_TASK_2026-11-07(1).md`  
**Workspace:** `C:\Users\Abdalmlek\Downloads\DEV - Copy\HARDWARE STORE`  
**النطاق:** Storefront وAdmin داخل `apps/web` فقط. لم يُعدل Backend أو API Contracts أو Database أو Migrations أو Docker أو Dependencies.

---

## 1. Executive Summary

نُفذ تكليف `RIGX Interaction, Shopping UX & Global UI Polish` بالكامل داخل نسخة العمل، مع معالجة الأسباب الجذرية بدل ترقيع الأعراض. النتيجة الأساسية:

- توحيد كل Carousels/Rails في مكوّن واحد يدعم الأسهم في الاتجاهين، السحب بالماوس واللمس، إيقاف الحركة التلقائية عند Hover/Focus/Drag/إخفاء الصفحة، واحترام `prefers-reduced-motion`.
- إصلاح Header وMega Menu وAccount Menu وMobile Drawer: Viewport collision، Scroll داخلي، Outside click، Escape، Route close، Focus management، وARIA.
- إصلاح Admin Sidebar من جذره ليمتد بكامل الارتفاع مع Scroll داخلي واضح دون قص العناصر.
- تثبيت أبعاد بطاقات المنتجات والصور، إصلاح تضخم مصغّرات Gallery على الهاتف، وتحسين Hit-area مع الحفاظ على وسم «صورة توضيحية».
- توحيد Light/Dark عبر Semantic Tokens وإزالة المساحات الداكنة غير المقصودة، دون الاعتماد على Global mappings أعمى لكل `bg-slate-*`.
- تحويل Filters إلى Desktop Sidebar وMobile Drawer تعتمد فقط على السعر والفئات والماركات وSpec Facets القادمة من API، مع Draft/Apply/Reset/Close وChips قابلة للإزالة وClear All.
- فصل Add to Cart (Toast فقط) عن Buy Now (يستخدم Variant المحدد وينتقل إلى `/checkout` Demo).
- إضافة قسم `StorefrontDiscovery` تفاعلي واحد في منتصف Home يعتمد على بيانات Catalog الحقيقية، دون ادعاء توافق أو مخزون ودون تحويله إلى PC Builder.
- إصلاح Horizontal Overflow في صفحة المنتجات من جذره، وإزالة تكرار عنوان «الفلاتر» في Mobile Drawer.

**التحقق الفعلي النهائي:** `tsc --noEmit` = 0، Vitest = `28/28`، Next Build ناجح بـ22 Route، Playwright الكامل = `14 passed / 3 skipped by design`، وVisual QA = 36 لقطة After مقابل 12 Before مع `issues: 0` و`overflow: 0` في كل اللقطات.

---

## 2. Root Causes لكل مشكلة

### 2.1 Carousels / Product Rails
- **السبب الجذري:** كل شريط كان يملك منطق حركة منفصلًا؛ Brands كان Marquee بـCSS Animation، وCategory Carousel كان يعتمد على `setInterval` ومؤشر يدوي. لم يكن هناك Model موحد للـOffset أو Pointer Capture، ولم تكن هناك شروط Pause موحدة، فلم تكن الأسهم ثنائية الاتجاه ولا السحب ممكنًا، وكان Click بعد Drag يصل إلى الروابط.
- **الإصلاح:** مكوّن `storefront-rail.tsx` مشترك يحتفظ بـOffset منطقي من بداية السطر (Inline Start)، ويترجمه بصريًا حسب RTL/LTR. يملك Pointer drag/swipe مع Threshold، `setPointerCapture`، إيقاف Auto-scroll عند Hover/Focus/Drag/إخفاء الصفحة، واستماعًا لـ`prefers-reduced-motion`، وإخفاء/تعطيل الأسهم عند الحدود، ومنع Click بعد Drag.
- **عيب اكتُشف أثناء التحقق:** السحب فوق بطاقة رابط كان يطلق Native Link Drag فيمنع `pointermove`، فلم يتقدم الشريط. أُصلح بمنع `dragstart` على Rail Viewport (`onDragStart={event => event.preventDefault()}`)، وأُضيف اختبار Drag دائم في `foundation.spec.ts` يتحقق من تقدم المؤشر ومن عدم حدوث Navigation.

### 2.2 Header / Mega Menu / Dropdowns
- **السبب الجذري:** لوحة Mega Menu لم تكن مقيّدة بارتفاع Viewport مع Scroll داخلي، وإغلاق Outside click/Escape/Route change لم يكن موحدًا، ولم يكن Focus يعود إلى الزر بعد الإغلاق. Mobile Drawer كان يفتقر إلى إدارة Focus واضحة، وكانت أدوات Compare/Theme تزاحم الهيدر على الهاتف.
- **الإصلاح:** `max-height: calc(100dvh - 8rem)` مع `overflow-y: auto` و`overscroll-behavior: contain`، وإغلاق موحد مع إعادة Focus، وARIA (`aria-expanded`/`aria-controls`)، وتحويل الدرج إلى Dialog مع Focus trap/Escape، ونقل Theme إلى داخل الدرج وإخفاء Compare/Theme من الهيدر على الهاتف.

### 2.3 Admin Sidebar
- **السبب الجذري:** `.rigx-admin-sidebar` كان يحمل `position: relative; overflow: hidden`، ما قصّ العناصر الطويلة ومنع Scroll الداخلي الصحيح، ولم يكن العمود ممتدًا لكامل الارتفاع.
- **الإصلاح:** إزالة `overflow: hidden` وتحويل العمود إلى `sticky` بكامل الارتفاع (`h-dvh`) مع `overflow-y: auto` و`overscroll-behavior: contain` وScrollbar واضح، مع الحفاظ على Drawer منفصل للموبايل.

### 2.4 Product Cards / Images
- **السبب الجذري:** منصة الصورة لم تكن مثبتة الأبعاد، فكانت الصور ذات الأبعاد المختلفة تسبب Layout Shift وارتفاعات بطاقات متفاوتة. مصغّرات Gallery استخدمت `min-w-16` مع صورة داخلية، فتمددت إلى عرض الصورة الأصلي على الهاتف. بعض أزرار الإجراءات كانت أصغر من Hit-area المناسب.
- **الإصلاح:** تثبيت `aspect-ratio` وارتفاع منصة الصورة، `object-fit: contain`، تثبيت المصغّرات على `w-16 shrink-0 sm:w-full sm:min-w-0`، وتحسين Hit-area إلى 44px مع إبقاء وسم «صورة توضيحية».

### 2.5 Light Theme / Design System
- **السبب الجذري:** استخدام ألوان `bg-slate-*`/`text-slate-*`/`cyan-*` مباشرة في بعض الأسطح، ووجود Gradient مكرر في `:root` و`[data-theme]`، و`background-attachment: fixed` أنتج مساحات داكنة وشرائط غير مقصودة في Light.
- **الإصلاح:** Semantic Tokens (`--ds-*`) للأسطح والنص والحدود والـAccent، Tokens جديدة للـHero الفاتح والـProduct Stage، إزالة Gradient المكرر و`background-attachment: fixed`، وإصلاح `.rigx-button-secondary` ليعمل في الثيمين. لم يُستخدم Global mapping أعمى لكل `bg-slate-*`.

### 2.6 Filters
- **السبب الجذري:** شريط الأدوات كان يكرر Search/Category/Brand/Price، ولم يكن هناك فصل بين Draft والحالة المطبقة. التزامن مع URL استخدم `history.replaceState` + `PopStateEvent`، وهو لا يزامن `useSearchParams` في App Router، فبقيت الـRadio غير محددة. كما أن `.rigx-filter-trigger` بتعريف CSS غير طبقي كان يتجاوز `lg:hidden`، وعدم تطابق Breakpoint بين النموذج والأدوات أنتج Overflow بمقدار 55px عند 768/1024/1280.
- **الإصلاح:** Desktop Sidebar + Mobile Drawer، `activeFilters` كحالة محلية متزامنة فورًا، `router.replace(..., { scroll: false })` كمصدر حقيقة عند التنقل، Draft/Apply/Reset/Close، Chips قابلة للإزالة، Clear All، والفلاتر API-backed فقط (السعر/الفئات/الماركات/Spec Facets). أضيف Media Query يخفي `.rigx-filter-trigger` عند `lg`، ووحّد Breakpoint النموذج إلى `lg`، وأضيف `.rigx-catalog-sort` لتثبيت عرض الترتيب.

### 2.7 Add to Cart / Buy Now
- **السبب الجذري:** `addItem` كان يفتح Cart Drawer تلقائيًا، فلم يكن هناك Toast مستقل. لم يكن هناك Buy Now منفصل، ولم يكن اسم الـVariant يظهر في Toast أو ملخص Checkout.
- **الإصلاح:** Add to Cart يعرض Toast فقط ويبقي المستخدم في الصفحة، وBuy Now يستخدم الـVariant المحدد ويضيفه ثم ينتقل إلى `/checkout`، ويظهر `variantName` في Toast وملخص Checkout.

### 2.8 تكرارات وOverflow
- **تكرار عنوان «الفلاتر»:** رأس الدرج + رأس لوحة الفلاتر كانا يعرضان العنوان نفسه. أضيف `showHead={false}` داخل الدرج مع إبقاء الرأس في Sidebar.
- **تضخم مصغّرات Gallery:** `min-w-16` سمح للصورة بتحديد العرض. استُبدل بـ`w-16 shrink-0`.
- **Overflow صفحة المنتجات:** تفاعل `.rigx-filter-trigger` غير الطبقي مع `lg:hidden`، واختلاف Breakpoint بين النموذج والأدوات. أُصلح الاثنان معًا، وأصبح Overflow = 0 على كل المقاسات من 360 إلى 1920.

---

## 3. القرارات التصميمية وأسبابها

1. **مكوّن Rail واحد بدل ترقيع كل شريط:** يقلل الازدواجية، ويضمن سلوكًا متطابقًا في RTL/LTR، ويمنح Pointer/Keyboard/Reduced Motion مصدر حقيقة واحدًا.
2. **Offset منطقي من Inline Start:** يجعل اتجاه السحب والأسهم متسقًا في العربية والإنجليزية دون شروط `dir` متفرقة.
3. **Semantic Tokens بدل Classes لونية مباشرة:** يمنع المساحات الداكنة غير المقصودة ويجعل Light/Dark قابلين للصيانة.
4. **Desktop Sidebar + Mobile Drawer للفلاتر:** يطابق كثافة المعلومات على Desktop ويحافظ على مساحة المحتوى على الهاتف.
5. **Draft/Apply/Reset للفلاتر:** يمنع إعادة تحميل النتائج مع كل تغيير، ويجعل «تطبيق» و«إعادة ضبط» واضحين، مع Chips تعكس الحالة المطبقة.
6. **الفلاتر API-backed فقط:** لا فلاتر مخزون/خصم/جديد/مستعمل/Condition لعدم وجود بيانات مقابلة؛ إضافة أي منها تتطلب قرارًا وBackend/Migration مستقلين.
7. **Add to Cart = Toast فقط:** يحافظ على سياق التصفح، ويترك فتح السلة قرارًا للمستخدم.
8. **Buy Now منفصل:** مسار شراء مباشر باستخدام Variant المحدد، مع البقاء داخل Demo ولا ادعاء Order حقيقي.
9. **StorefrontDiscovery:** قسم تفاعلي واحد يربط بيانات Catalog الحقيقية بصريًا، دون تكرار Rails/Filters ودون ادعاء توافق أو مخزون.
10. **Breakpoint `lg` للفلاتر:** يمنع التداخل بين Sidebar وDrawer ويحل Overflow من جذره.
11. **إخفاء Compare/Theme على الهاتف:** تقليل ازدحام الهيدر، مع إبقاء Theme داخل Mobile Drawer.
12. **إظهار اسم الـVariant:** يجعل ما سيُضاف إلى السلة أو الـCheckout واضحًا قبل المتابعة.

---

## 4. القسم الجديد في منتصف Home

**المكوّن:** `apps/web/src/components/storefront-discovery.tsx`  
**الموضع:** بعد قسم الفئات/الماركات وقبل «أحدث المنتجات» في `storefront-home.tsx`.  
**البيانات:** أول 5 منتجات منشورة من Catalog API الحقيقي (`data.products.items.slice(0, 5)`)، مع الصورة والسعر والمواصفات المسجلة.  
**التفاعل:** قائمة اختيار منتج + لوحة تفاصيل تعرض الصورة والاسم والفئة والماركة والسعر والمواصفات، مع CTA لفتح صفحة المنتج.  
**السبب:** إضافة قيمة استكشافية مختلفة عن Rails (التي تعرض شرائح أفقية) وعن Filters (التي تفلتر القائمة)، دون تحويل الصفحة إلى PC Builder ودون اختراع توافق أو مخزون أو أسعار غير موجودة. لا يعرض القسم أي ادعاء توافق بين القطع.

---

## 5. الملفات الجديدة / المعدلة / المحذوفة

### جديدة (4)
- `apps/web/src/components/storefront-rail.tsx` — منطق Rail المشترك.
- `apps/web/src/components/storefront-discovery.tsx` — قسم Product Explorer.
- `apps/web/src/components/storefront-section-heading.tsx` — رأس قسم موحد.
- `apps/web/src/components/cart-toast.tsx` — Toast إضافة السلة.

### معدلة (15)
- `apps/web/src/app/globals.css` — Tokens، Rail، Discovery، Filters، Toast، Hero، Cards، Admin Sidebar، Media Queries.
- `apps/web/src/components/site-header.tsx` — Mega Menu، Account Menu، Mobile Drawer، Theme/Compare، ARIA/Focus.
- `apps/web/src/components/storefront-category-carousel.tsx` — استخدام Rail المشترك.
- `apps/web/src/components/storefront-home.tsx` — ربط Discovery واستبدال Marquee بـRail.
- `apps/web/src/components/admin-shell.tsx` — Sidebar ممتد مع Scroll داخلي.
- `apps/web/src/components/catalog-products-experience.tsx` — Sidebar/Drawer، Draft/Apply/Reset، Chips، URL Sync، Overflow.
- `apps/web/src/components/catalog-product-view.tsx` — Buy Now وVariant.
- `apps/web/src/components/product-detail-gallery.tsx` — تثبيت المصغّرات على الهاتف.
- `apps/web/src/components/cart-provider.tsx` — Toast وvariantName وعدم فتح Drawer تلقائيًا.
- `apps/web/src/components/checkout-experience.tsx` — إظهار Variant في ملخص Checkout.
- `apps/web/src/components/app-chrome.tsx` — ربط CartToast.
- `apps/web/src/lib/i18n.tsx` — مفاتيح AR/EN الجديدة.
- `apps/web/tests/e2e/foundation.spec.ts` — Rail + Mega Menu/Mobile Drawer.
- `apps/web/tests/e2e/storefront.spec.ts` — الفلاتر الجديدة وDrawer.
- `apps/web/tests/e2e/cart-demo.spec.ts` — Toast/Buy Now/390px.

### وثائق
- `PROGRESS.md`، `NEXT_TASK.md`، `MODEL_HANDOFF.md` — تحديث الحالة ونقطة الاستمرارية.
- `RIGX_INTERACTION_SHOPPING_UX_REPORT_2026-11-07.md` — هذا التقرير.

### محذوفة
- لا ملفات مصدر محذوفة.

**ملاحظة:** `apps/web/package.json` و`apps/web/playwright.config.ts` لم يتغيرا (Hash مطابق لنسخة الرجوع)؛ لم تُضف أي Dependency.

---

## 6. سلوك Carousel / Menu / Filters / Cart / Buy Now قبل وبعد

| المجال | قبل | بعد |
|---|---|---|
| Carousel/Rail | Marquee أو Interval منفصل، أسهم غير مكتملة، لا Drag/Swipe، لا Pause موحد | Rail مشترك، أسهم ثنائية الاتجاه، Drag/Swipe، Pause عند Hover/Focus/Drag/إخفاء الصفحة، Reduced Motion، منع Click بعد Drag |
| Mega Menu | ارتفاع غير مقيّد، إغلاق غير موحد | `max-height` مع Scroll داخلي، Outside click/Escape/Route close، إعادة Focus، ARIA |
| Mobile Drawer | ازدحام أدوات، إدارة Focus محدودة | Drawer لمسي مع Focus trap/Escape، Theme داخل الدرج، إخفاء Compare/Theme من الهيدر |
| Admin Sidebar | `overflow: hidden` يقص العناصر | Sticky بكامل الارتفاع مع Scroll داخلي واضح |
| Product Cards | أبعاد صور متغيرة وLayout Shift | منصة صورة مثبتة، `object-fit: contain`، بطاقات متسقة |
| Filters | تكرار أدوات، URL لا يزامن `useSearchParams`، Desktop فقط | Sidebar/Drawer، Draft/Apply/Reset/Close، Chips/Clear All، API-backed فقط، URL متزامن |
| Add to Cart | يفتح Drawer تلقائيًا | Toast فقط ويبقى المستخدم في الصفحة |
| Buy Now | غير موجود كمسار منفصل | يستخدم Variant المحدد ويذهب إلى `/checkout` Demo |
| Light Theme | مساحات داكنة وشرائط غير مقصودة | Semantic Tokens، Hero فاتح بورق دافئ، لا `background-attachment: fixed` |

---

## 7. الاختبارات التي شُغلت فعلًا ونتائجها

| الاختبار | الأمر | النتيجة |
|---|---|---|
| TypeScript | `npx tsc --noEmit` داخل `apps/web` | **نجح**، بلا Output |
| Vitest | `npm test` | **8 ملفات / 28 اختبارًا ناجحًا**، 2.47s |
| Next Build | `npm run build` | **نجح**، Next.js 16.4.5، 22 Route |
| Playwright الكامل | `npx playwright test` مع `API_ORIGIN=http://127.0.0.1:8085` و`NEXT_PUBLIC_PREVIEW_MODE=true` و`E2E_WEB_PORT=3006` | **14 passed / 3 skipped by design**، 28.4s |
| Visual QA After | سكربت Playwright مؤقت (حُذف بعد التشغيل) | **36 لقطة، issues: 0، overflow: 0** |
| Reduced Motion | ضمن Visual QA | `transition-duration = 1e-05s`، الـTransform بقي ثابتًا بعد 4.6s |
| Keyboard Rail | ضمن Visual QA | `data-active-index` تغير من 0 إلى 1 بعد ArrowLeft |
| Drag / Swipe | ضمن Playwright `foundation.spec.ts` | `data-active-index` تغير من 0 إلى 1 بعد سحب 170px، ولم يحدث Navigation. التحقق اليدوي المنفصل أكد `is-dragging=true` أثناء السحب والـTransform `matrix(1,0,0,1,172,0)` |

**الاختبارات المتجاوزة بالتصميم (3):**
1. `catalog.spec.ts` — يحتاج `E2E_DB_CONTAINER` و`E2E_DB_NAME` و`E2E_DB_USER` (PostgreSQL test DB معزولة).
2. `customer-identity.spec.ts` — يحتاج نفس متغيرات PostgreSQL لإيصال رموز البريد الوهمية.
3. `storefront.spec.ts` — اختبار Login Security يحتاج `E2E_PREVIEW_OWNER_USERNAME` و`E2E_PREVIEW_OWNER_PASSWORD`.

لم تُشغّل هذه الاختبارات الثلاثة ولم تُدّع نجاحها. الاختبارات الأخرى (Foundation، SEO، Storefront، Comparison، Cart) شُغلت فعليًا ونجحت.

---

## 8. مصفوفة Screenshots

**Before:** 12 لقطة في `.backups/visual-qa-interaction/before/`.  
**After:** 36 لقطة في `.backups/visual-qa-interaction/after/` مع `qa-summary.json`.

| المجموعة | العدد | الحالات |
|---|---:|---|
| Desktop 1440 | 14 | Home AR/EN Light/Dark، Mega Menu، Products Light EN/Dark AR، Product Detail، Compare، Cart Drawer، Cart، Checkout، Login، Register |
| Mobile 390 | 7 | Home Light/Dark AR، Products، Filters Drawer، Product Detail، Cart Drawer، Login Dark AR |
| Responsive Matrix | 12 | Home + Products عند 360/430/768/1024/1280/1920 |
| Reduced Motion | 2 | Home 1440 و390 |
| Keyboard | 1 | Home 1440 مع تركيز Rail |

- كل اللقطات After سُجلت بـ`overflow: 0`.
- Full-page استُخدمت لتتبع الخلفية حتى Footer، مع تحميل الصور المؤجلة قبل الالتقاط.
- اللقطات لا تحتوي أسرارًا أو بيانات دخول؛ صفحات Auth التُقطت كزائر غير مسجل.

---

## 9. القيود التي لم يمكن التحقق منها

1. **Admin/Account بصريًا بجلسة مصادقة حقيقية:** لم يُنفذ لعدم استخدام بيانات دخول أو قراءة `.env.preview.local`. تم التحقق من Login gate وShell code وBuild فقط، ولم يُسجل Admin كـVisual QA Passed.
2. **الاختبارات الثلاثة المتجاوزة:** تحتاج PostgreSQL test DB معزولة وCredentials غير متاحة في هذه الجلسة.
3. **Touch على جهاز فعلي:** سُلّط الضوء على Mobile Context مع `hasTouch` وPointer Events، لكن لم يُختبر على جهاز لمسي فيزيائي.
4. **Git Diff:** لا يوجد Git repository في Workspace، لذا الاعتماد على مقارنة Hash مع نسخة الرجوع `.backups/rigx-interaction-ux-2026-11-07-pre/`.
5. **صور المنتجات:** Placeholders من Seed وليست عيب واجهة؛ وسم «صورة توضيحية» محفوظ.
6. **Checkout:** Demo أمامي فقط؛ لا Order أو Stock أو Payment أو Shipping حقيقي.

---

## 10. فحص الأمان وعدم لمس الأصل / Backend / DB

- **الأصل المحمي:** `C:\Users\Abdalmlek\Downloads\DEV\HARDWARE STORE` — لم تُنفذ عليه أي كتابة. فحص Read-only أظهر **0 ملفات معدلة اليوم** داخله.
- **ملفات البيئة:** `.env` و`.env.example` و`.env.preview.example` و`.env.preview.local` — الهاش مطابق بين الأصل ونسخة العمل (**SAME** للأربعة). لم تُقرأ محتوياتها ولم تُطبع أي أسرار.
- **Backend/API:** **0 ملفات معدلة اليوم** تحت `apps/api` (باستثناء مخرجات Build المتجاهلة).
- **قاعدة البيانات/Migrations/Docker:** لا تغييرات؛ لم تُشغّل Migration أو Seed أو Volume أو `prune` أو `compose down -v`.
- **Dependencies:** `apps/web/package.json` و`apps/web/playwright.config.ts` مطابقان لنسخة الرجوع؛ لا تثبيت جديد.
- **الأسرار:** لا Secrets أو API Keys أو Credentials أو رموز جلسات في الكود أو التقرير أو اللقطات أو الأرشيف.
- **الأرشيف:** يستبعد `.env` و`.env.preview.local` الفعليين و`node_modules` و`.next` و`bin` و`obj` و`.git` و`.backups` و`test-results` و`playwright-report` و`*.log` و`tsconfig.tsbuildinfo` وملفات QA المؤقتة.

---

## 11. أرشيف تسليم نظيف

- **اسم الأرشيف:** `HARDWARE_STORE_RIGX_INTERACTION_UX_2026-11-07.zip`
- **الموضع:** `.backups/HARDWARE_STORE_RIGX_INTERACTION_UX_2026-11-07.zip`
- **عدد الملفات:** `258`
- **الحجم غير المضغوط:** `2,557,871` بايت (`2.44` MB)
- **حجم الأرشيف:** `701,059` بايت (`0.67` MB)
- **SHA256:** `5DA4F09F022ECAF7351BD1247551342B4BE89EAF2F5B3E7D99BACA2B25790D50`

**التحقق:** فُتح الأرشيف وقرأت كل مدخلاته للتأكد من سلامة الضغط، وفحصت القائمة ضد أسماء محظورة (`.env` الفعلي، `node_modules`، `.next`، `bin`، `obj`، `.git`، `.backups`، ملفات QA المؤقتة) — **0 مدخلات محظورة**.

**ملاحظة شفافية:** هذا التقرير مستند تسليم خارج الأرشيف، حتى تبقى بصمة الأرشيف قابلة للتحقق دون مرجعية ذاتية. الأرشيف يضم المصدر والوثائق التشغيلية (`PROGRESS.md`، `NEXT_TASK.md`، `MODEL_HANDOFF.md` وغيرها)، بينما التقرير وملف `<zip>.sha256` يجاوران الأرشيف في `.backups/`.

---

## 12. نقطة الاستمرارية التالية

- **الخطوة المباشرة:** مراجعة المالك للنتيجة على `http://127.0.0.1:3006` (AR/EN، Light/Dark، الهاتف/Desktop)، ثم اعتماد تكليف Interaction/Shopping UX.
- **لا تبدأ** M5 أو M3.2f أو M3.3 أو تحويل Cart/Checkout إلى Order حقيقي قبل قرار نطاق واعتماد منفصل.
- **عند الاعتماد:** يمكن فتح مهمة منفصلة لتحويل Demo Cart إلى Cart/Order عبر Backend، مع تصميم Transactions وStock Reservation وIdempotency واختبارات PostgreSQL — وهي خارج نطاق هذه الدفعة.
- **القيود المتبقية:** Admin/Account Visual QA بجلسة مصرح بها، والاختبارات الثلاثة المتجاوزة، وTouch على جهاز فيزيائي.
- **أوامر الاستئناف:**
  ```powershell
  # من apps/web
  $env:API_ORIGIN="http://127.0.0.1:8085"
  $env:NEXT_PUBLIC_PREVIEW_MODE="true"
  npx tsc --noEmit
  npm test
  npm run build
  $env:E2E_WEB_PORT="3006"
  npx playwright test
  ```
- **مراجع الاستئناف:** `PROGRESS.md`، `NEXT_TASK.md`، `MODEL_HANDOFF.md`، وهذا التقرير.
