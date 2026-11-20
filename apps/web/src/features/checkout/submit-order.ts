import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import type { CouponProblemKey } from "@/lib/coupon-problem";
import { badRequestOutcome } from "./checkout-errors";

export type OrderOutcome =
  | { status: "placed"; order: Schemas["CustomerOrderView"] }
  /** 400: the server's field messages (shown as a list). */
  | { status: "invalid"; messages: string[] }
  /** 400: the coupon was refused at order time. */
  | { status: "coupon"; problem: CouponProblemKey }
  /** 409: an item in the cart is gone or sold out. */
  | { status: "gone" }
  /** 401: guest checkout is off and nobody is signed in. */
  | { status: "signIn" }
  | { status: "failed" };

/** Sends the order and says what happened; it never throws. */
export async function submitOrder(request: Schemas["CheckoutRequest"]): Promise<OrderOutcome> {
  try {
    return { status: "placed", order: await required(unwrap(browserApi().POST("/api/v1/orders", { body: request }))) };
  } catch (error) {
    if (!(error instanceof ApiError)) return { status: "failed" };
    if (error.status === 401) return { status: "signIn" };
    if (error.status === 409) return { status: "gone" };
    if (error.status === 400) {
      return badRequestOutcome(error.fieldErrors);
    }
    return { status: "failed" };
  }
}
