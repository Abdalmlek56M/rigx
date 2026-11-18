import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Category, SpecDefinition } from "./categories-api.ts";
import {
  categoryForm,
  effectiveSpecs,
  emptyCategoryForm,
  flattenTree,
  findPath,
  parentOptions,
  specRowValid,
  specRows,
  toCategoryInput,
  toSpecDefinitions
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
      name: "Cards", slug: "gpu", parentId: null, description: null, iconStorageKey: null, sortOrder: 4,
      isActive: true, rowVersion: null, code: "GPU", showInMenu: true, showInBuilder: false
    });
  });

  it("keeps the image and the row version when editing", () => {
    const existing = { ...cat("gpu"), iconStorageKey: "categories/1/a.webp", rowVersion: 7, sortOrder: 3 };
    const form = categoryForm(existing);
    assert.deepEqual([form.sortOrder, form.showInMenu, form.showInBuilder], ["3", true, false]);
    const input = toCategoryInput(form, existing);
    assert.deepEqual([input.iconStorageKey, input.rowVersion, input.sortOrder], ["categories/1/a.webp", 7, 3]);
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
