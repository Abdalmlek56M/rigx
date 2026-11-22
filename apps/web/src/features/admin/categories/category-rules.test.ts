import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Category, SpecDefinition } from "./categories-api.ts";
import {
  buildTree,
  categoryForm,
  categoryFormValid,
  effectiveSpecs,
  emptyCategoryForm,
  flattenTree,
  findPath,
  moveSpec,
  nextSortOrder,
  offeredPrices,
  parentIdOf,
  parentOptions,
  reorderChildren,
  siblingIds,
  specRowValid,
  specRows,
  toCategoryInput,
  toSpecDefinitions,
  withKeys
} from "./category-rules.ts";

const spec = (key: string, name = key): SpecDefinition => ({ key, name, type: "Text", options: null, unit: null, isFilter: false, isCompatibility: false });

function cat(id: string, children: Category[] = [], specs: SpecDefinition[] = [], parentId: string | null = null): Category {
  return {
    id, parentId, name: id, slug: id.toLowerCase(), description: null, iconStorageKey: null, sortOrder: 0,
    isActive: true, isEffectivelyActive: true, rowVersion: 1, code: null, showInMenu: true, showInBuilder: false, specs, children
  };
}

const leaf = cat("amd", [], [spec("socket", "Socket")], "processors");
const intel = cat("intel");
const processors = cat("processors", [intel, leaf], [spec("socket", "Socket"), spec("cores", "Cores")]);
const gpu = cat("gpu", [cat("nvidia")], [spec("memory", "Memory")]);
const tree = [processors, gpu];

describe("category tree", () => {
  it("nests the flat admin list by parent, keeping its order; an unknown parent becomes a root", () => {
    const flat = [cat("gpu"), cat("intel", [], [], "processors"), cat("orphan", [], [], "gone"), cat("amd", [], [], "processors"), cat("processors")];
    const nested = buildTree(flat);
    assert.deepEqual(nested.map((node) => node.id), ["gpu", "orphan", "processors"]);
    assert.deepEqual(nested[2]!.children.map((node) => node.id), ["intel", "amd"]);
  });

  it("flattens the visible rows and hides collapsed subtrees", () => {
    assert.deepEqual(flattenTree(tree, new Set()).map((row) => [row.node.id, row.depth]), [
      ["processors", 0], ["intel", 1], ["amd", 1], ["gpu", 0], ["nvidia", 1]
    ]);
    assert.deepEqual(flattenTree(tree, new Set(["processors"])).map((row) => row.node.id), ["processors", "gpu", "nvidia"]);
  });

  it("finds the path from the root", () => {
    assert.deepEqual(findPath(tree, "amd").map((node) => node.id), ["processors", "amd"]);
    assert.deepEqual(findPath(tree, "missing"), []);
  });

  it("offers every category as a parent except the node and its subtree", () => {
    assert.deepEqual(parentOptions(tree, "processors").map((option) => option.id), ["gpu", "nvidia"]);
    assert.deepEqual(parentOptions(tree, null).map((option) => option.id), ["processors", "intel", "amd", "gpu", "nvidia"]);
  });
});

describe("offered prices", () => {
  const board = { ...cat("board"), priceOptions: { alone: false, withBoard: true, inBuild: true } };
  const offered = [{ ...cat("parts"), children: [board] }];

  it("reads the switches of the item's own category, however deep", () => {
    assert.deepEqual(offeredPrices(offered, "board"), { alone: false, withBoard: true, inBuild: true });
  });

  it("follows the parent: a price the parent switched off is off below it, and a child can only switch off more", () => {
    const child = { ...cat("amd"), priceOptions: { alone: true, withBoard: false, inBuild: true } };
    const grandchild = cat("am5");
    const parent = { ...cat("cpus"), priceOptions: { alone: false, withBoard: true, inBuild: true }, children: [{ ...child, children: [grandchild] }] };
    assert.deepEqual(offeredPrices([parent], "cpus"), { alone: false, withBoard: true, inBuild: true });
    assert.deepEqual(offeredPrices([parent], "amd"), { alone: false, withBoard: false, inBuild: true });
    assert.deepEqual(offeredPrices([parent], "am5"), { alone: false, withBoard: false, inBuild: true });
  });

  it("uses the child's own switches when the parent's and the child's leave nothing", () => {
    const child = { ...cat("amd"), priceOptions: { alone: false, withBoard: true, inBuild: false } };
    const parent = { ...cat("cpus"), priceOptions: { alone: false, withBoard: false, inBuild: true }, children: [child] };
    assert.deepEqual(offeredPrices([parent], "amd"), { alone: false, withBoard: true, inBuild: false });
  });

  it("offers everything while no category is chosen or the category is unknown", () => {
    const all = { alone: true, withBoard: true, inBuild: true };
    assert.deepEqual(offeredPrices(offered, null), all);
    assert.deepEqual(offeredPrices(offered, "missing"), all);
    assert.deepEqual(offeredPrices(offered, "parts"), all);
  });
});

