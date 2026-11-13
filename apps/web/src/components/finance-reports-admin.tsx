"use client";

import { useEffect, useState } from "react";
import { createExpense, getBusinessReport, getExpenses, type BusinessReport, type ExpenseRecord } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, TextField } from "@/components/ui";

function today() { return new Date().toISOString().slice(0, 10); }

export function FinanceReportsAdmin() {
  const { messages } = useLocale();
  const [from, setFrom] = useState(() => { const d = new Date(); d.setDate(1); return d.toISOString().slice(0, 10); });
  const [to, setTo] = useState(today());
  const [report, setReport] = useState<BusinessReport | null>(null);
  const [expenses, setExpenses] = useState<ExpenseRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ amount: "", category: "", classification: "Direct", note: "" });

  async function load() {
    setLoading(true); setError(null);
    try {
      const [reportData, expenseData] = await Promise.all([getBusinessReport(from, to), getExpenses(from, to)]);
      setReport(reportData); setExpenses(expenseData);
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
    finally { setLoading(false); }
  }
  useEffect(() => { void load(); /* eslint-disable-next-line */ }, []);

  async function addExpense() {
    const amount = Number(form.amount);
    if (!form.category.trim() || !Number.isFinite(amount) || amount <= 0) { setError(messages.retailInvalidPrice); return; }
    try {
      await createExpense({ expenseDate: to, amount, category: form.category.trim(), classification: form.classification, note: form.note.trim() || null, orderId: null, unitId: null, attachmentReference: null });
      setForm({ amount: "", category: "", classification: "Direct", note: "" });
      await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }

  const rows: Array<[string, number]> = report ? [
    [messages.financeGrossSales, report.grossSales], [messages.financeDiscounts, report.discounts],
    [messages.financeRefunds, report.refunds], [messages.financeCogs, report.costOfGoods],
    [messages.financeItemDirect, report.itemDirectExpenses], [messages.financeAdditionalDirect, report.additionalDirectExpenses],
    [messages.financeNetItem, report.netItemProfit], [messages.financeOverhead, report.generalOverhead],
    [messages.financeStoreNet, report.storeNetResult], [messages.financePartnerAccrued, report.partnerAccrued],
    [messages.financePartnerPaid, report.partnerPaid], [messages.financePartnerRecoverable, report.partnerRecoverable],
  ] : [];

  return (
    <main className="p-4 sm:p-6 lg:p-8"><div className="mx-auto grid max-w-6xl gap-5">
      <PageIntro eyebrow="RIGX / FINANCE" title={messages.financeTitle} lead={messages.financeLead} />
      {error ? <Message tone="error">{error}</Message> : null}
      <section className="rigx-admin-card flex flex-wrap items-end gap-3 p-4">
        <TextField label={messages.financeFrom} value={from} onChange={(event) => setFrom(event.target.value)} type="date" dir="ltr" />
        <TextField label={messages.financeTo} value={to} onChange={(event) => setTo(event.target.value)} type="date" dir="ltr" />
        <SecondaryButton type="button" onClick={() => void load()}>{messages.financeRefresh}</SecondaryButton>
      </section>
      {loading ? <LoadingCard /> : report ? (
        <>
          <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {rows.map(([label, value]) => (
              <div key={label} className="rigx-admin-card p-4"><p className="text-xs font-bold text-[var(--ds-text-muted)]">{label}</p><p className="text-xl font-black" dir="ltr">{value.toLocaleString()} EGP</p></div>
            ))}
          </section>
          <section className="rigx-admin-card grid gap-3 p-4">
            <h2 className="font-bold">{messages.financeExpenseTitle}</h2>
            <div className="grid gap-2 sm:grid-cols-4">
              <TextField label={messages.financeAmount} value={form.amount} onChange={(event) => setForm({ ...form, amount: event.target.value })} inputMode="decimal" dir="ltr" />
              <TextField label={messages.financeCategory} value={form.category} onChange={(event) => setForm({ ...form, category: event.target.value })} />
              <select value={form.classification} onChange={(event) => setForm({ ...form, classification: event.target.value })} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm">
                <option value="Direct">{messages.financeDirect}</option>
                <option value="GeneralOverhead">{messages.financeOverheadOption}</option>
              </select>
              <PrimaryButton type="button" onClick={() => void addExpense()}>{messages.financeAddExpense}</PrimaryButton>
            </div>
            <ul className="grid gap-1 text-sm">{expenses.map((item) => <li key={item.id} className="flex flex-wrap justify-between gap-2"><span>{item.expenseDate} · {item.category} · {item.classification}</span><span dir="ltr">{item.amount.toLocaleString()} EGP</span></li>)}</ul>
          </section>
        </>
      ) : null}
    </div></main>
  );
}
