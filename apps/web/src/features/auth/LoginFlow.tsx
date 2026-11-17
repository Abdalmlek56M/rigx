"use client";

import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import { ApiError, unwrap } from "@rigx/api-client";
import { useRouter } from "@/i18n/navigation";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { classifyCodeFailure, safeNext, type SignInFailure } from "./auth-rules";
import { CodeStep } from "./CodeStep";
import { EmailStep } from "./EmailStep";
import { refreshSession } from "./session-store";
import { useSession } from "./use-session";

type Step = { name: "email" } | { name: "code"; resendAfterSeconds: number; sentAt: number };

/** Email-code and Google sign-in; on success the session is refreshed and the person goes on to `next`. */
export function LoginFlow({ next }: { next: string | undefined }) {
  const t = useTranslations("auth.errors");
  const router = useRouter();
  const session = useSession();
  const [step, setStep] = useState<Step>({ name: "email" });
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const [failure, setFailure] = useState<SignInFailure | null>(null);
  const target = safeNext(next);

  useEffect(() => {
    if (session.status === "signedIn") router.replace(target);
  }, [session.status, router, target]);

  async function finish(): Promise<void> {
    await refreshSession();
    router.replace(target);
  }

  async function sendCode(address: string) {
    setBusy(true);
    setFailure(null);
    try {
      const sent = await required(unwrap(browserApi().POST("/api/v1/identity/email-code/request", { body: { email: address } })));
      setEmail(address);
      setStep({ name: "code", resendAfterSeconds: sent.resendAfterSeconds, sentAt: Date.now() });
    } catch (error) {
      setFailure({ kind: error instanceof ApiError && error.status === 429 ? "rateLimited" : "generic" });
    } finally {
      setBusy(false);
    }
  }

  async function verify(code: string, rememberDevice: boolean) {
    setBusy(true);
    setFailure(null);
    try {
      await unwrap(browserApi().POST("/api/v1/identity/email-code/verify", { body: { email, code, rememberDevice } }));
      await finish();
    } catch (error) {
      setFailure(error instanceof ApiError ? classifyCodeFailure(error.status, error.problem["attemptsLeft"]) : { kind: "generic" });
    } finally {
      setBusy(false);
    }
  }

  async function google(idToken: string) {
    setBusy(true);
    setFailure(null);
    try {
      await unwrap(browserApi().POST("/api/v1/identity/google", { body: { idToken, rememberDevice: true } }));
      await finish();
    } catch {
      setFailure({ kind: "google" });
    } finally {
      setBusy(false);
    }
  }

  const message = failure === null ? null : failure.kind === "wrongCode" ? t("wrongCode", { count: failure.attemptsLeft }) : t(failure.kind);

  if (step.name === "email") {
    return <EmailStep initialEmail={email} busy={busy} error={message} onSubmit={sendCode} onGoogle={google} />;
  }
  return (
    <CodeStep
      email={email}
      resendAfterSeconds={step.resendAfterSeconds}
      sentAt={step.sentAt}
      busy={busy}
      error={message}
      codeDead={failure?.kind === "expired" || failure?.kind === "tooManyAttempts"}
      onSubmit={verify}
      onResend={() => void sendCode(email)}
      onChangeEmail={() => {
        setFailure(null);
        setStep({ name: "email" });
      }}
    />
  );
}
