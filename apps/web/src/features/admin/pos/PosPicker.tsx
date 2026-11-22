"use client";

import { useCallback, useState, type ReactNode } from "react";
import { useSearchParams } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { ChipRadio, FormSection } from "@/features/checkout/form-parts";
import { usePermissions } from "../use-permissions";
import { SearchBox } from "../kit/FilterBar";
import { listBuilds, searchItems } from "./pos-api";
import { buildLine, itemLine, shownPrice, soleMatch, type BuildPick, type CartLine, type ItemPick } from "./pos-rules";
import { PlusIcon, RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

type Kind = "item" | "build";

const condition = (value: string) => (value === "New" ? "New" : "Used");

function Result({ children, action }: { children: ReactNode; action: ReactNode }) {
  return (
    <li className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 rounded-control border border-line p-3">
      <div className="flex min-w-0 flex-col gap-1">{children}</div>
      {action}
    </li>
  );
}

function ItemResults({ query, onAdd }: { query: string; onAdd: (line: CartLine) => void }) {
  const t = useTranslations("admin.pos.picker");
  const locale = useLocale();
  const { has } = usePermissions();
  const fullInventory = has("Inventory.View");
  const { state, reload } = useLoad(useCallback(() => searchItems(query, fullInventory), [query, fullInventory]));
  return (
    <Status state={state} reload={reload} isEmpty={(items) => items.length === 0}>
      {(items: ItemPick[]) => items.map((pick) => (
        <Result
          key={pick.code}
          action={<Button icon={PlusIcon} onClick={() => onAdd(itemLine(pick))}>{t("add")}</Button>}
        >
          <span className="flex flex-wrap items-center gap-2">
            <span dir="ltr" className="font-mono text-[13px] font-bold">{pick.code}</span>
            <span dir="auto" className="text-sm [unicode-bidi:isolate]">{pick.name}</span>
            <ConditionBadge condition={condition(pick.condition)} />
            <span className="text-xs text-ink-muted">{t("available", { count: pick.available })}</span>
          </span>
          <span className="flex flex-wrap gap-x-4 gap-y-0.5 text-xs text-ink-muted">
            {pick.priceOptions.map((option) => (
              <span key={option}>{t(`prices.${option}`)} <b dir="ltr" className="text-ink">{formatEgp(shownPrice(pick, option), locale)}</b></span>
            ))}
          </span>
        </Result>
      ))}
    </Status>
  );
}

function BuildResults({ query, onAdd }: { query: string; onAdd: (line: CartLine) => void }) {
  const t = useTranslations("admin.pos.picker");
  const locale = useLocale();
  const { has } = usePermissions();
  const manage = has("Catalog.ManageProducts");
  const { state, reload } = useLoad(useCallback(() => listBuilds(manage), [manage]));
  const needle = query.toLowerCase();
  const matching = state.status === "ready"
    ? { ...state, data: state.data.filter((pick) => !needle || `${pick.code} ${pick.name}`.toLowerCase().includes(needle)) }
    : state;
  return (
    <Status state={matching} reload={reload} isEmpty={(builds) => builds.length === 0}>
      {(builds: BuildPick[]) => builds.map((pick) => (
        <Result
          key={pick.code}
          action={<Button icon={PlusIcon} onClick={() => onAdd(buildLine(pick))}>{t("add")}</Button>}
        >
          <span className="flex flex-wrap items-center gap-2">
            <span dir="ltr" className="font-mono text-[13px] font-bold">{pick.code}</span>
            <span dir="auto" className="text-sm [unicode-bidi:isolate]">{pick.name}</span>
            {pick.parts ? <span className="text-xs text-ink-muted">{t("parts", { count: pick.parts.length })}</span> : null}
          </span>
          <span className="text-xs text-ink-muted">{t("buildPrice")} <b dir="ltr" className="text-ink">{formatEgp(pick.salePrice, locale)}</b></span>
        </Result>
      ))}
    </Status>
  );
}

interface StatusProps<T> {
  state: { status: "loading" } | { status: "error" } | { status: "ready"; data: T[] };
  reload: () => void;
  isEmpty: (data: T[]) => boolean;
  children: (data: T[]) => ReactNode;
}

function Status<T>({ state, reload, isEmpty, children }: StatusProps<T>) {
  const t = useTranslations("admin.pos.picker");
  if (state.status === "loading") return <p role="status" className="m-0 text-sm text-ink-muted">{t("loading")}</p>;
  if (state.status === "error") {
    return (
      <p role="alert" className="m-0 flex flex-wrap items-center gap-3 text-sm font-bold text-error-text">
        {t("failed")}
        <Button icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
      </p>
    );
  }
  if (isEmpty(state.data)) return <p className="m-0 text-sm text-ink-muted">{t("empty")}</p>;
  return <ul className="m-0 flex max-h-96 list-none flex-col gap-2 overflow-y-auto p-0">{children(state.data)}</ul>;
}

/** "ماذا تبيع؟": search the stock items or pick a build, and add it to the cart. Enter on a typed or scanned code adds that item at once. */
export function PosPicker({ onAdd }: { onAdd: (line: CartLine) => void }) {
  const t = useTranslations("admin.pos.picker");
  const { has } = usePermissions();
  const fullInventory = has("Inventory.View");
  const [kind, setKind] = useState<Kind>("item");
  // `?q=` opens the picker already searching (the inventory page's "Sell" action passes the item code).
  const [query, setQuery] = useState(useSearchParams().get("q") ?? "");

  async function addFromCode(text: string): Promise<boolean> {
    if (kind !== "item" || text === "") return false;
    try {
      const only = soleMatch(await searchItems(text, fullInventory), text);
      if (!only) return false;
      onAdd(itemLine(only));
      return true;
    } catch {
      return false;
    }
  }
  return (
    <FormSection title={t("title")}>
      <div role="radiogroup" aria-label={t("title")} className="flex flex-wrap gap-2">
        {(["item", "build"] as const).map((value) => (
          <ChipRadio key={value} name="pos-kind" label={t(`kinds.${value}`)} selected={kind === value} onSelect={() => setKind(value)} />
        ))}
      </div>
      <SearchBox label={t("search")} value={query} onChange={setQuery} focusKey="pos-items" onEnter={addFromCode} />
      {kind === "item" ? <ItemResults query={query} onAdd={onAdd} /> : <BuildResults query={query} onAdd={onAdd} />}
    </FormSection>
  );
}
