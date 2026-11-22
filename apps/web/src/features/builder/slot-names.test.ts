import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { CategoryNode } from "../../lib/categories.ts";
import { localizeSlots } from "./slot-names.ts";

const node = (id: string, name: string, nameEn: string | null): CategoryNode => ({ id, parentId: null, name, nameEn, slug: id, specs: [], children: [] });
const tree = [node("c1", "معالجات", "Processors"), node("c2", "بوردات", null)];
const slots = [
  { slot: "CPU", name: "معالجات", categoryIds: ["c1"] },
  { slot: "BOARD", name: "بوردات", categoryIds: ["c2"] },
  { slot: "FAN", name: "مراوح", categoryIds: ["c1"] },
  { slot: "RGB", name: "إضاءة", categoryIds: ["gone"] }
];

describe("localizeSlots", () => {
  it("names a known slot in the singular, whatever the category is called", () => {
    assert.deepEqual(localizeSlots(slots, tree, "ar").slice(0, 2).map((entry) => entry.name), ["معالج", "بوردة"]);
    assert.deepEqual(localizeSlots(slots, tree, "en").slice(0, 2).map((entry) => entry.name), ["Processor", "Motherboard"]);
  });

  it("names every slot of the blueprint", () => {
    const codes = ["CPU", "BOARD", "RAM", "GPU", "SSD", "PSU", "COOL", "CASE"].map((slot) => ({ slot, name: "x", categoryIds: [] }));
    assert.deepEqual(localizeSlots(codes, tree, "ar").map((entry) => entry.name), ["معالج", "بوردة", "رامات", "كرت شاشة", "تخزين", "باور سبلاي", "تبريد", "كيسة"]);
  });

  it("falls back to the category's name in the visitor's language for a slot it does not know", () => {
    assert.deepEqual(localizeSlots(slots, tree, "en").slice(2).map((entry) => entry.name), ["Processors", "إضاءة"]);
    assert.deepEqual(localizeSlots(slots, tree, "ar").slice(2).map((entry) => entry.name), ["معالجات", "إضاءة"]);
  });
});
