"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { PERIODS, type Period } from "./dashboard-rules";
import { CheckIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

interface PeriodSwitchProps {
  period: Period;
  from: string;
  to: string;
  onChange: (changes: { period: Period; from?: string; to?: string }) => void;
}

const segment = "h-9 whitespace-nowrap rounded-control px-3.5 text-[13px]";

/** Today / 7 days / this month / this year / a custom range (two dates and "show"). */
export function PeriodSwitch({ period, from, to, onChange }: PeriodSwitchProps) {
  const t = useTranslations("admin.dashboard.period");
  const [draft, setDraft] = useState({ from, to });
  const draftValid = Boolean(draft.from && draft.to && draft.from <= draft.to);

  return (
    <div className="flex flex-wrap items-center gap-2.5">
      <div role="group" aria-label={t("label")} className="inline-flex max-w-full flex-wrap gap-1 rounded-card border border-line bg-surface p-1">
        {PERIODS.map((key) => (
          <button
            key={key} type="button" aria-pressed={period === key}
            onClick={() => onChange(key === "custom" ? { period: key } : { period: key, from: "", to: "" })}
            className={`${segment} ${period === key ? "bg-carbon font-bold text-on-carbon" : "font-medium hover:bg-line"}`}
          >
            {t(key)}
          </button>
        ))}
      </div>
      {period === "custom" ? (
        <form
          className="flex flex-wrap items-end gap-2"
          onSubmit={(event) => {
            event.preventDefault();
            if (draftValid) onChange({ period, ...draft });
          }}
        >
          <label className="flex flex-col gap-1 text-xs font-bold">
            {t("from")}
            <input type="date" value={draft.from} max={draft.to || undefined} onChange={(event) => setDraft({ ...draft, from: event.target.value })} className="h-11 rounded-control border border-input-line bg-surface px-2 text-sm" />
          </label>
          <label className="flex flex-col gap-1 text-xs font-bold">
            {t("to")}
            <input type="date" value={draft.to} min={draft.from || undefined} onChange={(event) => setDraft({ ...draft, to: event.target.value })} className="h-11 rounded-control border border-input-line bg-surface px-2 text-sm" />
          </label>
          <Button type="submit" variant="primary" icon={CheckIcon} disabled={!draftValid}>{t("apply")}</Button>
        </form>
      ) : null}
    </div>
  );
}
