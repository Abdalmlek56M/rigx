import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  catalogCanonicalPath,
  activeFilters, catalogHref, pageWindow, parseCatalogParams, toApiQuery, toSearchParams, withPatch
} from "./catalog-query.ts";

const parse = (params: Record<string, string>) => parseCatalogParams(params);

describe("parseCatalogParams", () => {
  it("defaults to everything (sold-out included), newest, page 1", () => {
    const state = parse({});
    assert.deepEqual([state.stock, state.sort, state.page, state.condition, state.category], ["all", "newest", 1, null, null]);
  });
  it("reads every filter and ignores junk", () => {
    const state = parse({ category: "gpu", condition: "used", stock: "in", min: "100", max: "x", sort: "price_asc", page: "3", "spec.vram": "8GB" });
    assert.deepEqual(
      [state.category, state.condition, state.stock, state.min, state.max, state.sort, state.page, state.specs],
      ["gpu", "Used", "in", 100, null, "price_asc", 3, { vram: "8GB" }]
    );
    const junk = parse({ page: "-2", sort: "nope", stock: "out" });
    assert.deepEqual([junk.page, junk.sort, junk.stock], [1, "newest", "all"]);
  });
});

describe("the grid / list view", () => {
  it("is a grid unless the URL says list, and only the list is written back", () => {
    assert.equal(parse({}).view, "grid");
    assert.equal(parse({ view: "list" }).view, "list");
    assert.equal(parse({ view: "table" }).view, "grid");
    assert.equal(toSearchParams(parse({ view: "list" })).toString(), "view=list");
    assert.equal(toSearchParams(parse({ view: "grid" })).toString(), "");
  });
  it("keeps the page when only the view changes, and survives other changes", () => {
    const state = parse({ category: "gpu", page: "3" });
    assert.equal(withPatch(state, { view: "list" }).page, 3);
    assert.equal(withPatch(parse({ view: "list", page: "3" }), { condition: "New" }).view, "list");
    assert.equal(catalogHref(parse({ view: "list" }), { sort: "price_asc" }), "/products?sort=price_asc&view=list");
  });
  it("is not part of the canonical address, a filter chip or the API query", () => {
    const state = parse({ category: "gpu", view: "list" });
    assert.equal(catalogCanonicalPath(state), "/products?category=gpu");
    assert.equal(activeFilters(parse({ view: "list" })).length, 0);
    assert.equal("view" in toApiQuery(state, { categoryId: null, brandId: null }), false);
  });
});

describe("URL and API mapping", () => {
  it("leaves defaults out of the URL and round-trips", () => {
    const state = parse({ category: "gpu", brand: "msi", condition: "New", stock: "in", min: "500", sort: "price_desc", page: "2", "spec.vram": "8GB" });
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
    const state = parse({ stock: "in", min: "10", "spec.vram": "8GB", category: "gpu" });
    const none = { categoryId: null, brandId: null };
    const query = toApiQuery(state, { categoryId: "cat-1", brandId: null });
    assert.deepEqual([query.inStock, query.minPrice, query.categoryId, query["spec.vram"]], [true, 10, "cat-1", "8GB"]);
    assert.equal(toApiQuery(state, none)["spec.vram"], undefined);
    assert.equal(toApiQuery(parse({}), none).inStock, undefined);
  });
  it("reads the brand slug in any case and sends the brand id", () => {
    const state = parse({ brand: " MSI " });
    assert.equal(state.brand, "msi");
    assert.equal(toApiQuery(state, { categoryId: null, brandId: "b-1" }).brandId, "b-1");
    assert.equal(toApiQuery(state, { categoryId: null, brandId: null }).brandId, undefined);
    assert.equal(catalogHref(parse({}), { brand: "asus" }), "/products?brand=asus");
  });
  it("asks only for available items when the owner hides sold-out ones", () => {
    assert.equal(toApiQuery(parse({}), { categoryId: null, brandId: null }, false).inStock, true);
  });
});

describe("activeFilters", () => {
  it("lists removable chips, each clearing only itself", () => {
    const state = parse({ category: "gpu", brand: "msi", condition: "Used", min: "100", "spec.vram": "8GB", "spec.bus": "pcie" });
    const chips = activeFilters(state);
    assert.deepEqual(chips.map((c) => c.id), ["category", "brand", "condition", "price", "spec.vram", "spec.bus"]);
    assert.deepEqual(chips.find((c) => c.id === "brand")?.clear, { brand: null });
    const vram = chips.find((c) => c.id === "spec.vram");
    assert.deepEqual(vram?.clear, { specs: { bus: "pcie" } });
    assert.deepEqual(activeFilters(parse({})), []);
    assert.deepEqual(activeFilters(parse({ stock: "in" }))[0]?.clear, { stock: "all" });
  });
});

describe("pageWindow", () => {
  it("shows first, last and neighbours with gaps", () => {
    assert.deepEqual(pageWindow(1, 3), [1, 2, 3]);
    assert.deepEqual(pageWindow(5, 10), [1, "gap", 4, 5, 6, "gap", 10]);
    assert.deepEqual(pageWindow(2, 10), [1, 2, 3, "gap", 10]);
  });
});

describe("catalogCanonicalPath", () => {
  it("keeps only the category and the page", () => {
    const state = parseCatalogParams({ category: "gpu", condition: "Used", sort: "price_asc", search: "3060", page: "2", "spec.vram": "12GB" });
    assert.equal(catalogCanonicalPath(state), "/products?category=gpu&page=2");
    assert.equal(catalogCanonicalPath(parseCatalogParams({ stock: "in" })), "/products");
  });
});
