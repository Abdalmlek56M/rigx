import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Build, BuildPart } from "./builds-api.ts";
import {
  allocationDifference, allocationForm, allocationSum, allocationValid, canDisassemble, canPublish, canUnpublish, cardValid, copiesCount, defaultAllocation,
  buildOption, emptyNewPart, otherBuilds, filterBuilds, isLocked, newPartValid, pickBuild, sellModeKey, statusKey, toAllocationInputs, toCardForm, toEditRequest, toNewPartBody,
  toPartBody, toPartForm, emptyPlannedRow, pickPlannedModel, plannedRows, plannedRowsValid, plannedTotal, toPlannedInputs, typePlannedName
} from "./build-rules.ts";

const part = (patch: Partial<BuildPart> = {}): BuildPart => ({
  unitId: "u1", categoryId: null, name: "RX 6600", stockItemCode: "RX-0001", condition: "Used", acquisitionCost: 1000, standalonePrice: 1500, partPrice: 1400,
  warrantyLabel: "3 شهور", warrantyMonths: 3, warrantyOverridden: false, partnerPercentOverride: null, partnerId: null, allocationPercent: 50, allocatedAmount: 700, ...patch
});

const build = (patch: Partial<Build> = {}): Build => ({
  plannedParts: [], id: "b1", code: "BLD-01", name: "تجميعة ألعاب", publicTitleAr: "ألعاب", publicTitleEn: "Gaming PC", salePrice: 5000, status: "Draft", usage: "Gaming",
  sellMode: "FullOrSplit", warrantyLabel: null, warrantyMonths: null, copies: 1, suitableForAr: null, suitableForEn: null, photos: [], units: [part()],
  componentCost: 4000, partsTotal: 4800, allocatedTotal: 5000, rowVersion: 3, ...patch
});

describe("build status and list", () => {
  it("maps known words and falls back to other", () => {
    assert.equal(statusKey("Published"), "Published");
    assert.equal(statusKey("Weird"), "other");
    assert.equal(sellModeKey("Full"), "Full");
    assert.equal(sellModeKey(""), "other");
  });

  it("allows only the transitions the status permits", () => {
    assert.ok(canPublish(build()) && !canPublish(build({ status: "Published" })));
    assert.ok(canUnpublish(build({ status: "Published" })) && !canUnpublish(build()));
    assert.ok(canDisassemble(build()) && canDisassemble(build({ status: "Published" })) && !canDisassemble(build({ status: "Sold" })));
    assert.ok(isLocked(build({ status: "Sold" })) && isLocked(build({ status: "Disassembled" })) && !isLocked(build()));
  });

  it("filters by status and by code, name or public title", () => {
    const list = [build(), build({ id: "b2", code: "BLD-02", name: "Office", publicTitleAr: null, publicTitleEn: null, status: "Sold" })];
    assert.equal(filterBuilds(list, "", "").length, 2);
    assert.deepEqual(filterBuilds(list, "Sold", "").map((entry) => entry.id), ["b2"]);
    assert.deepEqual(filterBuilds(list, "", "gaming").map((entry) => entry.id), ["b1"]);
    assert.deepEqual(filterBuilds(list, "", " bld-02 ").map((entry) => entry.id), ["b2"]);
    assert.equal(filterBuilds(list, "Sold", "gaming").length, 0);
  });

  it("picks this build out of the list a remove or replace returned", () => {
    assert.equal(pickBuild([build({ id: "other" }), build()], "b1")?.id, "b1");
    assert.equal(pickBuild([], "b1"), undefined);
  });
});

describe("other builds", () => {
  it("leaves out this build and the ones that are history", () => {
    const list = [build(), build({ id: "b2" }), build({ id: "b3", status: "Sold" }), build({ id: "b4", status: "Published" })];
    assert.deepEqual(otherBuilds(list, "b1").map((entry) => entry.id), ["b2", "b4"]);
    assert.deepEqual(buildOption(build()), { value: "b1", label: "BLD-01 — تجميعة ألعاب" });
  });
});

describe("build card", () => {
  it("reads the build into the form and back, with the row version", () => {
    const request = toEditRequest(toCardForm(build()), build());
    assert.equal(request.name, "تجميعة ألعاب");
    assert.equal(request.salePrice, 5000);
    assert.equal(request.sellMode, "FullOrSplit");
    assert.equal(request.warrantyLabel, null);
    assert.equal(request.rowVersion, 3);
  });

  it("uses the parts total when the sale price is left empty", () => {
    assert.equal(toEditRequest({ ...toCardForm(build()), salePrice: "" }, build()).salePrice, 4800);
  });

  it("needs a name, a sale price that is empty or not negative, and a whole number of copies", () => {
    const form = toCardForm(build());
    assert.ok(cardValid(form));
    assert.ok(!cardValid({ ...form, name: " " }));
    assert.ok(!cardValid({ ...form, salePrice: "-1" }));
    assert.ok(cardValid({ ...form, salePrice: "" }));
    assert.equal(copiesCount("2"), 2);
    assert.equal(copiesCount("0"), 0);
    assert.equal(copiesCount("1.5"), 0);
  });
});

