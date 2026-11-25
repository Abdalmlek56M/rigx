import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Category } from "../categories/categories-api.ts";
import type { Partner } from "../expenses/expenses-api.ts";
import type { StockItem, StockUnit } from "./inventory-api.ts";
import {
  categoryPath, countTooFewPhotos, expectedProfit, groupByBuild, itemCost, itemFormValid, itemModes, itemPartner, leftOutOfStockValue, modesOk, pickedIds, STOCK_AGE_DAYS, stockAge, stockState, toItemForm, toUpdateRequest, MIN_PHOTOS_TO_PUBLISH, photosTooFew, triState, unitPercent, unitStatusKey
} from "./inventory-rules.ts";

const unit = (patch: Partial<StockUnit>): StockUnit => ({
  id: "u", status: "Available", condition: "Used", serialNumber: null, acquisitionCost: 1000, partnerPercentOverride: null, partnerId: null,
  assemblyId: null, receivedAtUtc: "2026-11-01T00:00:00Z", rowVersion: 1, build: null, excludedFromStockValue: false, ...patch
});

const item = (units: StockUnit[], patch: Partial<StockItem> = {}): StockItem => ({
  id: "i", code: "RX-1", condition: "Used", categoryId: "c2", brandId: "b1", productId: "p1", modelName: "RX 6600", price: 2000, withBoardExtra: null, aloneExtra: 150,
  prices: { inBuild: 2000, withBoard: 2000, alone: 2150 }, warrantyLabel: null, warrantyMonths: null, hasBox: false, isHidden: false, isPublished: true,
  internalNote: null, specs: { vram: "8" }, photos: [], availableCount: units.filter((entry) => entry.status === "Available").length, daysInStock: 3,
  createdAtUtc: "2026-11-01T00:00:00Z", rowVersion: 7, excludedFromStockValue: false, units, ...patch
});

const partner = (patch: Partial<Partner> = {}): Partner => ({
  id: "pa", name: "Hany", phone: null, joinDate: "2026-01-01", defaultPercent: 30, basis: "FinancedParts", lossSharingEnabled: false,
  isActive: true, notes: null, capitalBalance: 0, due: 0, recoverable: 0, unsoldFinancedUnits: 0, rowVersion: 1, ...patch
});

describe("inventory figures", () => {
  it("adds the cost of the available units only", () => {
    const stock = item([unit({ acquisitionCost: 1000 }), unit({ acquisitionCost: 500 }), unit({ status: "Sold", acquisitionCost: 9000 })]);
    assert.equal(itemCost(stock), 1500);
    assert.equal(expectedProfit(stock), 2000 * 2 - 1500);
  });

  it("shows no cost when the server hid it or nothing is available", () => {
    assert.equal(itemCost(item([unit({ acquisitionCost: null })])), null);
    assert.equal(expectedProfit(item([unit({ acquisitionCost: null })])), null);
    assert.equal(itemCost(item([unit({ status: "Sold" })])), null);
  });

  it("tells available, in a build and sold out apart", () => {
    assert.equal(stockState(item([unit({}), unit({ status: "InAssembly" })])), "available");
    assert.equal(stockState(item([unit({ status: "InAssembly" })])), "inBuild");
    assert.equal(stockState(item([unit({ status: "Sold" })])), "soldOut");
  });

  it("finds the first unit's partner and its share", () => {
    const stock = item([unit({}), unit({ partnerId: "pa", partnerPercentOverride: 25 })]);
    assert.deepEqual(itemPartner(stock, [partner()]), { name: "Hany", percent: 25 });
    assert.deepEqual(itemPartner(item([unit({ partnerId: "pa" })]), [partner()]), { name: "Hany", percent: 30 });
    assert.equal(itemPartner(stock, []), null);
    assert.equal(itemPartner(item([unit({})]), [partner()]), null);
  });

  it("maps unit statuses", () => {
    assert.equal(unitStatusKey("InAssembly"), "InAssembly");
    assert.equal(unitStatusKey("Whatever"), "other");
  });
});

describe("inventory filters", () => {
  it("reads the tri-state values", () => {
    assert.equal(triState("true"), true);
    assert.equal(triState("false"), false);
    assert.equal(triState(""), undefined);
  });

  it("keeps only picked rows that are still on the page", () => {
    assert.deepEqual(pickedIds(new Set(["a", "gone"]), [item([], { id: "a" }), item([], { id: "b" })]), ["a"]);
  });

  it("writes the category path", () => {
    const tree = [{ id: "c1", name: "كروت", children: [{ id: "c2", name: "AMD", children: [] }] }] as unknown as Category[];
    assert.equal(categoryPath(tree, "c2"), "كروت › AMD");
    assert.equal(categoryPath(tree, null), "");
  });
});

describe("stock value exclusion", () => {
  it("is off by default and saved with the form", () => {
    const stock = item([unit({})]);
    assert.equal(toItemForm(stock).excludedFromStockValue, false);
    assert.equal(toUpdateRequest(toItemForm(stock), stock).excludedFromStockValue, false);
    const left = item([unit({})], { excludedFromStockValue: true });
    assert.equal(toItemForm(left).excludedFromStockValue, true);
    assert.equal(toUpdateRequest({ ...toItemForm(left), excludedFromStockValue: false }, left).excludedFromStockValue, false);
  });

  it("leaves a unit out when its own flag or its item's flag is set", () => {
    assert.equal(leftOutOfStockValue(item([]), unit({})), false);
    assert.equal(leftOutOfStockValue(item([]), unit({ excludedFromStockValue: true })), true);
    assert.equal(leftOutOfStockValue(item([], { excludedFromStockValue: true }), unit({})), true);
  });
});

