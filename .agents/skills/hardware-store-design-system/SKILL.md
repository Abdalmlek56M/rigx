---
name: hardware-store-design-system
description: طبق وطور نظام RIGX البصري الموحد عبر Storefront وAdmin بالاستفادة من مراجع المالك، مع Semantic Tokens وخلفية محيطية مشتركة وRTL/LTR وثيمات وحركة مسؤولة.
---

# HARDWARE STORE / RIGX Design System

اقرأ [نظام التصميم والتوكنز](references/design-tokens.md) قبل أي تغيير بصري واسع. افحص المكونات والطبقات الموجودة أولًا، وطور النظام الحالي بدل إنشاء Design System موازٍ.

## الهوية المعتمدة

RIGX هو Graphite/Obsidian مع Orange Accent، لكنه ليس نسخًا حرفيًا لأي Demo:

- **Light:** ورق دافئ (Warm Paper) مع Graphite Ink وOrange للتفاعل.
- **Dark:** Obsidian/Graphite مع Ambient Glow برتقالي خفيف وGrid تقني.
- **Storefront وAdmin:** نظام واحد؛ Admin أكثر كثافة وتحكمًا لكنه يستخدم نفس الـTokens والخطوط والحالات. Storefront أكثر تعبيرًا، وAdmin عملي كثيف المعلومات دون Hero تسويقي أو مؤثرات مشتتة.
- **المراجع HTML:** مصادر مبادئ Layout وHierarchy وMotion فقط. لا تُنسخ Palettes أو النصوص أو الأرقام أو الوعود التجارية، ولا تُجمع اتجاهات المراجع المختلفة في صفحة واحدة.
- **أولوية المعلومة:** وضوح المنتج والمواصفات والسعر وسهولة الشراء يسبق التأثير البصري.

## قواعد التنفيذ

1. المنتج والمواصفات والسعر هي محور Hierarchy؛ لا تستخدم بطاقات Dashboard أو Eyebrow/Pills زخرفية بلا وظيفة.
2. استخدم Semantic Tokens في `globals.css` (`--ds-*`) بدل تكرار ألوان Hex داخل المكونات. الأزرق/البنفسجي/الـCyan ليست ألوان الهوية؛ أي utility قديم من `blue-*`/`cyan-*` يجب أن يُربط بالـAccent أو يُستبدل.
3. استخدم الخصائص المنطقية (`start/end`, `ms/me`, `ps/pe`) لضمان RTL/LTR، وثبت الـSKU والأرقام والأكواد التقنية باتجاه LTR.
4. Dark Mode يغطي الخلفية والسطح والنص والحدود والحالات، لا الخلفية وحدها. اختبر التباين فعليًا.
5. الخلفية المحيطية مسؤولية طبقة تطبيق مشتركة واحدة. افحص `AppChrome` و`SiteChrome` و`SiteFooter` و`layout.tsx` و`globals.css` قبل إضافة أي Layer. اجعلها مستمرة من أعلى الصفحة حتى الفوتر: Grid + Radial Glows + Pointer Glow، ولا تكرر Gradient في `:root` أو في مكونات منفصلة، ولا تجعل الفوتر كتلة داكنة تحجب الطبقة.
6. الفوتر شفاف أو Surface متدرج فوق نفس الـAmbient Canvas، مع Accent Rule وحدود وتباين يضمنان القراءة.
7. الحركة تشرح استجابة أو تغير حالة. احترم `prefers-reduced-motion`، واستخدم Pointer Effects على الأجهزة الدقيقة فقط، ونظّف الـListeners والـRAF عند تغيير المسار أو إخفاء التبويب.
8. حافظ على Focus ظاهر، تباين AA، أهداف لمس 40–44px، وحالات Loading/Empty/Error حقيقية.
9. لا تعرض مؤشرات مراحل داخلية أو بيانات Demo للعميل في Production؛ استخدم Badge شرطيًا مثل `NEXT_PUBLIC_PREVIEW_MODE` عند الحاجة.
10. قبل إضافة نمط جديد، افحص أقرب مكون/Token موجود وأعد استخدامه. لا تنشئ Design System ثانيًا، ولا تضف Dependencies أو خطوطًا أو مكتبة حركة جديدة لمجرد المظهر إذا كان CSS والمكونات الحالية تكفي.

## مراجع المصدر

- الاتجاه البصري والتوكنز وحدود الخلفية: [design-tokens.md](references/design-tokens.md).
- Attribution ومصادر المهارات العامة: [upstream-sources.md](references/upstream-sources.md).
