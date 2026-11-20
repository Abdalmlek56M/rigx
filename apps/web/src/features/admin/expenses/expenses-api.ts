import { multipart, type Schemas, unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Expense = Schemas["ExpenseView"];
export type ExpenseSummary = Schemas["ExpenseSummaryView"];
export type ExpenseCreateRequest = Schemas["ExpenseCreateRequest"];
export type ExpenseType = Schemas["ExpenseTypeView"];
export type ExpenseTypeRequest = Schemas["ExpenseTypeRequest"];
export type RecurringExpense = Schemas["RecurringExpenseView"];
export type RecurringExpenseRequest = Schemas["RecurringExpenseRequest"];
export type MoneyAccount = Schemas["MoneyAccountView"];
export type Partner = Schemas["PartnerView"];

const api = () => browserApi();

export const fetchExpenses = (from: string, to: string) =>
  required(unwrap(api().GET("/api/v1/admin/retail/expenses", { params: { query: { from, to } } })));

export const fetchExpenseSummary = (from: string, to: string) =>
  required(unwrap(api().GET("/api/v1/admin/retail/expenses/summary", { params: { query: { from, to } } })));

export const createExpense = (input: ExpenseCreateRequest) =>
  required(unwrap(api().POST("/api/v1/admin/retail/expenses", { body: input })));

export const uploadExpenseReceipt = (id: string, file: File) => {
  const form = new FormData();
  form.set("image", file);
  return unwrap(api().PUT("/api/v1/admin/retail/expenses/{id}/receipt", { params: { path: { id } }, ...multipart(form) }));
};

/** The private receipt image (same-origin, so the session cookie applies). */
export const expenseReceiptUrl = (id: string) => `/api/v1/admin/retail/expenses/${id}/receipt`;

export const fetchExpenseTypes = () => required(unwrap(api().GET("/api/v1/admin/retail/expense-types")));
export const createExpenseType = (body: ExpenseTypeRequest) =>
  required(unwrap(api().POST("/api/v1/admin/retail/expense-types", { body })));
export const updateExpenseType = (id: string, body: ExpenseTypeRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/expense-types/{id}", { params: { path: { id } }, body })));

export const fetchRecurringExpenses = () => required(unwrap(api().GET("/api/v1/admin/retail/recurring-expenses")));
export const createRecurringExpense = (body: RecurringExpenseRequest) =>
  required(unwrap(api().POST("/api/v1/admin/retail/recurring-expenses", { body })));
export const updateRecurringExpense = (id: string, body: RecurringExpenseRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/recurring-expenses/{id}", { params: { path: { id } }, body })));

export const fetchMoneyAccounts = () => required(unwrap(api().GET("/api/v1/admin/retail/money/accounts")));
export const fetchPartners = () => required(unwrap(api().GET("/api/v1/admin/retail/partners")));
