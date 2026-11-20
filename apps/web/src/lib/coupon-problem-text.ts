"use client";

import { useTranslations } from "next-intl";
import { couponProblemKey } from "./coupon-problem";

/** The customer-facing sentence for a coupon problem code (never the raw code). */
export function useCouponProblemText(): (problem: string) => string {
  const t = useTranslations("coupon.problems");
  return (problem) => t(couponProblemKey(problem));
}
