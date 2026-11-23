"use client";

import { latinNumber } from "@/lib/digits";
import { listSeparator } from "@/lib/format";
import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formMessage } from "../form-message";
import { FormMessage } from "../kit/PageHeader";
import { saveAllocations } from "./builds-api";
import { allocationDifference, allocationForm, allocationSum, allocationValid, defaultAllocation, toAllocationInputs } from "./build-rules";
import { useBuildAction, type BuildActionProps } from "./use-build-action";
import { SaveIcon, UndoIcon } from "@/components/ui/icons";
import { cardClass } from "@/components/ui/Card";
import { Button } from "../kit/Button";

/** The revenue split between the parts: a percent per part that must add up to 100 (the difference is shown), or reset to each part's standalone share. */
export function BuildAllocations({ canEdit, ...action }: BuildActionProps & { canEdit: boolean }) {
  const t = useTranslations("admin.builds.split");
  const locale = useLocale();
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
      className={cardClass({ className: "gap-3" })}
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
              type="text" inputMode="decimal" dir="ltr" name={`allocation-${part.stockItemCode}`} value={form[part.unitId] ?? ""}
              onChange={(event) => setForm((current) => ({ ...current, [part.unitId]: latinNumber(event.target.value) }))}
              className="h-11 w-full rounded-control border border-input-line bg-surface px-3 text-start text-sm font-normal text-ink"
            />
          </label>
        ))}
      </fieldset>
      <p role="status" className={`m-0 text-sm font-bold ${valid ? "" : "text-error-text"}`}>
        {t("sum", { sum: allocationSum(form) })}
        {difference === 0 ? null : `${listSeparator(locale)}${t(difference > 0 ? "missing" : "over", { amount: Math.abs(difference) })}`}
      </p>
      <FormMessage message={formMessage(submit.errors)} />
      {canEdit ? (
        <div className="flex flex-wrap gap-2">
          <Button type="submit" variant="primary" icon={SaveIcon} loading={submit.busy} disabled={!valid}>{t("save")}</Button>
          <Button icon={UndoIcon} onClick={() => setForm(defaultAllocation(build.units))}>{t("reset")}</Button>
        </div>
      ) : null}
      {submit.dialog}
    </form>
  );
}
