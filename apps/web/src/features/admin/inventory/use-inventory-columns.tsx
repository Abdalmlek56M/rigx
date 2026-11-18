"use client";

import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { formatEgp } from "@/lib/format";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import type { Column } from "../kit/DataTable";
import { usePermissions } from "../use-permissions";
import type { StockItem } from "./inventory-api";
import { categoryPath, expectedProfit, itemCost, itemPartner, stockState } from "./inventory-rules";

interface ColumnsInput {
  tree: readonly Category[];
  partners: readonly Partner[];
  selected: ReadonlySet<string>;
  activeId: string;
  onToggle: (id: string) => void;
  onOpen: (id: string) => void;
}

const asCondition = (condition: string) => (condition === "New" ? "New" : "Used");

/** The inventory list columns; cost, profit and partner appear only for staff who may see them, and only when the API sent the values. */
export function useInventoryColumns({ tree, partners, selected, activeId, onToggle, onOpen }: ColumnsInput): Column<StockItem>[] {
  const t = useTranslations("admin.inventory");
  const locale = useLocale();
  const { has } = usePermissions();
  const none = <span className="text-ink-muted">{t("none")}</span>;
  const money = (value: number | null) => (value === null ? none : <span className="whitespace-nowrap font-bold">{formatEgp(value, locale)}</span>);

  const columns: Column<StockItem>[] = [
    {
      key: "pick",
      header: t("columns.pick"),
      cell: (item) => (
        <input
          type="checkbox" checked={selected.has(item.id)} onChange={() => onToggle(item.id)} aria-label={t("pickRow", { code: item.code })}
          className="h-5 w-5"
        />
      )
    },
    {
      key: "code",
      header: t("columns.code"),
      cell: (item) => (
        <button
          type="button" dir="ltr" aria-current={activeId === item.id ? "true" : undefined} onClick={() => onOpen(item.id)}
          className={`min-h-8 whitespace-nowrap font-mono text-[13px] font-bold ${activeId === item.id ? "text-orange-text" : ""}`}
        >
          {item.code}
        </button>
      )
    },
    { key: "model", header: t("columns.model"), cell: (item) => <span dir="auto" className="block max-w-56 truncate [unicode-bidi:isolate]">{item.modelName}</span> },
    { key: "category", header: t("columns.category"), cell: (item) => <span className="whitespace-nowrap text-xs text-ink-muted">{categoryPath(tree, item.categoryId) || t("none")}</span> },
    { key: "condition", header: t("columns.condition"), cell: (item) => <ConditionBadge condition={asCondition(item.condition)} /> },
    { key: "count", header: t("columns.count"), align: "end", cell: (item) => <span className="font-bold">{item.availableCount}</span> }
  ];

  if (has("Inventory.ViewCosts")) {
    columns.push({ key: "cost", header: t("columns.cost"), align: "end", cell: (item) => money(itemCost(item)) });
  }
  columns.push({ key: "price", header: t("columns.price"), align: "end", cell: (item) => money(item.prices.inBuild) });
  if (has("Inventory.ViewCosts")) {
    columns.push({
      key: "profit",
      header: t("columns.profit"),
      align: "end",
      cell: (item) => {
        const profit = expectedProfit(item);
        return profit === null ? none : <span className={`whitespace-nowrap font-bold ${profit < 0 ? "text-error" : ""}`}>{formatEgp(profit, locale)}</span>;
      }
    });
  }
  if (has("Finance.ViewReports")) {
    columns.push({
      key: "partner",
      header: t("columns.partner"),
      cell: (item) => {
        const partner = itemPartner(item, partners);
        return partner ? <span className="whitespace-nowrap">{t("partnerShare", { name: partner.name, percent: partner.percent })}</span> : none;
      }
    });
  }
  columns.push(
    { key: "status", header: t("columns.status"), cell: (item) => <span className="whitespace-nowrap">{t(`state.${stockState(item)}`)}</span> },
    {
      key: "visibility",
      header: t("columns.visibility"),
      cell: (item) => <span className={`whitespace-nowrap ${item.isHidden ? "text-ink-muted" : ""}`}>{item.isHidden ? t("hidden") : t("shown")}</span>
    },
    { key: "days", header: t("columns.days"), align: "end", cell: (item) => <span className="whitespace-nowrap">{t("days", { count: item.daysInStock })}</span> }
  );
  return columns;
}
