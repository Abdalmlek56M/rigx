import type { ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { IssueList } from "./IssueList";
import { powerShare, type Evaluation } from "./builder-summary";

/** Tone of a compatibility result or of the power use: green when fine, orange for a warning, red for a conflict. */
const TONE = { ok: "text-success", warning: "text-orange-text", conflict: "text-error" } as const;
const BAR = { ok: "bg-success", warning: "bg-orange", conflict: "bg-error" } as const;

interface BuildFactsProps {
  evaluation: Evaluation;
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
  /** More label / value rows under the power row (the builder adds new / used). */
  extraRows?: ReactNode;
}

/** The build's compatibility, estimated power (with a bar against the power supply), problems and total: the same block in the builder and on a shared build. */
export function BuildFacts({ evaluation, slotNames, specNames, extraRows }: BuildFactsProps) {
  const t = useTranslations("builder.summary");
  const locale = useLocale();
  const conflicts = evaluation.issues.some((issue) => issue.level === "Conflict");
  const status = evaluation.parts.length === 0 ? null : conflicts ? "conflict" : evaluation.issues.length > 0 ? "warning" : "ok";
  const share = powerShare(evaluation);
  const power = share === null ? null : share > 1 ? "conflict" : share > 0.8 ? "warning" : "ok";

  return (
    <>
      <div className="flex flex-col gap-2.5 text-sm">
        <div className="flex justify-between gap-2.5">
          <span className="text-ink-muted">{t("compatibility")}</span>
          <span className={`font-bold ${status ? TONE[status] : ""}`}>{status ? t(`status.${status}`) : "—"}</span>
        </div>
        <div className="flex flex-col gap-1.5">
          <div className="flex justify-between gap-2.5">
            <span className="text-ink-muted">{t("power")}</span>
            <span dir="ltr" className={`font-mono ${power ? TONE[power] : ""}`}>
              {evaluation.powerEstimateWatts != null ? `~ ${Math.round(evaluation.powerEstimateWatts)}W` : "—"}
              {evaluation.psuCapacityWatts != null ? ` / ${Math.round(evaluation.psuCapacityWatts)}W` : ""}
            </span>
          </div>
          {share !== null && power ? (
            <div role="presentation" className="h-1.5 overflow-hidden rounded-full bg-paper">
              <div className={`h-full rounded-full transition-[width] duration-500 motion-reduce:transition-none ${BAR[power]}`} style={{ width: `${Math.min(100, Math.round(share * 100))}%` }} />
            </div>
          ) : null}
        </div>
        {extraRows}
      </div>
      <IssueList issues={evaluation.issues} slotNames={slotNames} specNames={specNames} />
      <div className="flex items-baseline justify-between border-t border-dashed border-line pt-3.5">
        <span>{t("total")}</span>
        <span className="text-2xl font-bold">{formatEgp(evaluation.total, locale)}</span>
      </div>
    </>
  );
}
