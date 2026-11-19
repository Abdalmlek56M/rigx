import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Purchase } from "./purchases-api.ts";
import {
  dropPhotoCopy, emptyLine, emptyPurchase, lineFromItem, pickModel, toLineRequest, typeModel, isDraft, isOverpaid, kindKey, lineQuantity, purchaseFormValid, purchaseFromView,
  purchaseTotal, remainingAmount, sellerFormValid, statusKey, toPurchaseRequest, toSellerRequest, emptySeller, looksLikePhone, sellerQuery, withMethod, type LineForm, type PurchaseForm
} from "./purchase-rules.ts";

const line = (patch: Partial<LineForm> = {}): LineForm => ({ ...emptyLine("k"), modelName: "RX 6600", unitCost: "1000", price: "1500", ...patch });

/** The first line of a built request or form (a test fails when there is none). */
function firstLine<T>(lines: readonly T[] | null | undefined): T {
  const [first] = lines ?? [];
  assert.ok(first);
  return first;
}
const form = (patch: Partial<PurchaseForm> = {}): PurchaseForm => ({ ...emptyPurchase("2026-11-18", "k"), sellerId: "s1", lines: [line()], ...patch });

describe("purchase money", () => {
  it("totals quantity times cost over the lines; a used line counts once", () => {
    const lines = [line({ condition: "New", quantity: "3", unitCost: "200" }), line({ condition: "Used", quantity: "5", unitCost: "1000" })];
    assert.equal(lineQuantity(firstLine(lines.slice(1))), 1);
    assert.equal(purchaseTotal(lines), 1600);
  });

  it("never shows a remaining amount below zero", () => {
    assert.equal(remainingAmount(1000, 400, 100), 500);
    assert.equal(remainingAmount(1000, 1200, 0), 0);
  });

  it("spots paid money and trade-in above the total", () => {
    assert.equal(isOverpaid(form({ paidAmount: "900", tradeInValue: "200" })), true);
    assert.equal(isOverpaid(form({ paidAmount: "900", tradeInValue: "100" })), false);
  });

  it("clears the payment fields the method does not use", () => {
    const tradeIn = withMethod(form({ paidAmount: "500", tradeInValue: "300", tradeInNote: "cable" }), "TradeIn");
    assert.equal(tradeIn.paidAmount, "");
    assert.equal(tradeIn.tradeInValue, "300");
    const cash = withMethod(tradeIn, "Cash");
    assert.equal(cash.tradeInValue, "");
    assert.equal(cash.tradeInNote, "");
  });
});

describe("purchase form", () => {
  it("needs a seller and a valid line", () => {
    assert.equal(purchaseFormValid(form()), true);
    assert.equal(purchaseFormValid(form({ sellerId: "" })), false);
    assert.equal(purchaseFormValid(form({ lines: [line({ modelName: " " })] })), false);
    assert.equal(purchaseFormValid(form({ lines: [line({ unitCost: "" })] })), false);
    assert.equal(purchaseFormValid(form({ lines: [] })), false);
  });

  it("needs two parts for a whole PC", () => {
    assert.equal(purchaseFormValid(form({ kind: "FullPc" })), false);
    assert.equal(purchaseFormValid(form({ kind: "FullPc", lines: [line(), line({ key: "b" })] })), true);
  });

  it("needs the trade-in note and not more than the total", () => {
    assert.equal(purchaseFormValid(form({ paymentMethod: "TradeIn", tradeInValue: "500", tradeInNote: "" })), false);
    assert.equal(purchaseFormValid(form({ paymentMethod: "TradeIn", tradeInValue: "500", tradeInNote: "mouse" })), true);
    assert.equal(purchaseFormValid(form({ paidAmount: "5000" })), false);
  });

  it("builds the request: the build name only for a kept whole PC, no money for a pure trade-in", () => {
    const parts = toPurchaseRequest(form({ keepAsBuild: true, buildName: "X" }));
    assert.equal(parts.keepAsBuild, false);
    assert.equal(parts.buildName, null);
    const pc = toPurchaseRequest(form({ kind: "FullPc", keepAsBuild: true, buildName: " Gaming ", lines: [line(), line()] }), 4);
    assert.equal(pc.keepAsBuild, true);
    assert.equal(pc.buildName, "Gaming");
    assert.equal(pc.rowVersion, 4);
    const trade = toPurchaseRequest(form({ paymentMethod: "TradeIn", paidAmount: "50", tradeInValue: "500", tradeInNote: "mouse" }));
    assert.equal(trade.paidAmount, 0);
    assert.equal(trade.tradeInValue, 500);
    const cash = toPurchaseRequest(form({ paidAmount: "700", tradeInValue: "9", tradeInNote: "n" }));
    assert.equal(cash.tradeInValue, 0);
    assert.equal(cash.tradeInNote, null);
  });

  it("sends the partner percent only with a partner, and a used line with quantity 1", () => {
    const request = toPurchaseRequest(form({ lines: [line({ condition: "Used", quantity: "4", partnerPercent: "30" })] }));
    assert.equal(firstLine(request.lines).partnerPercent, null);
    assert.equal(firstLine(request.lines).quantity, 1);
    const withPartner = toPurchaseRequest(form({ lines: [line({ partnerId: "p", partnerPercent: "30" })] }));
    assert.equal(firstLine(withPartner.lines).partnerPercent, 30);
  });

  it("reads a saved purchase back into the form, keeping line specs", () => {
    const view = {
      seller: { id: "s9", kind: "Person", name: "A", phone: "010" }, kind: "FullPc", keepAsBuild: true, buildName: "B", purchaseDate: "2026-11-01", notes: null,
      paymentMethod: "CashAndTradeIn", paidAmount: 700, tradeInValue: 300, tradeInNote: "fan",
      lines: [{
        categoryId: "c", productId: null, modelName: "M", condition: "New", quantity: 2, unitCost: 100, price: 150, withBoardExtra: null, aloneExtra: 10,
        serialNumber: null, partnerPercent: null, partnerId: null, isHidden: true, warrantyLabel: null, hasBox: true, internalNote: null, specs: { a: "1" }
      }]
    } as unknown as Purchase;
    const restored = purchaseFromView(view, (index) => `k${index}`);
    assert.equal(restored.sellerId, "s9");
    assert.equal(restored.paymentMethod, "CashAndTradeIn");
    assert.equal(restored.paidAmount, "700");
    assert.equal(firstLine(restored.lines).key, "k0");
    assert.equal(firstLine(restored.lines).quantity, "2");
    assert.deepEqual(firstLine(restored.lines).specs, { a: "1" });
  });
});

