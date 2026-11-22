import { useTranslations } from "next-intl";
import { CheckIcon, ChevronStartIcon } from "@/components/ui/icons";

const STEPS = ["cart", "details", "confirmation"] as const;

/** السلة › البيانات والدفع › التأكيد (done steps get a check): `current` is the 1-based step the page is on. On a phone only the current step keeps its name, so the row never wraps (English names are long). */
export function CheckoutSteps({ current }: { current: 2 | 3 }) {
  const t = useTranslations("checkout.steps");

  return (
    <ol className="m-0 flex list-none items-center gap-x-3 p-0 text-sm text-ink-muted">
      {STEPS.map((step, index) => {
        const number = index + 1;
        const done = number < current;
        return (
          <li key={step} aria-current={number === current ? "step" : undefined} className={`flex items-center gap-3 ${number === current ? "font-bold text-ink" : ""}`}>
            {index > 0 ? <span aria-hidden="true" className="flex ltr:-scale-x-100"><ChevronStartIcon size={14} /></span> : null}
            <span className="flex items-center">
              <span className="me-1.5 font-mono">{number}</span>
              <span className={number === current ? "" : "max-sm:sr-only"}>{t(step)}</span>
              {done ? <span aria-hidden="true" className="ms-1.5 flex text-success"><CheckIcon size={14} /></span> : null}
            </span>
          </li>
        );
      })}
    </ol>
  );
}
