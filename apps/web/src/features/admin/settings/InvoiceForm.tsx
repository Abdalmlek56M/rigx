"use client";

import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { FieldGrid, FieldNote } from "./FieldGrid";
import { InvoicePreview } from "./InvoicePreview";
import { SectionForm, useSectionSave } from "./SectionForm";
import { useSectionDraft } from "./use-section-draft";
import { saveInvoice } from "./settings-api";
import { invoiceForm, invoiceValue, type SectionProps } from "./settings-rules";

/** ADM-Settings — invoice: number prefix, background logo, warranty QR and the terms line. */
export function InvoiceForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.invoice");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, set, dirty, reset } = useSectionDraft(invoiceForm(view.invoice));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveInvoice(invoiceValue(form), rowVersion))}
    >
      <FieldGrid>
        <TextField label={t("prefix")} name="numberPrefix" ltr maxLength={6} required value={form.numberPrefix} onChange={(numberPrefix) => set({ numberPrefix: numberPrefix.toUpperCase() })} />
      </FieldGrid>
      <FieldNote>{t("prefixHint")}</FieldNote>
      <div className="grid gap-1 sm:grid-cols-2">
        <CheckField label={t("backgroundLogo")} checked={form.showBackgroundLogo} onChange={(showBackgroundLogo) => set({ showBackgroundLogo })} />
        <CheckField label={t("warrantyQr")} checked={form.showWarrantyQr} onChange={(showWarrantyQr) => set({ showWarrantyQr })} />
      </div>
      <TextAreaField label={t("terms")} name="termsLine" value={form.termsLine} onChange={(termsLine) => set({ termsLine })} rows={2} maxLength={300} hint={t("termsHint")} />
      <InvoicePreview
        storeName={view.storeInfo.name ?? ""} prefix={form.numberPrefix} backgroundLogo={form.showBackgroundLogo}
        warrantyQr={form.showWarrantyQr} termsLine={form.termsLine}
      />
    </SectionForm>
  );
}