describe("labels and sellers", () => {
  it("maps known words and falls back to other", () => {
    assert.equal(statusKey("Draft"), "Draft");
    assert.equal(statusKey("Weird"), "other");
    assert.equal(kindKey("FullPc"), "FullPc");
    assert.equal(isDraft({ status: "Draft" }), true);
    assert.equal(isDraft({ status: "Received" }), false);
  });

  it("looks a seller up by phone or by name", () => {
    assert.equal(looksLikePhone(" +20 100-123 "), true);
    assert.equal(looksLikePhone("Ali"), false);
    assert.deepEqual(sellerQuery("0100 123"), { phone: "0100 123" });
    assert.deepEqual(sellerQuery(" Ali "), { q: "Ali" });
  });

  it("needs a seller name and phone, and sends optional fields as null", () => {
    assert.equal(sellerFormValid({ ...emptySeller(""), name: "A" }), false);
    const seller = { ...emptySeller("010"), name: " Ali ", nationalId: " " };
    assert.equal(sellerFormValid(seller), true);
    const request = toSellerRequest(seller, 3);
    assert.equal(request.name, "Ali");
    assert.equal(request.nationalId, null);
    assert.equal(request.governorate, null);
    assert.equal(request.rowVersion, 3);
  });
});

describe("copying an existing item", () => {
  const template = { itemId: "i1", code: "RX-0012", modelName: "RTX 4060 8GB", categoryId: "gpu", brandId: "msi", productId: null, specs: { vram: "8" }, photoCount: 3 };
  const suggestion = { modelName: "RTX 4060 8GB", categoryId: "gpu", productId: null, itemCount: 2, template };

  it("fills the data, keeps price, cost, condition and serial, and copies the photos on receiving", () => {
    const picked = pickModel(line({ price: "9000", unitCost: "7000", condition: "New", serialNumber: "" }), suggestion);
    assert.deepEqual([picked.brandId, picked.specs, picked.copyPhotosFromItemId, picked.copySource], ["msi", { vram: "8" }, "i1", { code: "RX-0012", photoCount: 3 }]);
    assert.deepEqual([picked.price, picked.unitCost, picked.condition], ["9000", "7000", "New"]);
    const request = toLineRequest(picked);
    assert.deepEqual([request.brandId, request.copyPhotosFromItemId, request.specs], ["msi", "i1", { vram: "8" }]);
  });

  it("copies no photos from an item that has none, and stops when the model is retyped or the copy is dropped", () => {
    assert.equal(pickModel(line(), { ...suggestion, template: { ...template, photoCount: 0 } }).copyPhotosFromItemId, "");
    const picked = pickModel(line(), suggestion);
    assert.equal(typeModel(picked, "RTX 4070").copyPhotosFromItemId, "");
    const dropped = dropPhotoCopy(picked);
    assert.deepEqual([dropped.copyPhotosFromItemId, dropped.brandId], ["", "msi"]);
    assert.equal(toLineRequest(dropped).copyPhotosFromItemId, null);
  });

  it("starts a new purchase line from an inventory item with nothing to pay or price yet", () => {
    const item = {
      id: "i1", code: "RX-0012", modelName: "RTX 4060 8GB", categoryId: "gpu", brandId: "msi", productId: null, specs: { vram: "8" },
      photos: [{ storageKey: "a", thumbnailStorageKey: "b" }]
    } as unknown as Parameters<typeof lineFromItem>[0];
    const copied = lineFromItem(item, "k9");
    assert.deepEqual([copied.key, copied.modelName, copied.categoryId, copied.copyPhotosFromItemId], ["k9", "RTX 4060 8GB", "gpu", "i1"]);
    assert.deepEqual([copied.price, copied.unitCost, copied.serialNumber], ["", "", ""]);
  });
});