describe("effective specs", () => {
  it("inherits the parent specs and lets a child override one in place", () => {
    const rows = effectiveSpecs(tree, "amd");
    assert.deepEqual(rows.map((row) => [row.spec.key, row.inheritedFrom?.id ?? null]), [
      ["socket", null],
      ["cores", "processors"]
    ]);
    assert.equal(rows[0]?.spec.name, "Socket");
  });

  it("has no rows for a missing category", () => {
    assert.deepEqual(effectiveSpecs(tree, "missing"), []);
  });
});

describe("category form", () => {
  it("starts empty with the defaults and maps the API body", () => {
    const form = { ...emptyCategoryForm(null), name: " Cards ", slug: " gpu ", sortOrder: "4", code: " GPU ", description: " " };
    assert.deepEqual(toCategoryInput(form, null), {
      name: "Cards", nameEn: null, slug: "gpu", parentId: null, description: null, iconStorageKey: null, sortOrder: 4,
      isActive: true, rowVersion: null, code: "GPU", showInMenu: true, showInBuilder: false,
      priceOptions: { alone: true, withBoard: true, inBuild: true }
    });
  });

  it("keeps the image and the row version when editing", () => {
    const existing = { ...cat("gpu"), iconStorageKey: "categories/1/a.webp", rowVersion: 7, sortOrder: 3 };
    const form = categoryForm(existing);
    assert.deepEqual([form.sortOrder, form.showInMenu, form.showInBuilder], ["3", true, false]);
    const input = toCategoryInput(form, existing);
    assert.deepEqual([input.iconStorageKey, input.rowVersion, input.sortOrder], ["categories/1/a.webp", 7, 3]);
  });

  it("carries the sale price switches and refuses a form with all three off", () => {
    const existing = { ...cat("gpu"), priceOptions: { alone: true, withBoard: false, inBuild: true } };
    const form = categoryForm(existing);
    assert.deepEqual([form.priceAlone, form.priceWithBoard, form.priceInBuild], [true, false, true]);
    assert.deepEqual(toCategoryInput(form, existing).priceOptions, { alone: true, withBoard: false, inBuild: true });
    assert.ok(categoryFormValid(form));
    assert.ok(!categoryFormValid({ ...form, priceAlone: false, priceInBuild: false }));
  });

  it("sends the English name trimmed, or null when it is empty", () => {
    const existing = { ...cat("gpu"), nameEn: "Graphics Cards" };
    assert.equal(categoryForm(existing).nameEn, "Graphics Cards");
    assert.equal(toCategoryInput({ ...categoryForm(existing), nameEn: "  GPUs " }, existing).nameEn, "GPUs");
    assert.equal(toCategoryInput({ ...categoryForm(existing), nameEn: "  " }, existing).nameEn, null);
  });
});

describe("spec rows", () => {
  it("maps specs to rows and back, splitting choice options only", () => {
    const rows = specRows([
      { key: "socket", name: "Socket", type: "Choice", options: ["AM4", "AM5"], unit: null, isFilter: true, isCompatibility: true },
      { key: "w", name: "W", type: "Number", options: null, unit: "W", isFilter: false, isCompatibility: false }
    ]);
    assert.deepEqual(rows.map((row) => [row.key, row.options, row.unit, row.isFilter]), [
      ["socket", "AM4, AM5", "", true],
      ["w", "", "W", false]
    ]);
    assert.deepEqual(toSpecDefinitions(rows), [
      { key: "socket", name: "Socket", type: "Choice", options: ["AM4", "AM5"], unit: null, isFilter: true, isCompatibility: true },
      { key: "w", name: "W", type: "Number", options: null, unit: "W", isFilter: false, isCompatibility: false }
    ]);
  });

  it("accepts a valid key and name, and needs options for a choice", () => {
    const base = { id: "row", key: "socket", name: "Socket", type: "Text", options: "", unit: "", isFilter: false, isCompatibility: false };
    assert.equal(specRowValid(base), true);
    assert.equal(specRowValid({ ...base, key: "Socket" }), true);
    assert.equal(specRowValid({ ...base, key: "1socket" }), false);
    assert.equal(specRowValid({ ...base, name: " " }), false);
    assert.equal(specRowValid({ ...base, type: "Choice" }), false);
    assert.equal(specRowValid({ ...base, type: "Choice", options: "AM4" }), true);
  });
});

