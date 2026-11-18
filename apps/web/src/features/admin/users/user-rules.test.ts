import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  activeFilter,
  groupPermissions,
  groupSelected,
  groupTitleKey,
  isSameAccount,
  permissionPrefix,
  restrictionRequest,
  restrictionValid,
  roleCodeValid,
  permissionLabel,
  roleLabel,
  roleNames,
  sameSet,
  setGroup,
  systemCodes,
  toggle,
  userView,
  usernameFromEmail
} from "./user-rules.ts";

describe("users view and filters", () => {
  it("opens on the users view and maps the active filter", () => {
    assert.equal(userView(undefined), "users");
    assert.equal(userView("roles"), "roles");
    assert.equal(userView("x"), "users");
    assert.equal(activeFilter("true"), true);
    assert.equal(activeFilter("false"), false);
    assert.equal(activeFilter(""), undefined);
  });

  it("prefills the username from the email and spots the signed-in account", () => {
    assert.equal(usernameFromEmail("Ahmed.Ali@Example.com"), "ahmed.ali");
    assert.equal(usernameFromEmail("plain"), "plain");
    assert.equal(isSameAccount("A@x.com", " a@X.com"), true);
    assert.equal(isSameAccount("a@x.com", "b@x.com"), false);
    assert.equal(isSameAccount("a@x.com", undefined), false);
  });

  it("names a user's roles from the roles list, labels built-in roles, falls back to the code", () => {
    const label = (role: string) => `label:${role}`;
    const roles = [{ id: "1", code: "Manager", name: "مدير" }, { id: "3", code: "Owner", name: "Owner" }];
    assert.deepEqual(roleNames({ roleIds: ["1", "2", "3"], roleCodes: ["Manager", "Seller", "Owner"] }, roles, label), ["مدير", "Seller", "label:Owner"]);
    assert.equal(roleLabel({ code: "Customer", name: "Customer" }, label), "label:Customer");
    assert.equal(roleLabel({ code: "Seller", name: "بياع" }, label), "بياع");
  });

  it("labels permission codes it knows", () => {
    const labels = { Sales: { ViewOrders: "يشوف الطلبات" } };
    assert.equal(permissionLabel("Sales.ViewOrders", labels), "يشوف الطلبات");
    assert.equal(permissionLabel("Sales.Other", labels), null);
    assert.equal(permissionLabel("Nope", labels), null);
  });

  it("toggles ids and compares selections without order", () => {
    assert.deepEqual(toggle(["a"], "b", true), ["a", "b"]);
    assert.deepEqual(toggle(["a", "b"], "a", false), ["b"]);
    assert.deepEqual(toggle(["a"], "a", true), ["a"]);
    assert.equal(sameSet(["a", "b"], ["b", "a"]), true);
    assert.equal(sameSet(["a"], ["a", "b"]), false);
  });
});

describe("permission picker", () => {
  const codes = ["System.Purge", "Sales.ViewOrders", "Zeta.Thing", "Sales.CreatePOS", "Finance.ViewReports", "Alpha.One"];

  it("groups by prefix: known groups in order, unknown after, codes sorted", () => {
    const groups = groupPermissions(codes);
    assert.deepEqual(groups.map((group) => group.prefix), ["Sales", "Finance", "System", "Alpha", "Zeta"]);
    assert.deepEqual(groups[0]?.codes, ["Sales.CreatePOS", "Sales.ViewOrders"]);
  });

  it("maps a prefix to a title key, unknown to null", () => {
    assert.equal(permissionPrefix("Sales.ViewOrders"), "Sales");
    assert.equal(permissionPrefix("NoDot"), "NoDot");
    assert.equal(groupTitleKey("Finance"), "Finance");
    assert.equal(groupTitleKey("Alpha"), null);
  });

  it("selects and clears a whole group and finds the System codes", () => {
    const sales = groupPermissions(codes)[0];
    assert.ok(sales);
    assert.equal(groupSelected(sales, ["Sales.CreatePOS"]), false);
    const all = setGroup(sales, ["System.Purge"], true);
    assert.equal(groupSelected(sales, all), true);
    assert.deepEqual(setGroup(sales, all, false), ["System.Purge"]);
    assert.deepEqual(systemCodes(all), ["System.Purge"]);
    assert.deepEqual(systemCodes(["Sales.CreatePOS"]), []);
  });

  it("accepts the role codes the API accepts", () => {
    assert.equal(roleCodeValid("Cashier-2.a_b"), true);
    assert.equal(roleCodeValid("two words"), false);
    assert.equal(roleCodeValid(""), false);
  });
});

describe("restrictions", () => {
  it("needs a reason and, for a suspension, an end day", () => {
    assert.equal(restrictionValid({ type: "Ban", reason: "abuse", until: "" }), true);
    assert.equal(restrictionValid({ type: "Ban", reason: "abu", until: "" }), false);
    assert.equal(restrictionValid({ type: "Suspension", reason: "abuse", until: "" }), false);
    assert.equal(restrictionValid({ type: "Suspension", reason: "abuse", until: "2026-12-01" }), true);
  });

  it("ends a suspension at the end of the Cairo day and never ends a ban", () => {
    const suspension = restrictionRequest({ type: "Suspension", reason: " abuse ", until: "2026-12-01" });
    assert.equal(suspension.type, "Suspension");
    assert.equal(suspension.reason, "abuse");
    assert.equal(suspension.expiresAtUtc, "2026-12-01T21:59:59.999Z");
    assert.equal(restrictionRequest({ type: "Ban", reason: "abuse", until: "2026-12-01" }).expiresAtUtc, null);
  });
});
