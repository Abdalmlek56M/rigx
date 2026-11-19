"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDate, formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { PageHeader } from "../kit/PageHeader";
import { DataTable, type Column } from "../kit/DataTable";
import { useUrlFilters } from "../kit/use-url-filters";
import { usePermissions } from "../use-permissions";
import { cairoToday } from "../dashboard/dashboard-rules";
import { ExpenseForm } from "./ExpenseForm";
import { ExpensePeriodSwitch } from "./ExpensePeriodSwitch";
import { ExpenseReceiptDialog } from "./ExpenseReceiptDialog";
import { ExpenseTypesPanel } from "./ExpenseTypesPanel";
import { RecurringExpensesPanel } from "./RecurringExpensesPanel";
import {
  fetchExpenseSummary,
  fetchExpenseTypes,
  fetchExpenses,
  fetchMoneyAccounts,
  fetchPartners,
  fetchRecurringExpenses,
  type Expense
} from "./expenses-api";
import { expenseLink, expensePeriodRange, expenseSplits, parseExpensePeriod } from "./expense-rules";
import { EyeIcon, RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const FILTERS = { period: "month", from: "", to: "" };

function Kpi({ label, value, note }: { label: string; value: string; note?: string }) {
  return (
    <div className="flex flex-col gap-1 rounded-card border border-line bg-surface p-4">
      <span className="text-xs text-ink-muted">{label}</span>
      <b className="text-xl">{value}</b>
      {note ? <span className="text-xs text-ink-muted">{note}</span> : null}
    </div>
  );
}

function Body({ from, to, canViewVaults, canManage }: { from: string; to: string; canViewVaults: boolean; canManage: boolean }) {
  const t = useTranslations("admin.expenses");
  const locale = useLocale();
  const [today] = useState(() => cairoToday(new Date()));
  const [receiptId, setReceiptId] = useState<string | null>(null);
  const load = useCallback(async () => {
    const [expenses, summary, types, recurring, accounts, partners] = await Promise.all([
      fetchExpenses(from, to),
      fetchExpenseSummary(from, to),
      fetchExpenseTypes(),
      fetchRecurringExpenses(),
      canViewVaults ? fetchMoneyAccounts().catch(() => []) : Promise.resolve([]),
      fetchPartners().catch(() => [])
    ]);
    return { expenses, summary, types, recurring, accounts, partners };
  }, [from, to, canViewVaults]);
  const { state, reload } = useLoad(load);
  if (state.status === "loading") return <div aria-hidden="true" className="h-72 animate-pulse rounded-card border border-line bg-surface" />;
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
        <p className="m-0 font-bold">{t("failed")}</p>
        <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
      </div>
    );
  }

  const { expenses, summary, types, recurring, accounts, partners } = state.data;
  const accountsById = new Map(accounts.map((account) => [account.id, account.name]));
  const partnersById = new Map(partners.map((partner) => [partner.id, partner.name]));
  const change = summary.changePercent;
  const linkLabels = { order: t("links.order"), unit: t("links.unit"), build: t("links.build") };

  function paidFrom(expense: Expense): string {
    if (expense.paidByPartnerId) return partnersById.get(expense.paidByPartnerId) ?? t("none");
    if (expense.recurringExpenseId) return t("automatic");
    if (!canViewVaults) return t("none");
    return expense.moneyAccountId ? accountsById.get(expense.moneyAccountId) ?? t("defaultAccount") : t("defaultAccount");
  }

  const columns: Column<Expense>[] = [
    { key: "date", header: t("columns.date"), cell: (expense) => <span className="whitespace-nowrap text-xs">{formatDate(`${expense.expenseDate}T00:00:00Z`, locale)}</span> },
    { key: "category", header: t("columns.type"), cell: (expense) => <span className="font-bold">{expense.category}</span> },
    { key: "note", header: t("columns.note"), cell: (expense) => <span dir="auto" className="block max-w-56 truncate text-sm [unicode-bidi:isolate]">{expense.note ?? t("none")}</span> },
    {
      key: "link", header: t("columns.link"),
      cell: (expense) => {
        const link = expenseLink(expense);
        return <span className="text-xs text-ink-muted">{link ? linkLabels[link] : t("none")}</span>;
      }
    },
    { key: "amount", header: t("columns.amount"), align: "end", cell: (expense) => <span className="whitespace-nowrap font-bold">{formatEgp(expense.amount, locale)}</span> },
    { key: "paidFrom", header: t("columns.paidFrom"), cell: (expense) => <span className="text-xs text-ink-muted">{paidFrom(expense)}</span> },
    {
      key: "receipt", header: t("columns.receipt"),
      cell: (expense) => expense.hasReceipt ? <Button size="sm" variant="ghost" icon={EyeIcon} onClick={() => setReceiptId(expense.id)}>{t("receipt.open")}</Button> : <span className="text-xs text-ink-muted">{t("none")}</span>
    }
  ];

  return (
    <>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <Kpi label={t("kpi.total")} value={formatEgp(summary.total, locale)} note={change === null ? t("kpi.noComparison") : t("kpi.change", { percent: Math.abs(change) })} />
        <Kpi label={t("kpi.fixed")} value={formatEgp(summary.fixedMonthly, locale)} note={t("kpi.fixedNote")} />
        <Kpi label={t("kpi.percentOfSales")} value={summary.percentOfSales === null ? t("none") : `${summary.percentOfSales}%`} />
        <Kpi label={t("kpi.sales")} value={formatEgp(summary.grossSales, locale)} />
      </div>

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_380px] lg:items-start">
        <DataTable label={t("title")} columns={columns} state={{ status: "ready", data: expenses }} onRetry={reload} rowKey={(expense) => expense.id} empty={t("empty")} />
        <section className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
          <h2 className="m-0 text-[17px] font-bold">{t("splits.title")}</h2>
          {summary.byType.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("splits.empty")}</p> : (
            <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
              {expenseSplits(summary.byType).map((row) => (
                <li key={row.type} className="grid grid-cols-[100px_minmax(0,1fr)_90px] items-center gap-2 text-[13px]">
                  <span className="truncate">{row.type}</span>
                  <span aria-hidden="true" className="h-2.5 overflow-hidden rounded-full bg-line"><span className="block h-full rounded-full bg-carbon" style={{ width: `${row.width}%` }} /></span>
                  <span className="whitespace-nowrap text-end text-xs text-ink-muted">{formatEgp(row.amount, locale)}</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>

      {canManage ? <ExpenseForm types={types} accounts={accounts} partners={partners} canSeeVaults={canViewVaults} today={today} onCreated={() => reload()} onTypesChanged={reload} /> : null}
      <ExpenseTypesPanel types={types} canManage={canManage} onChanged={reload} />
      <RecurringExpensesPanel templates={recurring} types={types} accounts={accounts} canSeeVaults={canViewVaults} canManage={canManage} onChanged={reload} />
      {receiptId ? <ExpenseReceiptDialog id={receiptId} onClose={() => setReceiptId(null)} /> : null}
    </>
  );
}

/** ADM-Expenses: the period in the URL, totals, the list, the split, the form and the type/recurring settings. */
export function Expenses() {
  const t = useTranslations("admin.expenses");
  const { has } = usePermissions();
  const canViewVaults = has("Finance.ViewVaults");
  const canManage = has("Finance.ManageExpenses");
  const { values, set } = useUrlFilters(FILTERS);
  const period = parseExpensePeriod(values.period);
  const [today] = useState(() => cairoToday(new Date()));
  const range = expensePeriodRange(period, today, values);
  return (
    <>
      <PageHeader title={t("title")} />
      <ExpensePeriodSwitch period={period} from={values.from} to={values.to} onChange={set} />
      {range ? <Body from={range.from} to={range.to} canViewVaults={canViewVaults} canManage={canManage} /> : <p className="m-0 text-sm text-ink-muted">{t("pickRange")}</p>}
    </>
  );
}
