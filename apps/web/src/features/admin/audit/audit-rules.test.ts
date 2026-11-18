import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  AUDIT_FILTERS,
  auditOutcomeKey,
  auditRangeUtc,
  auditView,
  changeRows,
  formatValue,
  shortId
} from "./audit-rules.ts";

describe("audit filters and labels", () => {
  it("opens on the changes view and maps the outcome with a fallback", () => {
    assert.equal(auditView(undefined), "changes");
    assert.equal(auditView("security"), "security");
    assert.equal(auditView("weird"), "changes");
    assert.equal(auditOutcomeKey("Succeeded"), "Succeeded");
    assert.equal(auditOutcomeKey("Failed"), "Failed");
    assert.equal(auditOutcomeKey("Something"), "Other");
  });

  it("has empty date filters and no entry by default", () => {
    assert.deepEqual(
      [AUDIT_FILTERS.view, AUDIT_FILTERS.from, AUDIT_FILTERS.to, AUDIT_FILTERS.page, AUDIT_FILTERS.entry],
      ["changes", "", "", "1", ""]
    );
  });
});

describe("audit Cairo day range", () => {
  it("converts the Cairo start and end of the days to UTC instants", () => {
    assert.deepEqual(auditRangeUtc("2026-11-20", "2026-11-21"), {
      fromUtc: "2026-11-19T22:00:00.000Z",
      toUtc: "2026-11-21T21:59:59.999Z"
    });
    assert.deepEqual(auditRangeUtc("2026-07-01", "2026-07-01"), {
      fromUtc: "2026-06-30T21:00:00.000Z",
      toUtc: "2026-07-01T20:59:59.999Z"
    });
  });

  it("sends null for an empty or malformed date", () => {
    assert.deepEqual(auditRangeUtc("", ""), { fromUtc: null, toUtc: null });
    assert.deepEqual(auditRangeUtc("20/11/2026", "2026-11-20"), { fromUtc: null, toUtc: "2026-11-20T21:59:59.999Z" });
  });
});

describe("audit change values", () => {
  it("shows scalars, joins plain arrays and serialises nested shapes", () => {
    assert.deepEqual(
      [
        formatValue(null),
        formatValue(undefined),
        formatValue("نص"),
        formatValue(12.5),
        formatValue(true),
        formatValue(["a", "b"]),
        formatValue([1, 2]),
        formatValue([]),
        formatValue({ role: "Owner" }),
        formatValue([{ id: 1 }])
      ],
      ["", "", "نص", "12.5", "true", "a, b", "1, 2", "", '{"role":"Owner"}', '[{"id":1}]']
    );
  });

  it("turns a change list into before → after rows", () => {
    assert.deepEqual(
      changeRows([
        { field: "Name", before: "Old", after: "New" },
        { field: "Price", before: null, after: 1200 },
        { field: "Roles", before: ["A"], after: ["A", "B"] }
      ]),
      [
        { field: "Name", before: "Old", after: "New" },
        { field: "Price", before: "", after: "1200" },
        { field: "Roles", before: "A", after: "A, B" }
      ]
    );
  });

  it("shortens ids for the table only", () => {
    assert.equal(shortId("0199a000-0000-7000-8000-000000000001"), "0199a000");
    assert.equal(shortId(null), "");
  });
});
