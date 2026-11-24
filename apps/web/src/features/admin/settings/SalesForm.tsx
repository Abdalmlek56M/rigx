"use client";

import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { FieldGrid, FieldNote, SubSection } from "./FieldGrid";
import { SectionForm, useSectionSave } from "./SectionForm";
import { useSectionDraft } from "./use-section-draft";
import { saveSales } from "./settings-api";
import { salesForm, salesValue, type SectionProps } from "./settings-rules";

/** ADM-Settings — orders and POS: stock reservation, the POS discount limit and the public sold feed. */
export function SalesForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.sales");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, set, dirty, reset } = useSectionDraft(salesForm(view.sales));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveSales(salesValue(form), rowVersion))}
    >
      <FieldGrid>
        <TextField label={t("reservationHours")} name="reservationHours" type="number" min="1" max="720" step="1" ltr required value={form.reservationHours} onChange={(reservationHours) => set({ reservationHours })} />
      </FieldGrid>
      <CheckField label={t("autoRelease")} hint={t("autoReleaseHint")} checked={form.reservationAutoRelease} onChange={(reservationAutoRelease) => set({ reservationAutoRelease })} />
      <FieldNote>{t("reservationHint")}</FieldNote>

      <SubSection title={t("posTitle")}>
        <FieldGrid>
          <TextField label={t("posDiscount")} name="posMaxDiscountPercent" type="number" min="0" max="100" step="0.01" ltr value={form.posMaxDiscountPercent} onChange={(posMaxDiscountPercent) => set({ posMaxDiscountPercent })} />
        </FieldGrid>
        <FieldNote>{t("posHint")}</FieldNote>
      </SubSection>
    </SectionForm>
  );
}
