import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { cacheTagsFor, CATALOG_TAG } from "./cache-tags.ts";

describe("cache tags", () => {
  it("tags the store's item, build and category responses", () => {
    for (const path of ["/api/v1/store/items?search=x", "/api/v1/store/items/rx-0001", "/api/v1/catalog/categories", "/api/v1/catalog/brands", "/api/v1/store/assemblies"]) {
      assert.deepEqual(cacheTagsFor(`http://127.0.0.1:8099${path}`), [CATALOG_TAG], path);
    }
  });

  it("leaves everything else untagged", () => {
    for (const path of ["/api/v1/storefront/settings", "/api/v1/admin/retail/stock-items", "/api/v1/identity/me"]) {
      assert.deepEqual(cacheTagsFor(`http://127.0.0.1:8099${path}`), [], path);
    }
  });
});
