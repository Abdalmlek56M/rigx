import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  addToCart,
  customerSearchReady,
  buildLine,
  EMPTY_POS_FORM,
  fillRemaining,
  itemLine,
  offeredOptions,
  paidTotal,
  paymentState,
  posPhone,
  soleMatch,
  toBuildPick,
  toItemPick,
  toSaleRequest,
  validateSale,
  withCustomer,
  type BuildLine,
  type BuildPick,
  type ItemPick,
  type PosForm
} from "./pos-rules.ts";

const item: ItemPick = {
  stockItemId: "s1", code: "RX-0153", name: "Ryzen 5 5600", condition: "Used", warrantyLabel: null,
  prices: { alone: 4000, withBoard: 3800, inBuild: 3600 }, available: 2, priceOptions: ["Alone", "WithBoard", "InBuild"]
};
const build: BuildPick = {
  assemblyId: "a1", code: "BLD-07", name: "Gaming build", salePrice: 30000, splittable: true,
  parts: [
    { unitId: "u1", name: "CPU", code: "RX-0153", condition: "Used", warrantyLabel: null, partPrice: 4000 },
    { unitId: "u2", name: "Board", code: "RX-0146", condition: "Used", warrantyLabel: null, partPrice: 3000 }
  ]
};
const form: PosForm = { ...EMPTY_POS_FORM, name: "Ahmed", phone: "+20 101 234 5772" };

describe("search results", () => {
  it("keeps the stock item id from the inventory and sends public items by code", () => {
    const fromStore = toItemPick({
      code: "RX-1", slug: "gpu", modelName: "GPU", condition: "New", categoryId: null, warrantyLabel: "سنة",
      prices: { alone: 1, withBoard: 1, inBuild: 1 }, available: 3, thumbnailStorageKey: null, secondThumbnailStorageKey: null, listedAtUtc: "", specs: {}, priceOptions: ["Alone", "WithBoard", "InBuild"]
    });
    assert.equal(fromStore.stockItemId, null);
    assert.equal(fromStore.available, 3);
  });

  it("offers only the prices the category offers and starts the line at the first one", () => {
    const card = {
      code: "RX-1", slug: "gpu", modelName: "GPU", condition: "New", categoryId: null, warrantyLabel: null,
      prices: { alone: 1000, withBoard: 900, inBuild: 800 }, available: 3, thumbnailStorageKey: null, secondThumbnailStorageKey: null, listedAtUtc: "", specs: {},
      priceOptions: ["WithBoard", "InBuild"]
    };
    const pick = toItemPick(card);
    assert.deepEqual(pick.priceOptions, ["WithBoard", "InBuild"]);
    const line = itemLine(pick);
    assert.deepEqual([line.priceOption, line.salePrice], ["WithBoard", "900"]);
    assert.deepEqual(offeredOptions(["InBuild", "Alone"]), ["Alone", "InBuild"]);
    assert.deepEqual(offeredOptions([]), ["Alone", "WithBoard", "InBuild"]);
    assert.deepEqual(toItemPick({ ...card, priceOptions: [] }).priceOptions, ["Alone", "WithBoard", "InBuild"]);
  });

  it("sells a public build whole only (its parts are not known)", () => {
    const pick = toBuildPick({
      id: "a1", code: "BLD-07", titleAr: "تجميعة", titleEn: null, salePrice: 30000, usage: null, sellMode: "FullOrSplit",
      copies: 1, warrantyLabel: null, suitableForAr: null, suitableForEn: null, photos: [], units: []
    });
    assert.equal(pick.splittable, false);
    assert.equal(pick.parts, null);
  });
});

describe("cart", () => {
  it("adds one more piece of the same item up to what is available", () => {
    let cart = addToCart([], itemLine(item));
    cart = addToCart(cart, itemLine(item));
    cart = addToCart(cart, itemLine(item));
    assert.equal(cart.length, 1);
    assert.equal(cart[0]!.kind === "item" && cart[0]!.quantity, 2);
  });
});

