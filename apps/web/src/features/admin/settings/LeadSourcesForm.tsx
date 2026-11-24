"use client";

import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { ListEditor } from "./ListEditor";
import { SectionForm, useSectionSave } from "./SectionForm";
import { useSectionDraft } from "./use-section-draft";
import { saveLeadSources } from "./settings-api";
import { leadSourcesForm, leadSourcesValue, type SectionProps } from "./settings-rules";

/** ADM-Settings — lead sources: the answers to "how did you hear about us?", in display order. */
export function LeadSourcesForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.leadSources");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, setForm, dirty, reset } = useSectionDraft(leadSourcesForm(view.leadSources));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveLeadSources(leadSourcesValue(form), rowVersion))}
    >
      <ListEditor
        label={t("listLabel")} items={form.sources} onChange={(sources) => setForm({ sources })}
        create={() => ""} addLabel={t("add")} max={30}
        renderItem={(source, update) => (
          <TextField label={t("source")} name="source" value={source} onChange={update} placeholder={t("source")} labelClassName="sr-only" />
        )}
      />
    </SectionForm>
  );
}
