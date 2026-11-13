"use client";

import { type FormEvent, useState } from "react";
import { requestPasswordReset } from "@/lib/api-client";
import { apiErrorMessage, fieldError } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import {
  AuthPanel,
  FormStack,
  Message,
  PageIntro,
  PrimaryButton,
  TextField,
  TextLink,
} from "@/components/ui";

export default function ForgotPasswordPage() {
  const { messages } = useLocale();
  const [email, setEmail] = useState("");
  const [emailError, setEmailError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [pending, setPending] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!email.trim()) return setEmailError(messages.required);
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) return setEmailError(messages.emailInvalid);

    setPending(true);
    setFormError(null);
    setEmailError(null);
    setSuccess(false);
    try {
      await requestPasswordReset(email.trim());
      setSuccess(true);
    } catch (error) {
      setEmailError(fieldError(error, "email") ?? null);
      setFormError(apiErrorMessage(error, messages));
    } finally {
      setPending(false);
    }
  }

  return (
    <AuthPanel>
      <PageIntro eyebrow={messages.recoveryEyebrow} title={messages.forgotTitle} lead={messages.forgotLead} />
      <form noValidate onSubmit={submit}>
        <FormStack>
          {success ? <Message tone="success">{messages.forgotSuccess}</Message> : null}
          {formError ? <Message tone="error">{formError}</Message> : null}
          <TextField
            name="email"
            type="email"
            label={messages.email}
            value={email}
            onChange={(event) => {
              setEmail(event.target.value);
              setEmailError(null);
            }}
            autoComplete="email"
            inputMode="email"
            dir="ltr"
            error={emailError ?? undefined}
            required
          />
          <PrimaryButton type="submit" pending={pending}>
            {pending ? messages.forgotPending : messages.forgotSubmit}
          </PrimaryButton>
          <p className="text-sm text-slate-500">
            <TextLink href="/login">{messages.navLogin}</TextLink>
          </p>
        </FormStack>
      </form>
    </AuthPanel>
  );
}
