import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { badgeLabel, isBadgeArea, parseSeen, seenKey, visibleCounts } from "./badge-rules.ts";

describe("sidebar badges", () => {
  it("shows a small number, caps it at 99+ and hides zero or missing counts", () => {
    assert.equal(badgeLabel(3), "3");
    assert.equal(badgeLabel(100), "99+");
    assert.equal(badgeLabel(0), null);
    assert.equal(badgeLabel(null), null);
    assert.equal(badgeLabel(undefined), null);
  });

  it("knows which areas carry a badge", () => {
    assert.equal(isBadgeArea("orders"), true);
    assert.equal(isBadgeArea("requests"), true);
    assert.equal(isBadgeArea("brands"), false);
    assert.equal(isBadgeArea(null), false);
  });

  it("clears the badge of the section being viewed and keeps the others", () => {
    assert.deepEqual(visibleCounts({ orders: 4, requests: 2 }, "orders"), { orders: 0, requests: 2 });
    assert.deepEqual(visibleCounts({ orders: 4, requests: null }, "pos"), { orders: 4, requests: null });
  });

  it("keeps each person's last visit apart and ignores a damaged stored value", () => {
    assert.notEqual(seenKey("ali", "orders"), seenKey("mona", "orders"));
    assert.equal(parseSeen("2026-11-21T10:00:00Z"), "2026-11-21T10:00:00Z");
    assert.equal(parseSeen("garbage"), null);
    assert.equal(parseSeen(null), null);
  });
});
