import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { latinNumber } from "./digits.ts";

describe("latinNumber", () => {
  it("turns Arabic-Indic and Persian digits into Western digits", () => {
    assert.equal(latinNumber("٥٠٠٠"), "5000");
    assert.equal(latinNumber("۱۲۳"), "123");
  });
  it("reads the Arabic decimal mark and a comma as a dot", () => {
    assert.equal(latinNumber("١٢٫٥"), "12.5");
    assert.equal(latinNumber("12,5"), "12.5");
  });
  it("keeps Western numbers and drops letters and spaces", () => {
    assert.equal(latinNumber("-250.75"), "-250.75");
    assert.equal(latinNumber("5 000 ج"), "5000");
    assert.equal(latinNumber(""), "");
  });
});
