import { useLocale, useTranslations } from "next-intl";
import { formatDateTime } from "@/lib/format";
import type { TrackingStep } from "./tracking-steps";

const DOT: Record<TrackingStep["state"], string> = {
  done: "border-success bg-success text-surface",
  current: "border-orange bg-orange text-on-orange",
  upcoming: "border-input-line bg-surface text-transparent"
};

/** The order's steps, top to bottom, with the date-time each was reached (Cairo time). */
export function TrackingTimeline({ steps }: { steps: TrackingStep[] }) {
  const t = useTranslations("orderStatus");
  const locale = useLocale();

  return (
    <ol className="m-0 flex list-none flex-col p-0">
      {steps.map((step, index) => (
        <li key={step.status} aria-current={step.state === "current" ? "step" : undefined} className="flex gap-3.5">
          <span className="flex flex-col items-center">
            <span
              aria-hidden="true"
              className={`flex h-[26px] w-[26px] shrink-0 items-center justify-center rounded-full border-2 text-xs font-bold ${step.status === "Cancelled" ? "border-error bg-error text-surface" : DOT[step.state]}`}
            >
              {step.state === "done" ? "✓" : step.status === "Cancelled" ? "✕" : "●"}
            </span>
            {index < steps.length - 1 ? <span aria-hidden="true" className={`w-0.5 flex-1 ${step.state === "done" ? "bg-success" : "bg-line"}`} /> : null}
          </span>
          <span className={`flex flex-col pb-5 ${step.state === "upcoming" ? "text-ink-muted" : ""}`}>
            <b className="text-[15px]">{t(step.status)}</b>
            {step.at ? <span dir="ltr" className="text-start font-mono text-xs text-ink-muted">{formatDateTime(step.at, locale)}</span> : null}
          </span>
        </li>
      ))}
    </ol>
  );
}
