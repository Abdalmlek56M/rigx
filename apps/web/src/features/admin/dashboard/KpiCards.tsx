import { useLocale, useTranslations } from "next-intl";
import type { ReactNode } from "react";
import { formatEgp } from "@/lib/format";
import { changeDirection } from "./dashboard-rules";
import type { DashboardData } from "./dashboard-api";

function Card({ label, value, note, dark = false }: { label: string; value: string; note: ReactNode; dark?: boolean }) {
  return (
    <div className={`flex min-w-0 flex-col gap-1.5 rounded-[10px] border px-[18px] py-4 ${dark ? "border-carbon bg-carbon text-on-carbon" : "border-line bg-surface"}`}>
      <span className={`text-[13px] ${dark ? "text-footer-text" : "text-ink-muted"}`}>{label}</span>
      <span className="truncate text-2xl font-bold">{value}</span>
      <span className={`text-xs ${dark ? "text-footer-text" : "text-ink-muted"}`}>{note}</span>
    </div>
  );
}

/** "▲ 12.5% vs the previous period" in green, "▼" in red, or nothing to compare with. */
function Change({ value }: { value: number | null }) {
  const t = useTranslations("admin.dashboard.kpi");
  const direction = changeDirection(value);
  if (direction === null || value === null) return <>{t("noComparison")}</>;
  const tone = direction === "up" ? "text-success" : direction === "down" ? "text-error" : "";
  const arrow = direction === "up" ? "▲" : direction === "down" ? "▼" : "•";
  return <span className={tone}>{t("change", { arrow, percent: Math.abs(value) })}</span>;
}

/** The six headline numbers: period sales, net profit and orders (against the previous period), and the shop now. */
export function KpiCards({ data }: { data: DashboardData }) {
  const t = useTranslations("admin.dashboard.kpi");
  const locale = useLocale();
  const money = (amount: number) => formatEgp(amount, locale);
  const { current, changes, snapshot } = data;

  return (
    <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
      <Card label={t("sales")} value={money(current.grossSales)} note={<Change value={changes.grossSales} />} />
      <Card label={t("netProfit")} value={money(current.netProfit)} note={<Change value={changes.netProfit} />} />
      <Card label={t("orders")} value={String(current.orders)} note={t("averageOrder", { amount: money(current.averageOrder) })} />
      <Card label={t("stockValue")} value={money(snapshot.stockValueAtCost)} note={t("atCost")} />
      <Card label={t("partnerDues")} value={money(snapshot.partnerDues)} note={t("partnerDuesNote")} />
      <Card label={t("cash")} value={money(snapshot.cashAvailable)} note={t("cashNote")} dark />
    </div>
  );
}
