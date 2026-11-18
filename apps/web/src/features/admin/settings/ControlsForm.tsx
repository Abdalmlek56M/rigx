"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { CheckField } from "./CheckField";
import { ListEditor } from "./ListEditor";
import { SectionForm, useSectionSave } from "./SectionForm";
import { controlsForm, controlsValue, HOMEPAGE_SECTION_KEYS, type ControlsForm as Form } from "./controls-rules";
import { saveControls } from "./settings-api";
import type { SectionProps } from "./settings-rules";

type Risk = "maintenance" | "guest";

/** ADM-Settings — store controls: maintenance, announcement, sold-out, countdown, guest checkout and the homepage order. */
export function ControlsForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.controls");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const [form, setForm] = useState<Form>(() => controlsForm(view.controls));
  const [risks, setRisks] = useState<Risk[]>([]);
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));

  function saveSection(rowVersion: number) {
    return saveControls(controlsValue(form), rowVersion);
  }

  function submitForm() {
    const found: Risk[] = [];
    if (!view.controls.maintenanceMode && form.maintenanceMode) found.push("maintenance");
    if (Boolean(view.controls.guestCheckoutEnabled) && !form.guestCheckoutEnabled) found.push("guest");
    if (found.length > 0) setRisks(found);
    else void save(saveSection);
  }

  return (
    <>
      <SectionForm
        title={t("title")} hint={t("hint")} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
        onSubmit={submitForm}
      >
        <CheckField label={t("maintenance")} hint={t("maintenanceHint")} checked={form.maintenanceMode} onChange={(maintenanceMode) => set({ maintenanceMode })} />
        <TextAreaField label={t("announcement")} name="announcementText" value={form.announcementText} onChange={(announcementText) => set({ announcementText })} rows={2} maxLength={200} hint={t("announcementHint")} />

        <div className="grid gap-1 border-t border-line pt-3 sm:grid-cols-2">
          <CheckField label={t("showSoldOut")} checked={form.showSoldOut} onChange={(showSoldOut) => set({ showSoldOut })} />
          <CheckField label={t("showCountdown")} checked={form.showOfferCountdown} onChange={(showOfferCountdown) => set({ showOfferCountdown })} />
          <CheckField label={t("guestCheckout")} hint={t("guestCheckoutHint")} checked={form.guestCheckoutEnabled} onChange={(guestCheckoutEnabled) => set({ guestCheckoutEnabled })} />
          <TextField label={t("minimumOrder")} name="minimumOrderAmount" type="number" min="0" max="10000000" step="0.01" ltr value={form.minimumOrderAmount} onChange={(minimumOrderAmount) => set({ minimumOrderAmount })} />
        </div>

        <div className="flex flex-col gap-2 border-t border-line pt-3">
          <h3 className="m-0 text-[15px] font-bold">{t("homepageTitle")}</h3>
          <p className="m-0 text-xs text-ink-muted">{t("homepageHint")}</p>
          <ListEditor
            fixed label={t("homepageLabel")} items={form.homepageSections}
            onChange={(homepageSections) => set({ homepageSections })}
            renderItem={(section, update) => {
              const known = HOMEPAGE_SECTION_KEYS.find((key) => key === section.key);
              return (
                <CheckField
                  label={known ? t(`homepage.${known}`) : section.key}
                  checked={section.visible} onChange={(visible) => update({ ...section, visible })}
                />
              );
            }}
          />
        </div>
      </SectionForm>

      <ConfirmDialog
        open={risks.length > 0} title={t("risk.title")} body={risks.map((risk) => t(`risk.${risk}`)).join(" ")}
        confirmLabel={t("risk.confirm")}
        onClose={() => setRisks([])}
        onConfirm={async () => {
          await save(saveSection);
        }}
      />
    </>
  );
}
