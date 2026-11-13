"use client";

import Link from "next/link";
import { type FormEvent, useState } from "react";
import { registerCustomer } from "@/lib/api-client";
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

type RegisterFields = {
  fullName: string;
  phoneNumber: string;
  username: string;
  email: string;
};

const initialFields: RegisterFields = {
  fullName: "",
  phoneNumber: "",
  username: "",
  email: "",
};

export default function RegisterPage() {
  const { messages } = useLocale();
  const [fields, setFields] = useState(initialFields);
  const [errors, setErrors] = useState<Partial<Record<keyof RegisterFields, string>>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [complete, setComplete] = useState(false);

  function update(field: keyof RegisterFields, value: string) {
    setFields((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors: typeof errors = {};
    if (!fields.fullName.trim()) nextErrors.fullName = messages.required;
    else if (fields.fullName.length > 255) nextErrors.fullName = messages.valueTooLong;
    if (fields.phoneNumber.length > 32) nextErrors.phoneNumber = messages.valueTooLong;
    if (!fields.username.trim()) nextErrors.username = messages.required;
    else if (fields.username.length > 128) nextErrors.username = messages.valueTooLong;
    if (!fields.email.trim()) nextErrors.email = messages.required;
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(fields.email.trim())) nextErrors.email = messages.emailInvalid;

    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors);
      return;
    }

    setPending(true);
    setFormError(null);
    try {
      await registerCustomer({
        fullName: fields.fullName,
        phoneNumber: fields.phoneNumber.trim() || undefined,
        username: fields.username,
        email: fields.email.trim(),
      });
      setComplete(true);
    } catch (error) {
      setErrors({
        fullName: fieldError(error, "fullName"),
        phoneNumber: fieldError(error, "phoneNumber"),
        username: fieldError(error, "username"),
        email: fieldError(error, "email"),
      });
      setFormError(apiErrorMessage(error, messages));
    } finally {
      setPending(false);
    }
  }

  if (complete) {
    return (
      <AuthPanel
        aside={
          <div>
            <p className="text-sm font-bold text-[var(--ds-accent)]">RIGX</p>
            <p className="mt-3 text-2xl font-bold leading-9">{messages.accountCardTitle}</p>
            <p className="mt-3 leading-7 text-white/70">{messages.accountCardBody}</p>
          </div>
        }
      >
        <PageIntro eyebrow={messages.registerEyebrow} title={messages.registerSuccessTitle} lead={messages.registerSuccessBody} />
        <Message tone="success">{messages.registerSuccessBody}</Message>
        <div className="mt-6 flex flex-wrap gap-4">
          <TextLink href="/resend-verification">{messages.resendVerification}</TextLink>
          <TextLink href="/login">{messages.navLogin}</TextLink>
        </div>
      </AuthPanel>
    );
  }

  return (
    <AuthPanel
      aside={
        <div>
          <p className="text-sm font-bold text-[var(--ds-accent)]">RIGX</p>
          <p className="mt-3 text-2xl font-bold leading-9">{messages.accountCardTitle}</p>
          <p className="mt-3 leading-7 text-white/70">{messages.accountCardBody}</p>
          <p className="mt-8 text-sm text-[var(--ds-accent)]">
            {messages.haveAccount}{" "}
            <Link href="/login" className="font-bold text-white underline underline-offset-4">
              {messages.navLogin}
            </Link>
          </p>
        </div>
      }
    >
      <PageIntro eyebrow={messages.registerEyebrow} title={messages.registerTitle} lead={messages.registerLead} />
      <form noValidate onSubmit={submit}>
        <FormStack>
          {formError ? <Message tone="error">{formError}</Message> : null}
          <TextField
            label={messages.fullName}
            value={fields.fullName}
            onChange={(event) => update("fullName", event.target.value)}
            autoComplete="name"
            maxLength={255}
            error={errors.fullName}
            required
          />
          <TextField
            label={`${messages.phoneNumber} (${messages.optional})`}
            value={fields.phoneNumber}
            onChange={(event) => update("phoneNumber", event.target.value)}
            autoComplete="tel"
            inputMode="tel"
            maxLength={32}
            error={errors.phoneNumber}
          />
          <TextField
            label={messages.username}
            value={fields.username}
            onChange={(event) => update("username", event.target.value)}
            autoComplete="username"
            maxLength={128}
            hint={messages.usernameHint}
            error={errors.username}
            dir="ltr"
            required
          />
          <TextField
            label={messages.email}
            type="email"
            value={fields.email}
            onChange={(event) => update("email", event.target.value)}
            autoComplete="email"
            maxLength={255}
            error={errors.email}
            dir="ltr"
            required
          />
          <PrimaryButton type="submit" pending={pending}>
            {pending ? messages.registerPending : messages.registerSubmit}
          </PrimaryButton>
          <p className="text-sm text-slate-500">
            {messages.haveAccount} <TextLink href="/login">{messages.navLogin}</TextLink>
          </p>
        </FormStack>
      </form>
    </AuthPanel>
  );
}
