import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { filterComboOptions } from "./combo-rules.ts";

const options = [
  { value: "1", label: "ASUS ROG Strix B650E-F" },
  { value: "2", label: "MSI PRO B660M-A" },
  { value: "3", label: "كروت الشاشة › NVIDIA" },
  { value: "4", label: "Kingston Fury 32GB" }
];

describe("combo box filtering", () => {
  it("returns everything for an empty query", () => {
    assert.deepEqual(filterComboOptions(options, "  ").map((option) => option.value), ["1", "2", "3", "4"]);
  });

  it("matches anywhere and ignores English case", () => {
    assert.deepEqual(filterComboOptions(options, "rog").map((option) => option.value), ["1"]);
    assert.deepEqual(filterComboOptions(options, "B660").map((option) => option.value), ["2"]);
    assert.deepEqual(filterComboOptions(options, "32gb").map((option) => option.value), ["4"]);
  });

  it("matches Arabic text anywhere", () => {
    assert.deepEqual(filterComboOptions(options, "الشاشة").map((option) => option.value), ["3"]);
    assert.deepEqual(filterComboOptions(options, "nvidia").map((option) => option.value), ["3"]);
  });

  it("returns no options when nothing matches", () => {
    assert.deepEqual(filterComboOptions(options, "zzz"), []);
  });
});
