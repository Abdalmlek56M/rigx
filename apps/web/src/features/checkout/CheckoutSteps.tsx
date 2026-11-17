import { useTranslations } from "next-intl";

const STEPS = ["cart", "details", "confirmation"] as const;

/** السلة ✓ › البيانات والدفع › التأكيد: `current` is the 1-based step the page is on. */
export function CheckoutSteps({ current }: { current: 2 | 3 }) {
  const t = useTranslations("checkout.steps");

  return (
    <ol className="m-0 flex list-none flex-wrap items-center gap-x-3 gap-y-1 p-0 text-sm text-ink-muted">
      {STEPS.map((step, index) => {
        const number = index + 1;
        const done = number < current;
        return (
          <li key={step} aria-current={number === current ? "step" : undefined} className={`flex items-center gap-3 ${number === current ? "font-bold text-ink" : ""}`}>
            {index > 0 ? <span aria-hidden="true">›</span> : null}
            <span>
              <span className="me-1.5 font-mono">{number}</span>
              {t(step)}
              {done ? <span aria-hidden="true"> ✓</span> : null}
            </span>
          </li>
        );
      })}
    </ol>
  );
}
