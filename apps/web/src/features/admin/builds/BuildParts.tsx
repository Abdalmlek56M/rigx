"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import { DataTable, type Column } from "../kit/DataTable";
import { partnerShare } from "../stock/stock-fields";
import { AddNewPartPanel } from "./AddNewPartPanel";
import { AddStockPartPanel } from "./AddStockPartPanel";
import { PartEditDialog } from "./PartEditDialog";
import { RemovePartDialog } from "./RemovePartDialog";
import { ReplacePartDialog } from "./ReplacePartDialog";
import type { BuildPart } from "./builds-api";
import type { BuildActionProps } from "./use-build-action";
import { BoxIcon, PencilIcon, PlusIcon, SwapIcon, TrashIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

export interface BuildLookups {
  tree: readonly Category[];
  /** Every partner, or `null` when the staff member cannot read partners (the partner column and fields are then left out). */
  partners: readonly Partner[] | null;
  warrantyLabels: readonly string[] | null;
}

type Panel = { kind: "edit" | "replace" | "remove"; part: BuildPart } | { kind: "stock" | "new" } | null;

/** The build's parts with their money, and the actions on them: edit, replace, remove, add from stock, add a new part. */
export function BuildParts({ canEdit, lookups, ...action }: BuildActionProps & { canEdit: boolean; lookups: BuildLookups }) {
  const t = useTranslations("admin.builds.parts");
  const locale = useLocale();
  const { build } = action;
  const [panel, setPanel] = useState<Panel>(null);
  const close = () => setPanel(null);
  const none = <span className="text-ink-muted">{t("none")}</span>;
  const rows = { status: "ready", data: build.units } as const;

  const columns: Column<BuildPart>[] = [
    {
      key: "part",
      header: t("columns.part"),
      cell: (part) => (
        <span className="flex min-w-36 flex-col gap-0.5">
          <span dir="auto" className="[unicode-bidi:isolate]">{part.name}</span>
          <Link href={`/admin/inventory?q=${encodeURIComponent(part.stockItemCode)}`} dir="ltr" className="font-mono text-xs text-ink-muted hover:underline">{part.stockItemCode}</Link>
        </span>
      )
    },
    { key: "condition", header: t("columns.condition"), cell: (part) => <ConditionBadge condition={part.condition === "New" ? "New" : "Used"} /> },
    {
      key: "cost", header: t("columns.cost"), align: "end",
      cell: (part) => (part.acquisitionCost === null ? none : <span className="whitespace-nowrap">{formatEgp(part.acquisitionCost, locale)}</span>)
    },
    { key: "price", header: t("columns.price"), align: "end", cell: (part) => <span className="whitespace-nowrap font-bold">{formatEgp(part.partPrice, locale)}</span> },
    {
      key: "warranty",
      header: t("columns.warranty"),
      cell: (part) => (
        <span className="flex flex-col gap-0.5">
          <span className="whitespace-nowrap">{part.warrantyLabel ?? none}</span>
          {part.warrantyOverridden ? <span className="text-xs text-orange-text">{t("ownWarranty")}</span> : null}
        </span>
      )
    }
  ];
  if (lookups.partners) {
    const partners = lookups.partners;
    columns.push({
      key: "partner",
      header: t("columns.partner"),
      cell: (part) => {
        const partner = partners.find((entry) => entry.id === part.partnerId);
        if (!partner) return none;
        return (
          <span className="flex flex-col gap-0.5 whitespace-nowrap">
            <span dir="auto" className="[unicode-bidi:isolate]">{partner.name}</span>
            <span className={`text-xs ${part.partnerPercentOverride === null ? "text-ink-muted" : "font-bold text-orange-text"}`}>{t("percent", { percent: partnerShare(partner, part.partnerPercentOverride) })}</span>
          </span>
        );
      }
    });
  }
  columns.push({
    key: "allocation",
    header: t("columns.allocation"),
    align: "end",
    cell: (part) => (
      <span className="flex flex-col items-end gap-0.5 whitespace-nowrap">
        <span className="font-bold">{t("percent", { percent: part.allocationPercent })}</span>
        {part.allocatedAmount === null ? null : <span className="text-xs text-ink-muted">{formatEgp(part.allocatedAmount, locale)}</span>}
      </span>
    )
  });
  if (canEdit) {
    columns.push({
      key: "actions",
      header: t("columns.actions"),
      cell: (part) => (
        <span className="flex flex-wrap gap-1.5">
          <Button size="sm" icon={PencilIcon} onClick={() => setPanel({ kind: "edit", part })}>{t("edit")}</Button>
          <Button size="sm" icon={SwapIcon} onClick={() => setPanel({ kind: "replace", part })}>{t("replace")}</Button>
          <Button size="sm" variant="danger" icon={TrashIcon} onClick={() => setPanel({ kind: "remove", part })}>{t("remove")}</Button>
        </span>
      )
    });
  }

  return (
    <section className="flex min-w-0 flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold">{t("title", { count: build.units.length })}</h2>
        {canEdit ? (
          <div className="flex flex-wrap gap-2">
            <Button size="sm" icon={BoxIcon} onClick={() => setPanel({ kind: "stock" })}>{t("addFromStock")}</Button>
            <Button size="sm" icon={PlusIcon} onClick={() => setPanel({ kind: "new" })}>{t("addNew")}</Button>
          </div>
        ) : null}
      </div>
      <DataTable label={t("title", { count: build.units.length })} columns={columns} state={rows} onRetry={action.onStale} rowKey={(part) => part.unitId} empty={t("empty")} />
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      {panel?.kind === "stock" ? <AddStockPartPanel {...action} onClose={close} /> : null}
      {panel?.kind === "new" ? <AddNewPartPanel {...action} lookups={lookups} onClose={close} /> : null}
      {panel?.kind === "edit" ? <PartEditDialog key={panel.part.unitId} {...action} part={panel.part} lookups={lookups} onClose={close} /> : null}
      {panel?.kind === "replace" ? <ReplacePartDialog key={panel.part.unitId} {...action} part={panel.part} onClose={close} /> : null}
      {panel?.kind === "remove" ? <RemovePartDialog key={panel.part.unitId} {...action} part={panel.part} onClose={close} /> : null}
    </section>
  );
}
