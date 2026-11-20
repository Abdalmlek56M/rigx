import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { brandSlug } from "./brand-slug.ts";

describe("brand slug", () => {
  it("lowercases words and joins them with one dash", () => {
    assert.equal(brandSlug("  ASUS ROG  "), "asus-rog");
    assert.equal(brandSlug("G.Skill"), "g-skill");
    assert.equal(brandSlug("Kingston Fury 32GB"), "kingston-fury-32gb");
  });

  it("trims the dashes at both ends", () => {
    assert.equal(brandSlug("--NVIDIA--"), "nvidia");
    assert.equal(brandSlug("A/B"), "a-b");
  });

  it("leaves nothing for a name with no English letters or digits", () => {
    assert.equal(brandSlug("ماركة عربية"), "");
    assert.equal(brandSlug("  "), "");
  });
});
