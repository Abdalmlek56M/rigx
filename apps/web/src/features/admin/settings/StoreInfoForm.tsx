"use client";

import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { InvoicePreview } from "./InvoicePreview";
import { LogoCard } from "./LogoCard";
import { SectionForm, useSectionSave } from "./SectionForm";
import { useSectionDraft } from "./use-section-draft";
import { saveStoreInfo } from "./settings-api";
import { storeInfoForm, storeInfoValue, type SectionProps } from "./settings-rules";

/** ADM-Settings — store data: identity, pickup details, page summaries and the store logo. */
export function StoreInfoForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.storeInfo");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, set, dirty, reset } = useSectionDraft(storeInfoForm(view.storeInfo));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveStoreInfo(storeInfoValue(form), rowVersion))}
    >
      <div className="grid gap-3 sm:grid-cols-2">
        <TextField label={t("name")} name="name" value={form.name} onChange={(name) => set({ name })} error={submit.errors.fields.name} required />
        <TextField label={t("domain")} name="domain" value={form.domain} onChange={(domain) => set({ domain })} ltr placeholder="rigxhardware.com" error={submit.errors.fields.domain} />
        <TextField label={t("email")} name="email" type="email" value={form.email} onChange={(email) => set({ email })} ltr error={submit.errors.fields.email} />
        <TextField label={t("whatsAppHours")} name="whatsAppHours" value={form.whatsAppHours} onChange={(whatsAppHours) => set({ whatsAppHours })} />
      </div>

      <LogoCard kind="store" storageKey={view.logos.storeLogoKey ?? null} onSaved={onSaved} />
      <InvoicePreview
        storeName={form.name} prefix={view.invoice.numberPrefix ?? "INV"} backgroundLogo={view.invoice.showBackgroundLogo ?? true}
        warrantyQr={view.invoice.showWarrantyQr ?? true} termsLine={view.invoice.termsLine ?? ""}
      />

      <div className="flex flex-col gap-3 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("pickupTitle")}</h3>
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("pickupAddress")} name="pickupAddress" value={form.pickupAddress} onChange={(pickupAddress) => set({ pickupAddress })} />
          <TextField label={t("pickupHours")} name="pickupHours" value={form.pickupHours} onChange={(pickupHours) => set({ pickupHours })} />
        </div>
        <TextAreaField label={t("pickupInstructions")} name="pickupInstructions" value={form.pickupInstructions} onChange={(pickupInstructions) => set({ pickupInstructions })} rows={2} maxLength={1000} />
        <p className="m-0 text-xs text-ink-muted">{t("pickupHint")}</p>
      </div>

      <div className="flex flex-col gap-3 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("summariesTitle")}</h3>
        <div className="grid gap-3 lg:grid-cols-3">
          <TextAreaField label={t("warrantySummary")} name="warrantySummary" value={form.warrantySummary} onChange={(warrantySummary) => set({ warrantySummary })} rows={3} maxLength={1000} />
          <TextAreaField label={t("serviceSummary")} name="serviceSummary" value={form.serviceSummary} onChange={(serviceSummary) => set({ serviceSummary })} rows={3} maxLength={1000} />
          <TextAreaField label={t("consultationSummary")} name="consultationSummary" value={form.consultationSummary} onChange={(consultationSummary) => set({ consultationSummary })} rows={3} maxLength={1000} />
        </div>
      </div>
    </SectionForm>
  );
}
