import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { accountForm, accountFormValid, accountRequest, activeBalance, emptyAccountForm } from "./account-rules.ts";

describe("money accounts", () => {
  it("builds the request from the form, trimming the name and reading numbers safely", () => {
    const request = accountRequest({ ...emptyAccountForm, name: " درج الفرع ", openingBalance: "250.5", sortOrder: "2.9" }, null);
    assert.deepEqual([request.name, request.openingBalance, request.sortOrder, request.rowVersion], ["درج الفرع", 250.5, 2, null]);
    assert.equal(accountRequest({ ...emptyAccountForm, openingBalance: "" }, 3).openingBalance, 0);
    assert.equal(accountRequest({ ...emptyAccountForm, openingBalance: "abc" }, 3).rowVersion, 3);
  });

  it("fills the form from an account and needs a name", () => {
    const form = accountForm({ name: "محفظة", kind: "Wallet", openingBalance: 10, isActive: false, sortOrder: 4 } as never);
    assert.deepEqual([form.kind, form.openingBalance, form.isActive], ["Wallet", "10", false]);
    assert.equal(accountFormValid(emptyAccountForm), false);
    assert.equal(accountFormValid({ ...emptyAccountForm, name: "x" }), true);
  });

  it("adds up only the accounts in use", () => {
    assert.equal(activeBalance([{ balance: 100, isActive: true }, { balance: 50, isActive: false }, { balance: 25.5, isActive: true }]), 125.5);
  });
});
