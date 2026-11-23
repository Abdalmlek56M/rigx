"use client";

import { useLocale, useTranslations } from "next-intl";
import type { ReactNode } from "react";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { ChevronDownIcon, ChevronStartIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import { BuildStatusBadge } from "../builds/BuildStatusBadge";
import { IconButton } from "../kit/Button";
import type { Column } from "../kit/DataTable";
import { usePermissions } from "../use-permissions";
import { InventoryShortcuts } from "./InventoryShortcuts";
import type { StockItem } from "./inventory-api";
import { categoryPath, expectedProfit, itemCost, itemPartner, stockAge, stockState, type StockAge, type StockRow } from "./inventory-rules";

interface ColumnsInput {
  tree: readonly Category[];
  partners: readonly Partner[];
  selected: ReadonlySet<string>;
  onToggle: (id: string) => void;
  /** Opens or closes the parts of a build. */
  onToggleBuild: (id: string) => void;
  onQuickEdit: (item: StockItem) => void;
  onToggleHidden: (item: StockItem) => void;
  /** The item whose visibility is being changed right now. */
  busyId: string | null;
}

/** The badge tone of an item's time in stock; a fresh item shows plain text. */
const AGE_TONES: Record<Exclude<StockAge, "fresh">, StatusTone> = { slow: "warning", stale: "danger" };

const asCondition = (condition: string) => (condition === "New" ? "New" : "Used");

/** The inventory list columns; cost, profit and partner appear only for staff who may see them, and only when the API sent the values. */
export function useInventoryColumns({ tree, partners, selected, onToggle, onToggleBuild, onQuickEdit, onToggleHidden, busyId }: ColumnsInput): Column<StockRow>[] {
  const t = useTranslations("admin.inventory");
  const locale = useLocale();
  const { has } = usePermissions();
  const none = <span className="text-ink-muted">{t("none")}</span>;
  const money = (value: number | null) => (value === null ? none : <span className="whitespace-nowrap font-bold">{formatEgp(value, locale)}</span>);

  // A build header row has its own cells in a few columns; every other column is empty for it, and an item row uses its item.
  const forItem = (cell: (item: StockItem, underBuild: boolean) => ReactNode) => (row: StockRow) => (row.kind === "item" ? cell(row.item, row.underBuild) : null);

  const columns: Column<StockRow>[] = [
    {
      key: "pick",
      header: t("columns.pick"),
      cell: forItem((item) => (
        <input
          type="checkbox" checked={selected.has(item.id)} onChange={() => onToggle(item.id)} aria-label={t("pickRow", { code: item.code })}
          className="h-5 w-5"
        />
      ))
    },
    {
      key: "code",
      header: t("columns.code"),
      cell: (row) => row.kind === "build" ? (
        <Link href={`/admin/builds/${row.build.id}`} dir="ltr" className="inline-block min-h-8 whitespace-nowrap font-mono text-[13px] font-bold text-orange-text hover:underline">
          {row.build.code}
        </Link>
      ) : (
        <Link href={`/admin/inventory/${row.item.id}`} dir="ltr" className="inline-block min-h-8 whitespace-nowrap font-mono text-[13px] font-bold text-orange-text hover:underline">
          {row.item.code}
        </Link>
      )
    },
    {
      key: "model",
      header: t("columns.model"),
      phoneFull: true,
      cell: (row) => row.kind === "build" ? (
        <span className="flex items-center gap-1.5">
          <IconButton
            icon={row.open ? ChevronDownIcon : ChevronStartIcon} size="sm" label={t(row.open ? "build.collapse" : "build.expand", { name: row.build.name })}
            aria-expanded={row.open} onClick={() => onToggleBuild(row.build.id)}
          />
          <span dir="auto" className="max-w-52 truncate font-bold [unicode-bidi:isolate]">{row.build.name}</span>
          <span className="whitespace-nowrap text-xs text-ink-muted">{t("build.parts", { count: row.count })}</span>
        </span>
      ) : (
        <Link href={`/admin/inventory/${row.item.id}`} dir="auto" className={`block max-w-56 truncate font-bold hover:text-orange-text hover:underline [unicode-bidi:isolate] ${row.underBuild ? "ms-5" : ""}`}>
          {row.item.modelName}
        </Link>
      )
    },
    {
      key: "shortcuts",
      header: t("columns.shortcuts"),
      phoneFull: true,
      cell: forItem((item) => <InventoryShortcuts item={item} onQuickEdit={onQuickEdit} onToggleHidden={onToggleHidden} busy={busyId === item.id} />)
    },
    { key: "category", header: t("columns.category"), cell: forItem((item) => <span className="whitespace-nowrap text-xs text-ink-muted">{categoryPath(tree, item.categoryId) || t("none")}</span>) },
    { key: "condition", header: t("columns.condition"), cell: forItem((item) => <ConditionBadge condition={asCondition(item.condition)} />) },
    { key: "count", header: t("columns.count"), align: "end", cell: forItem((item) => <span className="font-bold">{item.availableCount}</span>) }
  ];

  if (has("Inventory.ViewCosts")) {
    columns.push({ key: "cost", header: t("columns.cost"), align: "end", cell: forItem((item) => money(itemCost(item))) });
  }
  columns.push({ key: "price", header: t("columns.price"), align: "end", cell: forItem((item) => money(item.prices.inBuild)) });
  if (has("Inventory.ViewCosts")) {
    columns.push({
      key: "profit",
      header: t("columns.profit"),
      align: "end",
      cell: forItem((item) => {
        const profit = expectedProfit(item);
        return profit === null ? none : <span className={`whitespace-nowrap font-bold ${profit < 0 ? "text-error-text" : ""}`}>{formatEgp(profit, locale)}</span>;
      })
    });
  }
  if (has("Finance.ViewReports")) {
    columns.push({
      key: "partner",
      header: t("columns.partner"),
      cell: forItem((item) => {
        const partner = itemPartner(item, partners);
        return partner ? <span className="whitespace-nowrap">{t("partnerShare", { name: partner.name, percent: partner.percent })}</span> : none;
      })
    });
  }
  columns.push(
    {
      key: "status",
      header: t("columns.status"),
      cell: (row) => row.kind === "build" ? <BuildStatusBadge status={row.build.status} /> : <span className="whitespace-nowrap">{t(`state.${stockState(row.item)}`)}</span>
    },
    {
      key: "visibility",
      header: t("columns.visibility"),
      cell: forItem((item) => <span className={`whitespace-nowrap ${item.isHidden ? "text-ink-muted" : ""}`}>{item.isHidden ? t("hidden") : t("shown")}</span>)
    },
    {
      key: "days",
      header: t("columns.days"),
      align: "end",
      cell: forItem((item) => {
        const age = stockAge(item.daysInStock, item.availableCount);
        const text = t("days", { count: item.daysInStock });
        return age === "fresh" ? <span className="whitespace-nowrap">{text}</span> : <StatusBadge tone={AGE_TONES[age]}>{text}</StatusBadge>;
      })
    }
  );
  return columns;
}
