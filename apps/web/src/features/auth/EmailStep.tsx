"use client";

import { useTranslations } from "next-intl";
import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/Button";
import { TextField } from "@/components/ui/TextField";
import { GOOGLE_SIGN_IN_ENABLED, GoogleButton } from "./GoogleButton";

interface EmailStepProps {
  initialEmail: string;
  busy: boolean;
  error: string | null;
  onSubmit: (email: string) => void;
  onGoogle: (idToken: string) => void;
}

export function EmailStep({ initialEmail, busy, error, onSubmit, onGoogle }: EmailStepProps) {
  const t = useTranslations("auth.email");
  const [email, setEmail] = useState(initialEmail);
  const [invalid, setInvalid] = useState(false);

  function submit(event: FormEvent) {
    event.preventDefault();
    const trimmed = email.trim();
    if (!trimmed.includes("@")) return setInvalid(true);
    setInvalid(false);
    onSubmit(trimmed);
  }

  return (
    <form onSubmit={submit} noValidate className="flex flex-col gap-5">
      <div className="flex flex-col gap-1.5">
        <h1 className="m-0 type-page-title">{t("title")}</h1>
        <p className="m-0 text-[15px] text-ink-muted">{t("subtitle")}</p>
      </div>
      {GOOGLE_SIGN_IN_ENABLED ? (
        <>
          <GoogleButton onCredential={onGoogle} />
          <div className="flex items-center gap-3 text-[13px] text-ink-muted">
            <span className="h-px flex-1 bg-line" />
            {t("google")}
            <span className="h-px flex-1 bg-line" />
          </div>
        </>
      ) : null}
      <TextField
        label={t("label")}
        name="email"
        type="email"
        ltr
        autoComplete="email"
        placeholder={t("placeholder")}
        value={email}
        onChange={setEmail}
        error={invalid ? t("invalid") : null}
      />
      {error ? <p role="alert" className="m-0 text-sm font-bold text-error">{error}</p> : null}
      <Button type="submit" variant="accent" loading={busy} className="w-full">
        {busy ? t("sending") : t("send")}
      </Button>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
    </form>
  );
}
