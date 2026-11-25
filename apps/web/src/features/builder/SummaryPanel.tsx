"use client";

import { useState, type ComponentProps } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { formatEgp } from "@/lib/format";
import { BuildFacts } from "./BuildFacts";
import { SummaryActions } from "./SummaryActions";
import type { Evaluation } from "./builder-summary";

type ActionProps = Omit<ComponentProps<typeof SummaryActions>, "evaluation" | "checking" | "slotNames">;

interface SummaryPanelProps extends ActionProps {
  evaluation: Evaluation;
  checking: boolean;
  checkFailed: boolean;
  slotCount: number;
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
}

/**
 * "تجميعتك": the title with how many slots are filled (a segment per slot), the build's facts and total, then the actions and the share box.
 * On a phone it is a bottom bar with the total that expands.
 */
export function SummaryPanel({ evaluation, checking, checkFailed, slotCount, slotNames, specNames, ...actions }: SummaryPanelProps) {
  const t = useTranslations("builder.summary");
  const locale = useLocale();
  const [expanded, setExpanded] = useState(false);
  const filled = new Set(evaluation.parts.map((part) => part.slot)).size;

  return (
    <aside
      aria-label={t("label")}
      aria-busy={checking}
      data-sticky-bar="lg"
      className="fixed inset-x-0 bottom-0 z-40 flex max-h-[85dvh] flex-col gap-4 overflow-y-auto rounded-t-[14px] border-t border-line bg-surface p-4 pb-[max(1rem,env(safe-area-inset-bottom))] shadow-lift lg:static lg:z-auto lg:max-h-none lg:rounded-card lg:border lg:p-5 lg:shadow-soft"
    >
      <div className="flex flex-col gap-2.5">
        <div className="flex items-center justify-between gap-3">
          <h2 className="type-section-title m-0">{t("title")}</h2>
          <span className="type-label whitespace-nowrap max-lg:hidden">{t("filled", { count: filled, total: slotCount })}</span>
          <span className="ms-auto text-xl font-bold tabular-nums lg:hidden">{formatEgp(evaluation.total, locale)}</span>
          <Button type="button" variant="secondary" size="sm" aria-expanded={expanded} onClick={() => setExpanded(!expanded)} className="lg:hidden">
            {expanded ? t("hide") : t("show")}
          </Button>
        </div>
        <div aria-hidden="true" className="flex gap-1">
          {Array.from({ length: slotCount }, (_, index) => (
            <span key={index} className={`h-1.5 flex-1 rounded-full transition-colors duration-300 motion-reduce:transition-none ${index < filled ? "bg-orange" : "bg-line"}`} />
          ))}
        </div>
      </div>
      <div className={`${expanded ? "flex" : "hidden"} flex-col gap-4 lg:flex`}>
        {checkFailed ? <p role="alert" className="m-0 text-sm text-error-text">{t("checkFailed")}</p> : null}
        <BuildFacts evaluation={evaluation} slotNames={slotNames} specNames={specNames} />
        <SummaryActions evaluation={evaluation} checking={checking} slotNames={slotNames} {...actions} />
      </div>
    </aside>
  );
}
