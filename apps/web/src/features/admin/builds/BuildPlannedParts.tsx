"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { formatEgp } from "@/lib/format";
import { formMessage } from "../form-message";
import { FormMessage } from "../kit/PageHeader";
import { ModelInput } from "../purchases/ModelInput";
import { BrandSelect } from "../stock/BrandSelect";
import { CategorySelect } from "../stock/CategorySelect";
import type { BuildLookups } from "./BuildParts";
import { savePlannedParts } from "./builds-api";
import {
  emptyPlannedRow, pickPlannedModel, plannedRows, plannedRowsValid, plannedTotal, toPlannedInputs, typePlannedName, type PlannedRow
} from "./build-rules";
import { useBuildAction, type BuildActionProps } from "./use-build-action";
import { BoxIcon, PlusIcon, SaveIcon, TrashIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

let rowCounter = 0;
const newRowKey = () => `planned-${++rowCounter}`;

/**
 * The build's typed parts: parts the owner lists although they are not in stock (made to order). Nothing is created or reserved;
 * a model that is in stock is linked to that item. The whole list is saved at once. The editor remounts it after every build change.
 */
export function BuildPlannedParts({ canEdit, lookups, ...action }: BuildActionProps & { canEdit: boolean; lookups: BuildLookups }) {
  const t = useTranslations("admin.builds.planned");
  const locale = useLocale();
  const { build } = action;
  const { submit, run } = useBuildAction(action);
  const [rows, setRows] = useState<PlannedRow[]>(() => plannedRows(build.plannedParts));
  const change = (key: string, next: (row: PlannedRow) => PlannedRow) => setRows((current) => current.map((row) => (row.key === key ? next(row) : row)));
  const field = (index: number, name: string) => submit.errors.fields[`parts.${index}.${name}`] ?? submit.errors.fields[`parts[${index}].${name}`];

  async function save() {
    if (!plannedRowsValid(rows)) return;
    await run(() => savePlannedParts(build.id, toPlannedInputs(rows), build.rowVersion), t("saved"));
  }

  return (
    <section className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
        {rows.length > 0 ? (
          <span className="text-sm"><span className="text-ink-muted">{t("total")}</span> <span className="font-bold">{formatEgp(plannedTotal(rows), locale)}</span></span>
        ) : null}
      </div>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <fieldset disabled={!canEdit} className="m-0 flex min-w-0 flex-col gap-3 border-0 p-0">
        {rows.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("empty")}</p> : null}
        <ul className="m-0 flex list-none flex-col gap-3 p-0">
          {rows.map((row, index) => (
            <li key={row.key} className="grid items-start gap-3 rounded-control border border-line bg-paper p-3 sm:grid-cols-2 xl:grid-cols-[minmax(0,2fr)_minmax(0,1fr)_minmax(0,1fr)_minmax(0,1fr)_auto]">
              <div className="flex min-w-0 flex-col gap-1">
                <ModelInput
                  name={`planned${index}-name`} value={row.name} categoryId={row.categoryId} error={field(index, "name")}
                  onType={(text) => change(row.key, (current) => typePlannedName(current, text))}
                  onPick={(suggestion) => change(row.key, (current) => pickPlannedModel(current, suggestion))}
                />
                {row.stockItemCode ? (
                  <span className="flex items-center gap-1 text-xs font-bold text-ink-muted"><BoxIcon size={14} />{t("linked", { code: row.stockItemCode })}</span>
                ) : null}
              </div>
              <CategorySelect tree={lookups.tree} name={`planned${index}-category`} value={row.categoryId} onChange={(categoryId) => change(row.key, (current) => ({ ...current, categoryId }))} />
              <BrandSelect brands={lookups.brands} tree={lookups.tree} categoryId={row.categoryId} name={`planned${index}-brand`} value={row.brandId} onChange={(brandId) => change(row.key, (current) => ({ ...current, brandId }))} />
              <TextField
                label={t("price")} name={`planned${index}-price`} type="number" ltr value={row.price} error={field(index, "price")}
                onChange={(price) => change(row.key, (current) => ({ ...current, price }))}
              />
              <Button size="sm" variant="danger" icon={TrashIcon} className="self-end" onClick={() => setRows((current) => current.filter((entry) => entry.key !== row.key))}>
                {t("remove")}
              </Button>
            </li>
          ))}
        </ul>
        <FormMessage message={formMessage(submit.errors)} />
        {canEdit ? (
          <div className="flex flex-wrap gap-2">
            <Button icon={PlusIcon} onClick={() => setRows((current) => [...current, emptyPlannedRow(newRowKey())])}>{t("add")}</Button>
            <Button variant="primary" icon={SaveIcon} loading={submit.busy} disabled={!plannedRowsValid(rows)} onClick={() => void save()}>{t("save")}</Button>
          </div>
        ) : null}
      </fieldset>
      {submit.dialog}
    </section>
  );
}
