import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { areaFor, firstAreaHref, isStaff, visibleNav } from "./admin-nav.ts";

describe("visibleNav", () => {
  it("shows only the areas the permissions open and drops empty groups", () => {
    const nav = visibleNav(["Sales.ViewOrders", "Requests.View"]);
    assert.deepEqual(nav.map((group) => group.key), ["sales", "customers"]);
    assert.deepEqual(nav.flatMap((group) => group.areas.map((area) => area.key)), ["orders", "requests"]);
  });
  it("opens an area with any one of its permissions", () => {
    const keys = visibleNav(["System.ManageBackups"]).flatMap((group) => group.areas.map((area) => area.key));
    assert.deepEqual(keys, ["system"]);
  });
});

describe("isStaff", () => {
  it("is false for a customer (no permissions) and unknown permissions", () => {
    assert.equal(isStaff([]), false);
    assert.equal(isStaff(["Something.Else"]), false);
  });
  it("is true with any admin permission", () => {
    assert.equal(isStaff(["Catalog.View"]), true);
  });
});

describe("areaFor", () => {
  it("matches the dashboard only on /admin itself", () => {
    assert.equal(areaFor("/admin")?.key, "dashboard");
    assert.equal(areaFor("/admin/orders")?.key, "orders");
  });
  it("finds the brands page next to the categories, opened by the catalog permission", () => {
    assert.equal(areaFor("/admin/brands")?.key, "brands");
    assert.deepEqual(visibleNav(["Catalog.View"]).flatMap((group) => group.areas.map((area) => area.key)), ["categories", "brands"]);
  });
  it("finds the stock import page, which only the receive-goods permission opens", () => {
    assert.equal(areaFor("/admin/stock/import")?.key, "stockImport");
    assert.deepEqual(visibleNav(["Purchasing.ReceiveGoods"]).flatMap((group) => group.areas.map((area) => area.key)), ["stockImport"]);
    assert.deepEqual(visibleNav(["Purchasing.View"]).flatMap((group) => group.areas.map((area) => area.key)), ["purchases"]);
  });
  it("finds the policies page in the content group, opened by the store-settings permission", () => {
    assert.equal(areaFor("/admin/policies")?.key, "policies");
    assert.deepEqual(visibleNav(["System.StoreSettings"]).find((group) => group.key === "content")?.areas.map((area) => area.key), ["articles", "faq", "policies"]);
  });
  it("puts detail pages under their list", () => {
    assert.equal(areaFor("/admin/orders/123")?.key, "orders");
  });
  it("does not match a longer name or an unknown path", () => {
    assert.equal(areaFor("/admin/ordersx"), null);
    assert.equal(areaFor("/admin/nope"), null);
  });
});

describe("firstAreaHref", () => {
  it("is the first visible area, or null", () => {
    assert.equal(firstAreaHref(["Inventory.View", "Sales.ViewOrders"]), "/admin/orders");
    assert.equal(firstAreaHref([]), null);
  });
});