describe("sale request", () => {
  it("sends the item's id, price option and typed sale price; the phone in its local form", () => {
    const line = { ...itemLine(item), priceOption: "WithBoard" as const, salePrice: "3700" };
    const request = toSaleRequest([line], form, true);
    assert.deepEqual(request.items, [{
      stockItemId: "s1", assemblyId: null, code: null, quantity: 1, priceOption: "WithBoard", salePrice: 3700, parts: null
    }]);
    assert.equal(request.customer?.phone, "01012345772");
    assert.equal(request.address, null);
    assert.equal(request.deliveryFee, null);
    assert.equal(request.confirm, true);
  });

  it("sends a split build's chosen parts with their own prices", () => {
    const line: BuildLine = { ...buildLine(build), mode: "split", parts: { u1: { selected: true, salePrice: "3900" }, u2: { selected: false, salePrice: "3000" } } };
    const request = toSaleRequest([line], form, false);
    assert.deepEqual(request.items?.[0]?.parts, [{ unitId: "u1", salePrice: 3900 }]);
    assert.equal(request.items?.[0]?.salePrice, null);
    assert.equal(request.confirm, false);
  });

  it("sends the coupon code upper-cased, or null when empty", () => {
    assert.equal(toSaleRequest([itemLine(item)], form, true).couponCode, null);
    assert.equal(toSaleRequest([itemLine(item)], { ...form, couponCode: " ahmed10 " }, true).couponCode, "AHMED10");
  });

  it("sends only filled payments and an empty delivery fee as the settings' fee", () => {
    const request = toSaleRequest([itemLine(item)], {
      ...form, fulfillment: "Delivery", governorate: "C", area: "Nasr City", street: "Abbas",
      payments: [{ key: "a", method: "Cash", amount: "1000" }, { key: "b", method: "InstaPay", amount: "" }]
    }, true);
    assert.deepEqual(request.payments, [{ method: "Cash", amount: 1000, moneyAccountId: null }]);
    assert.equal(request.deliveryFee, null);
    assert.equal(request.address?.governorate, "C");
  });
});

describe("checks", () => {
  it("needs lines, a name, a valid phone and the delivery address", () => {
    assert.deepEqual(validateSale([], { ...EMPTY_POS_FORM, fulfillment: "Delivery" }, null),
      ["lines", "name", "phone", "governorate", "area", "street"]);
  });

  it("refuses a bad sale price, a split build with no part and payments above the total", () => {
    const bad = { ...itemLine(item), salePrice: "12.345" };
    const split: BuildLine = { ...buildLine(build), mode: "split" };
    assert.deepEqual(validateSale([bad, split], form, null), ["salePrice", "parts"]);
    assert.deepEqual(validateSale([itemLine(item)], { ...form, payments: [{ key: "a", method: "Cash", amount: "5000" }] }, 4000), ["overpaid"]);
    assert.deepEqual(validateSale([itemLine(item)], { ...form, payments: [{ key: "a", method: "Cash", amount: "0" }] }, 4000), ["payments"]);
  });

  it("reads +20 and 0020 as a local mobile", () => {
    assert.equal(posPhone("00201012345772"), "01012345772");
    assert.equal(posPhone("4915112345678"), "4915112345678");
  });
});

describe("payments", () => {
  it("fills the first row with what the other rows leave", () => {
    const rows = fillRemaining([{ key: "a", method: "Cash", amount: "" }, { key: "b", method: "InstaPay", amount: "1500.5" }], 4000);
    assert.equal(rows[0]!.amount, "2499.5");
    assert.equal(paidTotal(rows), 4000);
  });
});

describe("POS customer", () => {
  it("searches from 4 phone digits or 2 letters of a name", () => {
    assert.equal(customerSearchReady("010"), false);
    assert.equal(customerSearchReady(" 0100 "), true);
    assert.equal(customerSearchReady("+20 1"), false);
    assert.equal(customerSearchReady("م"), false);
    assert.equal(customerSearchReady("مني"), true);
  });

  it("takes the picked customer's phone and fills only the empty fields", () => {
    const form: PosForm = { ...EMPTY_POS_FORM, name: "Mona A.", phone: "0100", governorate: "" };
    const next = withCustomer(form, {
      name: "Mona Adel", phone: "01007771234", phoneHasWhatsApp: false, email: "mona@example.com", orderCount: 2, isBlacklisted: false,
      address: { governorate: "CAIRO", area: "Nasr City", street: "Abbas", building: null, landmark: null }
    });
    assert.deepEqual(
      [next.name, next.phone, next.phoneHasWhatsApp, next.email, next.governorate, next.area, next.building],
      ["Mona A.", "01007771234", false, "mona@example.com", "CAIRO", "Nasr City", ""]
    );
  });
});

describe("POS payment state", () => {
  it("compares what was paid with the total", () => {
    assert.deepEqual(paymentState(null, 100), { kind: "unpriced" });
    assert.deepEqual(paymentState(1000, 400.5), { kind: "due", amount: 599.5 });
    assert.deepEqual(paymentState(1000, 1000), { kind: "settled" });
    assert.deepEqual(paymentState(1000, 1200), { kind: "over", amount: 200 });
  });
});

describe("soleMatch", () => {
  const pick = (code: string): ItemPick => ({ ...item, code });
  it("takes the item whose code is exactly what was typed, even among several", () => {
    assert.equal(soleMatch([pick("RX-10"), pick("RX-1")], " rx-1 ")?.code, "RX-1");
  });
  it("takes the only item found", () => {
    assert.equal(soleMatch([pick("RX-7")], "gpu")?.code, "RX-7");
  });
  it("adds nothing when several items could be meant or none was found", () => {
    assert.equal(soleMatch([pick("RX-1"), pick("RX-2")], "rx"), null);
    assert.equal(soleMatch([], "rx"), null);
  });
});
