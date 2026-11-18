"use client";

import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { DataTable, type Column } from "../kit/DataTable";
import { unitStatusKey } from "../inventory/inventory-rules";
import type { FinancedPart } from "./partners-api";

/** The units this partner financed: status (a unit in a build links to it), cost and share; a share changed for one unit is orange, and it is changed in the inventory. */
export function PartnerParts({ parts }: { parts: readonly FinancedPart[] }) {
  const t = useTranslations("admin.partners.parts");
  const status = useTranslations("admin.inventory.units.status");
  const locale = useLocale();

  const columns: Column<FinancedPart>[] = [
    {
      key: "code",
      header: t("columns.code"),
      cell: (part) => <Link href={`/admin/inventory?q=${encodeURIComponent(part.stockItemCode)}`} dir="ltr" className="whitespace-nowrap font-mono text-[13px] font-bold hover:underline">{part.stockItemCode}</Link>
    },
    { key: "name", header: t("columns.name"), cell: (part) => <span dir="auto" className="[unicode-bidi:isolate]">{part.name}</span> },
    {
      key: "status",
      header: t("columns.status"),
      cell: (part) =>
        part.assemblyId ? (
          <Link href={`/admin/builds/${part.assemblyId}`} className="whitespace-nowrap font-bold hover:underline">{status(unitStatusKey(part.status))}</Link>
        ) : (
          <span className="whitespace-nowrap">{status(unitStatusKey(part.status))}</span>
        )
    },
    { key: "cost", header: t("columns.cost"), align: "end", cell: (part) => <span className="whitespace-nowrap">{formatEgp(part.cost, locale)}</span> },
    {
      key: "percent",
      header: t("columns.percent"),
      align: "end",
      cell: (part) => (
        <span className="flex flex-col items-end gap-0.5 whitespace-nowrap">
          <span className={part.percentOverridden ? "font-bold text-orange-text" : ""}>{t("percent", { percent: part.percent })}</span>
          {part.percentOverridden ? <span className="text-xs text-orange-text">{t("changed")}</span> : null}
        </span>
      )
    }
  ];

  return (
    <section className="flex min-w-0 flex-col gap-3">
      <h3 className="m-0 text-[15px] font-bold">{t("title", { count: parts.length })}</h3>
      <DataTable label={t("title", { count: parts.length })} columns={columns} state={{ status: "ready", data: parts }} onRetry={() => undefined} rowKey={(part) => part.unitId} empty={t("empty")} />
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
    </section>
  );
}
