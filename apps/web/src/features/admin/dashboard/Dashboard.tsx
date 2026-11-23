"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatDate } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";
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
import { BoxIcon, RegisterIcon } from "@/components/ui/icons";
import { buttonClass, buttonIconSize } from "../kit/Button";

const FILTERS = { period: "month", from: "", to: "" };

function Skeleton() {
  return (
    <div aria-hidden="true" className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
        {Array.from({ length: 6 }, (_, index) => <LoadingCard key={index} className="h-[104px]" />)}
      </div>
      <LoadingCard className="h-80" />
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
      <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />
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
            {has("Purchasing.ReceiveGoods") ? <Link href="/admin/purchases" className={buttonClass()}><BoxIcon size={buttonIconSize()} />{t("receive")}</Link> : null}
            {has("Sales.CreatePOS") ? <Link href="/admin/pos" className={buttonClass({ variant: "primary" })}><RegisterIcon size={buttonIconSize()} />{t("newSale")}</Link> : null}
          </>
        }
      />
      <PeriodSwitch key={`${values.from}|${values.to}`} period={period} from={values.from} to={values.to} onChange={set} />
      {range ? <Content from={range.from} to={range.to} /> : <p className="m-0 text-sm text-ink-muted">{t("pickRange")}</p>}
    </>
  );
}
