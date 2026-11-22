import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { makeMain } from "./photo-order.ts";

describe("photo order", () => {
  it("makes a photo the main one and keeps the rest in order", () => {
    assert.deepEqual(makeMain(["a", "b", "c", "d"], "c"), ["c", "a", "b", "d"]);
    assert.deepEqual(makeMain(["a", "b"], "a"), ["a", "b"]);
    assert.deepEqual(makeMain(["a", "b"], "zzz"), ["a", "b"]);
  });
});
