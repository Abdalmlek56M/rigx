import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  addLine, buildFreshness, buildLine, cartWhatsAppText, clampQuantity, itemCount, itemLine, parseStoredLines, reconcile, removeLine, setLineQuantity,
  subtotal, type CartLine, type FreshItem
} from "./cart-rules.ts";

const line = (overrides: Partial<CartLine> = {}): CartLine => ({
  kind: "Item", code: "RX-0001", modelName: "Ryzen 5 5600", condition: "New", warrantyLabel: null, thumbnailStorageKey: null,
  priceOption: "Alone", unitPrice: 100, quantity: 1, maxQuantity: 5, ...overrides
});

const build = (overrides: Partial<CartLine> = {}): CartLine => ({
  kind: "Build", code: "BLD-01", modelName: "Gaming PC", condition: null, warrantyLabel: null, thumbnailStorageKey: null,
  priceOption: "InBuild", unitPrice: 20000, quantity: 1, maxQuantity: 2, ...overrides
});

describe("line builders", () => {
  it("makes an item line with the chosen price option", () => {
    const made = itemLine({ code: "RX-1", modelName: "m", condition: "Used", warrantyLabel: null, thumbnailStorageKey: null, available: 1 }, "WithBoard", 90);
    assert.deepEqual([made.kind, made.priceOption, made.unitPrice, made.quantity, made.maxQuantity], ["Item", "WithBoard", 90, 1, 1]);
  });
  it("makes a build line priced InBuild with no condition", () => {
    const made = buildLine({ code: "BLD-01", title: "Gaming PC", warrantyLabel: null, thumbnailStorageKey: null, salePrice: 20000, copies: 2 });
    assert.deepEqual([made.kind, made.condition, made.priceOption, made.maxQuantity], ["Build", null, "InBuild", 2]);
  });
});

describe("clampQuantity", () => {
  it("keeps New within 1..max", () => {
    assert.equal(clampQuantity(line(), 0), 1);
    assert.equal(clampQuantity(line(), 3), 3);
    assert.equal(clampQuantity(line(), 99), 5);
  });
  it("forces Used to 1", () => {
    assert.equal(clampQuantity(line({ condition: "Used" }), 4), 1);
  });
  it("lets a build go up to its copies", () => {
    assert.equal(clampQuantity(build(), 5), 2);
  });
});

describe("addLine", () => {
  it("merges same code and price option, clamped to the lot", () => {
    const result = addLine([line({ quantity: 4 })], line({ quantity: 3 }));
    assert.equal(result.length, 1);
    assert.equal(result[0]?.quantity, 5);
  });
  it("keeps different price options apart", () => {
    assert.equal(addLine([line()], line({ priceOption: "WithBoard" })).length, 2);
  });
  it("never lets a Used item reach 2", () => {
    const used = line({ condition: "Used", maxQuantity: 1 });
    assert.equal(addLine([used], used)[0]?.quantity, 1);
  });
});

describe("totals and edits", () => {
  const lines = [line({ quantity: 2, unitPrice: 100 }), line({ code: "RX-0002", unitPrice: 50 })];
  it("counts units and sums the subtotal", () => {
    assert.equal(itemCount(lines), 3);
    assert.equal(subtotal(lines), 250);
  });
  it("removes by code and price option", () => {
    assert.deepEqual(removeLine(lines, "RX-0001", "Alone").map((l) => l.code), ["RX-0002"]);
    assert.equal(removeLine(lines, "RX-0001", "WithBoard").length, 2);
  });
  it("sets a clamped quantity", () => {
    assert.equal(setLineQuantity(lines, "RX-0001", "Alone", 50)[0]?.quantity, 5);
  });
});

describe("reconcile", () => {
  const fresh = (available: number): FreshItem => ({ available, prices: { alone: 90, withBoard: 80, inBuild: 70 } });
  it("updates price by option and clamps to what is available", () => {
    const result = reconcile([line({ quantity: 4, priceOption: "WithBoard" })], new Map([["RX-0001", fresh(2)]]));
    assert.equal(result.lines[0]?.unitPrice, 80);
    assert.equal(result.lines[0]?.quantity, 2);
    assert.equal(result.removed, 0);
  });
  it("drops gone and sold-out lines, keeps unknown ones", () => {
    const lines = [line(), line({ code: "RX-0002" }), line({ code: "RX-0003" })];
    const result = reconcile(lines, new Map<string, FreshItem | null>([["RX-0001", null], ["RX-0002", fresh(0)]]));
    assert.deepEqual(result.lines.map((l) => l.code), ["RX-0003"]);
    assert.equal(result.removed, 2);
  });
});

