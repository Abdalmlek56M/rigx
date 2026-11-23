"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { Skeleton } from "@/components/ui/Skeleton";
import { Card } from "@/components/ui/Card";
import { fieldClass } from "@/components/ui/field-class";
import { CONDITIONS, type Condition } from "@/lib/condition";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { OptionRow } from "./OptionRow";
import { groupOptions } from "./option-groups";
import { showsInBuildPrice } from "./builder-summary";
import { slotLimit, takesSeveralParts } from "./slot-limits";
import { useOptions } from "./use-options";
import { ChoiceTile } from "@/components/ui/ChoiceTile";
import { conditionIcon } from "@/components/ui/condition-icon";

const SEARCH_DELAY_MS = 300;

interface OptionPanelProps {
  slot: string;
  slotName: string;
  /** The `parts` URL value of the chosen parts: what the options are checked against. */
  partsKey: string;
  selectedCodes: ReadonlySet<string>;
  /** Slots that already have a part, for the price shown on each choice. */
  filledSlots: readonly string[];
  /** A slot that takes several models (storage) holds all it can: nothing more can be added. */
  slotFull: boolean;
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
  onPick: (code: string) => void;
}

/** The active slot's options: condition tabs, search, models with their جديد / مستعمل choices, and "more". */
export function OptionPanel({ slot, slotName, partsKey, selectedCodes, filledSlots, slotFull, slotNames, specNames, onPick }: OptionPanelProps) {
  const t = useTranslations("builder.options");
  const tc = useTranslations("common.condition");
  const [condition, setCondition] = useState<Condition | null>(null);
  const [search, setSearch] = useState("");
  const query = useDebouncedValue(search.trim(), SEARCH_DELAY_MS);
  const { options, loading, failed, hasMore, more } = useOptions(slot, partsKey, condition, query);
  const groups = groupOptions(options, selectedCodes, showsInBuildPrice(filledSlots.filter((entry) => entry !== slot), slot), slotFull);
  const tabs: { value: Condition | null; label: string }[] = [{ value: null, label: t("all") }, ...CONDITIONS.map((value) => ({ value, label: tc(value) }))];

  return (
    <Card as="section" title={t("title", { slot: slotName })} titleId="builder-options-title" padding="md" className="gap-4">
      <div role="group" aria-label={t("conditionTabs")} className="grid grid-cols-3 gap-2">
        {tabs.map((tab) => (
          <ChoiceTile key={tab.label} icon={conditionIcon(tab.value)} label={tab.label} selected={condition === tab.value} onSelect={() => setCondition(tab.value)} />
        ))}
      </div>
      <span className="rounded-control bg-success/10 px-3 py-2 text-xs text-success">{takesSeveralParts(slot) ? t("hintSeveral", { max: slotLimit(slot).units }) : t("hint")}</span>
      <input
        type="search"
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        aria-label={t("search")}
        placeholder={t("search")}
        className={fieldClass}
      />
      {failed ? (
        <p role="alert" className="m-0 text-sm text-error-text">{t("failed")}</p>
      ) : groups.length === 0 && !loading ? (
        <p role="status" className="m-0 py-6 text-center text-ink-muted">{t("empty")}</p>
      ) : (
        <ul aria-busy={loading} className="m-0 flex list-none flex-col gap-2.5 p-0">
          {groups.length === 0 ? Array.from({ length: 3 }, (_, index) => <li key={index}><Skeleton className="h-[84px] w-full rounded-card" /></li>) : null}
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
        <Button type="button" variant="secondary" loading={loading} onClick={more} className="self-center">{t("more")}</Button>
      ) : null}
    </Card>
  );
}
