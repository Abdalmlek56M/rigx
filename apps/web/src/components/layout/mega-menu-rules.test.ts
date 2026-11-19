import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { conditionLinks } from "./mega-menu-rules.ts";

describe("conditionLinks", () => {
  it("shows no split while the stock is unknown or has no used items", () => {
    assert.deepEqual(conditionLinks(null), []);
    assert.deepEqual(conditionLinks({ new: 12, used: 0 }), []);
    assert.deepEqual(conditionLinks({ new: 0, used: 0 }), []);
  });
  it("shows new and used where both are in stock", () => {
    assert.deepEqual(conditionLinks({ new: 3, used: 1 }), ["New", "Used"]);
  });
  it("shows only used where nothing new is in stock", () => {
    assert.deepEqual(conditionLinks({ new: 0, used: 2 }), ["Used"]);
  });
});