describe("spec rows for the owner", () => {
  const row = (id: string, key: string, name: string) => ({ id, key, name, type: "Text", options: "", unit: "", isFilter: false, isCompatibility: false });

  it("generates a key for a row that has none and never reuses one", () => {
    const rows = withKeys([row("a", "field_1", "الأولى"), row("b", "", "الثانية"), row("c", "", "الثالثة")]);
    assert.deepEqual(rows.map((entry) => entry.key), ["field_1", "field_2", "field_3"]);
    assert.deepEqual(toSpecDefinitions([row("d", "", "المقاس")]).map((entry) => entry.key), ["field_1"]);
  });

  it("is valid without a key, but a written key must still be well formed", () => {
    assert.equal(specRowValid(row("a", "", "المقاس")), true);
    assert.equal(specRowValid(row("a", "Bad Key", "المقاس")), false);
  });

  it("moves a row up or down and stops at the ends", () => {
    const rows = [row("a", "a", "A"), row("b", "b", "B"), row("c", "c", "C")];
    assert.deepEqual(moveSpec(rows, "b", -1).map((entry) => entry.id), ["b", "a", "c"]);
    assert.deepEqual(moveSpec(rows, "b", 1).map((entry) => entry.id), ["a", "c", "b"]);
    assert.deepEqual(moveSpec(rows, "a", -1).map((entry) => entry.id), ["a", "b", "c"]);
    assert.deepEqual(moveSpec(rows, "c", 1).map((entry) => entry.id), ["a", "b", "c"]);
  });
});

describe("ordering categories by hand", () => {
  const ordered = [
    { ...cat("a"), sortOrder: 0 },
    { ...cat("b", [{ ...cat("b1", [], [], "b"), sortOrder: 0 }, { ...cat("b2", [], [], "b"), sortOrder: 4 }]), sortOrder: 5 }
  ];

  it("finds the parent of a category: null at the top, undefined when it is not there", () => {
    assert.equal(parentIdOf(ordered, "a"), null);
    assert.equal(parentIdOf(ordered, "b2"), "b");
    assert.equal(parentIdOf(ordered, "zzz"), undefined);
  });

  it("lists the ids under a parent in the tree's order", () => {
    assert.deepEqual(siblingIds(ordered, null), ["a", "b"]);
    assert.deepEqual(siblingIds(ordered, "b"), ["b1", "b2"]);
    assert.deepEqual(siblingIds(ordered, "a"), []);
    assert.deepEqual(siblingIds(ordered, "zzz"), []);
  });

  it("shows the children of one parent in a new order without touching the rest", () => {
    const top = reorderChildren(ordered, null, ["b", "a"]);
    assert.deepEqual(top.map((node) => node.id), ["b", "a"]);
    assert.deepEqual(top[0]!.children.map((node) => node.id), ["b1", "b2"]);
    const inner = reorderChildren(ordered, "b", ["b2", "b1"]);
    assert.deepEqual(inner.map((node) => node.id), ["a", "b"]);
    assert.deepEqual(inner[1]!.children.map((node) => node.id), ["b2", "b1"]);
  });

  it("puts a new category after the last one under its parent", () => {
    assert.equal(nextSortOrder(ordered, null), 6);
    assert.equal(nextSortOrder(ordered, "b"), 5);
    assert.equal(nextSortOrder(ordered, "a"), 0);
    assert.equal(nextSortOrder([], null), 0);
    assert.equal(emptyCategoryForm("b", 5).sortOrder, "5");
  });
});
