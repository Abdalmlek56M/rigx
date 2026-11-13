# RIGX Visual Foundation

## Direction

**Graphite / Obsidian + Ambient Orange.** واجهة تجارية تقنية Products-first: Graphite Ink للهيكل، Warm Paper في الوضع الفاتح، Obsidian في الوضع الداكن، وOrange Accent واحد للتفاعل والاختيار. المساحات تحريرية منظمة، والتفاصيل تقنية (Grid، SKU، Rules، Micro-interactions) تخدم فهم المنتج والمواصفات والسعر.

المراجع HTML التي راجعها المالك مصدر مبادئ فقط: Layouts، تسلسل Hero/Products، Ambient Glow، Footer غني، وMotion. لا تُنسخ Palettes أو النصوص أو الأسعار أو الوعود التجارية.

## Core Tokens (المصدر الفعلي: `apps/web/src/app/globals.css`)

| Token | Light | Dark | Use |
|---|---|---|---|
| `--ds-background` | `#f1eee8` | `#07090d` | Page canvas |
| `--ds-surface` | `#faf8f4` | `#10141a` | Cards, forms, drawers |
| `--ds-surface-muted` | `#e6e1d8` | `#161c24` | Secondary surfaces |
| `--ds-text` | `#12151a` | `#f4f1eb` | Primary content |
| `--ds-text-muted` | `#5b6270` | `#98a1ad` | Metadata |
| `--ds-border` | `#d5cfc4` | `#272e38` | Dividers and controls |
| `--ds-brand` | `#0c1016` | `#0a0d12` | Header/hero/footer ink |
| `--ds-accent` | `#f25a22` | `#ff6a2b` | Actions and selected state |
| `--ds-accent-strong` | `#c84613` | `#ff8b57` | Text links / strong accent |
| `--ds-accent-soft` | `#ffede3` | `#341a10` | Accent tint |
| `--ds-on-accent` | `#ffffff` | `#100d0a` | Text on accent |
| `--ds-grid` | `rgb(18 21 26 / 0.055)` | `rgb(255 255 255 / 0.045)` | Technical grid |
| `--ds-glow` | `rgb(242 90 34 / 0.12)` | `rgb(255 106 43 / 0.15)` | Ambient orange glow |
| `--ds-whatsapp` | `#1f9d55` | `#35c86f` | WhatsApp actions only |
| `--ds-danger` | `#c52a3a` | `#ff8a96` | Errors |
| `--ds-success` | `#147a5a` | `#65d7b3` | Success |

Radii: `--ds-radius-sm: 0.625rem`, `--ds-radius-md: 1rem`, `--ds-radius-lg: 1.5rem`.
Shadow: `--ds-shadow`.
Motion: `--rigx-motion-fast: 160ms`, `--rigx-motion-standard: 220ms`, `--rigx-motion-ease: cubic-bezier(0.2, 0.8, 0.2, 1)`.

## Typography

- Arabic: IBM Plex Sans Arabic / Noto Sans Arabic / Segoe UI.
- Latin: Inter / Segoe UI.
- Technical codes, SKU, prices and model numbers use LTR isolation; SKU may use monospace.
- Scale: 12, 14, 16, 20, 28, 40, 60px.
- Body line length: 65–80 characters maximum.

## Layout & Surfaces

- 4px spacing base; Storefront max width ~1440px; Admin max width ~1280px; reading copy ~720px.
- Breakpoints: 640, 768, 1024, 1280, 1536.
- Product image ratio: 4:3; card target width 240–320px.
- استخدم الحدود والمحاذاة قبل الظلال أو البطاقات المتداخلة.
- الجداول والبطاقات كثيفة المعلومات مسموحة عندما تخدم المنتج أو المواصفات؛ لا تستخدم Dashboard Cards كديكور.

## Ambient Canvas & Footer

- طبقة واحدة `.rigx-visual-engine` ثابتة بكامل الـViewport: Grid خفيف + 4 Radial Glows + Pointer Glow.
- `:root` و`[data-theme]` يحملان `background-color` فقط، دون تكرار Gradient.
- `.rigx-app-frame` عمود Flex، و`#main-content` يتمدد ليدفع الفوتر للأسفل.
- `.rigx-site-footer` شفاف/متدرج فوق نفس الـCanvas، مع Accent Rule وحدود وتباين. لا تستخدم `bg-slate-950` أو `!important` داكن يحجب الطبقة.
- Admin shell يستخدم Grid/Glow خاصًا به لأن Visual Engine مستبعد من `/admin`.

## RIGX Ambient Background and Footer Continuity (Owner Direction — 7 Nov 2026)

- اختار المالك عدة استكشافات HTML كمصدر إلهام. استخلص اللغة المشتركة (Graphite/Obsidian، Ambient Light خفيف، Warm Orange Glow، مساحات تحريرية، تفاصيل تقنية)، ولا تنقل Palettes فردية أو تحوّل كل Route إلى ثيم بصري مختلف.
- اعتبر قيم Semantic Colors الحالية نقطة البداية. إذا احتاج العمل البصري درجة أدق، أضف/عدّل Token دلاليًا فقط بعد مقارنة الوضعين وفحص التباين؛ وتجنب Hex مكتوبًا داخل Section معينة.
- يجب أن تستمر الخلفية المحيطية العامة بصريًا خلف محتوى الصفحة والفوتر بالكامل. استخدم طبقة تطبيق مشتركة واحدة، ولا تكرر Fixed Gradient Layers أو Canvases أو Pointer Listeners أو Animation Loops أو Observers في Sections/Routes منفصلة؛ أعد استخدام Visual Engine الحالي واضمن التنظيف.
- يظل Light Mode ثيمًا كاملًا من الدرجة الأولى؛ يجب أن يكون Ambient Orange مضبوطًا بما يحفظ تباين النص وصور المنتجات.
- يشترك Admin في Tokens والخطوط والحدود والFocus والMotion المنضبط، لكنه يحتفظ بـHierarchy تشغيلية كثيفة المعلومات بدل مؤثرات Storefront التسويقية.

## Principles

1. Product photography, specifications and price lead.
2. Spend visual emphasis in one place; keep surrounding chrome quiet.
3. Use borders and alignment before shadows or nested cards.
4. Avoid generic SaaS card grids, excessive gradients, tracked all-caps labels, and decorative badges.
5. Motion is responsive and optional, never required to understand state.
6. One identity across Storefront and Admin; variation in density, not in palette or typography.
