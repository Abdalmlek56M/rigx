"use client";

import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { FieldGrid, FieldNote } from "./FieldGrid";
import { SectionForm, useSectionSave } from "./SectionForm";
import { useSectionDraft } from "./use-section-draft";
import { saveReviews } from "./settings-api";
import { reviewsForm, reviewsValue, type SectionProps } from "./settings-rules";

/** ADM-Settings — reviews: on/off and how many approved reviews a product needs before its stars show. */
export function ReviewsForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.reviews");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, setForm, dirty, reset } = useSectionDraft(reviewsForm(view.reviews));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveReviews(reviewsValue(form), rowVersion))}
    >
      <CheckField label={t("enabled")} checked={form.enabled} onChange={(enabled) => setForm({ ...form, enabled })} />
      <FieldGrid>
        <TextField
          label={t("minimum")} name="minimumCountToShowStars" type="number" min="1" max="50" step="1" ltr required
          value={form.minimumCountToShowStars} onChange={(minimumCountToShowStars) => setForm({ ...form, minimumCountToShowStars })}
        />
      </FieldGrid>
      <FieldNote>{t("minimumHint")}</FieldNote>
    </SectionForm>
  );
}
