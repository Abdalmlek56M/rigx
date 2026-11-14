import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { CategoryNode } from "../../lib/categories.ts";
import { describeIssue, specNameMap } from "./issue-text.ts";

type Issue = Parameters<typeof describeIssue>[0];

const issue = (patch: Partial<Issue>): Issue => ({ level: "Conflict", code: "SpecMismatch", slot: "CPU", otherSlot: "BOARD", specKey: "socket", ...patch });

describe("describeIssue", () => {
  const slots = { CPU: "المعالج", BOARD: "اللوحة" };
  const specs = { socket: "السوكت" };

  it("names the slots and spec and flags conflicts", () => {
    assert.deepEqual(describeIssue(issue({}), slots, specs), {
      key: "specMismatch",
      values: { slot: "المعالج", other: "اللوحة", spec: "السوكت" },
      conflict: true
    });
  });
  it("maps every known code", () => {
    const keys = ["SpecMismatch", "OverLimit", "PowerOverCapacity", "PowerNearCapacity"].map((code) => describeIssue(issue({ code }), slots, specs).key);
    assert.deepEqual(keys, ["specMismatch", "overLimit", "powerOver", "powerNear"]);
  });
  it("uses generic for unknown codes and warnings are not conflicts", () => {
    const text = describeIssue(issue({ code: "Whatever", level: "Warning" }), slots, specs);
    assert.deepEqual([text.key, text.conflict], ["generic", false]);
  });
  it("falls back to raw keys and empty strings for missing parts", () => {
    assert.deepEqual(describeIssue(issue({ slot: "RAM", otherSlot: "ZZZ", specKey: "speed" }), slots, specs).values, { slot: "RAM", other: "ZZZ", spec: "speed" });
    assert.deepEqual(describeIssue(issue({ otherSlot: null, specKey: null }), slots, specs).values, { slot: "المعالج", other: "", spec: "" });
  });
});

describe("specNameMap", () => {
  const node = (specs: [string, string][], children: CategoryNode[] = []): CategoryNode => ({
    id: "1", parentId: null, name: "n", slug: "n", children,
    specs: specs.map(([key, name]) => ({ key, name, options: [], unit: null, isFilter: false }))
  });
  it("collects specs through the tree and keeps the first definition", () => {
    const tree = [node([["socket", "السوكت"]], [node([["socket", "Other"], ["vram", "الذاكرة"]])]), node([["vram", "Later"]])];
    assert.deepEqual(specNameMap(tree), { socket: "السوكت", vram: "الذاكرة" });
  });
  it("is empty for an empty tree", () => {
    assert.deepEqual(specNameMap([]), {});
  });
});
