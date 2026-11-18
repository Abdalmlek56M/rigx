"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { SectionForm, useSectionSave } from "./SectionForm";
import { saveInvoice } from "./settings-api";
import { invoiceForm, invoiceValue, type InvoiceForm as Form, type SectionProps } from "./settings-rules";

/** ADM-Settings — invoice: number prefix, background logo, warranty QR and the terms line. */
export function InvoiceForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.invoice");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const [form, setForm] = useState<Form>(() => invoiceForm(view.invoice));
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveInvoice(invoiceValue(form), rowVersion))}
    >
      <div className="grid gap-3 sm:grid-cols-2">
        <TextField label={t("prefix")} name="numberPrefix" ltr maxLength={6} required value={form.numberPrefix} onChange={(numberPrefix) => set({ numberPrefix: numberPrefix.toUpperCase() })} />
        <p className="m-0 self-end pb-3 text-xs text-ink-muted">{t("prefixHint")}</p>
      </div>
      <div className="grid gap-1 sm:grid-cols-2">
        <CheckField label={t("backgroundLogo")} checked={form.showBackgroundLogo} onChange={(showBackgroundLogo) => set({ showBackgroundLogo })} />
        <CheckField label={t("warrantyQr")} checked={form.showWarrantyQr} onChange={(showWarrantyQr) => set({ showWarrantyQr })} />
      </div>
      <TextAreaField label={t("terms")} name="termsLine" value={form.termsLine} onChange={(termsLine) => set({ termsLine })} rows={2} maxLength={300} hint={t("termsHint")} />
    </SectionForm>
  );
}
