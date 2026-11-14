"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { CONDITIONS, type Condition } from "@/lib/condition";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { OptionRow } from "./OptionRow";
import { groupOptions } from "./option-groups";
import { showsInBuildPrice } from "./builder-summary";
import { useOptions } from "./use-options";

const SEARCH_DELAY_MS = 300;

interface OptionPanelProps {
  slot: string;
  slotName: string;
  /** The `parts` URL value of the chosen parts: what the options are checked against. */
  partsKey: string;
  selectedCodes: ReadonlySet<string>;
  /** Slots that already have a part, for the price shown on each choice. */
  filledSlots: readonly string[];
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
  onPick: (code: string) => void;
}

/** The active slot's options: condition tabs, search, models with their جديد / مستعمل choices, and "more". */
export function OptionPanel({ slot, slotName, partsKey, selectedCodes, filledSlots, slotNames, specNames, onPick }: OptionPanelProps) {
  const t = useTranslations("builder.options");
  const tc = useTranslations("common.condition");
  const [condition, setCondition] = useState<Condition | null>(null);
  const [search, setSearch] = useState("");
  const query = useDebouncedValue(search.trim(), SEARCH_DELAY_MS);
  const { options, loading, failed, hasMore, more } = useOptions(slot, partsKey, condition, query);
  const groups = groupOptions(options, selectedCodes, showsInBuildPrice(filledSlots.filter((entry) => entry !== slot), slot));
  const tabs: { value: Condition | null; label: string }[] = [{ value: null, label: t("all") }, ...CONDITIONS.map((value) => ({ value, label: tc(value) }))];

  return (
    <section aria-labelledby="builder-options-title" className="flex min-w-0 flex-col gap-4 rounded-[10px] border border-line bg-surface p-4 md:p-[22px]">
      <div className="flex flex-wrap items-center justify-between gap-2.5">
        <h2 id="builder-options-title" className="m-0 text-[22px] font-bold">{t("title", { slot: slotName })}</h2>
        <div role="group" aria-label={t("conditionTabs")} className="flex gap-1 rounded-card bg-paper p-1">
          {tabs.map((tab) => (
            <button
              key={tab.label}
              type="button"
              aria-pressed={condition === tab.value}
              onClick={() => setCondition(tab.value)}
              className={`h-10 rounded-control px-3.5 text-[13px] ${condition === tab.value ? "bg-carbon text-on-carbon" : ""}`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>
      <span className="rounded-control bg-success/10 px-3 py-2 text-[13px] text-success">{t("hint")}</span>
      <input
        type="search"
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        aria-label={t("search")}
        placeholder={t("search")}
        className="h-11 w-full rounded-control border border-input-line bg-surface px-3 text-sm"
      />
      {failed ? (
        <p role="alert" className="m-0 text-sm text-error">{t("failed")}</p>
      ) : groups.length === 0 && !loading ? (
        <p role="status" className="m-0 py-6 text-center text-ink-muted">{t("empty")}</p>
      ) : (
        <ul aria-busy={loading} className={`m-0 flex list-none flex-col gap-2.5 p-0 ${loading ? "opacity-60" : ""}`}>
          {groups.map((group) => (
            <OptionRow
              key={group.key}
              group={group}
              slotNames={slotNames}
              specNames={specNames}
              onPick={onPick}
            />
          ))}
        </ul>
      )}
      {hasMore ? (
        <button
          type="button"
          onClick={more}
          disabled={loading}
          className="h-11 self-center rounded-control border-[1.5px] border-ink px-6 text-sm font-bold disabled:opacity-60"
        >
          {t("more")}
        </button>
      ) : null}
    </section>
  );
}
