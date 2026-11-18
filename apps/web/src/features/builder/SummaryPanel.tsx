"use client";

import { useState, type ComponentProps } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { IssueList } from "./IssueList";
import { SummaryActions } from "./SummaryActions";
import { conditionCounts, powerShare, type Evaluation } from "./builder-summary";

type ActionProps = Omit<ComponentProps<typeof SummaryActions>, "evaluation" | "checking" | "slotNames">;

interface SummaryPanelProps extends ActionProps {
  evaluation: Evaluation;
  checking: boolean;
  checkFailed: boolean;
  slotCount: number;
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
}

const label = "text-[#A9ABB0]";

/** "تجميعتك": count, compatibility, power, new / used, the total and the actions. On mobile it is a bottom bar that expands. */
export function SummaryPanel({ evaluation, checking, checkFailed, slotCount, slotNames, specNames, ...actions }: SummaryPanelProps) {
  const t = useTranslations("builder.summary");
  const locale = useLocale();
  const [expanded, setExpanded] = useState(false);
  const counts = conditionCounts(evaluation.parts);
  const conflicts = evaluation.issues.some((issue) => issue.level === "Conflict");
  const warnings = evaluation.issues.length > 0 && !conflicts;
  const share = powerShare(evaluation);
  const total = formatEgp(evaluation.total, locale);
  const status = evaluation.parts.length === 0 ? null : conflicts ? "conflict" : warnings ? "warning" : "ok";

  return (
    <aside
      aria-label={t("label")}
      aria-busy={checking}
      className="fixed inset-x-0 bottom-0 z-40 flex max-h-[85dvh] flex-col gap-4 overflow-y-auto rounded-t-[14px] bg-carbon p-4 pb-[max(1rem,env(safe-area-inset-bottom))] text-on-carbon lg:static lg:z-auto lg:max-h-none lg:rounded-[10px] lg:p-[22px]"
    >
      <div className="flex items-center justify-between gap-3">
        <span className="text-lg font-bold">{t("title")}</span>
        <span dir="ltr" className={`font-mono text-[13px] ${label}`}>{evaluation.parts.length} / {slotCount}</span>
        <span className="ms-auto text-xl font-bold lg:hidden">{total}</span>
        <button
          type="button"
          aria-expanded={expanded}
          onClick={() => setExpanded(!expanded)}
          className="h-11 rounded-control border border-header-line px-3 text-[13px] lg:hidden"
        >
          {expanded ? t("hide") : t("show")}
        </button>
      </div>
      <div className={`${expanded ? "flex" : "hidden"} flex-col gap-4 lg:flex`}>
        {checkFailed ? <p role="alert" className="m-0 text-sm text-[#FF8A7A]">{t("checkFailed")}</p> : null}
        <div className="flex flex-col gap-2 text-sm">
          <div className="flex justify-between gap-2.5">
            <span className={label}>{t("compatibility")}</span>
            <span className={`font-bold ${status === "conflict" ? "text-[#FF8A7A]" : status === "warning" ? "text-[#FFC94D]" : "text-[#3FD08A]"}`}>
              {status ? t(`status.${status}`) : "—"}
            </span>
          </div>
          <div className="flex justify-between gap-2.5">
            <span className={label}>{t("power")}</span>
            <span dir="ltr" className={`font-mono ${share !== null && share > 1 ? "text-[#FF8A7A]" : share !== null && share > 0.8 ? "text-[#FFC94D]" : ""}`}>
              {evaluation.powerEstimateWatts != null ? `~ ${Math.round(evaluation.powerEstimateWatts)}W` : "—"}
              {evaluation.psuCapacityWatts != null ? ` / ${Math.round(evaluation.psuCapacityWatts)}W` : ""}
            </span>
          </div>
          <div className="flex justify-between gap-2.5">
            <span className={label}>{t("conditions")}</span>
            <span>{t("counts", { newCount: counts.New, usedCount: counts.Used })}</span>
          </div>
        </div>
        <IssueList issues={evaluation.issues} slotNames={slotNames} specNames={specNames} />
        <div className="flex items-baseline justify-between border-t border-dashed border-header-line pt-3.5">
          <span>{t("total")}</span>
          <span className="text-2xl font-bold">{total}</span>
        </div>
        <SummaryActions evaluation={evaluation} checking={checking} slotNames={slotNames} {...actions} />
      </div>
    </aside>
  );
}
