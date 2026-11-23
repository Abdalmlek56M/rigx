"use client";

import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { cardClass } from "@/components/ui/Card";
import type { Build } from "./builds-api";

/** ADM-Build "الحساب": what the parts cost, what they add up to at their own prices, the sale price and the profit (only when the server sent the cost). */
export function BuildSummary({ build }: { build: Build }) {
  const t = useTranslations("admin.builds.summary");
  const locale = useLocale();
  const rows: { key: string; label: string; value: number; strong?: boolean }[] = [];
  if (build.componentCost !== null) rows.push({ key: "cost", label: t("cost"), value: build.componentCost });
  rows.push({ key: "partsTotal", label: t("partsTotal"), value: build.partsTotal }, { key: "salePrice", label: t("salePrice"), value: build.salePrice, strong: true });
  if (build.componentCost !== null) rows.push({ key: "profit", label: t("profit"), value: build.salePrice - build.componentCost, strong: true });

  return (
    <section className={cardClass({ className: "gap-2" })}>
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      <dl className="m-0 flex flex-col">
        {rows.map((row) => (
          <div key={row.key} className="flex items-center justify-between gap-3 border-t border-line py-2 first:border-t-0">
            <dt className="text-sm text-ink-muted">{row.label}</dt>
            <dd className={`m-0 whitespace-nowrap text-sm ${row.strong ? "font-bold" : ""}`}>{formatEgp(row.value, locale)}</dd>
          </div>
        ))}
      </dl>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
    </section>
  );
}
