/** Message keys for the order values the server sends as free strings; anything unknown has no key and is shown as it is. Pure, so it is tested. */
import type { OrderStatus } from "./order-rules";

function known<T extends string>(list: readonly T[], value: string): T | null {
  return list.find((item) => item === value) ?? null;
}

const METHODS = ["Cash", "CashOnDelivery", "InstaPay", "Wallet", "BankTransfer", "Split"] as const;
const FULFILLMENTS = ["Delivery", "Pickup"] as const;
const USAGES = ["Gaming", "StudyWork", "Editing", "Streaming", "Upgrade", "Other"] as const;
const FORWARD_STEPS = ["Confirmed", "Preparing", "Shipped", "Delivered"] as const satisfies readonly OrderStatus[];

export const methodKey = (method: string) => known(METHODS, method);
export const fulfillmentKey = (method: string) => known(FULFILLMENTS, method);
export const usageKey = (usage: string) => known(USAGES, usage);

/** The status a "move forward" button leads to (the button's label key); Completed and Cancelled have their own actions. */
export const forwardKey = (status: OrderStatus | null) => (status ? known(FORWARD_STEPS, status) : null);
