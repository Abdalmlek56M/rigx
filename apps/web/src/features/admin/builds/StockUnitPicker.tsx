"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { SearchBox } from "../kit/FilterBar";
import { fetchStockItems, type StockUnit } from "../inventory/inventory-api";
import { availableUnits } from "../inventory/inventory-rules";
import { RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/**
 * Free stock to pick one unit from: search the in-stock items, then choose one of the item's available units.
 * `categoryId` narrows the search (a replacement is usually the same kind of part); `pickedId` marks the chosen unit.
 */
export function StockUnitPicker({ categoryId, pickedId, actionLabel, onPick }: {
  categoryId?: string;
  pickedId: string;
  actionLabel: string;
  onPick: (unit: StockUnit) => void;
}) {
  const t = useTranslations("admin.builds.picker");
  const locale = useLocale();
  const [search, setSearch] = useState("");
  const { state, reload } = useLoad(useCallback(
    () => fetchStockItems({ search: search || undefined, categoryId, inStock: true, page: 1 }),
    [search, categoryId]
  ));

  return (
    <div className="flex min-w-0 flex-col gap-2">
      <SearchBox label={t("search")} value={search} onChange={setSearch} />
      {state.status === "loading" ? <div role="status" aria-label={t("loading")} className="h-24 animate-pulse rounded-control bg-line" /> : null}
      {state.status === "error" ? (
        <div role="alert" className="flex flex-wrap items-center gap-3">
          <span className="font-bold">{t("failed")}</span>
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
        </div>
      ) : null}
      {state.status === "ready" ? (
        <>
          {state.data.items.length === 0 ? <p className="m-0 py-4 text-center text-sm font-bold text-ink-muted">{t("empty")}</p> : null}
          <ul className="m-0 flex max-h-80 list-none flex-col gap-2 overflow-y-auto p-0">
            {state.data.items.map((item) => (
              <li key={item.id} className="flex flex-col gap-1.5 rounded-control border border-line bg-paper p-2.5">
                <span className="flex flex-wrap items-center gap-2">
                  <span dir="ltr" className="font-mono text-xs font-bold">{item.code}</span>
                  <span dir="auto" className="min-w-0 text-sm font-bold [unicode-bidi:isolate]">{item.modelName}</span>
                  <ConditionBadge condition={item.condition === "New" ? "New" : "Used"} />
                </span>
                <ul className="m-0 flex list-none flex-col gap-1 p-0">
                  {availableUnits(item).map((unit) => (
                    <li key={unit.id} className="flex flex-wrap items-center justify-between gap-2 text-xs">
                      <span className="flex flex-wrap items-center gap-x-3 text-ink-muted">
                        {unit.serialNumber ? <span dir="ltr" className="font-mono">{unit.serialNumber}</span> : <span>{t("noSerial")}</span>}
                        {unit.acquisitionCost === null ? null : <span>{t("cost", { amount: formatEgp(unit.acquisitionCost, locale) })}</span>}
                        <span>{t("price", { amount: formatEgp(item.prices.inBuild, locale) })}</span>
                      </span>
                      <button
                        type="button" aria-pressed={pickedId === unit.id} onClick={() => onPick(unit)}
                        className={`h-10 rounded-control border px-4 text-sm font-bold ${pickedId === unit.id ? "border-carbon bg-carbon text-on-carbon" : "border-input-line bg-surface"}`}
                      >
                        {pickedId === unit.id ? t("picked") : actionLabel}
                      </button>
                    </li>
                  ))}
                </ul>
              </li>
            ))}
          </ul>
          {state.data.total > state.data.items.length ? <p className="m-0 text-xs text-ink-muted">{t("narrow", { shown: state.data.items.length, total: state.data.total })}</p> : null}
        </>
      ) : null}
    </div>
  );
}
