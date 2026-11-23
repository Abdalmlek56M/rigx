import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { columnKey, displayCell, isNumeric, parseCsv } from "./export-sheet.ts";

describe("parseCsv", () => {
  it("reads the server's CSV: byte-order mark, quotes, commas and line breaks inside cells", () => {
    const sheet = parseCsv('﻿code,name,note\r\nRX-1,"Ryzen 5, box","said ""hi""\r\nthen left"\r\nRX-2,كارت,\r\n');
    assert.deepEqual(sheet.columns, ["code", "name", "note"]);
    assert.deepEqual(sheet.rows, [["RX-1", "Ryzen 5, box", 'said "hi"\r\nthen left'], ["RX-2", "كارت", ""]]);
  });
  it("drops the spreadsheet formula guard", () => {
    assert.deepEqual(parseCsv("note\r\n'=SUM(A1)\r\n'-5 off\r\n").rows, [["=SUM(A1)"], ["-5 off"]]);
  });
  it("handles an empty file and a last row without a line break", () => {
    assert.deepEqual(parseCsv(""), { columns: [], rows: [] });
    assert.deepEqual(parseCsv("a,b\r\n1,2").rows, [["1", "2"]]);
  });
});

describe("columnKey", () => {
  it("turns server column names into message keys", () => {
    assert.equal(columnKey("price with board"), "priceWithBoard");
    assert.equal(columnKey("trade-in"), "tradeIn");
    assert.equal(columnKey("code"), "code");
  });
});

describe("displayCell", () => {
  const words = { yes: "نعم", no: "لا" };
  it("writes yes/no in words", () => {
    assert.equal(displayCell("yes", "ar", words), "نعم");
    assert.equal(displayCell("no", "ar", words), "لا");
  });
  it("puts business times on the 12-hour clock and dates day first", () => {
    assert.equal(displayCell("2026-11-14 09:30", "ar", words), "14/11/2026، 9:30 ص");
    assert.equal(displayCell("2026-11-14 00:05", "ar", words), "14/11/2026، 12:05 ص");
    assert.equal(displayCell("2026-11-14 12:00", "en", words), "14/11/2026, 12:00 PM");
    assert.equal(displayCell("2026-11-14 21:45", "en", words), "14/11/2026, 9:45 PM");
    assert.equal(displayCell("2026-11-14", "ar", words), "14/11/2026");
  });
  it("leaves other text as it is", () => {
    assert.equal(displayCell("RX-12", "ar", words), "RX-12");
  });
});

describe("isNumeric", () => {
  it("knows numbers", () => {
    assert.equal(isNumeric("1500.5"), true);
    assert.equal(isNumeric("-20"), true);
    assert.equal(isNumeric("RX-1"), false);
    assert.equal(isNumeric(""), false);
  });
});
