import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { formatDate, formatDateTime, formatDateTimeFull, formatMonth } from "./format.ts";

describe("formatMonth / formatDate", () => {
  it("names the month in the visitor's language", () => {
    assert.equal(formatMonth("2026-11-14T10:00:00Z", "en"), "November");
    assert.equal(formatMonth("2026-11-14T10:00:00Z", "ar"), "نوفمبر");
  });
  it("formats a full date with Western digits", () => {
    assert.equal(formatDate("2026-11-14T10:00:00Z", "en"), "14 November 2026");
    assert.equal(formatDate("2026-11-14T10:00:00Z", "ar"), "14 نوفمبر 2026");
  });
  it("uses Cairo time at the day boundary", () => {
    assert.equal(formatDate("2026-06-30T22:30:00Z", "en"), "1 July 2026");
    assert.equal(formatMonth("2026-06-30T22:30:00Z", "en"), "July");
  });
});

describe("formatDateTime", () => {
  it("shows day/month and Cairo time", () => {
    assert.equal(formatDateTime("2026-11-13T12:22:00Z", "en"), "13/11 · 14:22");
    assert.equal(formatDateTime("2026-11-13T12:22:00Z", "ar"), "13/11 · 14:22");
  });
});

describe("formatDateTimeFull", () => {
  it("shows the full date and Cairo time", () => {
    assert.equal(formatDateTimeFull("2026-11-13T12:22:00Z", "en"), "13/11/2026 · 14:22");
    assert.equal(formatDateTimeFull("2026-11-13T12:22:00Z", "ar"), "13/11/2026 · 14:22");
  });
});
