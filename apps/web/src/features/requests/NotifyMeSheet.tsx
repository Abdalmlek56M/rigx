"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { ButtonLink } from "@/components/ui/Button";
import { Button } from "@/components/ui/Button";
import { TextField } from "@/components/ui/TextField";
import { RequestSheet } from "./RequestSheet";
import { isEgyptMobile, isEmail } from "./request-fields";
import { submitRequest, type SubmitOutcome } from "./submit-request";
import { ChoiceRadioTile } from "@/components/ui/ChoiceTile";
import { conditionIcon } from "@/components/ui/condition-icon";

export interface NotifyTarget {
  /** The RX code of the sold-out item (the server finds its model). */
  itemCode?: string;
  /** Shown to the customer, and sent as the model when there is no item code. */
  modelName: string;
}

interface NotifyMeSheetProps {
  open: boolean;
  onClose: () => void;
  target: NotifyTarget;
}

type Errors = Partial<Record<"name" | "phone" | "email", string>>;

/** "Any" is sent as no condition: any new or used item of the model ends the wait. */
const CONDITIONS = ["Any", "New", "Used"] as const;
type Condition = (typeof CONDITIONS)[number];

/** "بلغني لما يتوفر": phone required, email optional. Handles the "available now" answer (409) and success. */
export function NotifyMeSheet({ open, onClose, target }: NotifyMeSheetProps) {
  const t = useTranslations("notify");
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [condition, setCondition] = useState<Condition>("Any");
  const [errors, setErrors] = useState<Errors>({});
  const [busy, setBusy] = useState(false);
  const [outcome, setOutcome] = useState<SubmitOutcome | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const found: Errors = {};
    if (name.trim().length < 2) found.name = t("errors.name");
    if (!isEgyptMobile(phone)) found.phone = t("errors.phone");
    if (email.trim() && !isEmail(email)) found.email = t("errors.email");
    setErrors(found);
    if (Object.keys(found).length > 0) return;

    setBusy(true);
    setOutcome(
      await submitRequest("NotifyMe", {
        name: name.trim(),
        phone: phone.trim(),
        email: email.trim() || null,
        condition: condition === "Any" ? null : condition,
        ...(target.itemCode ? { itemCode: target.itemCode } : { modelName: target.modelName })
      })
    );
    setBusy(false);
  }

  return (
    <RequestSheet open={open} onClose={onClose} title={t("title")}>
      {outcome?.status === "sent" ? (
        <p role="status" className="text-sm leading-7">{t("sent", { model: target.modelName })}</p>
      ) : outcome?.status === "conflict" ? (
        <div role="status" className="flex flex-col gap-3 text-sm leading-7">
          <p>{t("availableNow")}</p>
          <ButtonLink
            href={`/products?search=${encodeURIComponent(target.modelName)}`}
            variant="secondary"
            size="sm"
            className="self-start"
            onClick={onClose}
          >
            {t("seeAvailable")}
          </ButtonLink>
        </div>
      ) : (
        <form onSubmit={submit} noValidate className="flex flex-col gap-3">
          <span dir="auto" className="text-[13px] text-ink-muted [unicode-bidi:isolate]">{target.modelName}</span>
          <div className="flex flex-col gap-1.5">
            <span className="text-[13px] font-bold">{t("condition")}</span>
            <div role="radiogroup" aria-label={t("condition")} className="grid grid-cols-3 gap-2">
              {CONDITIONS.map((value) => (
                <ChoiceRadioTile
                  key={value} name="notify-condition" value={value} icon={conditionIcon(value)} label={t(`conditions.${value}`)}
                  selected={condition === value} onSelect={() => setCondition(value)}
                />
              ))}
            </div>
          </div>
          <TextField label={t("name")} name="name" value={name} onChange={setName} autoComplete="name" required error={errors.name ?? null} />
          <TextField
            label={t("phone")} name="phone" type="tel" ltr autoComplete="tel" placeholder="01xxxxxxxxx" required
            value={phone} onChange={setPhone} error={errors.phone ?? null}
          />
          <TextField
            label={t("email")} name="email" type="email" ltr autoComplete="email" placeholder="name@gmail.com"
            value={email} onChange={setEmail} error={errors.email ?? null}
          />
          {outcome ? <p role="alert" className="text-sm text-error-text">{t(outcome.status === "invalid" ? "errors.invalid" : "errors.failed")}</p> : null}
          <Button
            type="submit"
            variant="primary"
            loading={busy}
            className="w-full"
          >
            {t("submit")}
          </Button>
          <p className="text-xs text-ink-muted">{t("note")}</p>
        </form>
      )}
    </RequestSheet>
  );
}
