import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  canSearchRecords, customerEntry, isPaletteShortcut, itemEntry, matchEntries, moveActive, normalizeSearch, orderEntry, purchaseEntry, recordSources, staticEntries,
  type PaletteEntry, type StaticLabels
} from "./palette-rules.ts";

const labels: StaticLabels = { area: (key) => `area:${key}`, group: (key) => `group:${key}`, action: (key) => `action:${key}` };
const ids = (entries: readonly PaletteEntry[]) => entries.map((entry) => entry.id);

describe("staticEntries", () => {
  it("offers only the pages and actions the permissions open", () => {
    const entries = staticEntries(["Sales.ViewOrders"], labels);
    assert.deepEqual(ids(entries), ["action:newOrders", "action:toggleTheme", "action:toggleDensity", "page:orders"]);
    assert.equal(entries.find((entry) => entry.id === "page:orders")?.hint, "group:sales");
  });
  it("offers the sale and purchase actions to the people who may do them", () => {
    const entries = staticEntries(["Sales.CreatePOS", "Purchasing.View", "Purchasing.CreateOrder"], labels);
    assert.ok(ids(entries).includes("action:newSale"));
    assert.ok(ids(entries).includes("action:newPurchase"));
    assert.deepEqual(entries.find((entry) => entry.id === "action:newSale")?.href, "/admin/pos");
  });
  it("offers nothing page-like to a person with no admin permission, only the two switches", () => {
    assert.deepEqual(ids(staticEntries([], labels)), ["action:toggleTheme", "action:toggleDensity"]);
  });
  it("marks the switches as commands, not links", () => {
    const theme = staticEntries([], labels).find((entry) => entry.id === "action:toggleTheme");
    assert.deepEqual([theme?.command, theme?.href], ["toggleTheme", undefined]);
  });
});

describe("normalizeSearch", () => {
  it("ignores case, vowel marks, alef, yeh and heh variants and Arabic digits", () => {
    assert.equal(normalizeSearch("  الطَلَبَات "), "الطلبات");
    assert.equal(normalizeSearch("إدارة"), "اداره");
    assert.equal(normalizeSearch("مستعمل ى"), "مستعمل ي");
    assert.equal(normalizeSearch("RX-١٢"), "rx-12");
  });
});

describe("matchEntries", () => {
  const entries: PaletteEntry[] = [
    { id: "1", kind: "page", label: "الطلبات", hint: "المبيعات" },
    { id: "2", kind: "page", label: "طلبات العملاء الجديدة" },
    { id: "3", kind: "page", label: "المخزون", hint: "الطلبات الواردة" }
  ];
  it("keeps everything in order for an empty query", () => {
    assert.deepEqual(ids(matchEntries(entries, "  ")), ["1", "2", "3"]);
  });
  it("ranks a label that starts with the word above a later word and a hint", () => {
    assert.deepEqual(ids(matchEntries(entries, "طلبات")), ["2", "1", "3"]);
  });
  it("needs every word to match", () => {
    assert.deepEqual(ids(matchEntries(entries, "طلبات جديده")), ["2"]);
    assert.deepEqual(ids(matchEntries(entries, "xyz")), []);
  });
  it("matches Arabic spellings loosely", () => {
    assert.deepEqual(ids(matchEntries(entries, "المخزُون")), ["3"]);
  });
});

describe("records", () => {
  it("searches only the kinds of record the permissions open", () => {
    assert.deepEqual(recordSources(["Sales.ViewOrders", "Inventory.View"]), ["orders", "items"]);
    assert.deepEqual(recordSources([]), []);
  });
  it("starts looking from two characters", () => {
    assert.equal(canSearchRecords("a"), false);
    assert.equal(canSearchRecords(" a "), false);
    assert.equal(canSearchRecords("rx"), true);
  });
  it("turns a record into an entry that opens its page", () => {
    assert.deepEqual(orderEntry({ id: "o1", reference: "ORD-1", customerName: "Mona" }), { id: "order:o1", kind: "order", label: "ORD-1", hint: "Mona", href: "/admin/orders?id=o1" });
    assert.equal(customerEntry({ id: "c1", name: "Mona", phone: "0100" }).href, "/admin/customers/c1");
    assert.equal(itemEntry({ id: "i1", code: "RX-1", modelName: "RTX" }).href, "/admin/inventory/i1");
    assert.deepEqual([purchaseEntry({ id: "p1", code: "PO-1", seller: { name: "Ali" } }).href, purchaseEntry({ id: "p1", code: "PO-1", seller: null }).hint], ["/admin/purchases/p1", undefined]);
  });
});

describe("moveActive", () => {
  it("wraps around and starts at the edge", () => {
    assert.equal(moveActive(-1, 3, 1), 0);
    assert.equal(moveActive(-1, 3, -1), 2);
    assert.equal(moveActive(2, 3, 1), 0);
    assert.equal(moveActive(0, 3, -1), 2);
    assert.equal(moveActive(0, 0, 1), -1);
  });
});

describe("isPaletteShortcut", () => {
  const key = (patch: Partial<Parameters<typeof isPaletteShortcut>[0]>) => ({ key: "k", ctrlKey: false, metaKey: false, altKey: false, shiftKey: false, ...patch });
  it("is Ctrl+K or Cmd+K and nothing else", () => {
    assert.equal(isPaletteShortcut(key({ ctrlKey: true })), true);
    assert.equal(isPaletteShortcut(key({ metaKey: true, key: "K" })), true);
    assert.equal(isPaletteShortcut(key({})), false);
    assert.equal(isPaletteShortcut(key({ ctrlKey: true, shiftKey: true })), false);
    assert.equal(isPaletteShortcut(key({ ctrlKey: true, key: "j" })), false);
  });
});
