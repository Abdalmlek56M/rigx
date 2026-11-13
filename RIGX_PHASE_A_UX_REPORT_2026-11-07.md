# RIGX — Phase A UX Report (7 نوفمبر 2026)

## 1. الهدف والنطاق

تحويل مراجعة Phase A إلى منتج متماسك فعليًا، لا قائمة تعديلات منفصلة:

- Header واحد بلا تكرار، بحث فوري حقيقي من Catalog API، وMega Menu أقسام هرمي.
- صور منتجات محلية موثقة، وFallback محايد عند غياب صورة دقيقة.
- توحيد أسطح البطاقات والمعرض والسلة وThumbnails الإدارة.
- إعادة بناء Home/Footer/Login بلا عناصر تجريبية أو وعود غير حقيقية.
- تعطيل كل مداخل المقارنة مع الإبقاء على الكود والبيانات.
- Admin Row Actions مشتركة، وشاشة Prototype لترتيب محتوى الصفحة الرئيسية.
- توحيد Light/Dark عبر Semantic Tokens وتنظيف المصطلحات العربية.

**لم يتم تعديل Backend أو قاعدة البيانات أو إضافة Seed products أو فتح M5.**

---

## 2. الأسباب الجذرية التي عولجت

| المشكلة | السبب الجذري | المعالجة |
|---|---|---|
| Header مزدحم وتكرار Language/Account | Utility strip منفصل + نسخ عناصر في Desktop/Mobile | حذف Utility strip، وإبقاء عناصر التحكم مرة واحدة (Desktop من `lg`، والدرج تحتها) |
| Ctrl+K بلا وظيفة | اختصار شكلي بلا Search API | حذف الاختصار، وربط البحث بـ `getCatalogProducts` مع Debounce 220ms وStale guard |
| Mega Menu يبعد الأقسام الفرعية | قائمة مسطحة موزعة على عرض الشاشة | لوحة من عمودين: شجرة الأقسام + تفاصيل القسم المحدد بجانبه |
| صور المنتجات Placeholder | قاعدة البيانات تحتوي صورًا مولدة وردية/بنفسجية | `demo-media.ts` + `ProductImage` يفضلان الصور المحلية الموثقة وFallback محايد |
| Comparison مفتوحة بلا اكتمال | مداخل متفرقة في Header/Mega/Cards/Home | `comparisonAvailable=false` + Badge `قريبًا` غير قابل للنقر، مع إبقاء الصفحة والكود |
| Row Actions مقطوعة/متعددة | `<details>` أصلية داخل جداول `overflow` | Popover مشتركة Portal، One-open guard، خارج/Escape/Route/Scroll/Resize، Focus restore |
| Home فيه Trust strip منفصل وعدادات | طبقة عرض قديمة | إزالة الشريط والعداد والنقطة الخضراء، وكتابة دليل اختيار عملي من 3 خطوات |
| Footer يعرض Live/Back-to-top وهمي | نصوص وأزرار بلا مصدر | أربعة أعمدة حقيقية فقط، بلا Newsletter/Social/Address/Hours |
| Login Aside غير ضروري | بطاقة تسويقية داخل شاشة الدخول | إزالة Aside، وتوسيط النموذج، وإبقاء Forgot/Register/Login فقط |
| مصطلحات عربية غير متسقة | خلط التصنيف/الماركة/المتغير | الأقسام، العلامات التجارية، إصدار المنتج حسب السياق |

---

## 3. ما تم تنفيذه

### Header والبحث
- شعار + بحث + سلة + Theme/Account/Language مرة واحدة.
- البحث يعرض صورة المنتج واسمه وSKU والقسم والسعر، مع حالات Loading/No results/Error وإعادة المحاولة.
- Keyboard: Arrow Up/Down/Home/End/Escape، `aria-activedescendant`، و`aria-expanded`.
- إغلاق النتائج عند النقر خارجها أو تغيير المسار، ومنع تداخل Mega Menu مع البحث.
- Mobile Drawer يحتوي الأقسام واللغة والثيم والحساب، مع Focus Trap وScroll Lock.

### Mega Menu
- زر `كل الأقسام` بدل `القطع والمكونات`.
- شجرة أقسام حقيقية من Catalog API، واختيار القسم يعرض أبناءه في نفس اللوحة.
- Hover/Click/Keyboard، Escape، Outside click، Route close، وإعادة التركيز.
- افتراضيًا يفتح أول قسم لديه أقسام فرعية بدل قسم فارغ.

