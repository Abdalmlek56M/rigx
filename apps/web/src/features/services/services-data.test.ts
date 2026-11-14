import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { SERVICES, textList } from "./services-data.ts";

describe("textList", () => {
  it("keeps only the strings of an array", () => {
    assert.deepEqual(textList(["a", 1, null, "b", { x: 1 }]), ["a", "b"]);
    assert.deepEqual(textList([]), []);
  });
  it("is empty for anything that is not an array", () => {
    for (const value of [undefined, null, "text", 5, { 0: "a" }]) assert.deepEqual(textList(value), []);
  });
});

describe("SERVICES", () => {
  it("has unique ids and exactly one highlighted card", () => {
    assert.equal(new Set(SERVICES.map((service) => service.id)).size, SERVICES.length);
    assert.equal(SERVICES.filter((service) => service.tone === "dark").length, 1);
  });
});
