"use client";

import { useTranslations } from "next-intl";
import { useEffect, useState, type FormEvent } from "react";
import { Button } from "@/components/ui/Button";
import { fieldClass } from "@/components/ui/field-class";
import { formatCountdown, maskEmail, normalizeCode } from "./auth-rules";

interface CodeStepProps {
  email: string;
  /** Seconds until a new code may be requested, counted from `sentAt`. */
  resendAfterSeconds: number;
  /** Changes with every code that is sent, restarting the countdown. */
  sentAt: number;
  busy: boolean;
  error: string | null;
  /** The code can no longer work: only "send a new code" is useful. */
  codeDead: boolean;
  onSubmit: (code: string, rememberDevice: boolean) => void;
  onResend: () => void;
  onChangeEmail: () => void;
}

export function CodeStep({ email, resendAfterSeconds, sentAt, busy, error, codeDead, onSubmit, onResend, onChangeEmail }: CodeStepProps) {
  const t = useTranslations("auth.code");
  const [code, setCode] = useState("");
  const [remember, setRemember] = useState(true);
  const secondsLeft = useCountdown(resendAfterSeconds, sentAt);

  function submit(event: FormEvent) {
    event.preventDefault();
    if (code.length === 6) onSubmit(code, remember);
  }

  function resend() {
    setCode("");
    onResend();
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-[18px]">
      <div className="flex flex-col gap-1.5">
        <h1 className="m-0 type-page-title">{t("title")}</h1>
        <p className="m-0 text-[15px] text-ink-muted">
          {t.rich("sentTo", {
            email: () => <bdi dir="ltr" className="font-mono text-sm text-ink">{maskEmail(email)}</bdi>
          })}
        </p>
      </div>
      <label className="flex flex-col gap-1.5 text-[13px] font-bold">
        {t("label")}
        <input
          name="code"
          dir="ltr"
          value={code}
          inputMode="numeric"
          autoComplete="one-time-code"
          maxLength={6}
          autoFocus
          aria-invalid={error ? true : undefined}
          onChange={(event) => setCode(normalizeCode(event.target.value))}
          className={`${fieldClass} h-[60px] text-center font-mono text-2xl font-medium tracking-[0.5em] ${error ? "border-2 border-error" : ""}`}
        />
      </label>
      {error ? <p role="alert" className="m-0 text-sm font-bold text-error">{error}</p> : null}
      <Button type="submit" variant="accent" loading={busy} disabled={code.length < 6 || codeDead} className="w-full">
        {busy ? t("submitting") : t("submit")}
      </Button>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-[13px]">
        {secondsLeft > 0 ? (
          <span className="text-ink-muted">
            {t.rich("resendIn", { time: () => <bdi dir="ltr" className="font-mono">{formatCountdown(secondsLeft)}</bdi> })}
          </span>
        ) : (
          <button type="button" onClick={resend} disabled={busy} className="min-h-11 font-bold text-orange-text">
            {t("resend")}
          </button>
        )}
        <button type="button" onClick={onChangeEmail} className="ms-auto min-h-11 font-bold text-orange-text">
          {t("changeEmail")}
        </button>
      </div>
      <label className="flex items-center gap-2 text-[13px]">
        <input type="checkbox" checked={remember} onChange={(event) => setRemember(event.target.checked)} className="h-4 w-4 accent-orange" />
        {t("remember")}
      </label>
    </form>
  );
}

/** Whole seconds left until `sentAt + seconds`, ticking once a second. */
function useCountdown(seconds: number, sentAt: number): number {
  const [left, setLeft] = useState(seconds);

  useEffect(() => {
    const tick = () => setLeft(Math.max(0, Math.ceil(seconds - (Date.now() - sentAt) / 1000)));
    tick();
    const timer = window.setInterval(tick, 1000);
    return () => window.clearInterval(timer);
  }, [seconds, sentAt]);

  return left;
}
