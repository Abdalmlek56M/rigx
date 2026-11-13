---
name: hardware-store-visual-qa
description: تحقق من Storefront وAdmin المرئيين والتفاعليين، بما في ذلك استمرارية الخلفية حتى Footer، Responsive وRTL/LTR وLight/Dark وReduced Motion وConsole/Network، باستخدام Browser أو Playwright.
---

# HARDWARE STORE / RIGX Visual QA

استخدم هذه المهارة عند أي تغيير واجهة مرئي، أو إصلاح Global CSS/Chrome/Footer، أو قبل قبول Storefront/Admin. افحص النتيجة الفعلية في المتصفح؛ Build الناجح وحده لا يثبت صحة التصميم.

## الإجراء

1. سجّل توفر Browser المدمج. استخدمه للفحص التفاعلي عندما يكون متاحًا، واستخدم Playwright لاختبارات E2E/Responsive القابلة للتكرار. اذكر سبب أي Fallback.
2. التقط Baseline قبل التغيير وبعده عند وجود مرجع أو تغيير بصري واسع. لا تستبدل Before Screenshots بصور جديدة بعد الإصلاح.
3. اختبر على الأقل 390px و1440px؛ أضف 360/430/768/1024/1280/1920 حسب التغيير المتأثر.
4. اختبر العربية RTL والإنجليزية LTR، وLight/Dark، وKeyboard Focus. اختبر Touch/Pointer عند وجود تفاعل يعتمد عليهما.
5. **الخلفية المحيطية والفوتر:** على صفحة قصيرة وأخرى طويلة، تحقق أن الـAmbient Canvas يستمر من أعلى الصفحة حتى الفوتر، وأن `.rigx-site-footer` شفاف/متدرج (`rgba(0,0,0,0)` أو Surface مقصود) لا كتلة داكنة تحجب الطبقة. افحص `footerBg` و`footerBgImage` وارتفاع الفوتر و`position: fixed` للـVisual Engine، وافحص stacking contexts وpseudo-elements وfixed layers عند ظهور فجوة.
6. **الصفحات الطويلة:** استخدم `fullPage: true` وتأكد من عدم انقطاع الـGrid/Glow أو تكرارها، ومن ثبات الفوتر أسفل المحتوى.
7. كرر اختبار الخلفية على Home وProduct/Listing وعلى Admin shell إذا تأثرت طبقة التطبيق المشتركة. لا تجعل Admin نسخة تسويقية من Storefront.
8. تحقق من Console Errors وFramework Overlays وBlank Page وNetwork failures والطلبات غير المتوقعة. افصل الأخطاء المتوقعة (مثل `401` لجلسة الزائر) عن الأخطاء الفعلية.
9. افحص Overflow الأفقي، القص، Overlap، Loading اللانهائي، الصور المكسورة، تباين النص، وارتفاع/مسافات Footer.
10. فعّل `prefers-reduced-motion` وتأكد من تعطيل Tilt/Transitions غير الضرورية، ومن توقف Pointer Glow وRAF/Observers/Listeners بعد إخفاء الصفحة أو فك المكون.
11. **Storefront وAdmin:** Admin محمي؛ لا تقرأ `.env` ولا تكشف بيانات دخول. إن لم تتوفر جلسة Admin مصرح بها، وثّق ما تحقق (Login gate، Shell code، Build) بوضوح ولا تدّعِ Visual QA كاملًا للإدارة.
12. افحص أن مؤشرات المراحل الداخلية (M4 أو ما شابه) وشارات Demo/Local غير ظاهرة للعميل في Production، وأن Badge النسخة التجريبية يظهر فقط عند تفعيل Preview Mode.
13. سجّل مسارات Screenshots ونتائج Browser/Playwright الفعلية. لا تكتب Screenshots أو التقارير داخل المستودع ما لم يطلب المستخدم ذلك.

## مراجع

- [visual-checklist.md](references/visual-checklist.md)
- [upstream-sources.md](references/upstream-sources.md)
