import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Category } from "../categories/categories-api.ts";
import type { Partner } from "../expenses/expenses-api.ts";
import type { StockItem, StockUnit } from "./inventory-api.ts";
import {
  categoryPath, expectedProfit, itemCost, itemFormValid, itemPartner, pickedIds, stockState, toItemForm, toUpdateRequest, triState, unitPercent, unitStatusKey
} from "./inventory-rules.ts";

const unit = (patch: Partial<StockUnit>): StockUnit => ({
  id: "u", status: "Available", condition: "Used", serialNumber: null, acquisitionCost: 1000, partnerPercentOverride: null, partnerId: null,
  assemblyId: null, receivedAtUtc: "2026-11-01T00:00:00Z", rowVersion: 1, ...patch
});

const item = (units: StockUnit[], patch: Partial<StockItem> = {}): StockItem => ({
  id: "i", code: "RX-1", condition: "Used", categoryId: "c2", productId: "p1", modelName: "RX 6600", price: 2000, withBoardExtra: null, aloneExtra: 150,
  prices: { inBuild: 2000, withBoard: 2000, alone: 2150 }, warrantyLabel: null, warrantyMonths: null, hasBox: false, isHidden: false, isPublished: true,
  internalNote: null, specs: { vram: "8" }, photos: [], availableCount: units.filter((entry) => entry.status === "Available").length, daysInStock: 3,
  createdAtUtc: "2026-11-01T00:00:00Z", rowVersion: 7, units, ...patch
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
  });

  it("needs a model and valid amounts", () => {
    const form = toItemForm(item([]));
    assert.equal(itemFormValid(form), true);
    assert.equal(itemFormValid({ ...form, modelName: " " }), false);
    assert.equal(itemFormValid({ ...form, price: "" }), false);
    assert.equal(itemFormValid({ ...form, withBoardExtra: "-5" }), false);
  });

  it("sends no percent without a partner", () => {
    assert.equal(unitPercent("", "30"), null);
    assert.equal(unitPercent("pa", ""), null);
    assert.equal(unitPercent("pa", "30"), 30);
  });
});
