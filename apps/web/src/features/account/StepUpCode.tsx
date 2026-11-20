"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { ApiError, unwrap } from "@rigx/api-client";
import { TextField } from "@/components/ui/TextField";
import { normalizeCode } from "@/features/auth/auth-rules";
import { browserApi } from "@/lib/api/browser";

interface StepUpCodeProps {
  /** Runs the protected action with the code; it throws when the action fails. */
  onConfirm: (code: string) => Promise<void>;
  onCancel: () => void;
}

/** Security changes ask for a fresh code emailed to the person: first send it, then type it and confirm. */
export function StepUpCode({ onConfirm, onCancel }: StepUpCodeProps) {
  const t = useTranslations("account.stepUp");
  const [sent, setSent] = useState(false);
  const [code, setCode] = useState("");
  const [busy, setBusy] = useState(false);
  const [problem, setProblem] = useState<"send" | "wrong" | "failed" | null>(null);
  const [secondsLeft, setSecondsLeft] = useState(0);
  const [resent, setResent] = useState(false);

  useEffect(() => {
    if (!sent || secondsLeft <= 0) return;
    const timer = window.setTimeout(() => setSecondsLeft((left) => Math.max(0, left - 1)), 1000);
    return () => window.clearTimeout(timer);
  }, [sent, secondsLeft]);

  async function send(again = false) {
    setBusy(true);
    setProblem(null);
    try {
      await unwrap(browserApi().POST("/api/v1/identity/step-up/request"));
      setSent(true);
      setCode("");
      setSecondsLeft(60);
      setResent(again);
    } catch {
      setProblem("send");
    } finally {
      setBusy(false);
    }
  }

  async function confirm() {
    setBusy(true);
    setProblem(null);
    try {
      await onConfirm(code);
    } catch (error) {
      setProblem(error instanceof ApiError && error.status === 403 ? "wrong" : "failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-3 rounded-control border border-line bg-sand p-4">
      <p className="m-0 text-sm">{sent ? t("sent") : t("intro")}</p>
      {sent ? <p className="m-0 text-xs text-ink-muted">{t("spam")}</p> : null}
      {sent ? (
        <TextField label={t("code")} name="stepUpCode" ltr value={code} onChange={(value) => setCode(normalizeCode(value))} autoComplete="one-time-code" />
      ) : null}
      {sent ? (
        <button
          type="button" disabled={busy || secondsLeft > 0} onClick={() => void send(true)}
          className="self-start text-sm font-bold text-orange-text underline disabled:cursor-not-allowed disabled:text-ink-muted disabled:no-underline"
        >
          {secondsLeft > 0 ? t("resendIn", { seconds: secondsLeft }) : t("resend")}
        </button>
      ) : null}
      {resent ? <p role="status" className="m-0 text-sm font-bold text-success">{t("resent")}</p> : null}
      {problem ? <p role="alert" className="m-0 text-sm text-error">{t(`errors.${problem}`)}</p> : null}
      <div className="flex flex-wrap gap-3">
        {sent ? (
          <button type="button" disabled={busy || code.length !== 6} onClick={() => void confirm()} className="h-11 rounded-control bg-orange px-5 font-bold text-on-orange disabled:opacity-60">{t("confirm")}</button>
        ) : (
          <button type="button" disabled={busy} onClick={() => void send()} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-60">{t("send")}</button>
        )}
        <button type="button" onClick={onCancel} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("cancel")}</button>
      </div>
    </div>
  );
}
