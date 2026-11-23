/** Money accounts (cash drawer, InstaPay, wallet, bank): the form, its request and the total. Pure, so it is tested. */
import type { MoneyAccount, MoneyAccountRequest, MoneyTransferRequest } from "./accounts-api";

export const ACCOUNT_KINDS = ["Cash", "InstaPay", "Wallet", "BankTransfer"] as const;
export type AccountKind = (typeof ACCOUNT_KINDS)[number];

export interface AccountForm {
  name: string;
  kind: AccountKind;
  openingBalance: string;
  isActive: boolean;
  sortOrder: string;
}

export const emptyAccountForm: AccountForm = { name: "", kind: "Cash", openingBalance: "0", isActive: true, sortOrder: "0" };

export const accountForm = (account: MoneyAccount): AccountForm => ({
  name: account.name,
  kind: ACCOUNT_KINDS.find((kind) => kind === account.kind) ?? "Cash",
  openingBalance: String(account.openingBalance),
  isActive: account.isActive,
  sortOrder: String(account.sortOrder)
});

const number = (value: string) => {
  const parsed = Number(value.trim() === "" ? 0 : value);
  return Number.isFinite(parsed) ? parsed : 0;
};

/** The request: a new account has no row version; an edit sends the one it was loaded with (the kind is fixed at creation). */
export function accountRequest(form: AccountForm, rowVersion: number | null): MoneyAccountRequest {
  return {
    name: form.name.trim(), kind: form.kind, openingBalance: number(form.openingBalance), isActive: form.isActive,
    sortOrder: Math.trunc(number(form.sortOrder)), rowVersion
  };
}

export const accountFormValid = (form: AccountForm) => form.name.trim() !== "";

export interface TransferForm {
  from: string;
  to: string;
  amount: string;
  date: string;
  note: string;
}

export const emptyTransferForm = (today: string): TransferForm => ({ from: "", to: "", amount: "", date: today, note: "" });

/** Two different accounts, an amount above zero with at most two decimals, and a date. */
export function transferValid(form: TransferForm): boolean {
  const amount = Number(form.amount);
  return form.from !== "" && form.to !== "" && form.from !== form.to && form.amount.trim() !== ""
    && Number.isFinite(amount) && amount > 0 && Math.round(amount * 100) === amount * 100 && form.date !== "";
}

export function transferRequest(form: TransferForm): MoneyTransferRequest {
  return { fromAccountId: form.from, toAccountId: form.to, amount: Number(form.amount), date: form.date, note: form.note.trim() || null };
}

const MOVEMENT_SOURCES = ["Sale", "Refund", "Purchase", "Expense", "PartnerPayout", "CapitalDeposit", "CapitalWithdrawal", "TransferIn", "TransferOut"] as const;
export type MovementSource = (typeof MOVEMENT_SOURCES)[number];

/** The message key of what produced a movement; a source this screen does not know reads as "other". */
export const movementSourceKey = (source: string): MovementSource | "other" => MOVEMENT_SOURCES.find((known) => known === source) ?? "other";

/** The balance of the accounts that are in use (a switched-off account is left out). */
export const activeBalance = (accounts: readonly Pick<MoneyAccount, "balance" | "isActive">[]) =>
  accounts.filter((account) => account.isActive).reduce((sum, account) => sum + account.balance, 0);