describe("inventory edit form", () => {
  it("round-trips the item and keeps specs, product and row version", () => {
    const stock = item([unit({})]);
    const request = toUpdateRequest({ ...toItemForm(stock), price: "2500", aloneExtra: "", internalNote: " " }, stock);
    assert.equal(request.price, 2500);
    assert.equal(request.aloneExtra, null);
    assert.equal(request.internalNote, null);
    assert.equal(request.productId, "p1");
    assert.deepEqual(request.specs, { vram: "8" });
    assert.equal(request.rowVersion, 7);
    assert.equal(request.brandId, "b1");
    assert.equal(toUpdateRequest({ ...toItemForm(stock), brandId: "" }, stock).brandId, null);
  });

  it("needs a model and valid amounts", () => {
    const form = toItemForm(item([]));
    assert.equal(itemFormValid(form), true);
    assert.equal(itemFormValid({ ...form, modelName: " " }), false);
    assert.equal(itemFormValid({ ...form, price: "" }), false);
    assert.equal(itemFormValid({ ...form, withBoardExtra: "-5" }), false);
  });

  it("follows the category until the owner sets the item's own ways to sell it, and sends that choice", () => {
    const category = { alone: false, withBoard: true, inBuild: true };
    const follows = toItemForm(item([]));
    assert.equal(follows.ownModes, false);
    assert.deepEqual(itemModes(follows, category), category);
    assert.equal(toUpdateRequest(follows, item([])).priceOptions, null);

    const stock = item([], { priceOptions: { alone: true, withBoard: false, inBuild: false } });
    const own = toItemForm(stock);
    assert.equal(own.ownModes, true);
    assert.deepEqual(itemModes(own, category), { alone: true, withBoard: false, inBuild: false });
    assert.deepEqual(toUpdateRequest(own, stock).priceOptions, { alone: true, withBoard: false, inBuild: false });
  });

  it("refuses an own choice with no way to sell the item", () => {
    const form = { ...toItemForm(item([])), ownModes: true, modes: { alone: false, withBoard: false, inBuild: false } };
    assert.equal(modesOk(form), false);
    assert.equal(itemFormValid(form), false);
    assert.equal(modesOk({ ...form, ownModes: false }), true);
  });

  it("sends no percent without a partner", () => {
    assert.equal(unitPercent("", "30"), null);
    assert.equal(unitPercent("pa", ""), null);
    assert.equal(unitPercent("pa", "30"), 30);
  });
});

describe("stock grouped by build", () => {
  const bld = { id: "b1", code: "BLD-01", name: "Gaming PC", status: "Draft" };
  const inBuild = unit({ status: "InAssembly", assemblyId: bld.id, build: { code: bld.code, name: bld.name, status: bld.status } });
  const rowsOf = (items: StockItem[], collapsed: string[] = []) => groupByBuild(items, new Set(collapsed));

  it("gathers the items of a build under one header placed at its first item", () => {
    const rows = rowsOf([item([unit({})], { id: "loose" }), item([inBuild], { id: "cpu" }), item([unit({})], { id: "mid" }), item([inBuild], { id: "gpu" })]);
    assert.deepEqual(rows.map((row) => `${row.kind}:${row.key}`), ["item:loose", "build:build-b1", "item:cpu", "item:gpu", "item:mid"]);
    assert.equal(rows[1]?.kind === "build" ? rows[1].count : 0, 2);
    assert.deepEqual(rows.flatMap((row) => (row.kind === "item" ? [row.underBuild] : [])), [false, true, true, false]);
  });

  it("shows only the header of a collapsed build and keeps an item with free units loose", () => {
    const rows = rowsOf([item([inBuild], { id: "cpu" }), item([inBuild, unit({})], { id: "lot" })], ["b1"]);
    assert.deepEqual(rows.map((row) => row.key), ["build-b1", "lot"]);
    assert.equal(rows[0]?.kind === "build" ? rows[0].open : true, false);
  });
});

describe("stock age", () => {
  it("flags an item that sat longer than the limits and keeps the limits in one place", () => {
    assert.deepEqual(STOCK_AGE_DAYS, { slow: 15, stale: 30 });
    assert.equal(stockAge(15, 2), "fresh");
    assert.equal(stockAge(16, 2), "slow");
    assert.equal(stockAge(30, 2), "slow");
    assert.equal(stockAge(31, 2), "stale");
  });
  it("never flags an item with nothing available", () => {
    assert.equal(stockAge(90, 0), "fresh");
  });
});

describe("items that are shown but not listed", () => {
  const withPhotos = (count: number) => ({ photos: Array.from({ length: count }, (_, index) => ({ storageKey: `k${index}`, thumbnailStorageKey: `t${index}` })) });

  it("needs three photos to be listed", () => {
    assert.equal(MIN_PHOTOS_TO_PUBLISH, 3);
    assert.equal(photosTooFew(withPhotos(0)), true);
    assert.equal(photosTooFew(withPhotos(2)), true);
    assert.equal(photosTooFew(withPhotos(3)), false);
  });

  it("counts the items with too few photos", () => {
    assert.equal(countTooFewPhotos([withPhotos(1), withPhotos(3), withPhotos(2), withPhotos(5)]), 2);
    assert.equal(countTooFewPhotos([]), 0);
  });
});
