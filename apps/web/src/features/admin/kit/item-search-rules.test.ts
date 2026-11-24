import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Schemas } from "@rigx/api-client";
import { hitForCode, searchable, toItemHit } from "./item-search-rules.ts";

const admin = { code: "RX-0154", modelName: "RTX 4060", photos: [{ storageKey: "a.webp", thumbnailStorageKey: "a-t.webp" }] } as unknown as Schemas["StockItemAdminView"];
const noPhoto = { code: "RX-0155", modelName: "Ryzen 5", photos: [] } as unknown as Schemas["StockItemAdminView"];
const store = { code: "RX-0200", modelName: "SSD", thumbnailStorageKey: "s-t.webp" } as unknown as Schemas["StoreItemCard"];

describe("item search hits", () => {
  it("reads a hit from the admin list or the store list", () => {
    assert.deepEqual(toItemHit(admin), { code: "RX-0154", name: "RTX 4060", thumbnailStorageKey: "a-t.webp" });
    assert.deepEqual(toItemHit(noPhoto), { code: "RX-0155", name: "Ryzen 5", thumbnailStorageKey: null });
    assert.deepEqual(toItemHit(store), { code: "RX-0200", name: "SSD", thumbnailStorageKey: "s-t.webp" });
  });

  it("searches from two characters", () => {
    assert.equal(searchable("r"), false);
    assert.equal(searchable(" r "), false);
    assert.equal(searchable("rx"), true);
  });

  it("finds the item behind a stored code in any case", () => {
    const hits = [toItemHit(admin), toItemHit(noPhoto)];
    assert.equal(hitForCode(hits, "rx-0155")?.name, "Ryzen 5");
    assert.equal(hitForCode(hits, " RX-0154 ")?.name, "RTX 4060");
    assert.equal(hitForCode(hits, "RX-9999"), null);
  });
});
