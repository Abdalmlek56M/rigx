import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { DEFAULT_VISIBILITY, moderationNote, parseVisibility } from "./build-visibility.ts";

describe("parseVisibility", () => {
  it("knows the three values and falls back to the link for anything else", () => {
    assert.equal(parseVisibility("Private"), "Private");
    assert.equal(parseVisibility("Public"), "Public");
    assert.equal(parseVisibility("Everyone"), DEFAULT_VISIBILITY);
    assert.equal(parseVisibility(null), DEFAULT_VISIBILITY);
    assert.equal(DEFAULT_VISIBILITY, "Link");
  });
});

describe("moderationNote", () => {
  it("is only for a public build that has a state", () => {
    assert.equal(moderationNote("Public", "Pending"), "pending");
    assert.equal(moderationNote("Public", "Approved"), "approved");
    assert.equal(moderationNote("Public", "Rejected"), "rejected");
    assert.equal(moderationNote("Public", null), null);
    assert.equal(moderationNote("Link", "Approved"), null);
    assert.equal(moderationNote("Private", "Pending"), null);
  });
});
