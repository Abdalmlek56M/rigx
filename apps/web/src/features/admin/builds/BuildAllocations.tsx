"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { formMessage } from "../form-message";
import { FormMessage } from "../kit/PageHeader";
import { saveAllocations } from "./builds-api";
import { allocationDifference, allocationForm, allocationSum, allocationValid, defaultAllocation, toAllocationInputs } from "./build-rules";
import { useBuildAction, type BuildActionProps } from "./use-build-action";

/** The revenue split between the parts: a percent per part that must add up to 100 (the difference is shown), or reset to each part's standalone share. */
export function BuildAllocations({ canEdit, ...action }: BuildActionProps & { canEdit: boolean }) {
  const t = useTranslations("admin.builds.split");
  const { build } = action;
  const { submit, run } = useBuildAction(action);
  const [form, setForm] = useState(() => allocationForm(build.units));
  const difference = allocationDifference(form);
  const valid = allocationValid(form);

  if (build.units.length === 0) return null;

  async function save() {
    if (!valid) return;
    await run(() => saveAllocations(build.id, toAllocationInputs(form), build.rowVersion));
  }

  return (
    <form
      className="flex min-w-0 flex-col gap-3 rounded-card border border-line bg-surface p-5"
      onSubmit={(event) => {
        event.preventDefault();
        void save();
      }}
    >
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <fieldset disabled={!canEdit} className="m-0 grid min-w-0 gap-3 border-0 p-0 sm:grid-cols-2 xl:grid-cols-3">
        {build.units.map((part) => (
          <label key={part.unitId} className="flex min-w-0 flex-col gap-1.5 text-[13px] font-bold">
            <span dir="auto" className="truncate [unicode-bidi:isolate]">{part.name}</span>
            <input
              type="number" dir="ltr" step="any" min="0" max="100" name={`allocation-${part.stockItemCode}`} value={form[part.unitId] ?? ""}
              onChange={(event) => setForm((current) => ({ ...current, [part.unitId]: event.target.value }))}
              className="h-11 w-full rounded-control border border-input-line bg-surface px-3 text-start text-sm font-normal text-ink"
            />
          </label>
        ))}
      </fieldset>
      <p role="status" className={`m-0 text-sm font-bold ${valid ? "" : "text-error"}`}>
        {t("sum", { sum: allocationSum(form) })}
        {difference === 0 ? null : ` · ${t(difference > 0 ? "missing" : "over", { amount: Math.abs(difference) })}`}
      </p>
      <FormMessage message={formMessage(submit.errors)} />
      {canEdit ? (
        <div className="flex flex-wrap gap-2">
          <button type="submit" disabled={submit.busy || !valid} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("save")}</button>
          <button type="button" onClick={() => setForm(defaultAllocation(build.units))} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("reset")}</button>
        </div>
      ) : null}
      {submit.dialog}
    </form>
  );
}
