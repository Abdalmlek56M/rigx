import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { FAVOURITES_MAX, parseStoredCodes, toggleCode } from "./favourite-rules.ts";

describe("parseStoredCodes", () => {
  it("keeps item and build codes once, in order", () => {
    assert.deepEqual(parseStoredCodes(JSON.stringify(["RX-0002", "BLD-01", "RX-0002", "BLD-01"])), ["RX-0002", "BLD-01"]);
  });
  it("keeps only strings shaped like an item or build code", () => {
    assert.deepEqual(parseStoredCodes(JSON.stringify(["RX-1", "BLD-7", "BLD", "BLD-01x", 5, null, "rx-2", "bld-1"])), ["RX-1", "BLD-7"]);
  });
  it("reads broken or missing storage as empty", () => {
    assert.deepEqual(parseStoredCodes(null), []);
    assert.deepEqual(parseStoredCodes("{"), []);
    assert.deepEqual(parseStoredCodes("{\"a\":1}"), []);
  });
  it("never returns more than the limit", () => {
    const many = Array.from({ length: FAVOURITES_MAX + 5 }, (_, index) => `RX-${index + 1}`);
    assert.equal(parseStoredCodes(JSON.stringify(many)).length, FAVOURITES_MAX);
  });
});

describe("toggleCode", () => {
  it("adds a new code at the front", () => {
    assert.deepEqual(toggleCode(["RX-1"], "RX-2"), ["RX-2", "RX-1"]);
  });
  it("removes a saved code", () => {
    assert.deepEqual(toggleCode(["RX-2", "RX-1"], "RX-2"), ["RX-1"]);
  });
  it("does not add to a full list but still removes from it", () => {
    const full = Array.from({ length: FAVOURITES_MAX }, (_, index) => `RX-${index + 1}`);
    assert.equal(toggleCode(full, "RX-9999").length, FAVOURITES_MAX);
    assert.equal(toggleCode(full, "RX-1").length, FAVOURITES_MAX - 1);
  });
});
