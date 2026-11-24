import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { ADMIN_NAV } from "./admin-nav.ts";
import { isGroupFolded, navCollapseKey, parseCollapsed, toggleCollapsed, visibleAreas } from "./nav-collapse.ts";

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

  it("knows which groups are folded", () => {
    assert.equal(isGroupFolded(["stock"], stock), true);
    assert.equal(isGroupFolded(["stock"], sales), false);
  });

  it("shows every area of an open group, whatever page is open", () => {
    assert.deepEqual(visibleAreas([], stock, null), stock.areas);
    assert.deepEqual(visibleAreas(["sales"], stock, stock.areas[0]!), stock.areas);
  });

  it("folds a group even when the page being viewed is inside it, keeping only that page's row", () => {
    const inventory = stock.areas[0]!;
    assert.deepEqual(visibleAreas(["stock"], stock, inventory), [inventory]);
    assert.deepEqual(visibleAreas(["stock"], stock, null), []);
    assert.deepEqual(visibleAreas(["stock"], stock, sales.areas[0]!), []);
  });
});
