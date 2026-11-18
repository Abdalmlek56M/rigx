import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Partner } from "../expenses/expenses-api.ts";
import {
  basisKey, capitalKindKey, capitalValid, defaultStatementRange, emptyCapitalForm, emptyPartnerForm, emptyPayoutForm, monthValid, partnerFormValid, partnerTotals,
  payMethodKey, payoutValid, percentOk, positiveAmount, previousMonth, statementDetail, statementKindKey, statementRangeValid, toCapitalRequest,
  toPartnerForm, toPartnerRequest, toPayoutRequest
} from "./partner-rules.ts";

const partner = (patch: Partial<Partner> = {}): Partner => ({
  id: "p", name: "أحمد", phone: "01000000000", joinDate: "2026-01-01", defaultPercent: 30, basis: "FinancedParts", lossSharingEnabled: true,
  isActive: true, notes: "ملاحظة", capitalBalance: 1000, due: 200, recoverable: 50, unsoldFinancedUnits: 2, rowVersion: 5, ...patch
});

describe("partner labels", () => {
  it("maps known words and falls back to other", () => {
    assert.equal(basisKey("WholeShop"), "WholeShop");
    assert.equal(basisKey("x"), "other");
    assert.equal(capitalKindKey("Withdrawal"), "Withdrawal");
    assert.equal(payMethodKey("BankTransfer"), "BankTransfer");
    assert.equal(payMethodKey("Cheque"), "other");
    assert.equal(statementKindKey("MonthlyShare"), "MonthlyShare");
    assert.equal(statementKindKey("Mystery"), "other");
  });

  it("shows only typed text on a statement line", () => {
    const line = (kind: string, description: string, reference: string | null = null) => ({ kind, description, reference });
    assert.equal(statementDetail(line("Earning", "Share of a sold part", "order-id")), null);
    assert.equal(statementDetail(line("MonthlyShare", "Share of the monthly shop profit", "2026-10")), null);
    assert.equal(statementDetail(line("Payout", "Cash", " R-7 ")), "R-7");
    assert.equal(statementDetail(line("Payout", "Cash")), null);
    assert.equal(statementDetail(line("CapitalDeposit", "Deposit")), null);
    assert.equal(statementDetail(line("CapitalDeposit", "InstaPay")), null);
    assert.equal(statementDetail(line("CapitalDeposit", "زيادة رأس المال")), "زيادة رأس المال");
    assert.equal(statementDetail(line("Reversal", "Returned order")), "Returned order");
  });
});

describe("partner totals", () => {
  it("adds capital, due, recoverable and unsold financed units", () => {
    const totals = partnerTotals([partner(), partner({ capitalBalance: 500.5, due: 0.1, recoverable: 0, unsoldFinancedUnits: 1 })]);
    assert.deepEqual(totals, { capital: 1500.5, due: 200.1, recoverable: 50, unsoldUnits: 3 });
    assert.deepEqual(partnerTotals([]), { capital: 0, due: 0, recoverable: 0, unsoldUnits: 0 });
  });
});

describe("partner form", () => {
  it("reads a partner into the form and back with the row version", () => {
    const request = toPartnerRequest(toPartnerForm(partner()), 5);
    assert.deepEqual(request, {
      name: "أحمد", phone: "01000000000", joinDate: "2026-01-01", defaultPercent: 30, basis: "FinancedParts", lossSharingEnabled: true, isActive: true, notes: "ملاحظة", rowVersion: 5
    });
    assert.equal(toPartnerRequest(emptyPartnerForm("2026-11-18")).rowVersion, undefined);
  });

  it("needs a name, a join date and a percent from 0 to 100", () => {
    const form = { ...emptyPartnerForm("2026-11-18"), name: "علي", defaultPercent: "30" };
    assert.ok(partnerFormValid(form));
    assert.ok(!partnerFormValid({ ...form, name: " " }));
    assert.ok(!partnerFormValid({ ...form, defaultPercent: "" }));
    assert.ok(!partnerFormValid({ ...form, defaultPercent: "101" }));
    assert.ok(!partnerFormValid({ ...form, joinDate: "" }));
    assert.ok(percentOk("0") && percentOk("100") && !percentOk("-1") && !percentOk("abc"));
  });

  it("sends an empty phone and notes as null", () => {
    const request = toPartnerRequest({ ...toPartnerForm(partner()), phone: " ", notes: "" });
    assert.equal(request.phone, null);
    assert.equal(request.notes, null);
  });
});

describe("capital and payouts", () => {
  it("needs an amount above zero", () => {
    assert.ok(positiveAmount("0.5") && !positiveAmount("0") && !positiveAmount("") && !positiveAmount("x"));
    assert.ok(capitalValid({ ...emptyCapitalForm("2026-11-18"), amount: "100" }));
    assert.ok(!capitalValid(emptyCapitalForm("2026-11-18")));
    assert.ok(!capitalValid({ ...emptyCapitalForm(""), amount: "100" }));
    assert.ok(payoutValid({ ...emptyPayoutForm(), amount: "10" }) && !payoutValid(emptyPayoutForm()));
  });

  it("builds the requests with the default account as null", () => {
    const capital = toCapitalRequest({ ...emptyCapitalForm("2026-11-18"), kind: "Withdrawal", amount: "250", note: " x " });
    assert.deepEqual(capital, { kind: "Withdrawal", amount: 250, entryDate: "2026-11-18", method: "Cash", note: "x", moneyAccountId: null });
    const payout = toPayoutRequest({ ...emptyPayoutForm(), amount: "75", method: "InstaPay", accountId: "a1", reference: " R1 " });
    assert.deepEqual(payout, { amount: 75, method: "InstaPay", reference: "R1", note: null, moneyAccountId: "a1" });
  });
});

describe("statement range and month close", () => {
  it("opens on this month so far and needs from not after to", () => {
    assert.deepEqual(defaultStatementRange("2026-11-18"), { from: "2026-11-01", to: "2026-11-18" });
    assert.ok(statementRangeValid({ from: "2026-11-01", to: "2026-11-01" }));
    assert.ok(!statementRangeValid({ from: "2026-11-02", to: "2026-11-01" }));
    assert.ok(!statementRangeValid({ from: "", to: "2026-11-01" }));
  });

  it("closes the previous month by default, across the new year too", () => {
    assert.deepEqual(previousMonth("2026-11-18"), { year: 2026, month: 10 });
    assert.deepEqual(previousMonth("2027-01-03"), { year: 2026, month: 12 });
  });

  it("accepts a real month only", () => {
    assert.ok(monthValid(2026, 10) && monthValid(2026, 12));
    assert.ok(!monthValid(2026, 0) && !monthValid(2026, 13) && !monthValid(1999, 5) && !monthValid(2026, 1.5));
  });
});
