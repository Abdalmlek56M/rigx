import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { download } from "@/lib/api/download";
import { required } from "@/lib/api/required";
import { backupFileName, exportFileName, type ExportDataset, type ExportFormat, type ResetSection } from "./system-rules.ts";

export type TrashEntry = Schemas["TrashEntry"];
export type BackupRun = Schemas["BackupRun"];
export type ResetResult = Schemas["DataResetResult"];

/** The step-up headers a protected action runs with (empty on the first try). */
type Headers = Record<string, string>;

const api = () => browserApi();

export const fetchTrash = (kind: string) =>
  required(unwrap(api().GET("/api/v1/admin/system/trash", { params: { query: kind ? { kind } : {} } })));

export const restoreTrash = (kind: string, id: string) =>
  unwrap(api().POST("/api/v1/admin/system/trash/{kind}/{id}/restore", { params: { path: { kind, id } } }));

export const purgeTrash = (kind: string, id: string, headers: Headers) =>
  unwrap(api().DELETE("/api/v1/admin/system/trash/{kind}/{id}", { params: { path: { kind, id } }, headers }));

export const fetchBackups = () => required(unwrap(api().GET("/api/v1/admin/system/backups")));

export const runBackup = () => required(unwrap(api().POST("/api/v1/admin/system/backups")));

export const verifyBackup = (id: string) =>
  required(unwrap(api().POST("/api/v1/admin/system/backups/{id}/verify", { params: { path: { id } } })));

export const downloadBackup = (id: string, fileName: string | null | undefined, headers: Headers) =>
  download(
    api().GET("/api/v1/admin/system/backups/{id}/download", { params: { path: { id } }, headers, parseAs: "blob" }),
    backupFileName(fileName, id)
  );

const EXPORT_PATHS = {
  orders: "/api/v1/admin/system/export/orders",
  "stock-items": "/api/v1/admin/system/export/stock-items",
  expenses: "/api/v1/admin/system/export/expenses",
  purchases: "/api/v1/admin/system/export/purchases",
  "money-movements": "/api/v1/admin/system/export/money-movements",
  audit: "/api/v1/admin/system/export/audit",
  customers: "/api/v1/admin/system/export/customers"
} as const satisfies Record<ExportDataset, string>;

export const downloadExport = (query: { dataset: ExportDataset; format: ExportFormat; from: string; to: string }, headers: Headers) =>
  download(
    api().GET(EXPORT_PATHS[query.dataset], {
      params: { query: { format: query.format, ...(query.from ? { from: query.from } : {}), ...(query.to ? { to: query.to } : {}) } },
      headers,
      parseAs: "blob"
    }),
    exportFileName(query.dataset, query.from, query.to, query.format)
  );

export const resetData = (sections: readonly ResetSection[], confirm: string, headers: Headers) =>
  required(unwrap(api().POST("/api/v1/admin/system/reset", { body: { sections: [...sections], confirm }, headers })));
