import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { localizedName } from "./localized-name.ts";

const category = { name: "معالجات", nameEn: "Processors" };

describe("localizedName", () => {
  it("uses the English name on /en", () => {
    assert.equal(localizedName(category, "en"), "Processors");
  });

  it("keeps the Arabic name on /ar, even when an English name exists", () => {
    assert.equal(localizedName(category, "ar"), "معالجات");
  });

  it("falls back to the Arabic name when the English one is missing, empty or whitespace", () => {
    assert.equal(localizedName({ name: "معالجات" }, "en"), "معالجات");
    assert.equal(localizedName({ name: "معالجات", nameEn: null }, "en"), "معالجات");
    assert.equal(localizedName({ name: "معالجات", nameEn: "" }, "en"), "معالجات");
    assert.equal(localizedName({ name: "معالجات", nameEn: "   " }, "en"), "معالجات");
  });

  it("trims a real English name", () => {
    assert.equal(localizedName({ name: "معالجات", nameEn: "  Processors  " }, "en"), "Processors");
  });
});
