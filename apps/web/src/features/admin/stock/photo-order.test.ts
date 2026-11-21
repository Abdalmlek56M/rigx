import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { makeMain, moveKey } from "./photo-order.ts";

describe("photo order", () => {
  it("moves a photo one step earlier or later", () => {
    assert.deepEqual(moveKey(["a", "b", "c"], 1, 0), ["b", "a", "c"]);
    assert.deepEqual(moveKey(["a", "b", "c"], 1, 2), ["a", "c", "b"]);
  });

  it("keeps the list when the move is outside it", () => {
    assert.deepEqual(moveKey(["a", "b"], 5, 0), ["a", "b"]);
    assert.deepEqual(moveKey(["a", "b"], 0, -3), ["a", "b"]);
    assert.deepEqual(moveKey(["a", "b"], 0, 9), ["b", "a"]);
  });

  it("makes a photo the main one and keeps the rest in order", () => {
    assert.deepEqual(makeMain(["a", "b", "c", "d"], "c"), ["c", "a", "b", "d"]);
    assert.deepEqual(makeMain(["a", "b"], "a"), ["a", "b"]);
    assert.deepEqual(makeMain(["a", "b"], "zzz"), ["a", "b"]);
  });
});
