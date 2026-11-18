import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  backupFileName, canReset, exportableDatasets, exportFileName, formatBytes, toggleResetSection, trashKindKey
} from "./system-rules.ts";

describe("trash kinds", () => {
  it("knows the kinds and leaves others as they are", () => {
    assert.equal(trashKindKey("faq"), "faq");
    assert.equal(trashKindKey("future-kind"), null);
  });
});

describe("reset sections", () => {
  it("ticks business with customers and unticks customers with business", () => {
    assert.deepEqual(toggleResetSection([], "customers"), ["customers", "business"]);
    assert.deepEqual(toggleResetSection(["customers", "business"], "business"), []);
    assert.deepEqual(toggleResetSection(["business", "customers"], "customers"), ["business"]);
    assert.deepEqual(toggleResetSection(["business"], "content"), ["business", "content"]);
  });

  it("needs a section and the exact word", () => {
    assert.equal(canReset(["content"], "RESET"), true);
    assert.equal(canReset(["content"], "reset"), false);
    assert.equal(canReset([], "RESET"), false);
  });
});

describe("export", () => {
  it("lists only the datasets the person may export", () => {
    assert.deepEqual(exportableDatasets((permission) => ["Sales.ViewOrders", "Customers.View"].includes(permission)), ["orders", "customers"]);
    assert.deepEqual(exportableDatasets(() => false), []);
  });

  it("builds the file name, leaving out empty dates", () => {
    assert.equal(exportFileName("orders", "2026-11-01", "2026-11-30", "xlsx"), "rigx-orders-2026-11-01-2026-11-30.xlsx");
    assert.equal(exportFileName("audit", "", "", "csv"), "rigx-audit.csv");
  });
});

describe("backups", () => {
  it("names the download and formats the size", () => {
    assert.equal(backupFileName("a.zip", "1"), "a.zip");
    assert.equal(backupFileName(null, "1"), "rigx-backup-1.zip");
    assert.equal(formatBytes(512), "512 B");
    assert.equal(formatBytes(5 * 1024 * 1024), "5.0 MB");
    assert.equal(formatBytes(120 * 1024 * 1024), "120 MB");
  });
});
