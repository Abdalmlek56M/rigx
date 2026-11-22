import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Brand } from "./brands-api.ts";
import { brandForm, brandFormValid, emptyBrandForm, filterBrands, slugFromName, suggestBrandsFirst, toBrandInput, toggleId } from "./brand-rules.ts";

const brand = (patch: Partial<Brand> = {}): Brand => ({
  id: "b1", name: "MSI", slug: "msi", description: "kept", logoStorageKey: "brands/x.webp", supportUrl: "https://msi.com",
  isActive: true, rowVersion: 4, sortOrder: 0, categoryIds: ["gpu"], ...patch
});

describe("slugFromName", () => {
  it("turns a brand name into a link part", () => {
    assert.equal(slugFromName("be quiet!"), "be-quiet");
    assert.equal(slugFromName("G.Skill"), "g-skill");
    assert.equal(slugFromName(" Western  Digital "), "western-digital");
    assert.equal(slugFromName("بلا"), "");
  });
});

describe("brand form", () => {
  it("needs a name and a valid link part", () => {
    assert.equal(brandFormValid(emptyBrandForm), false);
    assert.equal(brandFormValid({ ...emptyBrandForm, name: "ASUS", slug: "asus" }), true);
    assert.equal(brandFormValid({ ...emptyBrandForm, name: "ASUS", slug: "Asus Rog" }), false);
  });
  it("keeps the fields this screen does not edit and sends the row version", () => {
    const input = toBrandInput({ ...brandForm(brand()), name: " MSI Gaming ", isActive: false }, brand());
    assert.deepEqual(input, {
      name: "MSI Gaming", slug: "msi", description: "kept", logoStorageKey: "brands/x.webp", supportUrl: "https://msi.com", isActive: false, rowVersion: 4,
      categoryIds: ["gpu"]
    });
    assert.equal(toBrandInput(emptyBrandForm, null).rowVersion, null);
  });
  it("sends the suggested categories the form holds and copies the brand's, not shares them", () => {
    const source = brand();
    const form = brandForm(source);
    assert.deepEqual(form.categoryIds, ["gpu"]);
    assert.notEqual(form.categoryIds, source.categoryIds);
    assert.deepEqual(toBrandInput({ ...form, categoryIds: ["gpu", "ram"] }, source).categoryIds, ["gpu", "ram"]);
    assert.deepEqual(toBrandInput(emptyBrandForm, null).categoryIds, []);
  });
  it("adds or takes out one category", () => {
    assert.deepEqual(toggleId(["a"], "b"), ["a", "b"]);
    assert.deepEqual(toggleId(["a", "b"], "a"), ["b"]);
  });
});

describe("suggestBrandsFirst", () => {
  const list = [brand({ id: "1", categoryIds: [] }), brand({ id: "2", categoryIds: ["ram"] }), brand({ id: "3", categoryIds: ["gpu", "ram"] }), brand({ id: "4", categoryIds: ["case"] })];
  it("puts the brands suggested for the category or one above it first, marked, and keeps the order inside each part", () => {
    const result = suggestBrandsFirst(list, ["parent", "ram"]);
    assert.deepEqual(result.map((entry) => [entry.brand.id, entry.suggested]), [["2", true], ["3", true], ["1", false], ["4", false]]);
  });
  it("marks nothing and keeps every brand when no category is chosen", () => {
    const result = suggestBrandsFirst(list, []);
    assert.deepEqual(result.map((entry) => [entry.brand.id, entry.suggested]), [["1", false], ["2", false], ["3", false], ["4", false]]);
  });
});

describe("filterBrands", () => {
  it("matches part of the name in any case", () => {
    const list = [brand(), brand({ id: "b2", name: "ASUS", slug: "asus" })];
    assert.deepEqual(filterBrands(list, "as").map((entry) => entry.name), ["ASUS"]);
    assert.equal(filterBrands(list, "  ").length, 2);
  });
});
