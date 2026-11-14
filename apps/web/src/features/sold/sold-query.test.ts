import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { SOLD_PAGE_SIZE, governorateLabel, parseSoldParams, soldHref, soldPageCount } from "./sold-query.ts";

describe("parseSoldParams", () => {
  it("defaults to everything on page 1", () => {
    assert.deepEqual(parseSoldParams({}), { kind: null, category: null, page: 1 });
  });
  it("reads kind, category and page", () => {
    assert.deepEqual(parseSoldParams({ kind: "builds", page: "3" }), { kind: "Build", category: null, page: 3 });
    assert.deepEqual(parseSoldParams({ category: " gpu " }), { kind: null, category: "gpu", page: 1 });
  });
  it("lets a category win over kind", () => {
    assert.deepEqual(parseSoldParams({ kind: "builds", category: "gpu" }), { kind: null, category: "gpu", page: 1 });
  });
  it("ignores junk kinds and bad pages", () => {
    assert.equal(parseSoldParams({ kind: "items" }).kind, null);
    for (const page of ["0", "-4", "abc", "", "1"]) assert.equal(parseSoldParams({ page }).page, 1);
    assert.equal(parseSoldParams({ page: "2.9" }).page, 2);
    assert.equal(parseSoldParams({ category: "  " }).category, null);
  });
  it("takes the first of repeated params", () => {
    assert.equal(parseSoldParams({ page: ["4", "5"] }).page, 4);
  });
});

describe("soldHref", () => {
  const empty = parseSoldParams({});
  it("leaves defaults out and round-trips", () => {
    assert.equal(soldHref(empty), "/sold");
    assert.equal(soldHref(empty, { kind: "Build", page: 2 }), "/sold?kind=builds&page=2");
    const state = { kind: null, category: "cpu", page: 3 };
    const query = soldHref(state).split("?")[1] ?? "";
    assert.deepEqual(parseSoldParams(Object.fromEntries(new URLSearchParams(query))), state);
  });
});

describe("soldPageCount", () => {
  it("is at least 1 and rounds up", () => {
    assert.equal(soldPageCount(0), 1);
    assert.equal(soldPageCount(SOLD_PAGE_SIZE), 1);
    assert.equal(soldPageCount(SOLD_PAGE_SIZE + 1), 2);
  });
});

describe("governorateLabel", () => {
  const names = new Map([["CAI", "القاهرة"]]);
  it("names a known code, case-insensitively, else keeps the text", () => {
    assert.equal(governorateLabel(names, " cai "), "القاهرة");
    assert.equal(governorateLabel(names, "Atlantis"), "Atlantis");
  });
  it("is null for empty values", () => {
    assert.equal(governorateLabel(names, null), null);
    assert.equal(governorateLabel(names, "   "), null);
  });
});
