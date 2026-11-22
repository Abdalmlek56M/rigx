import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { CategoryNode } from "../../lib/categories.ts";
import { localizeSlots } from "./slot-names.ts";

const node = (id: string, name: string, nameEn: string | null): CategoryNode => ({ id, parentId: null, name, nameEn, slug: id, specs: [], children: [] });
const tree = [node("c1", "معالجات", "Processors"), node("c2", "بوردات", null)];
const slots = [
  { slot: "CPU", name: "معالجات", categoryIds: ["c1"] },
  { slot: "BOARD", name: "بوردات", categoryIds: ["c2"] },
  { slot: "RAM", name: "رامات", categoryIds: ["gone"] }
];

describe("localizeSlots", () => {
  it("uses the English category name in English and the Arabic one in Arabic", () => {
    assert.deepEqual(localizeSlots(slots, tree, "en").map((entry) => entry.name), ["Processors", "بوردات", "رامات"]);
    assert.deepEqual(localizeSlots(slots, tree, "ar").map((entry) => entry.name), ["معالجات", "بوردات", "رامات"]);
  });
});
