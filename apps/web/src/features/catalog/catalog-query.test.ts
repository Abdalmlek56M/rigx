import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  activeFilters, catalogHref, pageWindow, parseCatalogParams, toApiQuery, toSearchParams, withPatch
} from "./catalog-query.ts";

const parse = (params: Record<string, string>, showSoldOut = true) => parseCatalogParams(params, { showSoldOut });

describe("parseCatalogParams", () => {
  it("defaults to available, newest, page 1", () => {
    const state = parse({});
    assert.deepEqual([state.stock, state.sort, state.page, state.condition, state.category], ["in", "newest", 1, null, null]);
  });
  it("reads every filter and ignores junk", () => {
    const state = parse({ category: "gpu", condition: "used", stock: "out", min: "100", max: "x", sort: "price_asc", page: "3", "spec.vram": "8GB" });
    assert.deepEqual(
      [state.category, state.condition, state.stock, state.min, state.max, state.sort, state.page, state.specs],
      ["gpu", "Used", "out", 100, null, "price_asc", 3, { vram: "8GB" }]
    );
    assert.equal(parse({ page: "-2", sort: "nope", stock: "weird" }).page, 1);
  });
  it("forces availability to in when sold-out items are hidden", () => {
    assert.equal(parse({ stock: "all" }, false).stock, "in");
  });
});

describe("URL and API mapping", () => {
  it("leaves defaults out of the URL and round-trips", () => {
    const state = parse({ category: "gpu", condition: "New", stock: "all", min: "500", sort: "price_desc", page: "2", "spec.vram": "8GB" });
    const query = toSearchParams(state).toString();
    assert.deepEqual(parse(Object.fromEntries(new URLSearchParams(query))), state);
    assert.equal(toSearchParams(parse({})).toString(), "");
  });
  it("goes back to page 1 on a change and drops specs when the category changes", () => {
    const state = parse({ category: "gpu", page: "4", "spec.vram": "8GB" });
    assert.equal(withPatch(state, { condition: "New" }).page, 1);
    assert.deepEqual(withPatch(state, { category: "cpu" }).specs, {});
    assert.equal(withPatch(state, { page: 5 }).page, 5);
    assert.equal(catalogHref(parse({}), {}), "/products");
    assert.equal(catalogHref(parse({}), { condition: "Used" }), "/products?condition=Used");
  });
  it("maps stock to inStock and adds spec params only with a category id", () => {
    const state = parse({ stock: "out", min: "10", "spec.vram": "8GB", category: "gpu" });
    const query = toApiQuery(state, "cat-1");
    assert.deepEqual([query.inStock, query.minPrice, query.categoryId, query["spec.vram"]], [false, 10, "cat-1", "8GB"]);
    assert.equal(toApiQuery(state, null)["spec.vram"], undefined);
    assert.equal(toApiQuery(parse({ stock: "all" }), null).inStock, undefined);
  });
});

describe("activeFilters", () => {
  it("lists removable chips, each clearing only itself", () => {
    const state = parse({ category: "gpu", condition: "Used", min: "100", "spec.vram": "8GB", "spec.bus": "pcie" });
    const chips = activeFilters(state);
    assert.deepEqual(chips.map((c) => c.id), ["category", "condition", "price", "spec.vram", "spec.bus"]);
    const vram = chips.find((c) => c.id === "spec.vram");
    assert.deepEqual(vram?.clear, { specs: { bus: "pcie" } });
    assert.deepEqual(activeFilters(parse({})), []);
  });
});

describe("pageWindow", () => {
  it("shows first, last and neighbours with gaps", () => {
    assert.deepEqual(pageWindow(1, 3), [1, 2, 3]);
    assert.deepEqual(pageWindow(5, 10), [1, "gap", 4, 5, 6, "gap", 10]);
    assert.deepEqual(pageWindow(2, 10), [1, 2, 3, "gap", 10]);
  });
});
