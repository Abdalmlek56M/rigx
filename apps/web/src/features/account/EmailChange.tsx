"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { STEP_UP_HEADER, unwrap } from "@rigx/api-client";
import { TextField } from "@/components/ui/TextField";
import { browserApi } from "@/lib/api/browser";
import { isEmail } from "@/features/requests/request-fields";
import { StepUpCode } from "./StepUpCode";

type Step = "closed" | "email" | "code" | "done";

/** Shows the email on the account and changes it: new address → emailed code → a confirmation link goes to the new address. */
export function EmailChange({ email }: { email: string }) {
  const t = useTranslations("account.profile.email");
  const [step, setStep] = useState<Step>("closed");
  const [next, setNext] = useState("");
  const [invalid, setInvalid] = useState(false);

  async function request(code: string) {
    await unwrap(browserApi().POST("/api/v1/identity/email-change/request", { body: { newEmail: next.trim() }, headers: { [STEP_UP_HEADER]: code } }));
    setStep("done");
  }

  function proceed() {
    const valid = isEmail(next);
    setInvalid(!valid);
    if (valid) setStep("code");
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex flex-col gap-0.5">
          <span className="text-[13px] font-bold">{t("label")}</span>
          <span dir="ltr" className="text-start text-sm">{email}</span>
        </div>
        {step === "closed" || step === "done" ? (
          <button type="button" onClick={() => { setNext(""); setStep("email"); }} className="inline-flex min-h-11 items-center text-sm font-bold text-orange-text underline hover:text-orange-text-hover">{t("change")}</button>
        ) : null}
      </div>
      {step === "done" ? <p role="status" className="m-0 text-sm text-success">{t("sent")}</p> : null}
      {step === "email" ? (
        <div className="flex flex-col gap-3">
          <TextField label={t("newEmail")} name="newEmail" type="email" ltr autoComplete="email" value={next} onChange={setNext} error={invalid ? t("invalid") : null} />
          <div className="flex flex-wrap gap-3">
            <button type="button" onClick={proceed} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("continue")}</button>
            <button type="button" onClick={() => setStep("closed")} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("cancel")}</button>
          </div>
        </div>
      ) : null}
      {step === "code" ? <StepUpCode onConfirm={request} onCancel={() => setStep("closed")} /> : null}
    </div>
  );
}
