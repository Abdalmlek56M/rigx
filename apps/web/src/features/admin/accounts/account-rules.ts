/** Money accounts (cash drawer, InstaPay, wallet, bank): the form, its request and the total. Pure, so it is tested. */
import type { MoneyAccount, MoneyAccountRequest } from "./accounts-api";

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

/** The balance of the accounts that are in use (a switched-off account is left out). */
export const activeBalance = (accounts: readonly Pick<MoneyAccount, "balance" | "isActive">[]) =>
  accounts.filter((account) => account.isActive).reduce((sum, account) => sum + account.balance, 0);
