"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { DataTable, type Column } from "../kit/DataTable";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import {
  emptySpecRow,
  specRowValid,
  specRows,
  toSpecDefinitions,
  type EffectiveSpec,
  type SpecRow
} from "./category-rules";
import { saveCategorySpecs, type Category } from "./categories-api";

const SPEC_TYPES = ["Choice", "Number", "YesNo", "Text"] as const;
const inputClass = "h-9 rounded-control border border-input-line bg-surface px-2 text-sm";
const checkClass = "h-4 w-4 accent-orange";

/** The read-only specs table (inherited ones, or own ones for a viewer without the manage permission). */
function ReadOnlySpecs({ rows, label, onRetry }: { rows: readonly SpecRow[]; label: string; onRetry: () => void }) {
  const t = useTranslations("admin.categories.specs");
  const columns: Column<SpecRow>[] = [
    { key: "name", header: t("name"), cell: (row) => <span>{row.name}<span dir="ltr" className="ms-1 font-mono text-[11px] text-ink-muted">{row.key}</span></span> },
    { key: "type", header: t("type"), cell: (row) => t(`types.${row.type as "Choice" | "Number" | "YesNo" | "Text"}`) },
    { key: "filter", header: t("filter"), cell: (row) => (row.isFilter ? "✓" : "") },
    { key: "compatibility", header: t("compatibility"), cell: (row) => (row.isCompatibility ? "✓" : "") }
  ];
  return <DataTable label={label} columns={columns} state={{ status: "ready", data: rows }} onRetry={onRetry} rowKey={(row) => row.id} empty={t("empty")} />;
}

/** The category's own specs (editable) below the inherited read-only ones. */
export function CategorySpecsEditor({ category, effective, canManage, onSaved, onStale }: {
  category: Category;
  effective: readonly EffectiveSpec[];
  canManage: boolean;
  onSaved: (saved: Category) => void;
  onStale: () => void;
}) {
  const t = useTranslations("admin.categories.specs");
  const submit = useSubmit();
  const [rows, setRows] = useState<SpecRow[]>(() => specRows(category.specs));
  const inherited = effective.filter((row) => row.inheritedFrom !== null);
  const inheritedSources = [...new Set(inherited.map((row) => row.inheritedFrom?.name ?? ""))].filter(Boolean).join(" · ");
  const allValid = rows.every(specRowValid);

  function patch(id: string, change: Partial<SpecRow>) {
    setRows((current) => current.map((row) => row.id === id ? { ...row, ...change } : row));
  }

  async function save() {
    if (!canManage || !allValid) return;
    await submit.run(async () => {
      try {
        onSaved(await saveCategorySpecs(category.id, toSpecDefinitions(rows), category.rowVersion));
      } catch (error) {
        onStale();
        throw error;
      }
    }, t("saved"));
  }

  const ownColumns: Column<SpecRow>[] = [
    { key: "key", header: t("key"), cell: (row) => <input aria-label={t("key")} dir="ltr" value={row.key} onChange={(event) => patch(row.id, { key: event.target.value })} className={`${inputClass} w-28 font-mono`} /> },
    { key: "name", header: t("name"), cell: (row) => <input aria-label={t("name")} value={row.name} onChange={(event) => patch(row.id, { name: event.target.value })} className={`${inputClass} w-36`} /> },
    {
      key: "type", header: t("type"), cell: (row) => (
        <select aria-label={t("type")} value={row.type} onChange={(event) => patch(row.id, { type: event.target.value })} className={`${inputClass} w-28`}>
          {SPEC_TYPES.map((type) => <option key={type} value={type}>{t(`types.${type}`)}</option>)}
        </select>
      )
    },
    {
      key: "options", header: t("options"), cell: (row) => (
        <input
          aria-label={t("options")} value={row.options} disabled={row.type !== "Choice"}
          onChange={(event) => patch(row.id, { options: event.target.value })} placeholder={row.type === "Choice" ? t("optionsHint") : ""}
          className={`${inputClass} w-40 disabled:opacity-50`}
        />
      )
    },
    { key: "unit", header: t("unit"), cell: (row) => <input aria-label={t("unit")} value={row.unit} onChange={(event) => patch(row.id, { unit: event.target.value })} className={`${inputClass} w-16`} /> },
    { key: "filter", header: t("filter"), cell: (row) => <input aria-label={t("filter")} type="checkbox" checked={row.isFilter} onChange={(event) => patch(row.id, { isFilter: event.target.checked })} className={checkClass} /> },
    { key: "compatibility", header: t("compatibility"), cell: (row) => <input aria-label={t("compatibility")} type="checkbox" checked={row.isCompatibility} onChange={(event) => patch(row.id, { isCompatibility: event.target.checked })} className={checkClass} /> },
    {
      key: "remove", header: "", cell: (row) => (
        <button type="button" onClick={() => setRows((current) => current.filter((entry) => entry.id !== row.id))} className="h-9 rounded-control px-2 text-xs font-bold text-error">
          {t("remove")}
        </button>
      )
    }
  ];

  return (
    <section className="flex flex-col gap-3 border-t border-line pt-4">
      <div className="flex flex-col gap-0.5">
        <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
        <p className="m-0 text-xs text-ink-muted">{t("inheritedNote")}</p>
      </div>
      {inherited.length > 0 ? (
        <div className="flex flex-col gap-1">
          <p className="m-0 text-xs text-ink-muted">{inheritedSources ? t("inheritedFrom", { parent: inheritedSources }) : t("inherited")}</p>
          <ReadOnlySpecs rows={specRows(inherited.map((row) => row.spec))} label={t("inherited")} onRetry={onStale} />
        </div>
      ) : null}

      {canManage ? (
        <form
          className="flex flex-col gap-3"
          onSubmit={(event) => {
            event.preventDefault();
            void save();
          }}
        >
          <DataTable label={t("title")} columns={ownColumns} state={{ status: "ready", data: rows }} onRetry={onStale} rowKey={(row) => row.id} empty={t("empty")} />
          <div className="flex flex-wrap gap-2">
            <button type="button" onClick={() => setRows((current) => [...current, emptySpecRow()])} className="h-11 rounded-control border border-carbon bg-surface px-4 text-sm font-bold">{t("add")}</button>
            <button type="submit" disabled={submit.busy || !allValid} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("save")}</button>
          </div>
          <FormMessage message={submit.errors.general} />
          {submit.dialog}
        </form>
      ) : (
        <ReadOnlySpecs rows={rows} label={t("title")} onRetry={onStale} />
      )}
      <p className="m-0 text-xs text-ink-muted">{t("keyHint")}</p>
      <p className="m-0 text-xs text-ink-muted">{t("note")}</p>
    </section>
  );
}
