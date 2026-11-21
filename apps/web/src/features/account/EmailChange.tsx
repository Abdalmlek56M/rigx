"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { STEP_UP_HEADER, unwrap } from "@rigx/api-client";
import { Button, IconButton } from "@/components/ui/Button";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { PencilIcon } from "@/components/ui/icons";
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
      <div className="flex flex-wrap items-start justify-between gap-2">
        <InfoGrid columns={1}>
          <InfoField label={t("label")} ltr value={email} />
        </InfoGrid>
        {step === "closed" || step === "done" ? (
          <IconButton
            icon={PencilIcon}
            label={t("change")}
            variant="secondary"
            size="sm"
            onClick={() => { setNext(""); setStep("email"); }}
          />
        ) : null}
      </div>
      {step === "done" ? <p role="status" className="m-0 text-sm text-success">{t("sent")}</p> : null}
      {step === "email" ? (
        <div className="flex flex-col gap-3">
          <TextField label={t("newEmail")} name="newEmail" type="email" ltr autoComplete="email" value={next} onChange={setNext} error={invalid ? t("invalid") : null} />
          <div className="flex flex-wrap gap-3">
            <Button type="button" variant="primary" onClick={proceed}>{t("continue")}</Button>
            <Button type="button" variant="secondary" onClick={() => setStep("closed")}>{t("cancel")}</Button>
          </div>
        </div>
      ) : null}
      {step === "code" ? <StepUpCode onConfirm={request} onCancel={() => setStep("closed")} /> : null}
    </div>
  );
}
