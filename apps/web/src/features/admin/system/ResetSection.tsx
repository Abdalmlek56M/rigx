"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { CheckIcon, TrashIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { SectionCard } from "./SectionCard";
import { resetData, type ResetResult } from "./system-api";
import { canReset, RESET_CONFIRM, RESET_SECTIONS, toggleResetSection, type ResetSection as Section } from "./system-rules.ts";

type Step = "closed" | "pick" | "type";

/**
 * The guided data reset: "تصفير البيانات" opens the list of what can be reset; the owner ticks items and presses تأكيد; a second
 * dialog asks them to type تصفير or reset; only then the reset runs (and the server still asks for the email code and its own word).
 */
export function ResetSection() {
  const t = useTranslations("admin.system.reset");
  const submit = useSubmit();
  const [step, setStep] = useState<Step>("closed");
  const [selected, setSelected] = useState<Section[]>([]);
  const [typed, setTyped] = useState("");
  const [result, setResult] = useState<ResetResult | null>(null);

  function close() {
    setStep("closed");
    setSelected([]);
    setTyped("");
  }

  async function reset() {
    if (!canReset(selected, typed)) return;
    await submit.run(async (headers) => {
      setResult(await resetData(selected, RESET_CONFIRM, headers));
      close();
    }, t("done"));
  }

  return (
    <SectionCard title={t("title")} hint={t("hint")} danger>
      <div className="flex flex-wrap items-center gap-3">
        <Button variant="danger" icon={TrashIcon} onClick={() => { setResult(null); setStep("pick"); }}>{t("open")}</Button>
        <p className="m-0 text-xs text-ink-muted">{t("stepUpNote")}</p>
      </div>
      {result ? <p role="status" className="m-0 rounded-control bg-paper px-3 py-2 text-sm font-bold">{t("result", { rows: result.rowsDeleted })}</p> : null}

      <Dialog open={step === "pick"} onClose={close} title={t("pickTitle")}>
        <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); if (selected.length > 0) setStep("type"); }}>
          <fieldset className="m-0 flex min-w-0 flex-col gap-1 border-0 p-0">
            <legend className="mb-2 text-sm text-ink-muted">{t("sections")}</legend>
            {RESET_SECTIONS.map((section) => (
              <label key={section} className="flex min-h-11 cursor-pointer items-start gap-3 rounded-control p-2 text-sm hover:bg-paper has-[:checked]:bg-paper">
                <input type="checkbox" checked={selected.includes(section)} onChange={() => setSelected(toggleResetSection(selected, section))} className="mt-0.5 size-4 accent-error" />
                <span className="flex flex-col gap-0.5">
                  <span className="font-bold">{t(`options.${section}.title`)}</span>
                  <span className="text-xs text-ink-muted">{t(`options.${section}.body`)}</span>
                </span>
              </label>
            ))}
          </fieldset>
          {selected.includes("customers") ? <p className="m-0 text-xs text-ink-muted">{t("customersNeedBusiness")}</p> : null}
          <div className="flex flex-wrap justify-end gap-2">
            <Button variant="ghost" onClick={close}>{t("cancel")}</Button>
            <Button type="submit" variant="danger" icon={CheckIcon} disabled={selected.length === 0}>{t("confirm")}</Button>
          </div>
        </form>
      </Dialog>

      <Dialog open={step === "type"} onClose={close} title={t("typeTitle")}>
        <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); void reset(); }}>
          <p className="m-0 text-sm text-ink-muted">{t("typeBody")}</p>
          <ul className="m-0 flex list-none flex-col gap-1 rounded-control bg-paper p-3 text-sm font-bold">
            {selected.map((section) => <li key={section}>{t(`options.${section}.title`)}</li>)}
          </ul>
          <TextField label={t("typeLabel")} name="confirm" autoComplete="off" value={typed} onChange={setTyped} error={submit.errors.fields.confirm} />
          <FormMessage message={submit.errors.general ?? submit.errors.fields.sections ?? null} />
          <div className="flex flex-wrap justify-end gap-2">
            <Button variant="ghost" onClick={() => { setTyped(""); setStep("pick"); }}>{t("back")}</Button>
            <Button type="submit" variant="danger" icon={TrashIcon} loading={submit.busy} disabled={!canReset(selected, typed)}>{t("button")}</Button>
          </div>
        </form>
      </Dialog>
      {submit.dialog}
    </SectionCard>
  );
}
