"use client";

import { type FormEvent, useState } from "react";
import { ApiError, resetPassword } from "@/lib/api-client";
import { apiErrorMessage, fieldError } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { passwordPolicyError } from "@/lib/password-policy";
import { useFragmentToken } from "@/lib/use-fragment-token";
import {
  AuthPanel,
  FormStack,
  LoadingCard,
  Message,
  PageIntro,
  PasswordField,
  PrimaryButton,
  TextLink,
} from "@/components/ui";

export default function ResetPasswordPage() {
  const { messages } = useLocale();
  const { token, ready } = useFragmentToken();
  const [password, setPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [complete, setComplete] = useState(false);

  if (!ready) return <LoadingCard />;

  if (!token) {
    return (
      <AuthPanel>
        <PageIntro eyebrow={messages.resetEyebrow} title={messages.verifyMissingTitle} lead={messages.invalidToken} />
        <TextLink href="/forgot-password">{messages.forgotTitle}</TextLink>
      </AuthPanel>
    );
  }

  if (complete) {
    return (
      <AuthPanel>
        <PageIntro eyebrow={messages.resetEyebrow} title={messages.resetSuccessTitle} lead={messages.resetSuccessBody} />
        <Message tone="success">{messages.resetSuccessBody}</Message>
        <div className="mt-6">
          <TextLink href="/login">{messages.navLogin}</TextLink>
        </div>
      </AuthPanel>
    );
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const policyError = passwordPolicyError(password, messages);
    if (policyError) {
      setPasswordError(policyError);
      return;
    }
    if (password !== confirmation) {
      setPasswordError(messages.passwordMismatch);
      return;
    }

    setPasswordError(null);
    setFormError(null);
    setPending(true);
    try {
      await resetPassword(token!, password);
      setPassword("");
      setConfirmation("");
      setComplete(true);
    } catch (error) {
      const serverPasswordError = fieldError(error, "newPassword");
      if (serverPasswordError) {
        setPasswordError(serverPasswordError);
      } else if (error instanceof ApiError && error.status === 400) {
        setFormError(messages.invalidToken);
      } else {
        setFormError(apiErrorMessage(error, messages));
      }
    } finally {
      setPending(false);
    }
  }

  return (
    <AuthPanel
      aside={
        <div>
          <p className="text-sm font-bold text-[var(--ds-accent)]">RIGX · {messages.resetEyebrow}</p>
          <p className="mt-3 text-2xl font-bold">{messages.resetTitle}</p>
          <p className="mt-3 leading-7 text-white/70">{messages.resetLead}</p>
        </div>
      }
    >
      <PageIntro eyebrow={messages.resetEyebrow} title={messages.resetTitle} lead={messages.resetLead} />
      <form noValidate onSubmit={submit}>
        <FormStack>
          {formError ? <Message tone="error">{formError}</Message> : null}
          <PasswordField
            label={messages.newPassword}
            value={password}
            onChange={(event) => {
              setPassword(event.target.value);
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
            error={password !== confirmation && confirmation.length > 0 ? messages.passwordMismatch : undefined}
            required
          />
          <PrimaryButton type="submit" pending={pending}>
            {pending ? messages.resetPending : messages.resetSubmit}
          </PrimaryButton>
        </FormStack>
      </form>
    </AuthPanel>
  );
}
