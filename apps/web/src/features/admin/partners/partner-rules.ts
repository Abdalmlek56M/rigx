/**
 * Rules for the partners screen: list totals, label keys, the partner / capital / payout forms, the statement range and the month to close.
 * Dates are Cairo business days as `YYYY-MM-DD`. Pure, so it is tested. The server stays the authority. Messages live under `admin.partners`.
 */
import type { Partner } from "../expenses/expenses-api";
import { amountOk, amountText, parseAmount } from "../stock/stock-fields.ts";
import type { CapitalRequest, PartnerRequest, PayoutRequest } from "./partners-api";

const known = <T extends string>(list: readonly T[], value: string): T | "other" => list.find((entry) => entry === value) ?? "other";

export const PARTNER_BASES = ["FinancedParts", "WholeShop"] as const;
export const CAPITAL_KINDS = ["Deposit", "Withdrawal"] as const;
export const PAY_METHODS = ["Cash", "InstaPay", "Wallet", "BankTransfer"] as const;
const STATEMENT_KINDS = ["CapitalDeposit", "CapitalWithdrawal", "Earning", "MonthlyShare", "Reversal", "Payout", "Recovery"] as const;

export type PartnerBasis = (typeof PARTNER_BASES)[number];
export type CapitalKind = (typeof CAPITAL_KINDS)[number];
export type PayMethod = (typeof PAY_METHODS)[number];

export const basisKey = (basis: string) => known(PARTNER_BASES, basis);
export const capitalKindKey = (kind: string) => known(CAPITAL_KINDS, kind);
export const payMethodKey = (method: string) => known(PAY_METHODS, method);
export const statementKindKey = (kind: string) => known(STATEMENT_KINDS, kind);

/**
 * What a statement line says besides its kind: the typed text (a note, a reason, a payout's reference). The server's own words for an earned share are
 * English, and a capital line without a note carries the bare kind or method word, so those show nothing.
 */
export function statementDetail(line: { kind: string; description: string; reference: string | null }): string | null {
  if (line.kind === "Earning" || line.kind === "MonthlyShare") return null;
  if (line.kind === "Payout") return line.reference?.trim() || null;
  const text = line.description.trim();
  return text === "" || [...CAPITAL_KINDS, ...PAY_METHODS].some((word) => word === text) ? null : text;
}

const round2 = (value: number) => Math.round(value * 100) / 100;

export interface PartnerTotals {
  capital: number;
  due: number;
  recoverable: number;
  unsoldUnits: number;
}

/** The KPI cards: what every partner put in, is owed, still has to give back, and how many of their financed units are unsold. */
export function partnerTotals(partners: readonly Partner[]): PartnerTotals {
  return {
    capital: round2(partners.reduce((sum, partner) => sum + partner.capitalBalance, 0)),
    due: round2(partners.reduce((sum, partner) => sum + partner.due, 0)),
    recoverable: round2(partners.reduce((sum, partner) => sum + partner.recoverable, 0)),
    unsoldUnits: partners.reduce((sum, partner) => sum + partner.unsoldFinancedUnits, 0)
  };
}

export interface PartnerForm {
  name: string;
  phone: string;
  joinDate: string;
  defaultPercent: string;
  basis: PartnerBasis;
  lossSharingEnabled: boolean;
  isActive: boolean;
  notes: string;
}

export const emptyPartnerForm = (today: string): PartnerForm => ({
  name: "", phone: "", joinDate: today, defaultPercent: "", basis: "FinancedParts", lossSharingEnabled: false, isActive: true, notes: ""
});

export const toPartnerForm = (partner: Partner): PartnerForm => ({
  name: partner.name,
  phone: partner.phone ?? "",
  joinDate: partner.joinDate,
  defaultPercent: amountText(partner.defaultPercent),
  basis: PARTNER_BASES.find((basis) => basis === partner.basis) ?? "FinancedParts",
  lossSharingEnabled: partner.lossSharingEnabled,
  isActive: partner.isActive,
  notes: partner.notes ?? ""
});

/** A percent from 0 to 100 (an empty field is not one). */
export function percentOk(text: string): boolean {
  const value = parseAmount(text);
  return amountOk(text) && value !== null && value <= 100;
}

export const partnerFormValid = (form: PartnerForm): boolean => form.name.trim() !== "" && form.joinDate !== "" && percentOk(form.defaultPercent);

/** The request for a new partner, or (with `rowVersion`) for saving an existing one. */
export const toPartnerRequest = (form: PartnerForm, rowVersion?: number): PartnerRequest => ({
  name: form.name.trim(),
  phone: form.phone.trim() || null,
  joinDate: form.joinDate,
  defaultPercent: parseAmount(form.defaultPercent) ?? 0,
  basis: form.basis,
  lossSharingEnabled: form.lossSharingEnabled,
  isActive: form.isActive,
  notes: form.notes.trim() || null,
  rowVersion
});

/** An amount above zero (money in or out is never zero). */
export function positiveAmount(text: string): boolean {
  const value = parseAmount(text);
  return value !== null && Number.isFinite(value) && value > 0;
}

export interface MoneyMoveForm {
  amount: string;
  method: PayMethod;
  /** Empty = the server's default account for the method. */
  accountId: string;
  note: string;
}

export interface CapitalForm extends MoneyMoveForm {
  kind: CapitalKind;
  date: string;
}

export interface PayoutForm extends MoneyMoveForm {
  reference: string;
}

export const emptyCapitalForm = (today: string): CapitalForm => ({ kind: "Deposit", amount: "", date: today, method: "Cash", accountId: "", note: "" });
export const emptyPayoutForm = (): PayoutForm => ({ amount: "", method: "Cash", accountId: "", reference: "", note: "" });

export const capitalValid = (form: CapitalForm): boolean => positiveAmount(form.amount) && form.date !== "";
export const payoutValid = (form: PayoutForm): boolean => positiveAmount(form.amount);

export const toCapitalRequest = (form: CapitalForm): CapitalRequest => ({
  kind: form.kind,
  amount: parseAmount(form.amount) ?? 0,
  entryDate: form.date || null,
  method: form.method,
  note: form.note.trim() || null,
  moneyAccountId: form.accountId || null
});

export const toPayoutRequest = (form: PayoutForm): PayoutRequest => ({
  amount: parseAmount(form.amount) ?? 0,
  method: form.method,
  reference: form.reference.trim() || null,
  note: form.note.trim() || null,
  moneyAccountId: form.accountId || null
});

export interface DateRange {
  from: string;
  to: string;
}

/** The statement opens on this month so far. */
export const defaultStatementRange = (today: string): DateRange => ({ from: `${today.slice(0, 8)}01`, to: today });

/** Both dates picked and the start not after the end (`YYYY-MM-DD` compares as text). */
export const statementRangeValid = (range: DateRange): boolean => range.from !== "" && range.to !== "" && range.from <= range.to;

/** The month to close by default: the one before today's month (Cairo). */
export function previousMonth(today: string): { year: number; month: number } {
  const year = Number(today.slice(0, 4));
  const month = Number(today.slice(5, 7));
  return month === 1 ? { year: year - 1, month: 12 } : { year, month: month - 1 };
}

/** A month number from 1 to 12 and a plausible year. */
export const monthValid = (year: number, month: number): boolean => Number.isInteger(year) && year >= 2000 && year <= 2100 && Number.isInteger(month) && month >= 1 && month <= 12;
