import { useTranslations } from "next-intl";
import { POWER_WARNING_SHARE, powerAdvice, powerFillPercent, type PowerState } from "./builder-summary";

const FILL: Record<PowerState, string> = { ok: "bg-success", near: "bg-orange", over: "bg-error", noSupply: "bg-orange", unknown: "bg-line" };
const TEXT: Record<PowerState, string> = { ok: "text-success", near: "text-orange-text", over: "text-error-text", noSupply: "text-orange-text", unknown: "text-ink-muted" };

/** The build's estimated draw against the power supply: a bar with the headroom mark, the numbers, and what to do when the supply is missing or too small. */
export function PowerMeter({ evaluation }: { evaluation: Parameters<typeof powerAdvice>[0] }) {
  const t = useTranslations("builder.power");
  const advice = powerAdvice(evaluation);
  const shown = advice.estimate === null ? "—" : advice.capacity === null ? t("estimateOnly", { estimate: advice.estimate }) : t("of", { estimate: advice.estimate, capacity: advice.capacity });

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-baseline justify-between gap-2.5">
        <span className="text-ink-muted">{t("label")}</span>
        <span className={`font-bold ${TEXT[advice.state]}`}>{shown}</span>
      </div>
      <div
        role="meter"
        aria-label={t("meter")}
        aria-valuemin={0}
        aria-valuemax={advice.capacity ?? 0}
        aria-valuenow={advice.estimate ?? 0}
        aria-valuetext={shown}
        className="relative h-2 rounded-full bg-paper"
      >
        <div className={`power-fill h-full rounded-full ${FILL[advice.state]}`} style={{ width: `${powerFillPercent(advice)}%` }} />
        {advice.capacity !== null ? <span aria-hidden="true" className="absolute -top-0.5 h-3 w-px bg-ink-muted/60" style={{ insetInlineStart: `${POWER_WARNING_SHARE * 100}%` }} /> : null}
      </div>
      <p role="status" className={`m-0 text-[13px] leading-6 ${TEXT[advice.state]}`}>
        {t(advice.state, { watts: advice.recommended ?? 0 })}
      </p>
    </div>
  );
}
