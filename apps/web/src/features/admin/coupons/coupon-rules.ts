/** Coupons (admin): the form ⇄ API, the status of a code and the report totals. Pure, so it is tested. */
import { cairoDate, cairoDayEndUtc, cairoDayStartUtc } from "../offers/offer-rules.ts";
import type { Coupon, CouponInput } from "./coupons-api";

export const DISCOUNT_KINDS = ["Percent", "Amount"] as const;
export type DiscountKind = (typeof DISCOUNT_KINDS)[number];

export const CODE_MIN = 3;
export const CODE_MAX = 30;
export const MAX_PERCENT = 90;

/** What the person types, kept to what a code may hold: upper-case letters, digits and dashes. */
export function normalizeCode(raw: string): string {
  return raw.toUpperCase().replace(/[^A-Z0-9-]/g, "").slice(0, CODE_MAX);
}

/** Letters and digits that are not mistaken for each other (no I, O, 0 or 1). */
const CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
const GENERATED_LENGTH = 6;
const GENERATE_TRIES = 20;

/** A short readable code that is not one of `taken` (compared without case); `null` when twenty tries all collided. */
export function generateCouponCode(taken: readonly string[], random: () => number = Math.random): string | null {
  const used = new Set(taken.map((code) => code.toUpperCase()));
  for (let attempt = 0; attempt < GENERATE_TRIES; attempt++) {
    const code = Array.from({ length: GENERATED_LENGTH }, () => CODE_ALPHABET[Math.floor(random() * CODE_ALPHABET.length)]).join("");
    if (!used.has(code)) return code;
  }
  return null;
}

export interface CouponForm {
  code: string;
  sourceName: string;
  kind: DiscountKind;
  value: string;
  maxDiscount: string;
  minOrderTotal: string;
  maxUses: string;
  oncePerCustomer: boolean;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export const EMPTY_COUPON_FORM: CouponForm = {
  code: "", sourceName: "", kind: "Percent", value: "", maxDiscount: "", minOrderTotal: "", maxUses: "",
  oncePerCustomer: true, startDate: "", endDate: "", isActive: true
};

const text = (value: number | null) => (value === null ? "" : String(value));

export function couponForm(coupon: Coupon): CouponForm {
  return {
    code: coupon.code,
    sourceName: coupon.sourceName,
    kind: coupon.discountKind === "Amount" ? "Amount" : "Percent",
    value: String(coupon.discountValue),
    maxDiscount: text(coupon.maxDiscount),
    minOrderTotal: text(coupon.minOrderTotal),
    maxUses: text(coupon.maxUses),
    oncePerCustomer: coupon.oncePerCustomer,
    startDate: coupon.startsAtUtc ? cairoDate(coupon.startsAtUtc) : "",
    endDate: coupon.endsAtUtc ? cairoDate(coupon.endsAtUtc) : "",
    isActive: coupon.isActive
  };
}

const number = (value: string): number | null => (value.trim() === "" ? null : Number(value));

export function toCouponInput(form: CouponForm, rowVersion: number): CouponInput {
  const percent = form.kind === "Percent";
  return {
    code: form.code,
    sourceName: form.sourceName.trim(),
    discountKind: form.kind,
    discountValue: Number(form.value),
    maxDiscount: percent ? number(form.maxDiscount) : null,
    minOrderTotal: number(form.minOrderTotal),
    maxUses: number(form.maxUses),
    oncePerCustomer: form.oncePerCustomer,
    startsAtUtc: form.startDate === "" ? null : cairoDayStartUtc(form.startDate),
    endsAtUtc: form.endDate === "" ? null : cairoDayEndUtc(form.endDate),
    isActive: form.isActive,
    rowVersion
  };
}

/** An end date set before the start date. */
export function endsBeforeStart(form: Pick<CouponForm, "startDate" | "endDate">): boolean {
  return form.startDate !== "" && form.endDate !== "" && form.endDate < form.startDate;
}

const positive = (value: string) => Number.isFinite(Number(value)) && Number(value) > 0;

export function couponValid(form: CouponForm): boolean {
  if (form.code.length < CODE_MIN || form.sourceName.trim() === "" || !positive(form.value)) return false;
  if (form.kind === "Percent" && Number(form.value) > MAX_PERCENT) return false;
  if (form.kind === "Percent" && form.maxDiscount.trim() !== "" && !positive(form.maxDiscount)) return false;
  if (form.minOrderTotal.trim() !== "" && !positive(form.minOrderTotal)) return false;
  if (form.maxUses.trim() !== "" && !(Number.isInteger(Number(form.maxUses)) && Number(form.maxUses) > 0)) return false;
  return !endsBeforeStart(form);
}

export type CouponStatus = "running" | "off" | "notStarted" | "ended";

/** Why a code works or not right now: switched off, not started yet, or ended (its end date or its uses ran out). */
export function couponStatus(coupon: Pick<Coupon, "isRunning" | "isActive" | "startsAtUtc">, now: Date): CouponStatus {
  if (coupon.isRunning) return "running";
  if (!coupon.isActive) return "off";
  if (coupon.startsAtUtc && Date.parse(coupon.startsAtUtc) > now.getTime()) return "notStarted";
  return "ended";
}

/** Only a code nobody used can be deleted; a used one is switched off instead. */
export const canDelete = (coupon: Pick<Coupon, "uses">): boolean => coupon.uses === 0;

/** Best earners first (revenue, then uses); codes without a revenue figure sort by uses. */
export function sortCoupons(coupons: readonly Coupon[]): Coupon[] {
  return [...coupons].sort((a, b) =>
    (b.revenue ?? 0) - (a.revenue ?? 0) || b.uses - a.uses || b.createdAtUtc.localeCompare(a.createdAtUtc));
}

export interface CouponTotals {
  uses: number;
  completedUses: number;
  revenue: number;
  discountGiven: number;
}

export function couponTotals(coupons: readonly Coupon[]): CouponTotals {
  return coupons.reduce<CouponTotals>((sum, coupon) => ({
    uses: sum.uses + coupon.uses,
    completedUses: sum.completedUses + coupon.completedUses,
    revenue: sum.revenue + (coupon.revenue ?? 0),
    discountGiven: sum.discountGiven + (coupon.discountGiven ?? 0)
  }), { uses: 0, completedUses: 0, revenue: 0, discountGiven: 0 });
}
