import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { DEFAULT_DENSITY, densityStorageKey, nextDensity, parseDensity } from "./density-rules.ts";

describe("table density", () => {
  it("is comfortable unless the stored value says compact", () => {
    assert.equal(DEFAULT_DENSITY, "comfortable");
    assert.equal(parseDensity(null), "comfortable");
    assert.equal(parseDensity("tight"), "comfortable");
    assert.equal(parseDensity("compact"), "compact");
  });
  it("keeps one choice per user", () => {
    assert.notEqual(densityStorageKey("owner"), densityStorageKey("cashier"));
    assert.ok(densityStorageKey("owner").endsWith("owner"));
  });
  it("switches to the other density", () => {
    assert.equal(nextDensity("comfortable"), "compact");
    assert.equal(nextDensity("compact"), "comfortable");
  });
});
