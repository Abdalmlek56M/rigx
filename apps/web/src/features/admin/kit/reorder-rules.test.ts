import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { dropOn, inOrder, moveKey, sameOrder } from "./reorder-rules.ts";

describe("moving an id", () => {
  it("moves an id one step earlier or later", () => {
    assert.deepEqual(moveKey(["a", "b", "c"], 1, 0), ["b", "a", "c"]);
    assert.deepEqual(moveKey(["a", "b", "c"], 1, 2), ["a", "c", "b"]);
  });

  it("keeps the list when the move is outside it", () => {
    assert.deepEqual(moveKey(["a", "b"], 5, 0), ["a", "b"]);
    assert.deepEqual(moveKey(["a", "b"], 0, -3), ["a", "b"]);
    assert.deepEqual(moveKey(["a", "b"], 0, 9), ["b", "a"]);
  });
});

describe("dropping an id on another", () => {
  it("takes the other's place whichever way it was dragged", () => {
    assert.deepEqual(dropOn(["a", "b", "c", "d"], "a", "c"), ["b", "c", "a", "d"]);
    assert.deepEqual(dropOn(["a", "b", "c", "d"], "d", "b"), ["a", "d", "b", "c"]);
  });

  it("changes nothing when dropped on itself or on an id that is not in the list", () => {
    assert.deepEqual(dropOn(["a", "b", "c"], "b", "b"), ["a", "b", "c"]);
    assert.deepEqual(dropOn(["a", "b", "c"], "b", "zzz"), ["a", "b", "c"]);
    assert.deepEqual(dropOn(["a", "b", "c"], "zzz", "a"), ["a", "b", "c"]);
  });

  it("compares two orders", () => {
    assert.equal(sameOrder(["a", "b"], ["a", "b"]), true);
    assert.equal(sameOrder(["a", "b"], ["b", "a"]), false);
    assert.equal(sameOrder(["a"], ["a", "b"]), false);
  });
});

describe("showing items in a chosen order", () => {
  it("sorts by the listed ids and leaves an unlisted item after them in its own order", () => {
    const items = [{ id: "a" }, { id: "b" }, { id: "c" }, { id: "d" }];
    assert.deepEqual(inOrder(items, ["c", "a"]).map((item) => item.id), ["c", "a", "b", "d"]);
    assert.deepEqual(inOrder(items, []).map((item) => item.id), ["a", "b", "c", "d"]);
  });
});
