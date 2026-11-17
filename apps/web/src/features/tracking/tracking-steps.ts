/** The statuses a customer can see (Completed is internal and reads as Delivered). Labels live in the `orderStatus` messages. */
export const ORDER_STATUSES = ["New", "Confirmed", "Preparing", "Shipped", "Delivered", "Cancelled"] as const;
export type OrderStatusKey = (typeof ORDER_STATUSES)[number];

export type StepState = "done" | "current" | "upcoming";

export interface TrackingStep {
  status: OrderStatusKey;
  state: StepState;
  /** When the order reached this step (ISO), or `null` if it has not yet. */
  at: string | null;
}

const DELIVERY_STEPS: readonly OrderStatusKey[] = ["New", "Confirmed", "Preparing", "Shipped", "Delivered"];
const PICKUP_STEPS: readonly OrderStatusKey[] = ["New", "Confirmed", "Preparing", "Delivered"];

/** The API status as one of the customer-visible ones; anything unknown reads as the first step. */
export function orderStatusKey(status: string): OrderStatusKey {
  if (status === "Completed") return "Delivered";
  return ORDER_STATUSES.find((known) => known === status) ?? "New";
}

/**
 * The timeline to draw: five steps for delivery, four for pickup (no Shipped), each done / current / upcoming;
 * a cancelled order shows only the cancelled state. `timeline` supplies the date-times.
 */
export function trackingSteps(status: string, fulfillmentMethod: string, timeline: readonly { status: string; at: string }[]): TrackingStep[] {
  const stamp = (step: OrderStatusKey) => timeline.find((entry) => orderStatusKey(entry.status) === step)?.at ?? null;
  const current = orderStatusKey(status);
  if (current === "Cancelled") return [{ status: "Cancelled", state: "current", at: stamp("Cancelled") }];

  const sequence = fulfillmentMethod === "Pickup" ? PICKUP_STEPS : DELIVERY_STEPS;
  const found = sequence.indexOf(current);
  // A status outside this sequence (Shipped on a pickup order) counts as the last step that has a date.
  const position = found >= 0 ? found : Math.max(0, sequence.findLastIndex((step) => stamp(step) !== null));
  const finished = sequence[position] === "Delivered";

  return sequence.map((step, index) => ({
    status: step,
    state: index < position || (index === position && finished) ? "done" : index === position ? "current" : "upcoming",
    at: stamp(step)
  }));
}
