# Identity Email Outbox

## الغرض
تسليم رسائل الهوية (Email Verification / Password Reset / Security Notification) بشكل دائم بعد Commit، بدل Channel في الذاكرة.

## المكونات
- `identity_email_outbox`: `kind`, `protected_payload`, `status`, `attempts`, `next_attempt_at_utc`, `processing_at_utc`, `processed_at_utc`, `last_error_code`, `created_at_utc`.
- `IdentityEmailOutboxWriter`: يشفر JSON بـData Protection Purpose `HardwareStore.IdentityEmailOutbox.v1`.
- `IdentityEmailOutboxDispatcher`: `IIdentityEmailDispatcher` الذي يحفظ بدل الإرسال المباشر.
- `IdentityEmailOutboxProcessor`: BackgroundService يفحص كل 5 ثوانٍ.

## الحالات
`Pending → Processing → Sent` أو `Failed → (Retry) → Sent` أو `DeadLetter` بعد 5 محاولات.
Backoff: `min(60, 2^attempts)` دقيقة. أي Processing أقدم من 10 دقائق يُسترد إلى Failed.

## الضمانات
- `FOR UPDATE SKIP LOCKED` يمنع إرسال الصف مرتين بين عمليات متزامنة.
- الـPayload يُمحى بعد النجاح (`[]`)؛ لا Tokens/URLs في السجل.
- Audit: `Identity.EmailDelivery.Sent` / `.Failed` مع kind/attempts فقط.
- مفتاح الحماية يجب أن يكون على Volume دائم: `DataProtection:KeyRingPath` في Compose، ويقرأه `Program.cs`.

## القيود
- الإدراج يحدث بعد Commit عبر Context مستقل؛ فجوة صغيرة عند Crash يمكن تعويضها بـResend.
- لا Provider إنتاجي/SMTP معتمد؛ In-Memory/Test فقط.
- يتطلب Backup/Secrets لمفاتيح Data Protection قبل الإنتاج.
