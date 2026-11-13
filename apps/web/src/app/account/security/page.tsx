"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { type FormEvent, useEffect, useRef, useState } from "react";
import { ApiError, changePassword, logoutAll, logoutCurrent } from "@/lib/api-client";
import { apiErrorMessage, fieldError } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { passwordPolicyError } from "@/lib/password-policy";
import { useSession } from "@/components/session-provider";
import {
  AuthPanel,
  FormStack,
  LoadingCard,
  Message,
  PageIntro,
  PasswordField,
  PrimaryButton,
  SecondaryButton,
  TextLink,
} from "@/components/ui";

type Completion = "password-changed" | null;

export default function AccountSecurityPage() {
  const { messages } = useLocale();
  const router = useRouter();
  const { status, user, refresh, markSignedOut } = useSession();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [pendingAction, setPendingAction] = useState<"password" | "logout" | "logout-all" | null>(null);
  const [completion, setCompletion] = useState<Completion>(null);
  const skipExpiredRedirect = useRef(false);

  useEffect(() => {
    if (status === "anonymous" && !completion && pendingAction === null && !skipExpiredRedirect.current) {
      router.replace(`/login?notice=session-expired&returnTo=${encodeURIComponent("/account/security")}`);
    }
  }, [completion, pendingAction, router, status]);

  if (completion === "password-changed") {
    return (
      <AuthPanel>
        <PageIntro eyebrow={messages.accountEyebrow} title={messages.resetSuccessTitle} lead={messages.changePasswordSuccess} />
        <Message tone="success">{messages.changePasswordSuccess}</Message>
        <div className="mt-6">
          <TextLink href="/login?notice=password-changed">{messages.navLogin}</TextLink>
        </div>
      </AuthPanel>
    );
  }

  if (status === "loading") return <LoadingCard />;
  if (status === "unavailable") {
    return (
      <AuthPanel>
        <PageIntro eyebrow={messages.accountEyebrow} title={messages.accountUnavailable} lead={messages.requestFailed} />
        <SecondaryButton type="button" onClick={() => void refresh()}>{messages.retry}</SecondaryButton>
      </AuthPanel>
    );
  }
  if (status === "anonymous" || !user) {
    return (
      <AuthPanel>
        <PageIntro eyebrow={messages.accountEyebrow} title={messages.accountTitle} lead={messages.accountRedirecting} />
        <TextLink href="/login">{messages.navLogin}</TextLink>
      </AuthPanel>
    );
  }

  async function submitPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!currentPassword) return setPasswordError(messages.required);
    const policyError = passwordPolicyError(newPassword, messages);
    if (policyError) return setPasswordError(policyError);
    if (newPassword !== confirmation) return setPasswordError(messages.passwordMismatch);

    setPendingAction("password");
    setPasswordError(null);
    setFormError(null);
    try {
      await changePassword(currentPassword, newPassword);
      markSignedOut();
      setCompletion("password-changed");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmation("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        markSignedOut();
        router.replace("/login?notice=session-expired");
        return;
      }
      setPasswordError(fieldError(error, "newPassword") ?? null);
      setFormError(apiErrorMessage(error, messages));
    } finally {
      setPendingAction(null);
    }
  }

  async function signOut(scope: "current" | "all") {
    setPendingAction(scope === "current" ? "logout" : "logout-all");
    setFormError(null);
    try {
      if (scope === "current") await logoutCurrent();
      else await logoutAll();
      skipExpiredRedirect.current = true;
      markSignedOut();
      router.replace(`/login?notice=${scope === "current" ? "signed-out" : "signed-out-all"}`);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        skipExpiredRedirect.current = false;
        markSignedOut();
        router.replace("/login?notice=session-expired");
        return;
      }
      setFormError(apiErrorMessage(error, messages));
    } finally {
      setPendingAction(null);
    }
  }

  return (
    <main className="mx-auto grid w-full max-w-6xl gap-6 px-5 py-10 sm:px-8 lg:grid-cols-[22rem_minmax(0,1fr)] lg:items-start lg:py-16">
      <aside className="rounded-3xl bg-[var(--ds-brand)] p-7 text-[var(--ds-text)] shadow-sm">
        <p className="text-xs font-bold tracking-[0.18em] text-white/60 uppercase">RIGX · {messages.accountEyebrow}</p>
        <h1 className="mt-3 text-3xl font-black">{messages.accountTitle}</h1>
        <dl className="mt-7 grid gap-5 text-sm">
          <div>
            <dt className="text-white/60">{messages.fullName}</dt>
            <dd className="mt-1 font-semibold text-white">{user.fullName}</dd>
          </div>
          <div>
            <dt className="text-white/60">{messages.username}</dt>
            <dd className="mt-1 font-semibold" dir="ltr">{user.username}</dd>
          </div>
          <div>
            <dt className="text-white/60">{messages.email}</dt>
            <dd className="mt-1 break-all font-semibold" dir="ltr">{user.email}</dd>
          </div>
          <div>
            <dt className="text-white/60">{messages.accountRoles}</dt>
            <dd className="mt-1 font-semibold">{user.roles.join("، ") || messages.notAvailable}</dd>
          </div>
        </dl>
      </aside>

      <section className="grid gap-6">
        <nav className="flex flex-wrap gap-3 text-sm font-semibold" aria-label={messages.accountTitle}>
          <Link href="/account/profile" className="text-[var(--ds-accent)] hover:text-[var(--ds-accent-strong)]">{messages.accountProfileTitle}</Link>
          <Link href="/account/sessions" className="text-[var(--ds-accent)] hover:text-[var(--ds-accent-strong)]">{messages.accountSessionsTitle}</Link>
          <Link href="/account/addresses" className="text-[var(--ds-accent)] hover:text-[var(--ds-accent-strong)]">{messages.addressTitle}</Link>
        </nav>
        <div className="rounded-3xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-6 shadow-sm sm:p-8">
          <PageIntro eyebrow={messages.accountEyebrow} title={messages.changePasswordTitle} lead={messages.changePasswordLead} />
          <form noValidate onSubmit={submitPassword}>
            <FormStack>
              {formError ? <Message tone="error">{formError}</Message> : null}
              <PasswordField
                label={messages.currentPassword}
                value={currentPassword}
                onChange={(event) => setCurrentPassword(event.target.value)}
                autoComplete="current-password"
                required
              />
              <PasswordField
                label={messages.newPassword}
                value={newPassword}
                onChange={(event) => {
                  setNewPassword(event.target.value);
                  setPasswordError(null);
                }}
                autoComplete="new-password"
                hint={messages.passwordHint}
                error={passwordError ?? undefined}
                required
              />
              <PasswordField
                label={messages.confirmPassword}
                value={confirmation}
                onChange={(event) => setConfirmation(event.target.value)}
                autoComplete="new-password"
                error={newPassword !== confirmation && confirmation.length > 0 ? messages.passwordMismatch : undefined}
                required
              />
              <PrimaryButton type="submit" pending={pendingAction === "password"}>
                {pendingAction === "password" ? messages.changePasswordPending : messages.changePasswordSubmit}
              </PrimaryButton>
            </FormStack>
          </form>
        </div>

        <div className="rounded-3xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-6 shadow-sm sm:p-8">
          <h2 className="text-xl font-bold text-[var(--ds-text)]">{messages.securityCardTitle}</h2>
          <p className="mt-2 leading-7 text-[var(--ds-text-muted)]">{messages.securityCardBody}</p>
          <div className="mt-5 flex flex-col gap-3 sm:flex-row">
            <SecondaryButton
              type="button"
              onClick={() => void signOut("current")}
              disabled={pendingAction !== null}
            >
              {messages.logoutCurrent}
            </SecondaryButton>
            <SecondaryButton
              type="button"
              onClick={() => void signOut("all")}
              disabled={pendingAction !== null}
              className="border-[var(--ds-danger)] text-[var(--ds-danger)] hover:bg-[var(--ds-surface-muted)]"
            >
              {messages.logoutAll}
            </SecondaryButton>
          </div>
          <p className="mt-4 text-sm text-[var(--ds-text-muted)]">
            <Link href="/" className="font-semibold text-[var(--ds-accent)] underline underline-offset-4">
              {messages.backHome}
            </Link>
          </p>
        </div>
      </section>
    </main>
  );
}

