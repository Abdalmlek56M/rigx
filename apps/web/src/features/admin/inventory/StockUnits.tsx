"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDate, formatEgp } from "@/lib/format";
import type { Partner } from "../expenses/expenses-api";
import { DataTable, type Column } from "../kit/DataTable";
import { financingPartners, partnerShare } from "../stock/stock-fields";
import { UnitPartnerDialog } from "./UnitPartnerDialog";
import type { StockItem, StockUnit } from "./inventory-api";
import { unitStatusKey } from "./inventory-rules";
import { SwapIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const READY = (units: readonly StockUnit[]) => ({ status: "ready", data: units }) as const;

/** The item's units: status, serial, cost (when the server sent it), partner with share, received date, and "change partner". */
export function StockUnits({ item, partners, canAssign, showCosts, onChanged, onStale }: {
  item: StockItem;
  partners: readonly Partner[];
  canAssign: boolean;
  showCosts: boolean;
  onChanged: () => Promise<void>;
  onStale: () => void;
}) {
  const t = useTranslations("admin.inventory.units");
  const locale = useLocale();
  const [editing, setEditing] = useState<StockUnit | null>(null);
  const none = <span className="text-ink-muted">{t("none")}</span>;

  const columns: Column<StockUnit>[] = [
    { key: "status", header: t("columns.status"), cell: (unit) => <span className="whitespace-nowrap">{t(`status.${unitStatusKey(unit.status)}`)}</span> },
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
  if (canAssign) {
    columns.push({
      key: "actions",
      header: t("columns.actions"),
      cell: (unit) => <Button size="sm" icon={SwapIcon} onClick={() => setEditing(unit)}>{t("changePartner")}</Button>
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
