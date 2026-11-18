"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { SectionForm, useSectionSave } from "./SectionForm";
import { saveSales } from "./settings-api";
import { salesForm, salesValue, type SalesForm as Form, type SectionProps } from "./settings-rules";

/** ADM-Settings — orders and POS: stock reservation, the POS discount limit and the public sold feed. */
export function SalesForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.sales");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const [form, setForm] = useState<Form>(() => salesForm(view.sales));
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveSales(salesValue(form), rowVersion))}
    >
      <div className="grid gap-3 sm:grid-cols-2">
        <TextField label={t("reservationHours")} name="reservationHours" type="number" min="1" max="720" step="1" ltr required value={form.reservationHours} onChange={(reservationHours) => set({ reservationHours })} />
        <CheckField label={t("autoRelease")} hint={t("autoReleaseHint")} checked={form.reservationAutoRelease} onChange={(reservationAutoRelease) => set({ reservationAutoRelease })} />
      </div>
      <p className="m-0 text-xs text-ink-muted">{t("reservationHint")}</p>

      <div className="flex flex-col gap-3 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("posTitle")}</h3>
        <div className="max-w-xs">
          <TextField label={t("posDiscount")} name="posMaxDiscountPercent" type="number" min="0" max="100" step="0.01" ltr value={form.posMaxDiscountPercent} onChange={(posMaxDiscountPercent) => set({ posMaxDiscountPercent })} />
        </div>
        <p className="m-0 text-xs text-ink-muted">{t("posHint")}</p>
      </div>

      <div className="flex flex-col gap-1 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("feedTitle")}</h3>
        <CheckField label={t("soldFeed")} hint={t("soldFeedHint")} checked={form.soldFeedEnabled} onChange={(soldFeedEnabled) => set({ soldFeedEnabled })} />
        <CheckField label={t("showGovernorate")} checked={form.soldFeedShowGovernorate} onChange={(soldFeedShowGovernorate) => set({ soldFeedShowGovernorate })} />
      </div>
    </SectionForm>
  );
}
