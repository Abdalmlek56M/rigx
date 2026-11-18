"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { SectionCard } from "./SectionCard";
import { resetData, type ResetResult } from "./system-api";
import { canReset, RESET_CONFIRM, RESET_SECTIONS, toggleResetSection, type ResetSection as Section } from "./system-rules.ts";

/** The danger zone: pick the sections, type RESET, confirm, then the step-up code. Shows the rows deleted and the backup taken first. */
export function ResetSection() {
  const t = useTranslations("admin.system.reset");
  const submit = useSubmit();
  const [selected, setSelected] = useState<Section[]>([]);
  const [typed, setTyped] = useState("");
  const [confirming, setConfirming] = useState(false);
  const [result, setResult] = useState<ResetResult | null>(null);

  async function reset() {
    await submit.run(async (headers) => {
      setResult(await resetData(selected, typed, headers));
      setSelected([]);
      setTyped("");
    }, t("done"));
  }

  return (
    <SectionCard title={t("title")} hint={t("hint")} danger>
      <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); if (canReset(selected, typed)) setConfirming(true); }}>
        <fieldset className="m-0 flex min-w-0 flex-col gap-2 border-0 p-0">
          <legend className="mb-1 text-[13px] font-bold">{t("sections")}</legend>
          {RESET_SECTIONS.map((section) => (
            <label key={section} className="flex min-h-11 items-start gap-2 text-sm">
              <input type="checkbox" checked={selected.includes(section)} onChange={() => setSelected(toggleResetSection(selected, section))} className="mt-1 size-4" />
              <span className="flex flex-col"><span className="font-bold">{t(`options.${section}.title`)}</span><span className="text-xs text-ink-muted">{t(`options.${section}.body`)}</span></span>
            </label>
          ))}
        </fieldset>
        {selected.includes("customers") ? <p className="m-0 text-xs text-ink-muted">{t("customersNeedBusiness")}</p> : null}
        <TextField label={t("typeLabel", { word: RESET_CONFIRM })} name="confirm" ltr value={typed} onChange={setTyped} error={submit.errors.fields.confirm} />
        <FormMessage message={submit.errors.general ?? submit.errors.fields.sections ?? null} />
        <button type="submit" disabled={submit.busy || !canReset(selected, typed)} className="h-11 self-start rounded-control bg-error px-5 font-bold text-surface disabled:opacity-50">{t("button")}</button>
        <p className="m-0 text-xs text-ink-muted">{t("stepUpNote")}</p>
        {submit.dialog}
      </form>
      {result ? (
        <p role="status" className="m-0 rounded-control bg-paper px-3 py-2 text-sm font-bold">
          {t("result", { rows: result.rowsDeleted })} <span dir="ltr" className="font-mono text-xs">{result.backupRunId}</span>
        </p>
      ) : null}
      <ConfirmDialog open={confirming} title={t("confirmTitle")} body={t("confirmBody")} confirmLabel={t("button")} danger onClose={() => setConfirming(false)} onConfirm={reset} />
    </SectionCard>
  );
}
