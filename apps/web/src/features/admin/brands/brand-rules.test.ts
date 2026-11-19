import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Brand } from "./brands-api.ts";
import { brandForm, brandFormValid, emptyBrandForm, filterBrands, slugFromName, toBrandInput } from "./brand-rules.ts";

const brand = (patch: Partial<Brand> = {}): Brand => ({
  id: "b1", name: "MSI", slug: "msi", description: "kept", logoStorageKey: "brands/x.webp", supportUrl: "https://msi.com",
  isActive: true, rowVersion: 4, ...patch
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
    assert.equal(brandFormValid({ name: "ASUS", slug: "asus", isActive: true }), true);
    assert.equal(brandFormValid({ name: "ASUS", slug: "Asus Rog", isActive: true }), false);
  });
  it("keeps the fields this screen does not edit and sends the row version", () => {
    const input = toBrandInput({ ...brandForm(brand()), name: " MSI Gaming ", isActive: false }, brand());
    assert.deepEqual(input, {
      name: "MSI Gaming", slug: "msi", description: "kept", logoStorageKey: "brands/x.webp", supportUrl: "https://msi.com", isActive: false, rowVersion: 4
    });
    assert.equal(toBrandInput(emptyBrandForm, null).rowVersion, null);
  });
});

describe("filterBrands", () => {
  it("matches part of the name in any case", () => {
    const list = [brand(), brand({ id: "b2", name: "ASUS", slug: "asus" })];
    assert.deepEqual(filterBrands(list, "as").map((entry) => entry.name), ["ASUS"]);
    assert.equal(filterBrands(list, "  ").length, 2);
  });
});
