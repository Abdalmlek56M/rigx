import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { addItem, moveItem, removeItem, replaceItem } from "./list-editor-rules.ts";

const list = ["a", "b", "c"];

describe("list editor helpers", () => {
  it("adds to the end and replaces one item", () => {
    assert.deepEqual(addItem(list, "d"), ["a", "b", "c", "d"]);
    assert.deepEqual(replaceItem(list, 1, "B"), ["a", "B", "c"]);
    assert.deepEqual(list, ["a", "b", "c"]);
  });

  it("removes one item", () => {
    assert.deepEqual(removeItem(list, 0), ["b", "c"]);
    assert.deepEqual(removeItem(list, 2), ["a", "b"]);
    assert.deepEqual(removeItem(list, 9), ["a", "b", "c"]);
  });

  it("moves up and down and keeps the same list at the edges", () => {
    assert.deepEqual(moveItem(list, 2, -1), ["a", "c", "b"]);
    assert.deepEqual(moveItem(list, 0, 1), ["b", "a", "c"]);
    assert.deepEqual(moveItem(list, 0, -1), ["a", "b", "c"]);
    assert.deepEqual(moveItem(list, 2, 1), ["a", "b", "c"]);
    assert.deepEqual(moveItem(list, 9, -1), ["a", "b", "c"]);
    assert.deepEqual(list, ["a", "b", "c"]);
  });
});
