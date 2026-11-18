"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { SectionForm, useSectionSave } from "./SectionForm";
import { saveWhatsApp } from "./settings-api";
import { WHATSAPP_PLACEHOLDERS, whatsAppForm, whatsAppValue, type SectionProps, type WhatsAppForm as Form } from "./settings-rules";

/** ADM-Settings — the ready WhatsApp messages, one per place the storefront sends one from. */
export function WhatsAppForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.whatsapp");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const [form, setForm] = useState<Form>(() => whatsAppForm(view.whatsAppTemplates));
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveWhatsApp(whatsAppValue(form), rowVersion))}
    >
      <div className="grid gap-3 lg:grid-cols-2">
        <TextAreaField label={t("general")} name="general" value={form.general} onChange={(general) => set({ general })} rows={2} maxLength={500} required />
        <TextAreaField label={t("product")} name="product" value={form.product} onChange={(product) => set({ product })} rows={2} maxLength={500} required />
        <TextAreaField label={t("build")} name="build" value={form.build} onChange={(build) => set({ build })} rows={2} maxLength={500} required />
        <TextAreaField label={t("order")} name="order" value={form.order} onChange={(order) => set({ order })} rows={2} maxLength={500} required />
        <TextAreaField label={t("warranty")} name="warranty" value={form.warranty} onChange={(warranty) => set({ warranty })} rows={2} maxLength={500} required />
        <TextAreaField label={t("requestReply")} name="requestReply" value={form.requestReply} onChange={(requestReply) => set({ requestReply })} rows={2} maxLength={500} required />
        <TextAreaField label={t("backInStock")} name="backInStock" value={form.backInStock} onChange={(backInStock) => set({ backInStock })} rows={2} maxLength={500} required />
      </div>
      <p dir="auto" className="m-0 text-xs text-ink-muted">{t("placeholders", { list: WHATSAPP_PLACEHOLDERS.join(" ") })}</p>
    </SectionForm>
  );
}
