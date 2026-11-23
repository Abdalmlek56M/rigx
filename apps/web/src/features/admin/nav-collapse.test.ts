import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { ADMIN_NAV } from "./admin-nav.ts";
import { isGroupOpen, navCollapseKey, parseCollapsed, toggleCollapsed } from "./nav-collapse.ts";

const stock = ADMIN_NAV.find((group) => group.key === "stock")!;
const sales = ADMIN_NAV.find((group) => group.key === "sales")!;

describe("nav collapse", () => {
  it("keeps one key per user", () => {
    assert.notEqual(navCollapseKey("a"), navCollapseKey("b"));
  });

  it("reads a stored list and ignores anything else", () => {
    assert.deepEqual(parseCollapsed('["stock","money"]'), ["stock", "money"]);
    assert.deepEqual(parseCollapsed('["stock",3,null]'), ["stock"]);
    for (const bad of [null, "", "{}", "oops", "42"]) assert.deepEqual(parseCollapsed(bad), []);
  });

  it("toggles a group in and out", () => {
    assert.deepEqual(toggleCollapsed([], "stock"), ["stock"]);
    assert.deepEqual(toggleCollapsed(["stock", "money"], "stock"), ["money"]);
  });

  it("opens a folded group only while the page being viewed is inside it", () => {
    const inventory = stock.areas[0]!;
    assert.equal(isGroupOpen(["stock"], stock, null), false);
    assert.equal(isGroupOpen(["stock"], stock, sales.areas[0]!), false);
    assert.equal(isGroupOpen(["stock"], stock, inventory), true);
    assert.equal(isGroupOpen([], stock, null), true);
  });
});
