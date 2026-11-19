import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Partner } from "../expenses/expenses-api.ts";
import { amountOk, amountText, applySuggestion, editModel, financingPartners, optionalAmountOk, parseAmount, partnerShare } from "./stock-fields.ts";

const partner = (patch: Partial<Partner>): Partner => ({
  id: "p", name: "P", phone: null, joinDate: "2026-01-01", defaultPercent: 30, basis: "FinancedParts", lossSharingEnabled: false,
  isActive: true, notes: null, capitalBalance: 0, due: 0, recoverable: 0, unsoldFinancedUnits: 0, rowVersion: 1, ...patch
});

describe("stock fields", () => {
  it("reads and writes optional amounts", () => {
    assert.equal(parseAmount(" "), null);
    assert.equal(parseAmount("1500.5"), 1500.5);
    assert.ok(Number.isNaN(parseAmount("abc")));
    assert.equal(amountText(null), "");
    assert.equal(amountText(250), "250");
  });

  it("checks amounts", () => {
    assert.equal(optionalAmountOk(""), true);
    assert.equal(optionalAmountOk("-1"), false);
    assert.equal(optionalAmountOk("x"), false);
    assert.equal(amountOk(""), false);
    assert.equal(amountOk("0"), true);
  });

  it("keeps active partners paid by financed parts", () => {
    const list = [partner({ id: "a" }), partner({ id: "b", isActive: false }), partner({ id: "c", basis: "WholeShop" })];
    assert.deepEqual(financingPartners(list).map((entry) => entry.id), ["a"]);
  });

  it("uses the unit percent before the partner default", () => {
    assert.equal(partnerShare(partner({}), null), 30);
    assert.equal(partnerShare(partner({}), 25), 25);
  });
});

const fields = () => ({ modelName: "RX", productId: "", categoryId: "", brandId: "", specs: {} as Record<string, string> });

describe("model suggestions", () => {
  it("fills the model, product and an empty category; typing frees the model again", () => {
    const picked = applySuggestion(fields(), { modelName: "RX 6600", categoryId: "c1", productId: "p1", itemCount: 2 });
    assert.equal(picked.productId, "p1");
    assert.equal(picked.categoryId, "c1");
    assert.equal(applySuggestion({ ...fields(), categoryId: "mine" }, { modelName: "X", categoryId: "c1", productId: null, itemCount: 0 }).categoryId, "mine");
    assert.equal(editModel(picked, "RX 6650").productId, "");
  });

  it("copies the category, brand and specs of a model already in stock", () => {
    const template = { itemId: "i1", code: "RX-0012", modelName: "RTX 4060", categoryId: "gpu", brandId: "msi", productId: null, specs: { vram: "8" }, photoCount: 3 };
    const picked = applySuggestion({ ...fields(), categoryId: "other", specs: { old: "x" } }, { modelName: "RTX 4060", categoryId: "gpu", productId: null, itemCount: 4, template });
    assert.deepEqual([picked.categoryId, picked.brandId, picked.specs], ["gpu", "msi", { vram: "8" }]);
  });
});
