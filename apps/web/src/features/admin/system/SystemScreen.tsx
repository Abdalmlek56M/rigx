"use client";

import { useTranslations } from "next-intl";
import { PageHeader } from "../kit/PageHeader";
import { usePermissions } from "../use-permissions";
import { BackupsSection } from "./BackupsSection";
import { ExportSection } from "./ExportSection";
import { ResetSection } from "./ResetSection";
import { exportableDatasets } from "./system-rules.ts";
import { TrashSection } from "./TrashSection";

/** ADM-System: trash, backups, export and the data reset, each shown only with its own permission; the reset is last. */
export function SystemScreen() {
  const t = useTranslations("admin.system");
  const { has } = usePermissions();
  const datasets = exportableDatasets(has);
  return (
    <>
      <PageHeader title={t("title")} />
      {has("System.Trash") ? <TrashSection canPurge={has("System.Purge")} /> : null}
      {has("System.ManageBackups") ? <BackupsSection canDownload={has("System.BackupDownload")} /> : null}
      {datasets.length > 0 ? <ExportSection datasets={datasets} /> : null}
      {has("System.ResetData") ? <ResetSection /> : null}
    </>
  );
}
