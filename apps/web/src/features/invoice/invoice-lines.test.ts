import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { isVoid, shapeLines, totalsRows } from "./invoice-lines.ts";

const part = (code: string) => ({
  code, name: code, condition: "New", serialNumber: null, warrantyLabel: null, warrantyEndsAtUtc: null, warrantyStatus: null, categoryId: null, category: null
});
const line = (code: string | null, parts: ReturnType<typeof part>[]) => ({
  code, name: "n", buildCode: null, quantity: 1, unitPrice: 10, lineTotal: 10, discount: 0, parts
});

describe("shapeLines", () => {
  it("does not list a single part twice", () => {
    const [row] = shapeLines([line("RX-1", [part("RX-1")])]);
    assert.equal(row?.listParts, false);
  });
  it("lists the parts of a build", () => {
    const [row] = shapeLines([line("BLD-1", [part("RX-1"), part("RX-2")])]);
    assert.equal(row?.listParts, true);
    assert.equal(row?.parts.length, 2);
  });
  it("lists parts when the only part has another code", () => {
    assert.equal(shapeLines([line("BLD-1", [part("RX-1")])])[0]?.listParts, true);
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
