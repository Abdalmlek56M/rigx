"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { CheckField } from "./CheckField";
import { SectionForm, useSectionSave } from "./SectionForm";
import { useSectionDraft } from "./use-section-draft";
import { saveLanguage } from "./settings-api";
import { languageForm, languageValue, type SectionProps } from "./settings-rules";

/** ADM-Settings — language: the default language and whether the EN switch exists. */
export function LanguageForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.language");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, setForm, dirty, reset } = useSectionDraft(languageForm(view.language));
  const [confirming, setConfirming] = useState(false);

  function submitForm() {
    void save((rowVersion) => saveLanguage(languageValue(form), rowVersion));
  }

  return (
    <>
      <SectionForm
        title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
        onSubmit={submitForm}
      >
        <div className="max-w-sm">
          <SelectField
            label={t("default")} name="defaultLanguage" value={form.defaultLanguage}
            onChange={(defaultLanguage) => setForm({ ...form, defaultLanguage })}
            options={[
              { value: "ar", label: t("arabic") },
              ...(form.englishEnabled ? [{ value: "en", label: t("englishName") }] : [])
            ]}
          />
        </div>
        <CheckField
          label={t("english")} hint={t("englishHint")} checked={form.englishEnabled}
          onChange={(englishEnabled) => {
            if (englishEnabled) setForm({ ...form, englishEnabled: true });
            else setConfirming(true);
          }}
        />
        <p className="m-0 text-xs text-ink-muted">{t("note")}</p>
      </SectionForm>
      <ConfirmDialog
        open={confirming} title={t("risk.title")} body={t("risk.body")} confirmLabel={t("risk.confirm")}
        onClose={() => setConfirming(false)}
        onConfirm={async () => {
          const next = { ...form, englishEnabled: false, defaultLanguage: form.defaultLanguage === "en" ? "ar" : form.defaultLanguage };
          setForm(next);
          await save((rowVersion) => saveLanguage(languageValue(next), rowVersion));
        }}
      />
    </>
  );
}
