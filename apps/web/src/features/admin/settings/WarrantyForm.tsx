"use client";

import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { ListEditor } from "./ListEditor";
import { SectionForm, useSectionSave } from "./SectionForm";
import { useSectionDraft } from "./use-section-draft";
import { saveWarrantyOptions } from "./settings-api";
import { emptyWarrantyOption, warrantyForm, warrantyValue, type SectionProps } from "./settings-rules";

const ROW = "md:grid-cols-[minmax(0,1fr)_140px]";

/** ADM-Settings — the warranty labels the stock form offers; an empty months box means the agent's warranty. */
export function WarrantyForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.warranty");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, setForm, dirty, reset } = useSectionDraft(warrantyForm(view.warrantyOptions));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveWarrantyOptions(warrantyValue(form), rowVersion))}
    >
      <ListEditor
        label={t("listLabel")} items={form.options} onChange={(options) => setForm({ options })}
        create={emptyWarrantyOption} addLabel={t("add")} max={20}
        columns={{ labels: [t("label"), t("months")], className: ROW }}
        renderItem={(option, update) => (
          <div className={`grid gap-2 ${ROW}`}>
            <TextField label={t("label")} name="warrantyLabel" value={option.label} labelClassName="md:sr-only" onChange={(label) => update({ ...option, label })} />
            <TextField label={t("months")} name="warrantyMonths" type="number" min="1" max="120" step="1" ltr value={option.months} labelClassName="md:sr-only" onChange={(months) => update({ ...option, months })} />
          </div>
        )}
      />
      <p className="m-0 text-xs text-ink-muted">{t("monthsHint")}</p>
    </SectionForm>
  );
}
