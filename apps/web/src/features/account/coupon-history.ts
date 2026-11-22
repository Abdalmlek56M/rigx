interface CouponUseLike {
  discount: number;
  orderStatus: string;
}

/** What the coupons took off in total; a cancelled order's coupon is not counted (the customer never paid for it). */
export function totalSaved(uses: readonly CouponUseLike[]): number {
  return uses.filter((use) => use.orderStatus !== "Cancelled").reduce((sum, use) => sum + use.discount, 0);
}
