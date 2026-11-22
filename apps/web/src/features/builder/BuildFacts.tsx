import type { ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { IssueList } from "./IssueList";
import { withoutPowerIssues } from "./issue-text";
import { PowerMeter } from "./PowerMeter";
import type { Evaluation } from "./builder-summary";

/** Tone of a compatibility result: green when fine, orange for a warning, red for a conflict (the build cannot be ordered). */
const TONE = { ok: "text-success", warning: "text-orange-text", conflict: "text-error-text" } as const;
const DOT = { ok: "ok", warning: "warn", conflict: "bad" } as const;

interface BuildFactsProps {
  evaluation: Evaluation;
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
  /** More label / value rows under the power meter (the builder adds new / used). */
  extraRows?: ReactNode;
}

/** The build's compatibility (a dot that pulses green, amber or red), the power meter with its advice, problems and total: the same block in the builder and on a shared build. */
export function BuildFacts({ evaluation, slotNames, specNames, extraRows }: BuildFactsProps) {
  const t = useTranslations("builder.summary");
  const locale = useLocale();
  const conflicts = evaluation.issues.some((issue) => issue.level === "Conflict");
  const status = evaluation.parts.length === 0 ? null : conflicts ? "conflict" : evaluation.issues.length > 0 ? "warning" : "ok";

  return (
    <>
      <div className="flex flex-col gap-3 text-sm">
        <div className="flex items-center justify-between gap-2.5">
          <span className="text-ink-muted">{t("compatibility")}</span>
          <span className={`flex items-center gap-2 font-bold ${status ? TONE[status] : ""}`}>
            <span aria-hidden="true" className="status-dot" data-tone={status ? DOT[status] : "idle"} />
            {status ? t(`status.${status}`) : "—"}
          </span>
        </div>
        <PowerMeter evaluation={evaluation} />
        {extraRows}
      </div>
      <IssueList issues={withoutPowerIssues(evaluation.issues)} slotNames={slotNames} specNames={specNames} />
      <div className="flex items-baseline justify-between border-t border-dashed border-line pt-3.5">
        <span>{t("total")}</span>
        <span className="text-2xl font-bold">{formatEgp(evaluation.total, locale)}</span>
      </div>
    </>
  );
}
