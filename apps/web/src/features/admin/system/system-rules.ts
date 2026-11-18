/** System page: trash kinds, reset section rules, export datasets by permission and download file names. Pure, so it is tested. */

/** The kinds the trash holds today; a kind the server adds later still shows (its code as-is). */
export const TRASH_KINDS = ["article", "faq", "media-asset", "blacklist"] as const;

export const SYSTEM_FILTERS = { kind: "" };

export function trashKindKey(kind: string): (typeof TRASH_KINDS)[number] | null {
  return TRASH_KINDS.find((known) => known === kind) ?? null;
}

export const RESET_SECTIONS = ["business", "content", "customers"] as const;
export type ResetSection = (typeof RESET_SECTIONS)[number];
export const RESET_CONFIRM = "RESET";

/** Ticks or unticks a section: customers needs business, so ticking customers ticks business and unticking business unticks customers. */
export function toggleResetSection(selected: readonly ResetSection[], section: ResetSection): ResetSection[] {
  if (selected.includes(section)) {
    return selected.filter((entry) => entry !== section && !(section === "business" && entry === "customers"));
  }
  const next = [...selected, section];
  return section === "customers" && !next.includes("business") ? [...next, "business"] : next;
}

/** The reset button is enabled only for a real choice and the exact typed word (case matters, like the server). */
export function canReset(selected: readonly ResetSection[], typed: string): boolean {
  return selected.length > 0 && typed === RESET_CONFIRM;
}

export const EXPORT_DATASETS = [
  { name: "orders", permission: "Sales.ViewOrders" },
  { name: "stock-items", permission: "Inventory.View" },
  { name: "expenses", permission: "Finance.ViewReports" },
  { name: "purchases", permission: "Purchasing.View" },
  { name: "money-movements", permission: "Finance.ViewVaults" },
  { name: "audit", permission: "System.ViewAuditLogs" },
  { name: "customers", permission: "Customers.View" }
] as const;
export type ExportDataset = (typeof EXPORT_DATASETS)[number]["name"];

export const EXPORT_FORMATS = ["csv", "xlsx", "xml"] as const;
export type ExportFormat = (typeof EXPORT_FORMATS)[number];

/** The datasets this person may export. */
export function exportableDatasets(has: (permission: string) => boolean): ExportDataset[] {
  return EXPORT_DATASETS.filter((dataset) => has(dataset.permission)).map((dataset) => dataset.name);
}

/** `rigx-<dataset>-<from>-<to>.<ext>`; an empty date (the server's last-30-days default) is left out. */
export function exportFileName(dataset: ExportDataset, from: string, to: string, format: ExportFormat): string {
  return `${["rigx", dataset, from, to].filter(Boolean).join("-")}.${format}`;
}

export const BACKUP_TRIGGERS = ["Scheduled", "Manual", "BeforeReset"] as const;
export const BACKUP_STATUSES = ["Succeeded", "Failed"] as const;

/** The label key of a backup trigger or status the screen knows; anything new shows as the server sent it. */
export function backupTriggerKey(trigger: string): (typeof BACKUP_TRIGGERS)[number] | null {
  return BACKUP_TRIGGERS.find((known) => known === trigger) ?? null;
}

export function backupStatusKey(status: string): (typeof BACKUP_STATUSES)[number] | null {
  return BACKUP_STATUSES.find((known) => known === status) ?? null;
}

/** Only a finished backup has a file to verify or download. */
export function backupSucceeded(run: { status: string }): boolean {
  return run.status === "Succeeded";
}

/** The backup's saved name, or a stable one when the run has none. */
export function backupFileName(fileName: string | null | undefined, id: string): string {
  return fileName || `rigx-backup-${id}.zip`;
}

/** "12 MB": backup sizes in binary units. */
export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  const units = ["KB", "MB", "GB", "TB"];
  let value = bytes / 1024;
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }
  return `${value.toFixed(value < 10 ? 1 : 0)} ${units[unit]}`;
}
