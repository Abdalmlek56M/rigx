import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  emptyExpenseForm,
  expenseLink,
  expensePeriodRange,
  expenseValid,
  parseExpensePeriod,
  paidFrom,
  toExpenseCreate
} from "./expense-rules.ts";

describe("expense period", () => {
  it("reads the four periods and falls back to this month", () => {
    assert.equal(parseExpensePeriod("lastMonth"), "lastMonth");
    assert.equal(parseExpensePeriod("year"), "year");
    assert.equal(parseExpensePeriod("week"), "month");
  });

  it("covers this month, last month, this year and a complete custom range", () => {
    const none = { from: "", to: "" };
    assert.deepEqual(expensePeriodRange("month", "2026-11-18", none), { from: "2026-11-01", to: "2026-11-18" });
    assert.deepEqual(expensePeriodRange("lastMonth", "2026-11-18", none), { from: "2026-10-01", to: "2026-10-31" });
    assert.deepEqual(expensePeriodRange("year", "2026-11-18", none), { from: "2026-01-01", to: "2026-11-18" });
    assert.deepEqual(expensePeriodRange("custom", "2026-11-18", { from: "2026-09-01", to: "2026-09-30" }), { from: "2026-09-01", to: "2026-09-30" });
    assert.equal(expensePeriodRange("custom", "2026-11-18", none), null);
  });
});

describe("expense form", () => {
  it("needs a date, a positive amount and a type", () => {
    const form = emptyExpenseForm("2026-11-18");
    assert.equal(expenseValid(form), false);
    assert.equal(expenseValid({ ...form, amount: "0", category: "إيجار" }), false);
    assert.equal(expenseValid({ ...form, amount: "500", category: "إيجار" }), true);
    assert.equal(expenseValid({ ...form, amount: "500", category: "إيجار", date: "18/11/2026" }), false);
  });

  it("maps the payer choice to exactly one field and always a general overhead", () => {
    assert.deepEqual(paidFrom({ ...emptyExpenseForm("2026-11-18"), paidFrom: "account:a1" }), { moneyAccountId: "a1", paidByPartnerId: null });
    assert.deepEqual(paidFrom({ ...emptyExpenseForm("2026-11-18"), paidFrom: "partner:p1" }), { moneyAccountId: null, paidByPartnerId: "p1" });
    assert.deepEqual(paidFrom(emptyExpenseForm("2026-11-18")), { moneyAccountId: null, paidByPartnerId: null });
    const input = toExpenseCreate({ date: "2026-11-18", amount: "1200.50", category: "كهرباء", note: " فاتورة ", paidFrom: "account:a1" });
    assert.deepEqual(input, {
      expenseDate: "2026-11-18", amount: 1200.5, category: "كهرباء", classification: "GeneralOverhead", note: "فاتورة",
      orderId: null, unitId: null, assemblyId: null, moneyAccountId: "a1", paidByPartnerId: null
    });
  });
});

describe("expense links", () => {

  it("names the one link an expense has, order first", () => {
    assert.equal(expenseLink({ orderId: "o", unitId: "u", assemblyId: "b" }), "order");
    assert.equal(expenseLink({ orderId: null, unitId: "u", assemblyId: "b" }), "unit");
    assert.equal(expenseLink({ orderId: null, unitId: null, assemblyId: "b" }), "build");
    assert.equal(expenseLink({ orderId: null, unitId: null, assemblyId: null }), null);
  });
});
