import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { StockImportResult, StockImportRow } from "./stock-import-api.ts";
import { canImport, conditionKey, fileProblem, importSummary, MAX_FILE_BYTES, rowHasErrors } from "./stock-import-rules.ts";

const row = (patch: Partial<StockImportRow> = {}): StockImportRow =>
  ({ row: 2, modelName: "RTX 3060", condition: "Used", quantity: 1, itemsToCreate: 1, errors: [], ...patch });
const result = (patch: Partial<StockImportResult> = {}): StockImportResult =>
  ({ rows: [row()], validRows: 1, errorRows: 0, itemsToCreate: 1, imported: false, items: null, ...patch });

describe("fileProblem", () => {
  it("accepts a small xlsx in any letter case", () => {
    assert.equal(fileProblem({ name: "stock.xlsx", size: 1000 }), null);
    assert.equal(fileProblem({ name: "STOCK.XLSX", size: MAX_FILE_BYTES }), null);
  });
  it("refuses another type or a file over 2 MB", () => {
    assert.equal(fileProblem({ name: "stock.xls", size: 1000 }), "type");
    assert.equal(fileProblem({ name: "stock.xlsx", size: MAX_FILE_BYTES + 1 }), "size");
  });
});

describe("rows", () => {
  it("marks a row with any error", () => {
    assert.equal(rowHasErrors(row()), false);
    assert.equal(rowHasErrors(row({ errors: ["القسم مطلوب"] })), true);
  });
  it("knows only the two conditions", () => {
    assert.equal(conditionKey("New"), "New");
    assert.equal(conditionKey("Used"), "Used");
    assert.equal(conditionKey(""), null);
  });
});

describe("importSummary", () => {
  it("is clean when no row has errors", () => {
    assert.deepEqual(importSummary(result({ rows: [row(), row({ row: 3 })], itemsToCreate: 2 })), { key: "clean", values: { rows: 2, errorRows: 0, items: 2 } });
  });
  it("counts the rows that need fixing", () => {
    const summary = importSummary(result({ rows: [row(), row({ errors: ["x"] })], errorRows: 1 }));
    assert.equal(summary.key, "withErrors");
    assert.equal(summary.values.errorRows, 1);
  });
});

describe("canImport", () => {
  it("needs a checked file with no bad rows and items to create", () => {
    assert.equal(canImport(null), false);
    assert.equal(canImport(result()), true);
    assert.equal(canImport(result({ errorRows: 1 })), false);
    assert.equal(canImport(result({ itemsToCreate: 0 })), false);
  });
  it("is off once the file was imported", () => {
    assert.equal(canImport(result({ imported: true })), false);
  });
});
