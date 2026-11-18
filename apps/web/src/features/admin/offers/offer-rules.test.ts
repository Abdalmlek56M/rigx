import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { ProductSummary } from "./offers-api.ts";
import {
  addOfferItem,
  cairoDate,
  cairoDayEndUtc,
  cairoDayStartUtc,
  emptyOfferForm,
  filterProducts,
  itemDiscount,
  offerDiscount,
  offerForm,
  offerStatusKey,
  offerValid,
  removeOfferItem,
  toOfferInput
} from "./offer-rules.ts";

const product = (id: string, name: string): ProductSummary => ({
  id, name, slug: id, sku: id.toUpperCase(), basePrice: 100, status: "Published", condition: "New",
  categoryName: "Cards", categorySlug: "cards", brandName: null, specifications: [], primaryImageStorageKey: null
});

describe("offer labels", () => {
  it("maps known statuses and falls back", () => {
    assert.equal(offerStatusKey("Published"), "Published");
    assert.equal(offerStatusKey("Expired"), "Other");
  });
});

describe("Cairo day to UTC", () => {
  it("uses UTC+2 in winter and UTC+3 in summer", () => {
    assert.equal(cairoDayStartUtc("2026-11-20"), "2026-11-19T22:00:00.000Z");
    assert.equal(cairoDayEndUtc("2026-11-20"), "2026-11-20T21:59:59.999Z");
    assert.equal(cairoDayStartUtc("2026-07-01"), "2026-06-30T21:00:00.000Z");
    assert.equal(cairoDayEndUtc("2026-07-01"), "2026-07-01T20:59:59.999Z");
  });

  it("reads a stored instant as its Cairo date", () => {
    assert.equal(cairoDate("2026-11-19T22:00:00.000Z"), "2026-11-20");
    assert.equal(cairoDate("2026-11-20T21:59:59.999Z"), "2026-11-20");
  });
});

describe("offer form", () => {
  it("maps a view to the form and back", () => {
    const form = offerForm({
      id: "o1", titleAr: "عرض", titleEn: "Offer", descriptionAr: null, descriptionEn: null,
      startsAtUtc: "2026-11-19T22:00:00Z", endsAtUtc: "2026-11-20T21:59:59.999Z", status: "Draft", priority: 2,
      createdAtUtc: "2026-11-01T00:00:00Z", updatedAtUtc: "2026-11-01T00:00:00Z", rowVersion: 3,
      items: [{ productId: "p1", productName: "Card", productSlug: "card", productSku: "C1", basePrice: 100, offerPrice: 90, thumbnailStorageKey: null, categorySlug: null, sortOrder: 0 }]
    });
    assert.deepEqual([form.startDate, form.endDate, form.priority, form.items[0]?.offerPrice], ["2026-11-20", "2026-11-20", "2", "90"]);
    const input = toOfferInput({ ...form, titleAr: " عرض ", priority: "4" });
    assert.deepEqual([input.titleAr, input.priority, input.startsAtUtc, input.endsAtUtc], ["عرض", 4, "2026-11-19T22:00:00.000Z", "2026-11-20T21:59:59.999Z"]);
    assert.deepEqual(input.items, [{ productId: "p1", offerPrice: 90 }]);
  });

  it("accepts a valid offer and refuses a bad price or window", () => {
    const base = { ...emptyOfferForm("2026-11-20"), titleAr: "عرض", titleEn: "Offer" };
    assert.equal(offerValid(base), false);
    const withItem = { ...base, items: [{ productId: "p1", name: "Card", sku: "C1", basePrice: 100, offerPrice: "90" }] };
    assert.equal(offerValid(withItem), true);
    assert.equal(offerValid({ ...withItem, items: [{ ...withItem.items[0]!, offerPrice: "100" }] }), false);
    assert.equal(offerValid({ ...withItem, items: [{ ...withItem.items[0]!, offerPrice: "0" }] }), false);
    assert.equal(offerValid({ ...withItem, endDate: "2026-11-19" }), false);
    assert.equal(offerValid({ ...withItem, endDate: "2026-11-21" }), true);
  });
});

describe("offer discount and products", () => {
  it("computes the discount of an item and the range of an offer", () => {
    assert.equal(itemDiscount(100, 90), 10);
    assert.equal(itemDiscount(200, 150), 25);
    assert.deepEqual(offerDiscount([{ basePrice: 100, offerPrice: 90 }, { basePrice: 200, offerPrice: 150 }]), { min: 10, max: 25 });
    assert.equal(offerDiscount([]), null);
  });

  it("filters products by text and keeps the chosen ones out", () => {
    const list = [product("p1", "RTX 4060"), product("p2", "RX 6600"), product("p3", "Ryzen 5")];
    assert.deepEqual(filterProducts(list, "rtx", []).map((item) => item.id), ["p1"]);
    assert.deepEqual(filterProducts(list, "", ["p2"]).map((item) => item.id), ["p1", "p3"]);
    assert.deepEqual(filterProducts(list, "zz", []), []);
  });

  it("adds a product at 10% off and removes it", () => {
    const added = addOfferItem([], product("p1", "RTX 4060"));
    assert.deepEqual([added[0]?.productId, added[0]?.offerPrice], ["p1", "90.00"]);
    assert.equal(addOfferItem(added, product("p1", "RTX 4060")).length, 1);
    assert.deepEqual(removeOfferItem(added, "p1"), []);
  });
});
