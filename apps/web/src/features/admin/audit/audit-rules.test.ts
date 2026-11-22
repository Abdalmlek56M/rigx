import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  AUDIT_FILTERS,
  auditOutcomeKey,
  auditRangeUtc,
  auditGroup,
  auditValue,
  auditView,
  deviceOf,
  diffRows,
  entryTitle,
  groupByDay,
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
  it("never shows ids or masked secrets as they are, and keeps times and nested data apart", () => {
    assert.deepEqual(
      [
        auditValue(null), auditValue(""), auditValue([]), auditValue(true), auditValue(false), auditValue(12.5), auditValue("نص"),
        auditValue("***"), auditValue("0199a000-0000-7000-8000-000000000001"), auditValue(["0199a000-0000-7000-8000-000000000001"]),
        auditValue("2026-11-19T10:00:00+00:00"), auditValue("2026-11-19"), auditValue(["a", "b"]), auditValue({ role: "Owner" })
      ],
      [
        { kind: "empty" }, { kind: "empty" }, { kind: "empty" }, { kind: "yes" }, { kind: "no" }, { kind: "text", text: "12.5" }, { kind: "text", text: "نص" },
        { kind: "masked" }, { kind: "linked", ids: ["0199a000-0000-7000-8000-000000000001"] }, { kind: "linked", ids: ["0199a000-0000-7000-8000-000000000001"] },
        { kind: "time", iso: "2026-11-19T10:00:00+00:00" }, { kind: "text", text: "2026-11-19" }, { kind: "text", text: "a، b" },
        { kind: "data", text: JSON.stringify({ role: "Owner" }, null, 2) }
      ]
    );
  });

  it("marks each field added, changed or removed, changed first, and skips fields empty on both sides", () => {
    assert.deepEqual(
      diffRows([
        { field: "Note", before: null, after: "جديد" },
        { field: "Price", before: 1000, after: 1200 },
        { field: "Barcode", before: "123", after: null },
        { field: "Empty", before: null, after: "" }
      ]).map((row) => [row.field, row.status]),
      [["Price", "changed"], ["Note", "added"], ["Barcode", "removed"]]
    );
  });

  it("names the record from its best title field, never from an id", () => {
    assert.equal(entryTitle([{ field: "Price", before: 1, after: 2 }, { field: "Name", before: "قديم", after: "RTX 3060" }]), "RTX 3060");
    assert.equal(entryTitle([{ field: "Code", before: "A-1", after: null }]), "A-1");
    assert.equal(entryTitle([{ field: "Name", before: null, after: "0199a000-0000-7000-8000-000000000001" }]), null);
    assert.equal(entryTitle([{ field: "Price", before: 1, after: 2 }]), null);
  });

  it("shortens ids for the security table only", () => {
    assert.equal(shortId("0199a000-0000-7000-8000-000000000001"), "0199a000");
    assert.equal(shortId(null), "");
  });
});

describe("audit grouping", () => {
  it("puts every table under a kind of work", () => {
    assert.deepEqual(
      ["invoices", "stock_items", "partner_settlements", "partners", "retail_expenses", "products", "store_settings", "roles", "persons", "mystery"].map(auditGroup),
      ["sales", "stock", "partners", "partners", "money", "catalog", "settings", "accounts", "customers", "other"]
    );
  });

  it("groups by Cairo day (today, yesterday, earlier) and inside a day by kind in a fixed order", () => {
    const entry = (id: string, occurredAtUtc: string, entity: string) => ({ id, occurredAtUtc, entity });
    const days = groupByDay([
      entry("1", "2026-11-19T21:30:00Z", "store_settings"), // 23:30 Cairo, the 19th
      entry("2", "2026-11-19T10:00:00Z", "invoices"),
      entry("3", "2026-11-19T09:00:00Z", "store_settings"),
      entry("4", "2026-11-18T22:30:00Z", "stock_items"), // 00:30 Cairo, the 19th
      entry("5", "2026-11-18T08:00:00Z", "partners"),
      entry("6", "2026-11-10T08:00:00Z", "invoices")
    ], "2026-11-19");
    assert.deepEqual(
      days.map((day) => [day.date, day.when, day.groups.map((group) => [group.group, group.entries.map((item) => item.id)])]),
      [
        ["2026-11-19", "today", [["sales", ["2"]], ["stock", ["4"]], ["settings", ["1", "3"]]]],
        ["2026-11-18", "yesterday", [["partners", ["5"]]]],
        ["2026-11-10", "earlier", [["sales", ["6"]]]]
      ]
    );
  });
});

describe("audit device", () => {
  it("names the browser and the system, not the raw user agent", () => {
    const ua = {
      chromeWindows: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
      edge: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0",
      safariIphone: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1",
      chromeAndroid: "Mozilla/5.0 (Linux; Android 14; SM-A546E) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",
      firefoxMac: "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.1; rv:132.0) Gecko/20100101 Firefox/132.0"
    };
    assert.deepEqual(deviceOf(ua.chromeWindows), { browser: "Chrome", os: "Windows" });
    assert.deepEqual(deviceOf(ua.edge), { browser: "Edge", os: "Windows" });
    assert.deepEqual(deviceOf(ua.safariIphone), { browser: "Safari", os: "iPhone" });
    assert.deepEqual(deviceOf(ua.chromeAndroid), { browser: "Chrome", os: "Android" });
    assert.deepEqual(deviceOf(ua.firefoxMac), { browser: "Firefox", os: "Mac" });
    assert.deepEqual(deviceOf("curl/8.4.0"), null);
    assert.equal(deviceOf(null), null);
  });
});
