"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDate, formatEgp } from "@/lib/format";
import type { Partner } from "../expenses/expenses-api";
import { DataTable, type Column } from "../kit/DataTable";
import { financingPartners, partnerShare } from "../stock/stock-fields";
import { UnitPartnerDialog } from "./UnitPartnerDialog";
import { setUnitExcluded, type StockItem, type StockUnit } from "./inventory-api";
import { leftOutOfStockValue, unitStatusKey } from "./inventory-rules";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { BanIcon, CheckIcon, SwapIcon } from "@/components/ui/icons";
import { IconButton } from "../kit/Button";
import { useToast } from "../kit/Toast";

const READY = (units: readonly StockUnit[]) => ({ status: "ready", data: units }) as const;

/** The item's units: status, serial, cost (when the server sent it), partner with share, received date, and "change partner". */
export function StockUnits({ item, partners, canAssign, canEditStockValue, showCosts, onChanged, onStale }: {
  item: StockItem;
  partners: readonly Partner[];
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

  const columns: Column<StockUnit>[] = [
    {
      key: "status",
      header: t("columns.status"),
      cell: (unit) => (
        <span className="flex flex-wrap items-center gap-1.5">
          <span className="whitespace-nowrap">{t(`status.${unitStatusKey(unit.status)}`)}</span>
          {leftOutOfStockValue(item, unit) ? <StatusBadge tone="neutral">{t("excludedBadge")}</StatusBadge> : null}
        </span>
      )
    },
    { key: "serial", header: t("columns.serial"), cell: (unit) => (unit.serialNumber ? <span dir="ltr" className="font-mono text-xs">{unit.serialNumber}</span> : none) }
  ];
  if (showCosts) {
    columns.push({
      key: "cost", header: t("columns.cost"), align: "end",
      cell: (unit) => (unit.acquisitionCost === null ? none : <span className="whitespace-nowrap font-bold">{formatEgp(unit.acquisitionCost, locale)}</span>)
    });
  }
  columns.push(
    {
      key: "partner",
      header: t("columns.partner"),
      cell: (unit) => {
        const partner = partners.find((entry) => entry.id === unit.partnerId);
        return partner ? <span className="whitespace-nowrap">{t("partnerShare", { name: partner.name, percent: partnerShare(partner, unit.partnerPercentOverride) })}</span> : none;
      }
    },
    { key: "received", header: t("columns.received"), cell: (unit) => <span className="whitespace-nowrap text-xs">{formatDate(unit.receivedAtUtc, locale)}</span> }
  );
  if (canAssign || canEditStockValue) {
    columns.push({
      key: "actions",
      header: t("columns.actions"),
      cell: (unit) => (
        <span className="flex items-center gap-1">
          {canAssign ? <IconButton icon={SwapIcon} size="sm" label={t("changePartner")} onClick={() => setEditing(unit)} /> : null}
          {canEditStockValue ? (
            <IconButton
              icon={unit.excludedFromStockValue === true ? CheckIcon : BanIcon} size="sm" loading={togglingId === unit.id}
              label={t(unit.excludedFromStockValue === true ? "countInStockValue" : "excludeFromStockValue")} onClick={() => void toggleExcluded(unit)}
            />
          ) : null}
        </span>
      )
    });
  }

  return (
    <div className="flex min-w-0 flex-col gap-2">
      <h3 className="m-0 text-[15px] font-bold">{t("title", { count: item.units.length })}</h3>
      <DataTable label={t("title", { count: item.units.length })} columns={columns} state={READY(item.units)} onRetry={onStale} rowKey={(unit) => unit.id} empty={t("empty")} />
      {editing ? (
        <UnitPartnerDialog key={editing.id} unit={editing} partners={financingPartners(partners)} onSaved={onChanged} onStale={onStale} onClose={() => setEditing(null)} />
      ) : null}
    </div>
  );
}
