# DeepSeek brief 02 — Branded HTML emails + "resend code"

Read `AGENTS.md` first. Work in your worktree on branch `deepseek/02-emails` (the owner creates it from the latest `main`). No migrations, no packages.

## Part A — "إعادة إرسال الكود" (web, small)
The owner asked for a code for a backup download, the email was late, and there was no way to ask again.

File: `apps/web/src/features/account/StepUpCode.tsx` (+ keys under `account.stepUp` in `messages/ar.json` and `en.json`).
- After the code is sent, show a text button "إعادة إرسال الكود" under the code field. It calls the same `send()`.
- Disabled for 60 seconds after each send, showing "إعادة الإرسال بعد {seconds} ث" (a countdown; clear the timer on unmount).
- After a resend, show "أرسلنا كودًا جديدًا. الكود القديم لم يعد صالحًا." (en: "We sent a new code. The old code no longer works.").
- Add one line under the intro after sending: "لم يصلك الكود؟ تحقق من مجلد الرسائل غير المرغوب فيها (Spam)." (en: "No code? Check your spam folder.").
- Rewrite the other `account.stepUp` texts in formal Arabic (no "اتبعت", "ابعت"); keep their keys.

## Part B — Branded HTML emails (API)
Today every email is plain text (`IsBodyHtml = false` in `src/Modules/Identity/SmtpEmailProvider.cs`). Make them look like a real store's emails.

Files you may change:
- `src/Modules/Identity/SmtpEmailProvider.cs`
- a new `src/Modules/Identity/EmailLayout.cs` (one HTML layout, used by all emails)
- tests in `tests/HardwareStore.Api.Tests/` (a new `EmailLayoutTests.cs`)

Rules:
- Send **both** a plain-text part and an HTML part (`AlternateView`), so spam filters and old clients are happy. Keep today's text bodies as the plain part (reword to formal Arabic).
- One layout: `dir="rtl"` `lang="ar"`, max width 560 px, white card on `#F4F4F1`, header bar `#121317` with the text "RIGX" in bold white (no image file), orange accent `#FF6A2B`, footer: "RIGX — قطع كمبيوتر جديدة ومستعملة" and "https://rigxhardware.com", plus "هذه رسالة آلية، يرجى عدم الرد عليها."
- Inline CSS only (email clients ignore `<style>` blocks), table-based layout, system Arabic font stack (`Tahoma, Arial, sans-serif`).
- **Code emails:** the 6-digit code in a big box (32 px, monospace, letter spacing, `dir="ltr"`), the expiry time ("صالح لمدة 10 دقائق" — read the real value from `ExpiresAtUtc`), and "إذا لم تطلب هذا الكود، تجاهل هذه الرسالة."
- **Link emails** (invitation, email change, order update, back in stock): one orange button with the link, and the raw link as text under it.
- **Every value you put in HTML must be HTML-encoded** (`System.Net.WebUtility.HtmlEncode`): names, item names, references. URLs only `https://` or the configured site origin; never build HTML from unencoded input.
- Subjects in formal Arabic, short, starting with the purpose: "كود الدخول إلى RIGX: 123456", "تحديث طلبك RX-… ", etc.
- Tests: the layout encodes `<script>` in a name; the code email contains the code and `dir="rtl"`; each message has a text and an HTML view.

## Done when
- `npm run typecheck`, `npm run lint`, `npm test` pass; `dotnet test tests/HardwareStore.Api.Tests --filter EmailLayout` passes, and the whole API test project builds.
- In the report: paste the HTML of the code email (so Claude can view it), and list every subject line.
