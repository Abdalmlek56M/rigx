"use client";

import { useState, type ComponentProps } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { formatEgp } from "@/lib/format";
import { BuildFacts } from "./BuildFacts";
import { SummaryActions } from "./SummaryActions";
import { conditionCounts, type Evaluation } from "./builder-summary";

type ActionProps = Omit<ComponentProps<typeof SummaryActions>, "evaluation" | "checking" | "slotNames">;

interface SummaryPanelProps extends ActionProps {
  evaluation: Evaluation;
  checking: boolean;
  checkFailed: boolean;
  slotCount: number;
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
}

/** "تجميعتك": count, compatibility, power, new / used, the total and the actions. On mobile it is a bottom bar that expands. */
export function SummaryPanel({ evaluation, checking, checkFailed, slotCount, slotNames, specNames, ...actions }: SummaryPanelProps) {
  const t = useTranslations("builder.summary");
  const locale = useLocale();
  const [expanded, setExpanded] = useState(false);
  const counts = conditionCounts(evaluation.parts);

  return (
    <aside
      aria-label={t("label")}
      aria-busy={checking}
      className="fixed inset-x-0 bottom-0 z-40 flex max-h-[85dvh] flex-col gap-4 overflow-y-auto rounded-t-[14px] border-t border-line bg-surface p-4 pb-[max(1rem,env(safe-area-inset-bottom))] shadow-lift lg:static lg:z-auto lg:max-h-none lg:rounded-card lg:border lg:p-[22px] lg:shadow-soft"
    >
      <div className="flex items-center justify-between gap-3">
        <span className="text-lg font-bold">{t("title")}</span>
        <span dir="ltr" className="font-mono text-[13px] text-ink-muted">{evaluation.parts.length} / {slotCount}</span>
        <span className="ms-auto text-xl font-bold lg:hidden">{formatEgp(evaluation.total, locale)}</span>
        <Button
          type="button"
          variant="secondary"
          size="sm"
          aria-expanded={expanded}
          onClick={() => setExpanded(!expanded)}
          className="lg:hidden"
        >
          {expanded ? t("hide") : t("show")}
        </Button>
      </div>
      <div className={`${expanded ? "flex" : "hidden"} flex-col gap-4 lg:flex`}>
        {checkFailed ? <p role="alert" className="m-0 text-sm text-error-text">{t("checkFailed")}</p> : null}
        <BuildFacts
          evaluation={evaluation}
          slotNames={slotNames}
          specNames={specNames}
          extraRows={
            <div className="flex justify-between gap-2.5">
              <span className="text-ink-muted">{t("conditions")}</span>
              <span>{t("counts", { newCount: counts.New, usedCount: counts.Used })}</span>
            </div>
          }
        />
        <SummaryActions evaluation={evaluation} checking={checking} slotNames={slotNames} {...actions} />
      </div>
    </aside>
  );
}
