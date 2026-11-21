import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Schemas } from "@rigx/api-client";
import { pickRelatedItems, RELATED_MAX } from "./related-items.ts";

type Card = Schemas["StoreItemCard"];

const card = (code: string, available = 3): Card => ({
  code,
  slug: code.toLowerCase(),
  modelName: code,
  condition: "New",
  available,
  prices: { alone: 100, withBoard: 100, inBuild: 100 }
} as Card);

describe("pickRelatedItems", () => {
  it("leaves out the current item, de-duplicates and puts available items first", () => {
    const items = pickRelatedItems("RX-0", [card("RX-1", 0), card("RX-0"), card("RX-2", 1), card("RX-1", 0)]);
    assert.deepEqual(items.map((item) => item.code), ["RX-2", "RX-1"]);
  });

  it("keeps the same category when it already has six items", () => {
    const same = ["RX-1", "RX-2", "RX-3", "RX-4", "RX-5", "RX-6"].map((code) => card(code));
    const items = pickRelatedItems("RX-0", same, [card("RX-7")]);
    assert.deepEqual(items.map((item) => item.code), ["RX-1", "RX-2", "RX-3", "RX-4", "RX-5", "RX-6"]);
  });

  it("fills from the parent category, then from the newest items", () => {
    const same = [card("RX-1"), card("RX-2")];
    const parent = [card("RX-3"), card("RX-4")];
    const newest = [card("RX-5"), card("RX-6"), card("RX-1")];
    assert.deepEqual(
      pickRelatedItems("RX-0", same, parent, newest).map((item) => item.code),
      ["RX-1", "RX-2", "RX-3", "RX-4", "RX-5", "RX-6"]
    );
  });

  it("never returns more than the cap", () => {
    const many = Array.from({ length: 20 }, (_, index) => card(`RX-${index + 1}`));
    assert.equal(pickRelatedItems("RX-0", many).length, RELATED_MAX);
  });
});
