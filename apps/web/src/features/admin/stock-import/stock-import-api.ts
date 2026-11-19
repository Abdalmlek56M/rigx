import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { download } from "@/lib/api/download";
import { required } from "@/lib/api/required";
import { TEMPLATE_FILE_NAME } from "./stock-import-rules.ts";

export type StockImportResult = Schemas["StockImportResult"];
export type StockImportRow = Schemas["StockImportRowView"];

const api = () => browserApi();

export const downloadImportTemplate = () =>
  download(api().GET("/api/v1/admin/retail/stock-import/template", { parseAs: "blob" }), TEMPLATE_FILE_NAME);

/** `dryRun` only checks the file; without it every row is imported (all or nothing). */
export const importStockFile = (file: File, dryRun: boolean) => {
  const form = new FormData();
  form.set("file", file);
  return required(unwrap(api().POST("/api/v1/admin/retail/stock-import", { params: { query: { dryRun } }, bodySerializer: () => form })));
};
