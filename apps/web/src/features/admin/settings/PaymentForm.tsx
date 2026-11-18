"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { SectionForm, useSectionSave } from "./SectionForm";
import { savePayment } from "./settings-api";
import { paymentForm, paymentValue, type PaymentForm as Form } from "./payment-rules";
import type { SectionProps } from "./settings-rules";

/** ADM-Settings — payment: the deposit rule, shipping upfront and the enabled methods with their details. */
export function PaymentForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.payment");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const [form, setForm] = useState<Form>(() => paymentForm(view.payment));
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => savePayment(paymentValue(form), rowVersion))}
    >
      <div className="grid gap-3 sm:grid-cols-2">
        <TextField label={t("depositThreshold")} name="depositThreshold" type="number" min="0" max="10000000" step="0.01" ltr value={form.depositThreshold} onChange={(depositThreshold) => set({ depositThreshold })} />
        <TextField label={t("depositPercent")} name="depositPercent" type="number" min="0" max="100" step="0.01" ltr value={form.depositPercent} onChange={(depositPercent) => set({ depositPercent })} />
      </div>
      <CheckField label={t("shippingUpfront")} hint={t("shippingUpfrontHint")} checked={form.shippingPaidUpfront} onChange={(shippingPaidUpfront) => set({ shippingPaidUpfront })} />
      <p className="m-0 text-xs text-ink-muted">{t("depositHint")}</p>

      <div className="flex flex-col gap-2 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("methodsTitle")}</h3>
        <CheckField label={t("cash")} checked={form.cashOnDeliveryEnabled} onChange={(cashOnDeliveryEnabled) => set({ cashOnDeliveryEnabled })} />
        <CheckField label={t("instaPay")} checked={form.instaPayEnabled} onChange={(instaPayEnabled) => set({ instaPayEnabled })} />
        {form.instaPayEnabled ? (
          <div className="max-w-md ps-6">
            <TextField label={t("instaPayAddress")} name="instaPayAddress" ltr value={form.instaPayAddress} onChange={(instaPayAddress) => set({ instaPayAddress })} />
          </div>
        ) : null}
        <CheckField label={t("wallet")} checked={form.walletEnabled} onChange={(walletEnabled) => set({ walletEnabled })} />
        {form.walletEnabled ? (
          <div className="max-w-md ps-6">
            <TextField label={t("walletNumber")} name="walletNumber" ltr value={form.walletNumber} onChange={(walletNumber) => set({ walletNumber })} />
          </div>
        ) : null}
        <p className="m-0 text-xs text-ink-muted">{t("methodsHint")}</p>
      </div>
    </SectionForm>
  );
}
