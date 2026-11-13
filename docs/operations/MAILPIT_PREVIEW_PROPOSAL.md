# Mailpit Preview Proposal

**الحالة:** مقترح غير مطبق. يحتاج موافقة منفصلة لأنه يضيف خدمة ومنفذًا إلى Preview.

## الوضع الحالي

- Development/Testing يستخدمان `InMemoryTestEmailProvider`.
- لا توجد Inbox UI للمالك، ولا يتم إرسال بريد حقيقي.
- Playwright يستخرج رمز التحقق من قاعدة Preview في اختبارات مخصصة، ولا يعرضه في Logs.
- Production يستخدم `SmtpEmailProvider` فقط عند توفير إعدادات SMTP الحقيقية، والتسجيل العام مغلق.

## الاقتراح

إضافة Mailpit إلى `compose.preview.yaml` فقط، بإصدار Stable محدد `v1.33.0`، دون Persistent Volume:

```diff
 services:
+  mailpit:
+    image: axllent/mailpit:v1.33.0
+    restart: unless-stopped
+    environment:
+      MP_MAX_MESSAGES: "500"
+    ports:
+      - "127.0.0.1:8025:8025"
+      - "127.0.0.1:1025:1025"
+
   api:
     environment:
+      Identity__Email__PreviewProvider: Mailpit
+      Identity__Email__Smtp__Host: mailpit
+      Identity__Email__Smtp__Port: "1025"
+      Identity__Email__Smtp__EnableSsl: "false"
+      Identity__Email__Smtp__FromAddress: no-reply@hardware-store.local
+      Identity__Email__Smtp__FromName: HARDWARE STORE Preview
+      Identity__Email__Smtp__TimeoutSeconds: "5"
```

هذه الإعدادات وحدها لا تكفي مع الكود الحالي: يجب تعديل تسجيل `IEmailProvider` ليسمح باختيار SMTP صراحةً في Preview مع بقاء InMemory هو الافتراضي للتطوير والاختبار العادي.

## المسارات المقترحة

- Mailpit UI: `http://127.0.0.1:8025`
- SMTP الداخلي من API: `mailpit:1025`
- SMTP من المضيف عند الحاجة: `127.0.0.1:1025`

## ضوابط مطلوبة عند الاعتماد

1. عدم إضافة Volume دائم؛ Mailpit بيانات مؤقتة فقط.
2. ربط المنفذين بـ`127.0.0.1` فقط.
3. عدم استخدام Mailpit في Production أو اعتباره مزود بريد تجاريًا.
4. بقاء روابط التحقق في البريد نفسه، وعدم تسجيلها في API Logs.
5. اختبار التسجيل وإعادة الإرسال والاستعادة وإشعارات الأمان من Inbox.
6. عدم تغيير رابط التحقق إلى OTP.

## مصدر الصورة

آخر إصدار Stable تم التحقق منه من GitHub Releases الرسمي في 2026-11-05 هو `v1.33.0`. لم يتم تنزيل الصورة أو تشغيلها في هذه الجولة.
