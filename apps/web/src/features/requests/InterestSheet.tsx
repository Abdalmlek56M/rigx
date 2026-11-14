"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { RequestSheet } from "./RequestSheet";
import { isEgyptMobile } from "./request-fields";
import { submitRequest, type SubmitOutcome } from "./submit-request";

interface InterestSheetProps {
  open: boolean;
  onClose: () => void;
  /** What the visitor is interested in (a service that is not open yet): sent as the request's subject. */
  subject: string;
}

type Errors = Partial<Record<"name" | "phone", string>>;

/** "سجّل اهتمامك": name and phone become a Contact request about `subject`; the store calls when the service opens. */
export function InterestSheet({ open, onClose, subject }: InterestSheetProps) {
  const t = useTranslations("interest");
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [errors, setErrors] = useState<Errors>({});
  const [busy, setBusy] = useState(false);
  const [outcome, setOutcome] = useState<SubmitOutcome | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const found: Errors = {};
    if (name.trim().length < 2) found.name = t("errors.name");
    if (!isEgyptMobile(phone)) found.phone = t("errors.phone");
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    setBusy(true);
    setOutcome(await submitRequest("Contact", { name: name.trim(), phone: phone.trim(), subject, message: t("message", { subject }) }));
    setBusy(false);
  }

  return (
    <RequestSheet open={open} onClose={onClose} title={t("title")}>
      {outcome?.status === "sent" ? (
        <p role="status" className="text-sm leading-7">{t("sent", { subject })}</p>
      ) : (
        <form onSubmit={submit} noValidate className="flex flex-col gap-3">
          <span dir="auto" className="text-[13px] text-ink-muted [unicode-bidi:isolate]">{subject}</span>
          <TextField label={t("name")} name="name" value={name} onChange={setName} autoComplete="name" required error={errors.name ?? null} />
          <TextField
            label={t("phone")} name="phone" type="tel" ltr autoComplete="tel" placeholder="01xxxxxxxxx" required
            value={phone} onChange={setPhone} error={errors.phone ?? null}
          />
          {outcome ? <p role="alert" className="text-sm text-error">{t(outcome.status === "invalid" ? "errors.invalid" : "errors.failed")}</p> : null}
          <button type="submit" disabled={busy} className="h-12 w-full rounded-control bg-carbon text-sm font-bold text-on-carbon disabled:opacity-60">
            {t("submit")}
          </button>
          <p className="text-xs text-ink-muted">{t("note")}</p>
        </form>
      )}
    </RequestSheet>
  );
}
