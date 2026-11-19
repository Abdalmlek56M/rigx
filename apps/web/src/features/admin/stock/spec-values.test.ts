import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { SpecDefinition } from "../categories/categories-api.ts";
import { foreignSpecs, setSpec, specLabel, specsForSchema } from "./spec-values.ts";

const spec = (key: string, patch: Partial<SpecDefinition> = {}): SpecDefinition => ({
  key, name: key, type: "Text", options: null, unit: null, isFilter: false, isCompatibility: false, ...patch
});

describe("spec values", () => {
  it("sets a value and clears it when emptied", () => {
    assert.deepEqual(setSpec({}, "vram", "8"), { vram: "8" });
    assert.deepEqual(setSpec({ vram: "8", bus: "128" }, "vram", " "), { bus: "128" });
  });

  it("keeps only the keys the category defines, or everything when it defines none", () => {
    const values = { vram: "8", socket: "AM5" };
    assert.deepEqual(specsForSchema(values, [spec("vram")]), { vram: "8" });
    assert.deepEqual(specsForSchema(values, []), values);
    assert.deepEqual(foreignSpecs(values, [spec("vram")]), [["socket", "AM5"]]);
    assert.deepEqual(foreignSpecs({ vram: "8" }, [spec("vram")]), []);
  });

  it("labels a spec with its unit", () => {
    assert.equal(specLabel(spec("vram", { name: "الذاكرة", unit: "GB" })), "الذاكرة (GB)");
    assert.equal(specLabel(spec("rgb", { name: "إضاءة" })), "إضاءة");
  });
});
