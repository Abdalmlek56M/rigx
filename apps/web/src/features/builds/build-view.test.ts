import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { buildCartLine, buildSuitableFor, buildTitle, showPartPrices, type BuildView } from "./build-view.ts";

const build = (patch: Partial<BuildView> = {}): BuildView => ({
  code: "BLD-7",
  titleAr: "جهاز ألعاب",
  titleEn: "Gaming PC",
  salePrice: 30000,
  sellMode: "FullOnly",
  copies: 2,
  warrantyLabel: "ضمان سنة",
  suitableForAr: "ألعاب",
  suitableForEn: "Games",
  photos: [{ storageKey: "a", thumbnailStorageKey: "a-thumb" }],
  ...patch
});

describe("buildTitle / buildSuitableFor", () => {
  it("picks the visitor's language", () => {
    assert.equal(buildTitle(build(), "ar"), "جهاز ألعاب");
    assert.equal(buildTitle(build(), "en"), "Gaming PC");
    assert.equal(buildSuitableFor(build(), "en"), "Games");
  });
  it("falls back to the other language, treating blank as missing", () => {
    assert.equal(buildTitle(build({ titleAr: "  " }), "ar"), "Gaming PC");
    assert.equal(buildTitle(build({ titleEn: null }), "en"), "جهاز ألعاب");
    assert.equal(buildSuitableFor(build({ suitableForAr: null }), "ar"), "Games");
  });
  it("uses the code when there is no title and null when nothing suits", () => {
    assert.equal(buildTitle(build({ titleAr: null, titleEn: "" }), "ar"), "BLD-7");
    assert.equal(buildSuitableFor(build({ suitableForAr: null, suitableForEn: null }), "ar"), null);
  });
});

describe("showPartPrices", () => {
  it("is true only for FullOrSplit", () => {
    assert.equal(showPartPrices({ sellMode: "FullOrSplit" }), true);
    assert.equal(showPartPrices({ sellMode: "FullOnly" }), false);
  });
});

describe("buildCartLine", () => {
  it("makes a Build line with the first photo's thumbnail and copies as the cap", () => {
    const line = buildCartLine(build(), "en", 2);
    assert.deepEqual(
      [line.kind, line.code, line.modelName, line.unitPrice, line.quantity, line.maxQuantity, line.thumbnailStorageKey, line.priceOption],
      ["Build", "BLD-7", "Gaming PC", 30000, 2, 2, "a-thumb", "InBuild"]
    );
  });
  it("defaults to one and has no thumbnail without photos", () => {
    const line = buildCartLine(build({ photos: [] }), "ar");
    assert.equal(line.quantity, 1);
    assert.equal(line.thumbnailStorageKey, null);
  });
});
