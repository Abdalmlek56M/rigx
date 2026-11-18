"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { DataTable, type Column } from "../kit/DataTable";
import { ExpenseTypeDialog } from "./ExpenseTypeDialog";
import type { ExpenseType } from "./expenses-api";

/** The pickable expense types; renaming one keeps old expenses as recorded. */
export function ExpenseTypesPanel({ types, canManage, onChanged }: { types: readonly ExpenseType[]; canManage: boolean; onChanged: () => void }) {
  const t = useTranslations("admin.expenses.types");
  const [editing, setEditing] = useState<ExpenseType | "new" | null>(null);
  const activeLabel = useTranslations("admin.expenses.active");

  const columns: Column<ExpenseType>[] = [
    { key: "name", header: t("name"), cell: (type) => <span className="font-bold">{type.name}</span> },
    { key: "sort", header: t("sortOrder"), align: "end", cell: (type) => <span className="text-sm">{type.sortOrder}</span> },
    { key: "active", header: t("active"), cell: (type) => <span className={`text-sm ${type.isActive ? "font-bold text-success" : "text-ink-muted"}`}>{type.isActive ? activeLabel("yes") : activeLabel("no")}</span> },
    ...(canManage ? [{ key: "edit", header: "", cell: (type: ExpenseType) => <button type="button" onClick={() => setEditing(type)} className="h-9 rounded-control px-2 text-xs font-bold text-orange-text">{t("edit")}</button> }] : [])
  ];

  return (
    <section className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
        {canManage ? <button type="button" onClick={() => setEditing("new")} className="h-11 rounded-control border border-carbon bg-surface px-4 text-sm font-bold">{t("add")}</button> : null}
      </div>
      <DataTable label={t("title")} columns={columns} state={{ status: "ready", data: types }} onRetry={onChanged} rowKey={(type) => type.id} empty={t("empty")} />
      {editing ? <ExpenseTypeDialog type={editing === "new" ? null : editing} onSaved={onChanged} onStale={onChanged} onClose={() => setEditing(null)} /> : null}
    </section>
  );
}
