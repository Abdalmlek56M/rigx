"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { type FormEvent, useState } from "react";
import { ApiError, login, requestEmailLoginOtp, verifyEmailLoginOtp } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import {
  AuthPanel,
  FormStack,
  Message,
  PageIntro,
  PasswordField,
  PrimaryButton,
  TextField,
  TextLink,
} from "@/components/ui";

function safeReturnTo(value: string | null): string {
  return value && value.startsWith("/") && !value.startsWith("//") ? value : "/account/security";
}

export function LoginExperience() {
  const { messages } = useLocale();
  const router = useRouter();
  const searchParams = useSearchParams();
  const session = useSession();
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [otpMode, setOtpMode] = useState(false);
  const [otpEmail, setOtpEmail] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [otpSent, setOtpSent] = useState(false);
  const [otpNotice, setOtpNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const notice = searchParams.get("notice");
  const returnTo = safeReturnTo(searchParams.get("returnTo"));

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!identifier.trim() || !password) return setError(messages.required);

    setPending(true);
    setError(null);
    try {
      await login(identifier, password);
      await session.refresh();
      router.replace(returnTo);
    } catch (requestError) {
      setError(
        requestError instanceof ApiError && requestError.status === 401
          ? messages.loginInvalid
          : apiErrorMessage(requestError, messages),
      );
    } finally {
      setPending(false);
    }
  }

  async function submitOtp(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!otpEmail.trim()) return setError(messages.required);
    setPending(true); setError(null);
    try {
      if (!otpSent) {
        await requestEmailLoginOtp(otpEmail.trim());
        setOtpSent(true);
        setOtpNotice(messages.loginOtpSent);
      } else {
        if (!otpCode.trim()) { setError(messages.required); return; }
        await verifyEmailLoginOtp(otpEmail.trim(), otpCode.trim());
        await session.refresh();
        router.replace(returnTo);
      }
    } catch (requestError) {
      setError(requestError instanceof ApiError && requestError.status === 401
        ? messages.loginOtpInvalid
        : apiErrorMessage(requestError, messages));
    } finally { setPending(false); }
  }

  return (
    <AuthPanel aside={
      <div className="auth-aside-copy">
        <span className="eyebrow">RIGX / ACCOUNT</span>
        <h2>حسابك في RIGX</h2>
        <p>طلباتك، عناوينك، قائمة المفضلة، ومراجعاتك في مكان واحد.</p>
        <ul className="auth-aside-list">
          <li>متابعة الطلبات وحالاتها</li>
          <li>عناوين التوصيل والاستلام</li>
          <li>المفضلة والمراجعات</li>
        </ul>
        <p className="inline-note">الدخول محمي بجلسة خادم ورمز CSRF. لا تشارك كلمة المرور مع أي شخص.</p>
      </div>
    }>
      <PageIntro eyebrow={messages.loginEyebrow} title={messages.loginTitle} lead={messages.loginLead} />
      {session.status === "authenticated" ? (
        <Message tone="info">
          {messages.accountTitle}:{" "}
          <Link className="font-bold underline underline-offset-4" href="/account/security">
            {messages.navAccount}
          </Link>
        </Message>
      ) : otpMode ? (
        <form noValidate onSubmit={submitOtp}>
          <FormStack>
            {otpNotice ? <Message tone="success">{otpNotice}</Message> : null}
            {error ? <Message tone="error">{error}</Message> : null}
            <TextField label={messages.loginOtpEmail} value={otpEmail} onChange={(event) => setOtpEmail(event.target.value)} type="email" autoComplete="email" dir="ltr" required />
            {otpSent ? (
              <TextField label={messages.loginOtpCode} value={otpCode} onChange={(event) => setOtpCode(event.target.value)} inputMode="numeric" autoComplete="one-time-code" dir="ltr" required />
            ) : null}
            <PrimaryButton type="submit" pending={pending}>
              {otpSent ? messages.loginOtpVerify : messages.loginOtpSend}
            </PrimaryButton>
            <button type="button" className="rigx-cart-text-link" onClick={() => { setOtpMode(false); setOtpSent(false); setError(null); }}>
              {messages.loginOtpBack}
            </button>
          </FormStack>
        </form>
      ) : (
        <form noValidate onSubmit={submit}>
          <FormStack>
            {notice === "signed-out" ? <Message tone="success">{messages.loggedOut}</Message> : null}
            {notice === "signed-out-all" ? <Message tone="success">{messages.loggedOutAll}</Message> : null}
            {notice === "password-changed" ? <Message tone="success">{messages.passwordChangedNotice}</Message> : null}
            {notice === "session-expired" ? <Message tone="error">{messages.sessionExpired}</Message> : null}
            {error ? <Message tone="error">{error}</Message> : null}
            <TextField
              label={messages.username}
              value={identifier}
              onChange={(event) => setIdentifier(event.target.value)}
              autoComplete="username"
              error={!identifier.trim() && error ? messages.required : undefined}
              required
            />
            <PasswordField
              label={messages.password}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
              error={!password && error ? messages.required : undefined}
              required
            />
            <div className="flex flex-wrap items-center justify-between gap-3 text-sm">
              <TextLink href="/forgot-password">{messages.forgotPassword}</TextLink>
              <span className="text-[var(--ds-text-muted)]">
                {messages.noAccount} <TextLink href="/register">{messages.createAccount}</TextLink>
              </span>
            </div>
            <PrimaryButton type="submit" pending={pending}>
              {pending ? messages.loginPending : messages.loginSubmit}
            </PrimaryButton>
            <button type="button" className="rigx-cart-text-link" onClick={() => { setOtpMode(true); setError(null); }}>
              {messages.loginOtpToggle}
            </button>
          </FormStack>
        </form>
      )}
    </AuthPanel>
  );
}
