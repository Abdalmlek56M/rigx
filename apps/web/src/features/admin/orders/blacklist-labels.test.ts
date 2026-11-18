import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { blacklistTitle, maskPhone } from "./blacklist-labels.ts";

describe("maskPhone", () => {
  it("keeps the first three and last three digits", () => {
    assert.equal(maskPhone("01012345678"), "010•••••678");
  });

  it("shows a short number as it is", () => {
    assert.equal(maskPhone("12345"), "12345");
  });
});

describe("blacklistTitle", () => {
  it("prefers the name, then the email, then the address", () => {
    assert.equal(blacklistTitle({ name: "Ali", email: "a@b.c", addressText: "Giza" }), "Ali");
    assert.equal(blacklistTitle({ name: null, email: "a@b.c", addressText: "Giza" }), "a@b.c");
    assert.equal(blacklistTitle({ name: null, email: null, addressText: "Giza" }), "Giza");
    assert.equal(blacklistTitle({ name: null, email: null, addressText: null }), null);
  });
});
