import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { categoryBySlug, categoryPath, effectiveSpecs, type CategoryNode, type SpecDef } from "./categories.ts";

const spec = (key: string, name = key): SpecDef => ({ key, name, options: [], unit: null, isFilter: false });
const node = (id: string, parentId: string | null, specs: SpecDef[], children: CategoryNode[] = []): CategoryNode => ({
  id, parentId, name: id, slug: id.toLowerCase(), specs, children
});

const tree = [node("CPU", null, [spec("socket"), spec("cores")], [node("AMD", "CPU", [spec("cores", "Core count")], [node("R5", "AMD", [spec("series")])])])];

describe("categories", () => {
  it("finds by slug and builds the path from the root", () => {
    assert.equal(categoryBySlug(tree, "r5")?.id, "R5");
    assert.deepEqual(categoryPath(tree, "R5").map((c) => c.id), ["CPU", "AMD", "R5"]);
    assert.deepEqual(categoryPath(tree, "nope"), []);
  });
  it("inherits specs, an own spec replacing the inherited one", () => {
    const specs = effectiveSpecs(tree, "R5");
    assert.deepEqual(specs.map((s) => s.key), ["socket", "cores", "series"]);
    assert.equal(specs.find((s) => s.key === "cores")?.name, "Core count");
  });
});
