import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  accountForm, accountFormValid, accountRequest, activeBalance, emptyAccountForm, emptyTransferForm, movementSourceKey, transferRequest, transferValid
} from "./account-rules.ts";

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

describe("transfers between accounts", () => {
  const form = { ...emptyTransferForm("2026-11-23"), from: "a", to: "b", amount: "150.50" };

  it("needs two different accounts, an amount above zero with at most two decimals, and a date", () => {
    assert.equal(transferValid(form), true);
    assert.equal(transferValid({ ...form, to: "a" }), false);
    assert.equal(transferValid({ ...form, from: "" }), false);
    assert.equal(transferValid({ ...form, amount: "0" }), false);
    assert.equal(transferValid({ ...form, amount: "10.555" }), false);
    assert.equal(transferValid({ ...form, amount: "" }), false);
    assert.equal(transferValid({ ...form, date: "" }), false);
  });

  it("builds the request with a trimmed note, or none", () => {
    assert.deepEqual(transferRequest({ ...form, note: " سحب للمحفظة " }), { fromAccountId: "a", toAccountId: "b", amount: 150.5, date: "2026-11-23", note: "سحب للمحفظة" });
    assert.equal(transferRequest(form).note, null);
  });

  it("names what produced a movement, and any other source as other", () => {
    assert.equal(movementSourceKey("TransferOut"), "TransferOut");
    assert.equal(movementSourceKey("Sale"), "Sale");
    assert.equal(movementSourceKey("Something"), "other");
  });
});
