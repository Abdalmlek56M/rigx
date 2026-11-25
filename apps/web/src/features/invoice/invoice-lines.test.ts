import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { customerName, isVoid, pieceCount, shapeLines, totalsRows } from "./invoice-lines.ts";

const part = (code: string, extra: Partial<{ serialNumber: string; condition: string; warrantyLabel: string }> = {}) => ({
  code, name: code, condition: "New", serialNumber: null as string | null, warrantyLabel: null as string | null, warrantyEndsAtUtc: null,
  warrantyStatus: null, categoryId: null, category: "قسم", ...extra
});
const line = (code: string | null, parts: ReturnType<typeof part>[], quantity = 1, name = "n") => ({
  code, name, buildCode: null, quantity, unitPrice: 10, lineTotal: 10 * quantity, discount: 0, parts
});

describe("shapeLines", () => {
  it("prints a single item as one row with its facts", () => {
    const [row] = shapeLines([line("RX-1", [part("RX-1", { serialNumber: "S1", warrantyLabel: "3 شهور" })])]);
    assert.equal(row?.kind, "item");
    assert.deepEqual(row?.kind === "item" && row.codes, ["RX-1"]);
    assert.deepEqual(row?.kind === "item" && row.serials, ["S1"]);
    assert.deepEqual(row?.kind === "item" && row.warranties, [{ label: "3 شهور", endsAtUtc: null }]);
  });
  it("keeps several units of one item on one row and lists their serials", () => {
    const [row] = shapeLines([line("RX-1", [part("RX-1", { serialNumber: "S1" }), part("RX-1", { serialNumber: "S2", condition: "Used" })], 2)]);
    assert.equal(row?.kind, "item");
    assert.deepEqual(row?.kind === "item" && row.serials, ["S1", "S2"]);
    assert.deepEqual(row?.kind === "item" && row.conditions, ["New", "Used"]);
    assert.equal(row?.quantity, 2);
  });
  it("groups a build's parts under its name, merging identical ones", () => {
    const rows = shapeLines([line("BLD-01", [part("RX-1"), part("RX-2", { serialNumber: "A" }), part("RX-2", { serialNumber: "B" })], 1, "تجميعة الألعاب (BLD-01)")]);
    const [build] = rows;
    assert.equal(build?.kind, "build");
    assert.equal(build?.kind === "build" && build.name, "تجميعة الألعاب");
    assert.deepEqual(build?.kind === "build" && build.parts.map((p) => p.quantity), [1, 2]);
    assert.equal(pieceCount(rows), 3);
  });
  it("numbers the lines from 1", () => {
    assert.deepEqual(shapeLines([line("RX-1", [part("RX-1")]), line("RX-2", [part("RX-2")])]).map((row) => row.no), [1, 2]);
  });
});

describe("customerName", () => {
  it("drops the internal build code and what was around it", () => {
    assert.equal(customerName("BLD-01"), "");
    assert.equal(customerName("BLD-12 - جهاز المونتاج"), "جهاز المونتاج");
    assert.equal(customerName("Gaming PC [BLD-3]"), "Gaming PC");
    assert.equal(customerName("RTX 4060 Ti"), "RTX 4060 Ti");
  });
});

describe("isVoid / totalsRows", () => {
  it("reads the status and the optional money rows", () => {
    assert.equal(isVoid("Void"), true);
    assert.equal(isVoid("Valid"), false);
    assert.deepEqual(totalsRows({ discount: 0, refunded: 0, paid: 0, remaining: 0 }), { discount: false, refunded: false, payments: false });
    assert.deepEqual(totalsRows({ discount: 50, refunded: 10, paid: 100, remaining: 0 }), { discount: true, refunded: true, payments: true });
  });
});
