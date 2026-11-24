"use client";

import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { FieldGrid, FieldNote, SubSection } from "./FieldGrid";
import { SectionForm, useSectionSave } from "./SectionForm";
import { savePayment } from "./settings-api";
import { paymentForm, paymentValue, TRANSFER_INSTRUCTIONS_MAX } from "./payment-rules";
import type { SectionProps } from "./settings-rules";
import { useSectionDraft } from "./use-section-draft";

/** ADM-Settings — payment: the deposit rule, shipping upfront, the enabled methods with their details and the transfer instructions. */
export function PaymentForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.payment");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, set, dirty, reset } = useSectionDraft(paymentForm(view.payment));
  const transfer = form.instaPayEnabled || form.walletEnabled;

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => savePayment(paymentValue(form), rowVersion))}
    >
      <CheckField label={t("depositRequired")} hint={t("depositRequiredHint")} checked={form.depositRequired} onChange={(depositRequired) => set({ depositRequired })} />
      {form.depositRequired ? (
        <FieldGrid>
          <TextField label={t("depositThreshold")} name="depositThreshold" type="number" min="0" max="10000000" step="0.01" ltr value={form.depositThreshold} onChange={(depositThreshold) => set({ depositThreshold })} />
          <TextField label={t("depositPercent")} name="depositPercent" type="number" min="0" max="100" step="0.01" ltr value={form.depositPercent} onChange={(depositPercent) => set({ depositPercent })} />
        </FieldGrid>
      ) : null}
      <CheckField label={t("shippingUpfront")} hint={t("shippingUpfrontHint")} checked={form.shippingPaidUpfront} onChange={(shippingPaidUpfront) => set({ shippingPaidUpfront })} />
      <FieldNote>{t("depositHint")}</FieldNote>

      <SubSection title={t("methodsTitle")}>
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
        <FieldNote>{t("methodsHint")}</FieldNote>
      </SubSection>

      <SubSection title={t("transferTitle")}>
        <TextAreaField
          label={t("transferInstructions")} name="transferInstructions" rows={4} maxLength={TRANSFER_INSTRUCTIONS_MAX}
          value={form.transferInstructions} onChange={(transferInstructions) => set({ transferInstructions })}
          hint={transfer ? t("transferHint") : t("transferOffHint")}
        />
        <FieldNote>{form.transferInstructions.length}/{TRANSFER_INSTRUCTIONS_MAX}</FieldNote>
      </SubSection>
    </SectionForm>
  );
}
