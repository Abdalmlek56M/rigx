# Report 1: formal-Arabic audit

Checked: `apps/web/messages/ar.json`, Arabic strings in `src/**/*.cs`, Arabic in `apps/web/src`, and `docs/12-policies.md` (the text the owner is about to publish).

## Summary
- `ar.json` is almost clean: **13 strings** still colloquial (8 on the customer side, 5 in the admin).
- The **biggest finding is outside the file**: `docs/12-policies.md` (the six policies about to be published) is written entirely in Egyptian dialect. The owner approved it on 19 Nov, but on 20 Nov asked for formal Arabic in policies. It must be rewritten before publishing.
- Server messages (`ApiErrorMessages.cs`, `EmailLayout.cs`) are formal. Only demo and seed strings are informal.

## Findings

### High: policies text (publish blocker, needs owner approval to change)
| Location | Current | Proposed |
|---|---|---|
| `docs/12-policies.md` lines 39, 40, 43, 94, 97, 100, 102, 147, 193, 195, 198, 201, 250, 261, 297, 298, 311, 315, 316, 320 (and the rest of the file) | "بنشحن لكل المحافظات"، "لو إنت اللي لغيت الطلب، العربون مابيرجعش"، "فلوسها بترجعلك كاملة"، "كلمنا على واتساب، وهنقولك الخطوات"، "خد نسخة من الداتا"، "الضمان متسجل على الفاتورة"، "علشان" | "نشحن إلى جميع المحافظات"، "إذا ألغيتَ الطلب فلا يُسترد العربون"، "يُردّ إليك المبلغ كاملًا"، "تواصل معنا عبر واتساب وسنرشدك إلى الخطوات"، "احتفظ بنسخة من بياناتك"، "الضمان مسجَّل على الفاتورة"، "لأجل" / "من أجل" |
Fix: rewrite the whole file in formal Arabic (about 350 lines), get the owner's sign-off, then publish. Sentence meaning must not change (money terms: عربون، استرداد، استبدال).

### Medium: customer-facing strings in `ar.json`
| Line | Key | Current | Proposed |
|---|---|---|---|
| 614-616 | consult steps | "تبعتلنا الاستخدام والميزانية" / "نبعتلك ترشيح أو اتنين" / "تختار، ونعدّل لو حبيت" | "ترسل إلينا الاستخدام والميزانية" / "نرسل إليك ترشيحًا أو اثنين" / "تختار، ونعدّل إن رغبت" |
| 660 | sourcing price | "السعر بيتأكد قبل الدفع" | "يُؤكَّد السعر قبل الدفع" |
| 692 | trade-in step 4 | "4. فلوس كاش أو خصم من الجديد" | "4. مبلغ نقدي أو خصم من سعر الجديد" |
| 1391, 1402 | policy and FAQ help link | "كلمنا على واتساب" | "تواصل معنا عبر واتساب" |
| 5355 | contact form error | "أدخل رقم موبايل مصريًا صحيحًا." | "أدخل رقم هاتف محمول مصريًا صحيحًا." |
| 2846 | sale customer hint | "جديداً"، "أولاً" | "جديدًا"، "أولًا" (the rest of the file writes tanween on the letter before the alif) |

### Medium: admin strings in `ar.json`
| Line | Key | Current | Proposed |
|---|---|---|---|
| 2272 | customer detail sign-out confirm | "هيخرج من كل الأجهزة المفتوحة." | "سيُخرَج العميل من جميع الأجهزة المفتوحة." |
| 2275 | blacklist confirm | "هيتشال من القائمة السوداء." | "سيُزال من القائمة السوداء." |
| 2500 | FAQ delete | "السؤال هيروح سلة المهملات 30 يوم." | "سينتقل السؤال إلى سلة المهملات لمدة 30 يومًا." |
| 2768 | article delete | "المقال هيروح سلة المهملات 30 يوم." | "سينتقل المقال إلى سلة المهملات لمدة 30 يومًا." |
| 2769 | article save first | "احفظ التعديلات الأول قبل النشر." | "احفظ التعديلات أولًا قبل النشر." |
| 2861 | POS new customer | "هيتسجل مع البيع." | "سيُسجَّل مع عملية البيع." |
| 2905 | POS summary | "بنحسب…" | "جارٍ الحساب…" |
| 2906 | POS summary failed | "مقدرناش نحدّث الملخص: {message}" | "تعذّر تحديث الملخص: {message}" |
| 3260 | split build | "جهاز كامل هيتفصّص قطع" | "جهاز كامل يُفكَّك إلى قطع" |
| 3495 | picker | "بنعرض {shown} من {total}. ضيّق البحث." | "يُعرض {shown} من {total}. ضيّق البحث." |
| 4693 | audit export | "موبايل البايع" | "هاتف البائع" (and line 4692 "البايع" → "البائع") |

### Low: server and seed strings
| Location | Current | Proposed |
|---|---|---|
| `PreviewDemoSeeder.cs:112` (demo only) | "كلّمنا قبل ما تيجي عشان نجهّز الطلب." | "يُرجى التواصل معنا قبل الحضور لنجهّز طلبك." |
| `PreviewDemoContent.cs:24, 50` (demo only) | "رقم الموبايل" | "رقم الهاتف" |
| `SimpleSettings.cs:99` (customer source list, saved in orders) | "صاحبي رشحكم" / "حاجة تانية" | "رشّحكم صديق" / "أخرى" (check whether old values already saved in the database need mapping) |
| `StockImportRules.cs:43` (import template header "السيريال") | "السيريال" | "الرقم التسلسلي". Check that the importer still accepts the old header so already-filled templates keep working. |

## Kept on purpose (not findings)
- `{الموبايل}` token in `WhatsAppTemplateSettings.cs:9` and its test in `contact.test.ts` (brief says keep).
- Comments in code that quote old phrases (`build-rules.ts:230`, `AssemblyEntities.cs:51`, `AssemblyRules.cs:22`, `FaqSection.tsx:5`): comments are not shown to anyone.
- "اتصل بنا"، "اترك"، "اتركه" at lines 185, 1780, 2393, 2870, 3430, 4613, 4987: these are imperative formal verbs, not colloquial passives.

## Questions for the owner
1. May I rewrite `docs/12-policies.md` in formal Arabic (same meaning) and send it back for approval before publishing? Recommended: yes.
2. For the "how did you hear about us" list (`SimpleSettings.cs:99`): are the saved values already used in reports, or can the labels be changed freely?

## What I did not check
- English strings, `en.json`, and article/FAQ text saved in the production database.
- Button labels inside generated PDFs or invoices (not found in text search).
