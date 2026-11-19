"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatDate } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { PageHeader } from "../kit/PageHeader";
import { useUrlFilters } from "../kit/use-url-filters";
import { usePermissions } from "../use-permissions";
import { AccountsPanel, AgingPanel, DemandPanel, PartnersPanel } from "./StockPanels";
import { CategoriesPanel, ConditionPanel, LeadSourcesPanel, TodoPanel, UsagesPanel } from "./BreakdownPanels";
import { fetchDashboard, type AdminDashboard } from "./dashboard-api";
import { cairoToday, parsePeriod, periodRange, type DateRange, type Period } from "./dashboard-rules";
import { KpiCards } from "./KpiCards";
import { PeriodSwitch } from "./PeriodSwitch";
import { SalesChart } from "./SalesChart";

const FILTERS = { period: "month", from: "", to: "" };

function Skeleton() {
  return (
    <div aria-hidden="true" className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
        {Array.from({ length: 6 }, (_, index) => <div key={index} className="h-[104px] animate-pulse rounded-card border border-line bg-surface" />)}
      </div>
      <div className="h-80 animate-pulse rounded-card border border-line bg-surface" />
    </div>
  );
}

function Content({ from, to }: DateRange) {
  const t = useTranslations("admin.dashboard");
  const locale = useLocale();
  const load = useCallback(() => fetchDashboard({ from, to }), [from, to]);
  const { state, reload } = useLoad<AdminDashboard>(load);

  if (state.status === "loading") return <div role="status" aria-label={t("loading")}><Skeleton /></div>;
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
        <p className="m-0 font-bold">{t("failed")}</p>
        <button type="button" onClick={reload} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("retry")}</button>
      </div>
    );
  }

  const { dashboard: data, newRequests, notifyMeDemand } = state.data;
  return (
    <>
      <p className="m-0 text-[13px] text-ink-muted">
        {t("comparedWith", { from: formatDate(data.previousFrom, locale), to: formatDate(data.previousTo, locale) })}
      </p>
      <KpiCards data={data} />
      <div className="grid gap-4 lg:grid-cols-[2fr_1fr]">
        <SalesChart days={data.days} />
        <TodoPanel todo={data.todo} newRequests={newRequests} />
      </div>
      <div className="grid gap-4 lg:grid-cols-3">
        <LeadSourcesPanel rows={data.leadSources} />
        <CategoriesPanel rows={data.topCategories} />
        <ConditionPanel data={data} />
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <UsagesPanel rows={data.usages} />
        <DemandPanel rows={notifyMeDemand} />
      </div>
      <AgingPanel aging={data.stockAging} />
      <div className="grid gap-4 lg:grid-cols-2">
        <PartnersPanel rows={data.partnerProfitsThisMonth} />
        <AccountsPanel rows={data.snapshot.accounts} />
      </div>
    </>
  );
}

const actionButton = "inline-flex h-11 items-center whitespace-nowrap rounded-control px-4 text-sm font-bold";

/** ADM-Dashboard: the owner's overview for a period (kept in the URL), against the previous period of the same length. */
export function Dashboard() {
  const t = useTranslations("admin.dashboard");
  const { has } = usePermissions();
  const { values, set } = useUrlFilters(FILTERS);
  const period: Period = parsePeriod(values.period);
  const [today] = useState(() => cairoToday(new Date()));
  const range = periodRange(period, today, values);

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={
          <>
            {has("Purchasing.ReceiveGoods") ? <Link href="/admin/purchases" className={`${actionButton} border border-input-line bg-surface`}>{t("receive")}</Link> : null}
            {has("Sales.CreatePOS") ? <Link href="/admin/pos" className={`${actionButton} bg-orange text-on-orange`}>{t("newSale")}</Link> : null}
          </>
        }
      />
      <PeriodSwitch key={`${values.from}|${values.to}`} period={period} from={values.from} to={values.to} onChange={set} />
      {range ? <Content from={range.from} to={range.to} /> : <p className="m-0 text-sm text-ink-muted">{t("pickRange")}</p>}
    </>
  );
}