describe("reconcile with the offered price options", () => {
  const offering = (priceOptions: string[]): FreshItem => ({ available: 5, prices: { alone: 90, withBoard: 80, inBuild: 70 }, priceOptions });
  const reconciled = (option: CartLine["priceOption"], priceOptions: string[]) =>
    reconcile([line({ priceOption: option, unitPrice: 1 })], new Map([["RX-0001", offering(priceOptions)]]));

  it("keeps a line whose option is still offered", () => {
    const result = reconciled("WithBoard", ["Alone", "WithBoard", "InBuild"]);
    assert.deepEqual([result.lines[0]?.priceOption, result.lines[0]?.unitPrice, result.removed], ["WithBoard", 80, 0]);
  });
  it("moves a line to the first offered option when its own was switched off", () => {
    const result = reconciled("Alone", ["WithBoard", "InBuild"]);
    assert.deepEqual([result.lines[0]?.priceOption, result.lines[0]?.unitPrice, result.removed], ["WithBoard", 80, 0]);
  });
  it("drops a line when the part is now sold inside a build only", () => {
    const result = reconciled("Alone", ["InBuild"]);
    assert.deepEqual([result.lines.length, result.removed], [0, 1]);
  });
  it("keeps a part bought as InBuild when inside a build is the option offered", () => {
    const result = reconciled("InBuild", ["InBuild"]);
    assert.deepEqual([result.lines[0]?.priceOption, result.lines[0]?.unitPrice], ["InBuild", 70]);
  });
  it("leaves build lines alone and treats an answer without the list as everything offered", () => {
    const fresh = new Map<string, FreshItem | null>([["BLD-01", buildFreshness({ salePrice: 18000, copies: 3 })], ["RX-0001", { available: 5, prices: { alone: 90, withBoard: 80, inBuild: 70 } }]]);
    const result = reconcile([build(), line({ priceOption: "Alone" })], fresh);
    assert.deepEqual(result.lines.map((l) => [l.code, l.priceOption, l.unitPrice]), [["BLD-01", "InBuild", 18000], ["RX-0001", "Alone", 90]]);
  });
});

describe("reconcile builds", () => {
  it("refreshes the price and copies of a build line, drops a gone one", () => {
    const fresh = new Map([["BLD-01", buildFreshness({ salePrice: 18000, copies: 1 })]]);
    const result = reconcile([build({ quantity: 2 })], fresh);
    assert.deepEqual([result.lines[0]?.unitPrice, result.lines[0]?.quantity], [18000, 1]);
    assert.equal(reconcile([build()], new Map([["BLD-01", null]])).lines.length, 0);
  });
});

describe("parseStoredLines", () => {
  it("reads lines saved before builds existed as items", () => {
    const old: Record<string, unknown> = { ...line() };
    delete old.kind;
    assert.equal(parseStoredLines(JSON.stringify([old]))[0]?.kind, "Item");
  });
  it("keeps build lines (no condition)", () => {
    assert.equal(parseStoredLines(JSON.stringify([build()]))[0]?.kind, "Build");
  });
  it("returns an empty cart for missing or broken data", () => {
    assert.deepEqual(parseStoredLines(null), []);
    assert.deepEqual(parseStoredLines("{nope"), []);
    assert.deepEqual(parseStoredLines('{"a":1}'), []);
  });
  it("drops malformed lines and keeps valid ones", () => {
    const raw = JSON.stringify([line(), { code: 5 }]);
    assert.equal(parseStoredLines(raw).length, 1);
  });
});

describe("cartWhatsAppText", () => {
  it("lists every line with its code, the quantity when above one, and the total", () => {
    const text = cartWhatsAppText([line({ quantity: 2 }), build()], { intro: "Order:", total: "Total" }, (amount) => `EGP ${amount}`);
    assert.equal(text, ["Order:", "- Ryzen 5 5600 (RX-0001) × 2", "- Gaming PC (BLD-01)", "Total: EGP 20200"].join("\n"));
  });
});
