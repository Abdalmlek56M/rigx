/** Rules for the opening-stock import screen: the file checks, the row status and the summary. Pure, so it is tested. Messages live under `admin.stockImport`. */
import type { StockImportResult, StockImportRow } from "./stock-import-api";

export const TEMPLATE_FILE_NAME = "rigx-stock-import-template.xlsx";

/** The server's limit (2 MB); checked here too so a wrong file is refused before it is sent. */
export const MAX_FILE_BYTES = 2 * 1024 * 1024;

export type FileProblem = "type" | "size";

/** What is wrong with a chosen file, or `null` when it can be sent. */
export function fileProblem(file: { name: string; size: number }): FileProblem | null {
  if (!file.name.toLowerCase().endsWith(".xlsx")) return "type";
  return file.size > MAX_FILE_BYTES ? "size" : null;
}

export const rowHasErrors = (row: StockImportRow): boolean => row.errors.length > 0;

/** The condition message key of a row; a row whose condition could not be read has none. */
export function conditionKey(condition: string): "New" | "Used" | null {
  return condition === "New" || condition === "Used" ? condition : null;
}

export interface ImportSummary {
  key: "clean" | "withErrors";
  values: { rows: number; errorRows: number; items: number };
}

/** The line under the table: every row is fine, or how many rows need fixing. */
export function importSummary(result: StockImportResult): ImportSummary {
  return {
    key: result.errorRows === 0 ? "clean" : "withErrors",
    values: { rows: result.rows.length, errorRows: result.errorRows, items: result.itemsToCreate }
  };
}

/** The import button works only on a checked file with no bad rows and something to import. */
export const canImport = (result: StockImportResult | null): result is StockImportResult =>
  result !== null && !result.imported && result.errorRows === 0 && result.itemsToCreate > 0;
