import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { boardSize, DEFAULT_LAYOUT, pcLayout } from "./pc-layout.ts";

const part = (slot: string, specs: Record<string, string> = {}, quantity = 1) => ({ slot, quantity, item: { specs } });

describe("pcLayout", () => {
  it("gives the most common build with no parts", () => {
    assert.deepEqual(pcLayout([]), DEFAULT_LAYOUT);
  });

  it("reads the board size from its form factor", () => {
    const board = (form_factor: string) => pcLayout([part("BOARD", { form_factor })]).board;
    assert.equal(board("ATX"), "atx");
    assert.equal(board("Micro-ATX"), "matx");
    assert.equal(board("mATX"), "matx");
    assert.equal(board("Mini-ITX"), "itx");
    assert.equal(boardSize(null), "atx");
  });

  it("reads the memory slots, with two as the mini-ITX default", () => {
    const slots = (specs: Record<string, string>) => pcLayout([part("BOARD", specs)]).ramSlots;
    assert.equal(slots({ memory_slots: "2" }), 2);
    assert.equal(slots({ memory_slots: "4" }), 4);
    assert.equal(slots({ form_factor: "Mini-ITX" }), 2);
    assert.equal(slots({ form_factor: "Mini-ITX", memory_slots: "4" }), 4);
    assert.equal(slots({ form_factor: "ATX" }), 4);
  });

  it("counts sticks as quantity times modules, capped at the slots", () => {
    const board = part("BOARD", { memory_slots: "4" });
    assert.equal(pcLayout([board, part("RAM", { modules: "2" })]).sticks, 2);
    assert.equal(pcLayout([board, part("RAM", { modules: "2" }, 2)]).sticks, 4);
    assert.equal(pcLayout([board, part("RAM", {}, 3)]).sticks, 3);
    assert.equal(pcLayout([board, part("RAM", { modules: "4" }, 2)]).sticks, 4);
    assert.equal(pcLayout([part("BOARD", { memory_slots: "2" }), part("RAM", { modules: "2" }, 2)]).sticks, 2);
  });

  it("gives the card two fans by default and keeps its fans between one and three", () => {
    const fans = (value?: string) => pcLayout([part("GPU", value === undefined ? {} : { fans: value })]).gpuFans;
    assert.equal(fans(), 2);
    assert.equal(fans("3"), 3);
    assert.equal(fans("1"), 1);
    assert.equal(fans("0"), 1);
    assert.equal(fans("7"), 3);
  });

  it("reads the case's own fans and keeps them to six", () => {
    const fans = (specs: Record<string, string>) => pcLayout([part("CASE", specs)]).caseFans;
    assert.equal(fans({}), 0);
    assert.equal(fans({ fans_included: "3" }), 3);
    assert.equal(fans({ fans_included: "9" }), 6);
  });

  it("reads Wi-Fi from yes or نعم", () => {
    const wifi = (value: string) => pcLayout([part("BOARD", { wifi: value })]).wifi;
    assert.equal(wifi("yes"), true);
    assert.equal(wifi("نعم"), true);
    assert.equal(wifi("no"), false);
    assert.equal(wifi(""), false);
    assert.equal(pcLayout([part("BOARD", {})]).wifi, false);
  });

  it("reads a liquid cooler from its type or from a radiator", () => {
    const cooler = (specs: Record<string, string>) => pcLayout([part("COOL", specs)]).cooler;
    assert.equal(cooler({ cooler_type: "مية" }), "liquid");
    assert.equal(cooler({ Type: "Liquid" }), "liquid");
    assert.equal(cooler({ radiator: "240mm" }), "liquid");
    assert.equal(cooler({ cooler_type: "Air" }), "air");
    assert.equal(cooler({}), "air");
  });

  it("turns the radiator size into one, two or three fans", () => {
    const fans = (radiator?: string) => pcLayout([part("COOL", radiator === undefined ? { type: "Liquid" } : { radiator })]).radiatorFans;
    assert.equal(fans("120"), 1);
    assert.equal(fans("240mm"), 2);
    assert.equal(fans("360"), 3);
    assert.equal(fans(), 3);
  });

  it("matches spec keys ignoring case and spaces", () => {
    const layout = pcLayout([part("BOARD", { "Memory Slots": "2", "Form Factor": "Micro-ATX", WiFi: "Yes" })]);
    assert.deepEqual([layout.ramSlots, layout.board, layout.wifi], [2, "matx", true]);
    assert.equal(pcLayout([part("GPU", { FANS: "3" })]).gpuFans, 3);
  });
});
