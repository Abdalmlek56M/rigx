import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { buildsHref, isBudget, parseBuildsParams, toApiQuery } from "./build-query.ts";

describe("parseBuildsParams", () => {
  it("reads usage and budget and ignores unknown values", () => {
    assert.deepEqual(parseBuildsParams({ usage: "Gaming", budget: "mid" }), { usage: "Gaming", budget: "mid" });
    assert.deepEqual(parseBuildsParams({ usage: "nope", budget: "huge" }), { usage: null, budget: null });
    assert.deepEqual(parseBuildsParams({}), { usage: null, budget: null });
  });
  it("takes the first of repeated params", () => {
    assert.deepEqual(parseBuildsParams({ usage: ["Editing", "Gaming"], budget: ["low", "high"] }), { usage: "Editing", budget: "low" });
  });
});

describe("isBudget", () => {
  it("accepts only the known budgets", () => {
    assert.equal(isBudget("high"), true);
    assert.equal(isBudget("medium"), false);
  });
});

describe("toApiQuery", () => {
  it("leaves absent bounds out", () => {
    assert.deepEqual(toApiQuery({ usage: null, budget: null }), {});
    assert.deepEqual(toApiQuery({ usage: "Gaming", budget: "low" }), { usage: "Gaming", maxPrice: 20000 });
    assert.deepEqual(toApiQuery({ usage: null, budget: "high" }), { minPrice: 35000 });
  });
  it("gives both bounds for the middle budget", () => {
    assert.deepEqual(toApiQuery({ usage: "Streaming", budget: "mid" }), { usage: "Streaming", minPrice: 20000, maxPrice: 35000 });
  });
});

describe("buildsHref", () => {
  it("leaves defaults out and round-trips through the parser", () => {
    const empty = { usage: null, budget: null };
    assert.equal(buildsHref(empty), "/builds");
    const href = buildsHref(empty, { usage: "StudyWork", budget: "mid" });
    assert.equal(href, "/builds?usage=StudyWork&budget=mid");
    const params = Object.fromEntries(new URLSearchParams(href.split("?")[1]));
    assert.deepEqual(parseBuildsParams(params), { usage: "StudyWork", budget: "mid" });
  });
  it("clears a filter when patched to null", () => {
    assert.equal(buildsHref({ usage: "Gaming", budget: "low" }, { budget: null }), "/builds?usage=Gaming");
  });
});
