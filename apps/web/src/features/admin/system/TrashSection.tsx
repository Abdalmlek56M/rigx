"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { DataTable, type Column } from "../kit/DataTable";
import { FilterBar, FilterSelect } from "../kit/FilterBar";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { useUrlFilters } from "../kit/use-url-filters";
import { SectionCard } from "./SectionCard";
import { purgeTrash, restoreTrash, fetchTrash, type TrashEntry } from "./system-api";
import { SYSTEM_FILTERS, TRASH_KINDS, trashKindKey } from "./system-rules.ts";
import { TrashIcon, UndoIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The trash: what was deleted, restore for `System.Trash`, delete forever for `System.Purge` (asks first, then the step-up code). */
export function TrashSection({ canPurge }: { canPurge: boolean }) {
  const t = useTranslations("admin.system.trash");
  const locale = useLocale();
  const { values, set } = useUrlFilters(SYSTEM_FILTERS);
  const { state, reload } = useLoad(useCallback(() => fetchTrash(values.kind), [values.kind]));
  const submit = useSubmit();
  const [purging, setPurging] = useState<TrashEntry | null>(null);

  const kindLabel = (kind: string) => {
    const known = trashKindKey(kind);
    return known ? t(`kinds.${known}`) : <span dir="ltr">{kind}</span>;
  };

  async function restore(entry: TrashEntry) {
    await submit.run(async () => {
      await restoreTrash(entry.kind, entry.id);
      reload();
    }, t("restored"));
  }

  async function purge(entry: TrashEntry) {
    await submit.run(async (headers) => {
      await purgeTrash(entry.kind, entry.id, headers);
      reload();
    }, t("purged"));
  }

  const columns: Column<TrashEntry>[] = [
    { key: "label", header: t("columns.label"), cell: (entry) => <span className="font-bold [overflow-wrap:anywhere]">{entry.label}</span> },
    { key: "kind", header: t("columns.kind"), cell: (entry) => kindLabel(entry.kind) },
    { key: "deletedBy", header: t("columns.deletedBy"), cell: (entry) => (entry.deletedByUserId ? <span dir="ltr" className="font-mono text-xs">{entry.deletedByUserId.slice(0, 8)}</span> : "—") },
    { key: "deletedAt", header: t("columns.deletedAt"), cell: (entry) => <span dir="ltr">{formatDateTime(entry.deletedAtUtc, locale)}</span> },
    { key: "purgeAfter", header: t("columns.purgeAfter"), cell: (entry) => <span dir="ltr">{formatDateTime(entry.purgeAfterUtc, locale)}</span> },
    {
      key: "actions", header: "", align: "end", cell: (entry) => (
        <div className="flex flex-wrap justify-end gap-2">
          <Button icon={UndoIcon} loading={submit.busy} onClick={() => void restore(entry)}>{t("restore")}</Button>
          {canPurge ? <Button variant="danger" icon={TrashIcon} disabled={submit.busy} onClick={() => setPurging(entry)}>{t("purge")}</Button> : null}
        </div>
      )
    }
  ];

  return (
    <SectionCard title={t("title")} hint={t("hint")}>
      <FilterBar>
        <FilterSelect
          label={t("kindLabel")} value={values.kind} onChange={(kind) => set({ kind })}
          options={[{ value: "", label: t("allKinds") }, ...TRASH_KINDS.map((kind) => ({ value: kind, label: t(`kinds.${kind}`) }))]}
        />
      </FilterBar>
      <FormMessage message={submit.errors.general} />
      <DataTable label={t("title")} columns={columns} state={state} onRetry={reload} rowKey={(entry) => `${entry.kind}:${entry.id}`} empty={t("empty")} />
      {submit.dialog}
      <ConfirmDialog
        open={purging !== null} title={t("purgeTitle")} body={t("purgeBody", { label: purging?.label ?? "" })} confirmLabel={t("purge")} danger
        onClose={() => setPurging(null)} onConfirm={() => (purging ? purge(purging) : Promise.resolve())}
      />
    </SectionCard>
  );
}
