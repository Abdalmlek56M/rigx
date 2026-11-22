import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { isOn, toggled } from "./reaction-rules.ts";

describe("toggled", () => {
  const none = { hearts: 0, likes: 0, hearted: false, liked: false, views: 0 };

  it("adds one and marks it when the visitor had not reacted", () => {
    assert.deepEqual(toggled(none, "Heart"), { hearts: 1, likes: 0, hearted: true, liked: false, views: 0 });
    assert.deepEqual(toggled({ hearts: 4, likes: 2, hearted: false, liked: true, views: 0 }, "Like"), { hearts: 4, likes: 1, hearted: false, liked: false, views: 0 });
  });

  it("takes one away and unmarks it when the visitor had already reacted", () => {
    assert.deepEqual(toggled({ hearts: 3, likes: 1, hearted: true, liked: false, views: 0 }, "Heart"), { hearts: 2, likes: 1, hearted: false, liked: false, views: 0 });
  });

  it("never goes below zero and leaves the other reaction alone", () => {
    assert.equal(toggled({ hearts: 0, likes: 5, hearted: true, liked: false, views: 0 }, "Heart").hearts, 0);
    assert.deepEqual(toggled({ hearts: 2, likes: 5, hearted: true, liked: true, views: 0 }, "Heart"), { hearts: 1, likes: 5, hearted: false, liked: true, views: 0 });
  });

  it("toggling twice gives back the start", () => {
    const start = { hearts: 7, likes: 3, hearted: false, liked: true, views: 0 };
    assert.deepEqual(toggled(toggled(start, "Heart"), "Heart"), start);
  });
});

describe("isOn", () => {
  it("reads the visitor's own mark", () => {
    assert.equal(isOn({ hearts: 1, likes: 1, hearted: true, liked: false, views: 0 }, "Heart"), true);
    assert.equal(isOn({ hearts: 1, likes: 1, hearted: true, liked: false, views: 0 }, "Like"), false);
  });
});
