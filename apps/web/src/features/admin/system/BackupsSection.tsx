"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { DataTable, type Column } from "../kit/DataTable";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { SectionCard } from "./SectionCard";
import { downloadBackup, fetchBackups, runBackup, verifyBackup, type BackupRun } from "./system-api";
import { backupStatusKey, backupSucceeded, backupTriggerKey, formatBytes } from "./system-rules.ts";

/** Backups: the list, "run now", verify, and download (asks first, then the step-up code). */
export function BackupsSection({ canDownload }: { canDownload: boolean }) {
  const t = useTranslations("admin.system.backups");
  const locale = useLocale();
  const { state, reload } = useLoad(useCallback(() => fetchBackups(), []));
  const submit = useSubmit();
  const [downloading, setDownloading] = useState<BackupRun | null>(null);

  async function runNow() {
    await submit.run(async () => {
      const run = await runBackup();
      reload();
      // A failed run is listed with its reason; the form shows the general "didn't work" message instead of a success toast.
      if (!backupSucceeded(run)) throw new Error(run.error ?? run.status);
    }, t("started"));
  }

  async function verify(run: BackupRun) {
    if (!run.id) return;
    const id = run.id;
    await submit.run(async () => {
      await verifyBackup(id);
      reload();
    }, t("verifiedDone"));
  }

  async function save(run: BackupRun) {
    if (!run.id) return;
    const id = run.id;
    await submit.run((headers) => downloadBackup(id, run.fileName, headers), t("downloaded"));
  }

  const verifiedCell = (run: BackupRun) => {
    if (!backupSucceeded(run)) return <span className="text-ink-muted">—</span>;
    if (run.verifyPassed == null) return <span className="text-ink-muted">{t("notVerified")}</span>;
    return run.verifyPassed
      ? <span className="font-bold">{t("verifiedOk", { tables: run.verifyTables ?? 0, rows: run.verifyRows ?? 0 })}</span>
      : <span className="font-bold text-error [overflow-wrap:anywhere]">{run.verifyError || t("verifiedFailed")}</span>;
  };

  const columns: Column<BackupRun>[] = [
    { key: "started", header: t("columns.started"), cell: (run) => <span dir="ltr">{run.startedAtUtc ? formatDateTime(run.startedAtUtc, locale) : "—"}</span> },
    { key: "trigger", header: t("columns.trigger"), cell: (run) => {
      const key = backupTriggerKey(run.trigger);
      return key ? t(`trigger.${key}`) : <span dir="ltr">{run.trigger}</span>;
    } },
    { key: "status", header: t("columns.status"), cell: (run) => {
      const key = backupStatusKey(run.status);
      return (
        <span className="flex flex-col gap-0.5">
          <span className={`font-bold ${backupSucceeded(run) ? "" : "text-error"}`}>{key ? t(`status.${key}`) : run.status}</span>
          {run.error ? <span dir="ltr" className="text-xs text-ink-muted [overflow-wrap:anywhere]">{run.error}</span> : null}
        </span>
      );
    } },
    { key: "size", header: t("columns.size"), align: "end", cell: (run) => <span dir="ltr">{run.sizeBytes ? formatBytes(run.sizeBytes) : "—"}</span> },
    { key: "verified", header: t("columns.verified"), cell: verifiedCell },
    {
      key: "actions", header: "", align: "end", cell: (run) => !backupSucceeded(run) ? null : (
        <div className="flex flex-wrap justify-end gap-2">
          <button type="button" disabled={submit.busy} onClick={() => void verify(run)} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold disabled:opacity-50">{t("verify")}</button>
          {canDownload ? <button type="button" disabled={submit.busy} onClick={() => setDownloading(run)} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold disabled:opacity-50">{t("download")}</button> : null}
        </div>
      )
    }
  ];

  return (
    <SectionCard title={t("title")} hint={t("hint")}>
      <div>
        <button type="button" disabled={submit.busy} onClick={() => void runNow()} className="h-11 rounded-control bg-carbon px-5 text-sm font-bold text-on-carbon disabled:opacity-50">{t("runNow")}</button>
      </div>
      <FormMessage message={submit.errors.general} />
      <DataTable label={t("title")} columns={columns} state={state} onRetry={reload} rowKey={(run) => run.id ?? `${run.startedAtUtc}`} empty={t("empty")} />
      {submit.dialog}
      <ConfirmDialog
        open={downloading !== null} title={t("downloadTitle")} body={t("downloadBody")} confirmLabel={t("download")}
        onClose={() => setDownloading(null)} onConfirm={() => (downloading ? save(downloading) : Promise.resolve())}
      />
    </SectionCard>
  );
}
