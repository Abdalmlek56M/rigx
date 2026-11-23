/** Expenses: the period switch, the form ⇄ API mapping and the linked kind. Pure, so it is tested. */
import { periodRange, type DateRange } from "../dashboard/dashboard-rules.ts";
import type { Expense, ExpenseCreateRequest } from "./expenses-api";

export const EXPENSE_PERIODS = ["month", "lastMonth", "year", "custom"] as const;
export type ExpensePeriod = (typeof EXPENSE_PERIODS)[number];

export function parseExpensePeriod(value: string): ExpensePeriod {
  return EXPENSE_PERIODS.find((period) => period === value) ?? "month";
}

/** The dates the chosen period covers (last month, this year to date, or the complete custom range); null while custom is incomplete. */
export function expensePeriodRange(period: ExpensePeriod, today: string, custom: { from: string; to: string }): DateRange | null {
  return periodRange(period, today, custom);
}

export interface ExpenseForm {
  date: string;
  amount: string;
  category: string;
  note: string;
  /** "", "account:<id>" or "partner:<id>". */
  paidFrom: string;
}

export function emptyExpenseForm(today: string): ExpenseForm {
  return { date: today, amount: "", category: "", note: "", paidFrom: "" };
}

export function expenseValid(form: ExpenseForm): boolean {
  const amount = Number(form.amount);
  return form.date !== "" && /^\d{4}-\d{2}-\d{2}$/.test(form.date) && Number.isFinite(amount) && amount > 0 && form.category !== "";
}

/** The two mutually exclusive payer fields; an empty choice leaves both null so the server uses the default cash account. */
export function paidFrom(form: ExpenseForm): { moneyAccountId: string | null; paidByPartnerId: string | null } {
  if (form.paidFrom.startsWith("account:")) return { moneyAccountId: form.paidFrom.slice("account:".length) || null, paidByPartnerId: null };
  if (form.paidFrom.startsWith("partner:")) return { moneyAccountId: null, paidByPartnerId: form.paidFrom.slice("partner:".length) || null };
  return { moneyAccountId: null, paidByPartnerId: null };
}

export function toExpenseCreate(form: ExpenseForm): ExpenseCreateRequest {
  return {
    expenseDate: form.date,
    amount: Number(form.amount),
    category: form.category,
    classification: "GeneralOverhead",
    note: form.note.trim() === "" ? null : form.note.trim(),
    orderId: null,
    unitId: null,
    assemblyId: null,
    ...paidFrom(form)
  };
}

export type ExpenseLink = "order" | "unit" | "build" | null;

/** What an expense is linked to (the view only carries ids, not their public references). */
export function expenseLink(expense: Pick<Expense, "orderId" | "unitId" | "assemblyId">): ExpenseLink {
  if (expense.orderId) return "order";
  if (expense.unitId) return "unit";
  if (expense.assemblyId) return "build";
  return null;
}