### الصور والأسطح
- 9 موديلات بصور محلية موثقة + Samsung بصورتين.
- NVIDIA RTX 5080 وIntel 265K وNZXT Kraken 360 RGB تستخدم Fallback فئوي محايد.
- `ProductImage` واحد للبطاقات والبحث والسلة والـHero والـDiscovery وThumbnails.
- معرض المنتج يدعم الصور المتعددة والـLightbox والـKeyboard، ويستخدم نفس الصور.
- Brand rail أصبح Typographic Tiles بأسماء العلامات الحقيقية بدل شعارات Placeholder.
- Rail: أسهم في منتصف الجانبين، حركة بطاقة واحدة، RTL/LTR، Drag/Swipe، بلا Progress وهمي.

### Home / Footer / Login
- Hero بلا عداد أو Caption أو Live dot.
- قسم اختيار عملي: ابدأ بالقسم → راجع المواصفات والسعر → أضف للسلة.
- إزالة وعد المقارنة من Home.
- Footer: `تصفح الأقسام`, `حسابك`, `المساعدة والمعلومات`, `RIGX` مع روابط حقيقية فقط.
- Login: Title/Lead/Fields/Forgot/Register/Login فقط، بلا Owner أو Session details.

### Admin
- Row Actions مشتركة في Catalog Products وTaxonomy، مع اختبارات.
- شاشة `Admin → المتجر → محتوى الصفحة الرئيسية`:
  - ترتيب/إظهار/إخفاء 7 أقسام.
  - تعديل عنوان/وصف/نوع/حد عناصر شرائط العلامات والأقسام.
  - ترتيب/إخفاء عناصر الشريط.
  - Auto-save في `localStorage` + تنبيه واضح بأنها Prototype.
- تنظيف أسطح الجداول/الحوارات/محرر المنتج/Dashboard إلى Tokens.
- الحفاظ على Sidebar الداكن في الثيمين كسطح تحكم مقصود.

---

## 4. الملفات الرئيسية المتغيرة

### Header / Navigation / Footer
- `apps/web/src/components/site-header.tsx`
- `apps/web/src/components/site-header.test.tsx`
- `apps/web/src/components/site-chrome.tsx`
- `apps/web/src/components/admin-shell.tsx`

### Storefront / Media
- `apps/web/src/components/product-image.tsx`
- `apps/web/src/lib/demo-media.ts`
- `apps/web/src/components/product-detail-gallery.tsx`
- `apps/web/src/components/catalog-product-card.tsx`
- `apps/web/src/components/storefront-home.tsx`
- `apps/web/src/components/storefront-rail.tsx`
- `apps/web/src/components/storefront-discovery.tsx`
- `apps/web/src/components/cart-line.tsx`
- `apps/web/src/components/catalog-product-view.tsx`
- `apps/web/public/demo/hardware/MANIFEST.md`

### Home Content Prototype
- `apps/web/src/lib/home-layout.ts`
- `apps/web/src/lib/home-layout.test.ts`
- `apps/web/src/components/home-layout-provider.tsx`
- `apps/web/src/components/home-content-admin.tsx`
- `apps/web/src/app/admin/home-content/page.tsx`
- `docs/storefront/HOME_CONTENT_PROTOTYPE.md`

### Admin / Design System
- `apps/web/src/components/row-action-menu.tsx`
- `apps/web/src/components/row-action-menu.test.tsx`
- `apps/web/src/components/catalog-admin-v2.tsx`
- `apps/web/src/components/catalog-taxonomy-admin.tsx`
- `apps/web/src/components/catalog-product-editor.tsx`
- `apps/web/src/components/admin-dashboard.tsx`
- `apps/web/src/components/ui.tsx`
- `apps/web/src/app/globals.css`

### Comparison / i18n
- `apps/web/src/lib/comparison.ts`
- `apps/web/src/components/comparison-ui.tsx`
- `apps/web/src/lib/i18n.tsx`

### Login / README
- `apps/web/src/components/login-experience.tsx`
- `README.md`

---

## 5. الاختبارات والتحقق الفعلي

| التحقق | النتيجة |
|---|---|
| `npx tsc --noEmit` | ✅ نجح |
| `npx vitest run` | ✅ 10 ملفات / 40 اختبارًا |
| `npm run build` | ✅ نجح — 23 Route |
| Header Vitest | ✅ Search/Debounce/Keyboard/No-results/Mega/Escape/Mobile/Admin/Auth |
| RowActionMenu Vitest | ✅ Open/Escape/Focus/Action/Outside/One-open |
| HomeLayout Vitest | ✅ Default/Parse/Order/Hidden/Limit/Storage key |
| Playwright Public QA | ✅ AR/RTL + EN/LTR، Light/Dark، 390×844 و1440×900 |
| Playwright Admin QA | ✅ Catalog + Row Actions + Home Content + Reorder |
| Console Errors | ✅ 0 (مع استثناء 401 المتوقع لـ identity/me قبل الدخول) |
| Network Failures | ✅ لا فشل وظيفي؛ فقط `identity/me` 401/aborted قبل الدخول |
| Images | ✅ لا صور ناقصة في Home، وصورة السلة هي نفس صورة البطاقة |

