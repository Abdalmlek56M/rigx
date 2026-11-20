import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type CouponCheck = Schemas["CouponCheckView"];

/** Asks the server whether the code works for this order total (and phone, for "once per customer"). Rate-limited: call it only when the customer presses apply. */
export const checkCoupon = (code: string, orderTotal: number, phone: string | null): Promise<CouponCheck> =>
  required(unwrap(browserApi().POST("/api/v1/store/coupons/check", { body: { code, orderTotal, phone } })));
