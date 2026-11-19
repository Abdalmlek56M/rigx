"use client";

import { useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { cairoToday } from "../dashboard/dashboard-rules";
import { fetchPartners, type Partner } from "../expenses/expenses-api";
import { DataTable, type Column } from "../kit/DataTable";
import { PageHeader } from "../kit/PageHeader";
import { useReveal } from "../kit/use-reveal";
import { useUrlFilters } from "../kit/use-url-filters";
import { usePermissions } from "../use-permissions";
import { MonthClosePanel } from "./MonthClosePanel";
import { NewPartnerDialog } from "./NewPartnerDialog";
import { PartnerDetail } from "./PartnerDetail";
import { basisKey, partnerTotals } from "./partner-rules";

const FILTERS = { partner: "" };

/** ADM-Partners: the partners with their capital and what they are owed, and the picked partner (agreement, financed parts, capital, payouts, statement) below. */
export function Partners() {
  const t = useTranslations("admin.partners");
  const basis = useTranslations("admin.partners.form.bases");
  const locale = useLocale();
  const { has } = usePermissions();
  const { values, set } = useUrlFilters(FILTERS);
  const { state, reload } = useLoad(fetchPartners);
  const [today] = useState(() => cairoToday(new Date()));
  const [creating, setCreating] = useState(false);
  const detailRef = useReveal<HTMLDivElement>(values.partner || null);
  const totals = useMemo(() => (state.status === "ready" ? partnerTotals(state.data) : null), [state]);
  const canManage = has("Finance.ManagePartnerAgreements");
  const money = (value: number) => <span className="whitespace-nowrap">{formatEgp(value, locale)}</span>;

  const columns: Column<Partner>[] = [
    {
      key: "name",
      header: t("columns.name"),
      cell: (partner) => (
        <button
          type="button" aria-current={values.partner === partner.id ? "true" : undefined} onClick={() => set({ partner: partner.id })}
          className={`min-h-8 text-start font-bold ${values.partner === partner.id ? "text-orange-text" : ""}`}
        >
          <span dir="auto" className="[unicode-bidi:isolate]">{partner.name}</span>
        </button>
      )
    },
    { key: "basis", header: t("columns.basis"), cell: (partner) => <span className="whitespace-nowrap">{basis(basisKey(partner.basis))}</span> },
    { key: "percent", header: t("columns.percent"), align: "end", cell: (partner) => t("percent", { percent: partner.defaultPercent }) },
    { key: "capital", header: t("columns.capital"), align: "end", cell: (partner) => money(partner.capitalBalance) },
    { key: "units", header: t("columns.units"), align: "end", cell: (partner) => partner.unsoldFinancedUnits },
    { key: "due", header: t("columns.due"), align: "end", cell: (partner) => <span className={partner.due > 0 ? "font-bold" : "text-ink-muted"}>{money(partner.due)}</span> },
    { key: "active", header: t("columns.active"), cell: (partner) => <span className={partner.isActive ? "" : "text-ink-muted"}>{partner.isActive ? t("active") : t("inactive")}</span> }
  ];

  const kpis = totals
    ? [
        { key: "capital", label: t("kpi.capital"), value: money(totals.capital) },
        { key: "due", label: t("kpi.due"), value: money(totals.due) },
        { key: "recoverable", label: t("kpi.recoverable"), value: money(totals.recoverable) },
        { key: "units", label: t("kpi.units"), value: totals.unsoldUnits }
      ]
    : [];

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={canManage ? <button type="button" onClick={() => setCreating(true)} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("new")}</button> : null}
      />
      {kpis.length > 0 ? (
        <dl className="m-0 grid grid-cols-2 gap-3 xl:grid-cols-4">
          {kpis.map((kpi) => (
            <div key={kpi.key} className="flex flex-col gap-1 rounded-card border border-line bg-surface p-4">
              <dt className="text-[13px] text-ink-muted">{kpi.label}</dt>
              <dd className="m-0 text-[19px] font-bold">{kpi.value}</dd>
            </div>
          ))}
        </dl>
      ) : null}
      <DataTable label={t("title")} columns={columns} state={state} onRetry={reload} rowKey={(partner) => partner.id} empty={t("empty")} />
      <div ref={detailRef} className="scroll-mt-20 empty:hidden">
        {values.partner ? <PartnerDetail key={values.partner} id={values.partner} today={today} onClose={() => set({ partner: "" })} onChanged={reload} /> : null}
      </div>
      {canManage ? <MonthClosePanel today={today} onClosed={reload} /> : null}
      {creating ? (
        <NewPartnerDialog
          today={today} onClose={() => setCreating(false)}
          onCreated={(partner) => {
            setCreating(false);
            reload();
            set({ partner: partner.id });
          }}
        />
      ) : null}
    </>
  );
}
