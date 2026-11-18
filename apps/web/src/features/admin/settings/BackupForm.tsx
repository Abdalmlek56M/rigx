"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { CheckField } from "./CheckField";
import { SectionForm, useSectionSave } from "./SectionForm";
import { saveBackup } from "./settings-api";
import { backupForm, backupValue, type BackupForm as Form, type SectionProps } from "./settings-rules";

/** ADM-Settings — the daily backup: on/off, the Cairo hour and how many days are kept. */
export function BackupForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.backup");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const [form, setForm] = useState<Form>(() => backupForm(view.backup));
  const [confirming, setConfirming] = useState(false);
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));

  return (
    <>
      <SectionForm
        title={t("title")} hint={t("hint")} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
        onSubmit={() => void save((rowVersion) => saveBackup(backupValue(form), rowVersion))}
      >
        <CheckField
          label={t("enabled")} hint={t("enabledHint")} checked={form.enabled}
          onChange={(enabled) => {
            if (enabled) set({ enabled: true });
            else setConfirming(true);
          }}
        />
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("hour")} name="hourCairo" type="number" min="0" max="23" step="1" ltr required value={form.hourCairo} onChange={(hourCairo) => set({ hourCairo })} />
          <TextField label={t("retention")} name="retentionDays" type="number" min="3" max="90" step="1" ltr required value={form.retentionDays} onChange={(retentionDays) => set({ retentionDays })} />
        </div>
        <p className="m-0 text-xs text-ink-muted">{t("hint2")}</p>
      </SectionForm>
      <ConfirmDialog
        open={confirming} title={t("risk.title")} body={t("risk.body")} confirmLabel={t("risk.confirm")}
        onClose={() => setConfirming(false)}
        onConfirm={async () => {
          const next = { ...form, enabled: false };
          setForm(next);
          await save((rowVersion) => saveBackup(backupValue(next), rowVersion));
        }}
      />
    </>
  );
}
