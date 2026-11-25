import { useLocale, useTranslations } from "next-intl";
import { LayersIcon, ShieldIcon } from "@/components/ui/icons";
import { formatEgp } from "@/lib/format";
import { FactLabel } from "./FactLabel";
import { IssueList } from "./IssueList";
import { withoutPowerIssues } from "./issue-text";
import { PowerMeter } from "./PowerMeter";
import { conditionCounts, type Evaluation } from "./builder-summary";

/** Tone of a compatibility result: green when fine, orange for a warning, red for a conflict (the build cannot be ordered). */
const TONE = { ok: "bg-success/12 text-success", warning: "bg-orange-soft text-orange-text", conflict: "bg-error/12 text-error-text" } as const;
const DOT = { ok: "ok", warning: "warn", conflict: "bad" } as const;

interface BuildFactsProps {
  evaluation: Evaluation;
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
}

/**
 * The build's facts in one framed sheet, in the order a buyer checks them: compatibility (a pill with a dot that pulses green, amber or red),
 * the power draw with its bar and advice, how many parts are new and used; then any problem in words, and the total in its own band.
 * The same block in the builder and on a shared build.
 */
export function BuildFacts({ evaluation, slotNames, specNames }: BuildFactsProps) {
  const t = useTranslations("builder.summary");
  const locale = useLocale();
  const conflicts = evaluation.issues.some((issue) => issue.level === "Conflict");
  const status = evaluation.parts.length === 0 ? null : conflicts ? "conflict" : evaluation.issues.length > 0 ? "warning" : "ok";
  const counts = conditionCounts(evaluation.parts);

  return (
    <>
      <div className="flex flex-col divide-y divide-line rounded-card border border-line text-sm">
        <div className="flex items-center justify-between gap-2.5 px-3.5 py-3">
          <FactLabel icon={ShieldIcon}>{t("compatibility")}</FactLabel>
          <span className={`inline-flex items-center gap-2 whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-bold ${status ? TONE[status] : "bg-paper text-ink-muted"}`}>
            <span aria-hidden="true" className="status-dot" data-tone={status ? DOT[status] : "idle"} />
            {status ? t(`status.${status}`) : t("status.none")}
          </span>
        </div>
        <div className="px-3.5 py-3">
          <PowerMeter evaluation={evaluation} />
        </div>
        <div className="flex items-center justify-between gap-2.5 px-3.5 py-3">
          <FactLabel icon={LayersIcon}>{t("conditions")}</FactLabel>
          <span className="flex items-center gap-1.5 text-xs font-bold">
            <span className="rounded-full bg-paper px-2.5 py-1 tabular-nums">{t("newCount", { count: counts.New })}</span>
            <span className="rounded-full bg-paper px-2.5 py-1 tabular-nums">{t("usedCount", { count: counts.Used })}</span>
          </span>
        </div>
      </div>
      <IssueList issues={withoutPowerIssues(evaluation.issues)} slotNames={slotNames} specNames={specNames} />
      <div className="flex items-center justify-between gap-3 rounded-card bg-orange-soft px-4 py-3.5">
        <span className="flex flex-col gap-0.5">
          <span className="text-sm font-bold">{t("total")}</span>
          <span className="type-label">{t("partsCount", { count: evaluation.parts.reduce((sum, part) => sum + part.quantity, 0) })}</span>
        </span>
        <span className="text-[26px] font-bold leading-none tabular-nums">{formatEgp(evaluation.total, locale)}</span>
      </div>
    </>
  );
}