describe("build parts", () => {
  it("shows a part's own warranty only when it was overridden", () => {
    assert.equal(toPartForm(part()).warrantyLabel, "");
    assert.equal(toPartForm(part({ warrantyOverridden: true, warrantyLabel: "سنة" })).warrantyLabel, "سنة");
  });

  it("sends no percent without a partner", () => {
    const body = toPartBody({ partPrice: "1200", warrantyLabel: "", partnerId: "", partnerPercent: "30" }, 4);
    assert.deepEqual(body, { partPrice: 1200, warrantyLabel: null, partnerId: null, partnerPercentOverride: null, rowVersion: 4 });
    assert.equal(toPartBody({ partPrice: "1", warrantyLabel: "", partnerId: "p", partnerPercent: "" }, 4).partnerPercentOverride, null);
    assert.equal(toPartBody({ partPrice: "1", warrantyLabel: "", partnerId: "p", partnerPercent: "30" }, 4).partnerPercentOverride, 30);
  });

  it("builds a new part request and keeps the serial only for a used one", () => {
    const form = { ...emptyNewPart(), modelName: " RX 6600 ", acquisitionCost: "1000", price: "1500", serialNumber: "SN1" };
    assert.ok(newPartValid(form));
    assert.ok(!newPartValid({ ...form, modelName: "" }));
    assert.ok(!newPartValid({ ...form, acquisitionCost: "" }));
    const used = toNewPartBody(form, 7);
    assert.equal(used.modelName, "RX 6600");
    assert.equal(used.serialNumber, "SN1");
    assert.equal(used.rowVersion, 7);
    assert.equal(toNewPartBody({ ...form, condition: "New" }, 7).serialNumber, null);
  });
});

describe("revenue split", () => {
  const parts = [part({ unitId: "a", allocationPercent: 60 }), part({ unitId: "b", allocationPercent: 40 })];

  it("adds the percents and shows what is missing", () => {
    const form = allocationForm(parts);
    assert.equal(allocationSum(form), 100);
    assert.equal(allocationDifference(form), 0);
    assert.ok(allocationValid(form));
    assert.equal(allocationDifference({ a: "60", b: "30" }), 10);
    assert.equal(allocationDifference({ a: "70", b: "40" }), -10);
    assert.equal(allocationSum({ a: "33.33", b: "66.67" }), 100);
  });

  it("refuses a split that is not 100, negative, over 100 or not a number", () => {
    assert.ok(!allocationValid({ a: "60", b: "30" }));
    assert.ok(!allocationValid({ a: "120", b: "-20" }));
    assert.ok(!allocationValid({ a: "abc", b: "100" }));
    assert.ok(!allocationValid({ a: "", b: "100" }));
    assert.ok(!allocationValid({}));
  });

  it("resets to each part's share of the standalone prices and always makes 100", () => {
    const reset = defaultAllocation([
      part({ unitId: "a", standalonePrice: 1000 }), part({ unitId: "b", standalonePrice: 1000 }), part({ unitId: "c", standalonePrice: 1000 })
    ]);
    assert.deepEqual(reset, { a: "33.33", b: "33.33", c: "33.34" });
    assert.ok(allocationValid(reset));
    const mixed = defaultAllocation([part({ unitId: "a", standalonePrice: null, partPrice: 300 }), part({ unitId: "b", standalonePrice: 100, partPrice: 9 })]);
    assert.deepEqual(mixed, { a: "75", b: "25" });
  });

  it("falls back to an equal split when every price is zero", () => {
    const reset = defaultAllocation([part({ unitId: "a", standalonePrice: 0, partPrice: 0 }), part({ unitId: "b", standalonePrice: 0, partPrice: 0 })]);
    assert.deepEqual(reset, { a: "50", b: "50" });
    assert.deepEqual(defaultAllocation([]), {});
  });

  it("sends the percents as numbers", () => {
    assert.deepEqual(toAllocationInputs({ a: "60", b: "40" }), [{ unitId: "a", percent: 60 }, { unitId: "b", percent: 40 }]);
  });
});

describe("typed build parts", () => {
  const template = { itemId: "i1", code: "RX-0007", modelName: "RTX 4060 8GB", categoryId: "gpu", brandId: "msi", productId: null, specs: {}, photoCount: 0 };

  it("links a picked model in stock and unlinks it when the name is retyped", () => {
    const row = pickPlannedModel(emptyPlannedRow("k"), { modelName: "RTX 4060 8GB", categoryId: "gpu", productId: null, itemCount: 1, template });
    assert.deepEqual([row.name, row.categoryId, row.brandId, row.stockItemId, row.stockItemCode], ["RTX 4060 8GB", "gpu", "msi", "i1", "RX-0007"]);
    assert.deepEqual([typePlannedName(row, "RTX 4070").stockItemId, typePlannedName(row, "RTX 4070").stockItemCode], ["", ""]);
  });

  it("sends new rows without an id, keeps saved ids, and needs a name and a non-negative price", () => {
    const saved = plannedRows([{ id: "p1", name: "Ryzen 5 7600", categoryId: null, brandId: null, price: 8000, stockItemId: null, stockItemCode: null }]);
    const rows = [...saved, { ...emptyPlannedRow("k2"), name: " B650M ", price: "" }];
    assert.deepEqual(toPlannedInputs(rows).map((input) => [input.id, input.name, input.price]), [["p1", "Ryzen 5 7600", 8000], [null, "B650M", null]]);
    assert.equal(plannedTotal(rows), 8000);
    assert.equal(plannedRowsValid(rows), true);
    assert.equal(plannedRowsValid([{ ...emptyPlannedRow("k3"), name: "" }]), false);
    assert.equal(plannedRowsValid([{ ...emptyPlannedRow("k4"), name: "X", price: "-1" }]), false);
  });
});