### لقطات After
- [Mobile Home](docs/review/phase-a-2026-11-07/mobile-home-dark.png)
- [Mobile Drawer](docs/review/phase-a-2026-11-07/mobile-drawer-dark.png)
- [Mobile Product](docs/review/phase-a-2026-11-07/mobile-product-dark.png)
- [Brands Rail](docs/review/phase-a-2026-11-07/brands-dark.png)
- [Mega Menu](docs/review/phase-a-2026-11-07/mega-menu-dark.png)
- [Instant Search](docs/review/phase-a-2026-11-07/search-dark.png)
- [Cart Dark](docs/review/phase-a-2026-11-07/cart-dark.png)
- [Cart Light](docs/review/phase-a-2026-11-07/cart-light.png)
- [Login](docs/review/phase-a-2026-11-07/login-dark.png)
- [Admin Catalog Dark](docs/review/phase-a-2026-11-07/admin-catalog-dark.png)
- [Admin Row Actions](docs/review/phase-a-2026-11-07/admin-row-actions-dark.png)
- [Admin Catalog Light](docs/review/phase-a-2026-11-07/admin-catalog-light.png)
- [Admin Home Content](docs/review/phase-a-2026-11-07/admin-home-content-dark.png)

---

## 6. قرارات مهمة

1. **Comparison متوقفة بالكامل** عبر `comparisonAvailable=false`؛ لا روابط `/compare` ولا تعديل حالة مقارنة. الصفحة والكود باقيان للاستئناف.
2. **الصور المحلية تتغلب على صور DB** في وضع الـPreview فقط، لأن صور DB الحالية Placeholders. لا تُرفع الصور إلى DB.
3. **Brand rail نصي** لأن شعارات العلامات في DB Placeholders مولدة.
4. **Mega Menu ثنائي اللوحة** يحافظ على العلاقة بين القسم وأبنائه بدل توزيعها على عرض الشاشة.
5. **Homepage Content Prototype** localStorage فقط، مع تنبيه ظاهر ومتطلبات CMS موثقة.
6. **Header Breakpoint** `lg`: عناصر التحكم في الصف الرئيسي من `lg`، والدرج يملكها تحته، بلا ازدواج.
7. **Light `--ds-on-accent`** أصبح داكنًا على البرتقالي لتحسين التباين بدل الأبيض منخفض التباين.

---

## 7. القيود والملاحظات

- **Homepage Content** غير دائم: لا API/DB ولا صلاحيات، والتغيير في متصفح المستخدم فقط. المتطلبات الدائمة في `docs/storefront/HOME_CONTENT_PROTOTYPE.md`.
- **3 موديلات** بلا صورة مطابقة مؤكدة: NVIDIA RTX 5080، Intel 265K، NZXT Kraken 360 RGB. Fallback فئوي مقصود.
- **Samsung 990 PRO** يستخدم صورة سلسلة/سعة مختلفة؛ موثقة في `MANIFEST.md` ولا تُقدَّم كصورة SKU دقيقة.
- **Brand logos** لا تُعرض من DB لأنها Placeholders؛ الأسماء النصية هي المصدر الحقيقي حاليًا.
- **Phase B:** الأدوات مثبتة (22 Skill + 4 Agents)، لكن 21st MCP ما زال 401 حتى Restart للخدمة مع `TWENTYFIRST_TOKEN`، وContext7 لا يزال DNS failure.
- **ملاحظة أمان:** استخدم QA الإداري جلسة Preview Owner محلية للتحقق البصري فقط. لم تُطبع أو تُسجل أو تُضمَّن أي Credentials في التقرير أو اللقطات أو الأرشيف. هذا استخدام محلي للـQA وسيُستبدل بجلسة اختبار آمنة في Phase C.
- لم يتم تعديل `.env*` أو قواعد البيانات أو Volumes أو النسخة الأصلية المحمية.
- لا Git repository؛ التسليم ZIP + SHA256.

---

## 8. الخطوة التالية

1. إغلاق Phase B: إعادة تشغيل OpenCode لتحميل `TWENTYFIRST_TOKEN`، والتحقق من 21st MCP، وإعادة فحص Context7.
2. **M3.2f — First Owner Bootstrap** وفق قيود Secrets/Idempotency/Concurrency.
3. **M3.3 — Operational Identity & RBAC** مع API/UI/Audit/Tests.
4. **لا تبدأ M5.** تبقى بوابة M5 معلقة على Q2.1–Q2.5 وQ6.3 وQ8.7.

---

**الحالة:** Phase A UX مكتملة ومختبرة. لم تُفتح M5. لم تُعدّل النسخة الأصلية المحمية.
