"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import { FilterSelect } from "../kit/FilterBar";
import { PageHeader } from "../kit/PageHeader";
import { useUrlFilters } from "../kit/use-url-filters";
import { BackupForm } from "./BackupForm";
import { ContactLinksForm } from "./ContactLinksForm";
import { ControlsForm } from "./ControlsForm";
import { InvoiceForm } from "./InvoiceForm";
import { LanguageForm } from "./LanguageForm";
import { LeadSourcesForm } from "./LeadSourcesForm";
import { PaymentForm } from "./PaymentForm";
import { ReviewsForm } from "./ReviewsForm";
import { SalesForm } from "./SalesForm";
import { fetchSettings, fetchShippingRates, type GovernorateRate, type SettingsView } from "./settings-api";
import { SETTINGS_FILTERS, SETTINGS_SECTIONS, settingsSection, type SectionProps, type SettingsSection } from "./settings-rules";
import { ShippingForm } from "./ShippingForm";
import { StoreInfoForm } from "./StoreInfoForm";
import { WarrantyForm } from "./WarrantyForm";
import { WatermarkForm } from "./WatermarkForm";
import { WhatsAppForm } from "./WhatsAppForm";
import { RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The one settings load: all sections and the governorate names the shipping groups need. */
async function loadSettings() {
  const [view, rates] = await Promise.all([fetchSettings(), fetchShippingRates()]);
  return { view, rates };
}

function SectionBody({ section, view, rates, onSaved, onStale }: SectionProps & {
  section: SettingsSection;
  rates: readonly GovernorateRate[];
}) {
  const props = { view, onSaved, onStale };
  switch (section) {
    case "store-info": return <StoreInfoForm {...props} />;
    case "contact-links": return <ContactLinksForm {...props} />;
    case "shipping": return <ShippingForm {...props} rates={rates} />;
    case "payment": return <PaymentForm {...props} />;
    case "controls": return <ControlsForm {...props} />;
    case "whatsapp-templates": return <WhatsAppForm {...props} />;
    case "reviews": return <ReviewsForm {...props} />;
    case "language": return <LanguageForm {...props} />;
    case "invoice": return <InvoiceForm {...props} />;
    case "lead-sources": return <LeadSourcesForm {...props} />;
    case "warranty-options": return <WarrantyForm {...props} />;
    case "sales": return <SalesForm {...props} />;
    case "watermark": return <WatermarkForm {...props} />;
    case "backup": return <BackupForm {...props} />;
    default: return null;
  }
}

/** ADM-Settings: one section at a time from the side list (a select on phones), each form saving on its own. */
export function SettingsScreen() {
  const t = useTranslations("admin.settings");
  const { values, set } = useUrlFilters(SETTINGS_FILTERS);
  const section = settingsSection(values.section);
  const load = useCallback(() => loadSettings(), []);
  const { state, reload } = useLoad(load);
  const [saved, setSaved] = useState<SettingsView | null>(null);
  const [generation, setGeneration] = useState(0);
  const view = saved ?? (state.status === "ready" ? state.data.view : null);

  const onSaved = (next: SettingsView) => setSaved(next);
  const onStale = () => {
    setSaved(null);
    setGeneration((current) => current + 1);
    reload();
  };
  const options = SETTINGS_SECTIONS.map((key) => ({ value: key, label: t(`sections.${key}`) }));

  return (
    <>
      <PageHeader title={t("title")} />
      <div className="lg:hidden">
        <FilterSelect label={t("sectionsLabel")} value={section} onChange={(next) => set({ section: next })} options={options} />
      </div>
      <div className="grid gap-4 lg:grid-cols-[220px_minmax(0,1fr)] lg:items-start">
        <nav aria-label={t("sectionsLabel")} className="hidden flex-col gap-1 rounded-card border border-line bg-surface p-2 lg:flex">
          {SETTINGS_SECTIONS.map((key) => (
            <button
              key={key} type="button" aria-current={key === section ? "page" : undefined}
              onClick={() => set({ section: key })}
              className={`h-10 rounded-control px-3 text-start text-sm font-bold ${key === section ? "bg-carbon text-on-carbon" : "hover:bg-paper"}`}
            >
              {t(`sections.${key}`)}
            </button>
          ))}
        </nav>

        <div className="min-w-0">
          {state.status === "error" ? (
            <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
              <p className="m-0 font-bold">{t("failed")}</p>
              <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
            </div>
          ) : view ? (
            <section key={`${section}-${generation}`} className="min-w-0 rounded-card border border-line bg-surface p-5">
              <SectionBody section={section} view={view} rates={state.status === "ready" ? state.data.rates.governorates : []} onSaved={onSaved} onStale={onStale} />
            </section>
          ) : (
            <div aria-hidden="true" className="h-64 animate-pulse rounded-card border border-line bg-surface" />
          )}
        </div>
      </div>
    </>
  );
}
