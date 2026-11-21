import { useTranslations } from "next-intl";
import { trackingSteps, type TrackingStep } from "@/features/tracking/tracking-steps";
import type { Order } from "./account-api";

const BAR: Record<TrackingStep["state"], string> = { done: "surface-strong", current: "bg-orange", upcoming: "bg-line" };

/** A compact progress strip for an order card: one bar per step and the name of the step it is at. */
export function OrderProgress({ order }: { order: Pick<Order, "status" | "fulfillmentMethod" | "timeline"> }) {
  const t = useTranslations("orderStatus");
  const steps = trackingSteps(order.status, order.fulfillmentMethod, order.timeline);
  const current = steps.find((step) => step.state === "current") ?? steps[steps.length - 1];

  if (!current) return null;

  return (
    <div className="flex flex-col gap-1.5">
      <div aria-hidden="true" className="flex gap-1">
        {steps.map((step) => (
          <span key={step.status} className={`h-1.5 flex-1 rounded-full ${step.status === "Cancelled" ? "bg-error" : BAR[step.state]}`} />
        ))}
      </div>
      <span className="text-sm font-bold">{t(current.status)}</span>
    </div>
  );
}
