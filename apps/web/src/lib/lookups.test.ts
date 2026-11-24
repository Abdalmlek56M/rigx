import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { findCategory, categoryById, type CategoryNode } from "./categories.ts";
import { formatEgp } from "./format.ts";
import { parseCondition } from "./condition.ts";
import { parseUsage, USAGES } from "./usage.ts";

const node = (id: string, parentId: string | null, children: CategoryNode[] = []): CategoryNode => ({
  id, parentId, name: id, slug: id.toLowerCase(), specs: [], children
});

const tree = [node("CPU", null, [node("AMD", "CPU", [node("R5", "AMD")]), node("INTEL", "CPU")]), node("GPU", null)];

describe("condition from text", () => {
  it("reads new and used in any letter case and with spaces", () => {
    assert.equal(parseCondition("New"), "New");
    assert.equal(parseCondition("used"), "Used");
    assert.equal(parseCondition("  NEW "), "New");
  });

  it("is null for anything else (the URL or API sent something unknown)", () => {
    assert.equal(parseCondition("refurbished"), null);
    assert.equal(parseCondition(""), null);
    assert.equal(parseCondition(null), null);
    assert.equal(parseCondition(undefined), null);
  });
});

describe("build usage from text", () => {
  it("reads every known usage exactly", () => {
    for (const usage of USAGES) assert.equal(parseUsage(usage), usage);
  });

  it("is null for an unknown usage, a different case or nothing", () => {
    assert.equal(parseUsage("gaming"), null);
    assert.equal(parseUsage("Office"), null);
    assert.equal(parseUsage(null), null);
    assert.equal(parseUsage(undefined), null);
  });
});

describe("category search through the whole tree", () => {
  it("finds a node at any depth, parents before children", () => {
    assert.equal(findCategory(tree, (item) => item.id === "R5")?.parentId, "AMD");
    assert.equal(findCategory(tree, (item) => item.id === "GPU")?.parentId, null);
  });

  it("returns the first match in tree order", () => {
    assert.equal(findCategory(tree, (item) => item.parentId === "CPU")?.id, "AMD");
  });

  it("finds by id and returns nothing for an unknown id", () => {
    assert.equal(categoryById(tree, "INTEL")?.slug, "intel");
    assert.equal(categoryById(tree, "NOPE"), undefined);
    assert.equal(categoryById([], "CPU"), undefined);
  });
});

describe("price text", () => {
  it("writes Western digits with thousands separators and the Arabic currency label", () => {
    assert.equal(formatEgp(8500, "ar"), "8,500 ج.م");
    assert.equal(formatEgp(1234567, "ar"), "1,234,567 ج.م");
  });

  it("puts the English label first in English", () => {
    assert.equal(formatEgp(8500, "en"), "EGP 8,500");
  });

  it("keeps at most two decimals and drops a zero fraction", () => {
    assert.equal(formatEgp(99.5, "en"), "EGP 99.5");
    assert.equal(formatEgp(99.999, "en"), "EGP 100");
    assert.equal(formatEgp(0, "ar"), "0 ج.م");
  });
});
