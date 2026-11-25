"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDate, formatEgp } from "@/lib/format";
import { actionsColumn, DataTable, type Column } from "../kit/DataTable";
import { PartnersFailed } from "../stock/PartnersFailed";
import { partnerShare } from "../stock/stock-fields";
import { loadedPartners, type PartnerSource } from "../stock/use-partners";
import { UnitPartnerDialog } from "./UnitPartnerDialog";
import { setUnitExcluded, type StockItem, type StockUnit } from "./inventory-api";
import { leftOutOfStockValue, unitStatusKey } from "./inventory-rules";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { BanIcon, CheckIcon, SwapIcon } from "@/components/ui/icons";
import { IconButton } from "../kit/Button";
import { useToast } from "../kit/Toast";

const READY = (units: readonly StockUnit[]) => ({ status: "ready", data: units }) as const;

/** The item's units: status, serial and received date; the cost (when the server sent it) and the partner with their share; the unit's actions. */
export function StockUnits({ item, partners, canAssign, canEditStockValue, showCosts, onChanged, onStale }: {
  item: StockItem;
  /** `null` when the staff member cannot read partners. */
  partners: PartnerSource | null;
  canAssign: boolean;
  /** May leave a unit out of the stock value (sees costs and may adjust stock). */
  canEditStockValue: boolean;
  showCosts: boolean;
  onChanged: () => Promise<void>;
  onStale: () => void;
}) {
  const t = useTranslations("admin.inventory.units");
  const locale = useLocale();
  const toast = useToast();
  const [editing, setEditing] = useState<StockUnit | null>(null);
  const [togglingId, setTogglingId] = useState<string | null>(null);
  const none = <span className="text-ink-muted">{t("none")}</span>;

  /** Leaves the unit out of the stock value or counts it again; the unit stays in stock and sellable. */
  async function toggleExcluded(unit: StockUnit) {
    setTogglingId(unit.id);
    try {
      await setUnitExcluded(unit.id, unit.excludedFromStockValue !== true);
      await onChanged();
      toast(t("stockValueDone"));
    } catch {
      toast(t("stockValueFailed"), "error");
    } finally {
      setTogglingId(null);
    }
  }

  // Status, serial and date share one column, so the table fits a narrow screen without scrolling sideways.
  const columns: Column<StockUnit>[] = [
    {
      key: "unit",
      header: t("columns.unit"),
      cell: (unit) => (
        <span className="flex min-w-0 flex-col items-start gap-1">
          <span className="flex flex-wrap items-center gap-1.5">
            <span className="whitespace-nowrap font-bold">{t(`status.${unitStatusKey(unit.status)}`)}</span>
            {leftOutOfStockValue(item, unit) ? <StatusBadge tone="neutral">{t("excludedBadge")}</StatusBadge> : null}
          </span>
          {unit.serialNumber ? (
            <span className="text-xs text-ink-muted">{t("columns.serial")} <bdi dir="ltr" className="break-all font-mono text-ink">{unit.serialNumber}</bdi></span>
          ) : null}
          <span className="text-xs text-ink-muted">{t("received", { date: formatDate(unit.receivedAtUtc, locale) })}</span>
        </span>
      )
    }
  ];
  // The cost (for staff who see it) with the partner under it.
  columns.push({
    key: "purchase",
    header: t(showCosts ? "columns.purchase" : "columns.partner"),
    cell: (unit) => {
      const partner = loadedPartners(partners).find((entry) => entry.id === unit.partnerId);
      const cost = showCosts && unit.acquisitionCost !== null ? unit.acquisitionCost : null;
      if (cost === null && !partner) return none;
      return (
        <span className="flex min-w-0 flex-col items-start gap-0.5">
          {cost === null ? null : <span className="whitespace-nowrap font-bold">{formatEgp(cost, locale)}</span>}
          {partner ? <span className="text-xs text-ink-muted">{t("partnerShare", { name: partner.name, percent: partnerShare(partner, unit.partnerPercentOverride) })}</span> : null}
        </span>
      );
    }
  });
  if (canAssign || canEditStockValue) {
    columns.push(actionsColumn((unit) => (
      <span className="flex items-center gap-1">
        {canAssign && partners ? <IconButton icon={SwapIcon} size="sm" label={t("changePartner")} onClick={() => setEditing(unit)} /> : null}
        {canEditStockValue ? (
          <IconButton
            icon={unit.excludedFromStockValue === true ? CheckIcon : BanIcon} size="sm" loading={togglingId === unit.id}
            label={t(unit.excludedFromStockValue === true ? "countInStockValue" : "excludeFromStockValue")} onClick={() => void toggleExcluded(unit)}
          />
        ) : null}
      </span>
    )));
  }

  return (
    <div className="flex min-w-0 flex-col gap-2">
      <h3 className="m-0 text-[15px] font-bold">{t("title", { count: item.units.length })}</h3>
      {partners?.state.status === "error" ? <PartnersFailed onRetry={partners.reload} /> : null}
      <DataTable label={t("title", { count: item.units.length })} columns={columns} state={READY(item.units)} onRetry={onStale} rowKey={(unit) => unit.id} empty={t("empty")} />
      {editing && partners ? (
        <UnitPartnerDialog key={editing.id} unit={editing} partners={partners} onSaved={onChanged} onStale={onStale} onClose={() => setEditing(null)} />
      ) : null}
    </div>
  );
}
