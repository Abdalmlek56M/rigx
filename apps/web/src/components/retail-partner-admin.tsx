"use client";

import { useEffect, useMemo, useState } from "react";
import {
  getPartnerAdjustments, getPartnerAgreements, getPartnerEarnings, getPartnerLedger, settlePartner, upsertPartnerAgreement,
  type PartnerAdjustment, type PartnerEarning, type PartnerLedger,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, TextField } from "@/components/ui";

export function RetailPartnerAdmin() {
  const { messages } = useLocale();
  const [earnings, setEarnings] = useState<PartnerEarning[]>([]);
  const [adjustments, setAdjustments] = useState<PartnerAdjustment[]>([]);
  const [ledger, setLedger] = useState<PartnerLedger[]>([]);
  const [agreements, setAgreements] = useState<Array<{ partnerName: string; defaultPercent: number; isActive: boolean; lossSharingEnabled: boolean }>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [form, setForm] = useState({ partnerName: "", percent: "10", lossSharing: false });

  async function load() {
    setLoading(true);
    try {
      const [earningList, adjustmentList, ledgerList, agreementList] = await Promise.all([
        getPartnerEarnings(), getPartnerAdjustments(), getPartnerLedger(), getPartnerAgreements(),
      ]);
      setEarnings(earningList); setAdjustments(adjustmentList); setLedger(ledgerList); setAgreements(agreementList);
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
    finally { setLoading(false); }
  }
  useEffect(() => { void load(); /* eslint-disable-next-line */ }, []);

  const partnerNames = useMemo(() => Array.from(new Set([
    ...ledger.map((item) => item.partnerName),
    ...earnings.map((item) => item.partnerName),
    ...adjustments.map((item) => item.partnerName),
  ])), [ledger, earnings, adjustments]);

  async function saveAgreement() {
    try {
      await upsertPartnerAgreement({
        partnerName: form.partnerName, defaultPercent: Number(form.percent), isActive: true,
        lossSharingEnabled: form.lossSharing, effectiveFrom: new Date().toISOString().slice(0, 10), effectiveTo: null,
      });
      setNotice(messages.retailSaved); await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function settle(entry: PartnerLedger) {
    if (entry.cashDue <= 0) return;
    try {
      await settlePartner({ partnerName: entry.partnerName, amount: entry.cashDue, method: "Cash", reference: null, note: "Cash due after recoverable offsets" });
      setNotice(messages.retailSaved); await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }

  return (
    <main className="p-4 sm:p-6 lg:p-8"><div className="mx-auto grid max-w-5xl gap-5">
      <PageIntro eyebrow="RIGX / PARTNERS" title={messages.retailPartnerTitle} lead={messages.retailPartnerLead} />
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}
      <section className="rigx-admin-card p-4">
        <h2 className="font-bold">{messages.retailAgreementTitle}</h2>
        <div className="mt-3 flex flex-wrap items-end gap-2">
          <TextField label={messages.retailPartnerName} value={form.partnerName} onChange={(event) => setForm((current) => ({ ...current, partnerName: event.target.value }))} />
          <TextField label={messages.retailPartnerPercent} value={form.percent} onChange={(event) => setForm((current) => ({ ...current, percent: event.target.value }))} inputMode="decimal" dir="ltr" />
          <label className="flex min-h-11 items-center gap-2 text-sm font-bold">
            <input type="checkbox" checked={form.lossSharing} onChange={(event) => setForm((current) => ({ ...current, lossSharing: event.target.checked }))} />
            {messages.retailLossSharing}
          </label>
          <PrimaryButton type="button" onClick={() => void saveAgreement()}>{messages.adminSaveChanges}</PrimaryButton>
        </div>
        <ul className="mt-3 grid gap-1 text-sm">
          {agreements.map((item) => (
            <li key={item.partnerName}>
              {item.partnerName} · {item.defaultPercent}% · {item.isActive ? messages.retailActive : messages.retailInactive}
              {item.lossSharingEnabled ? ` · ${messages.retailLossSharing}` : ""}
            </li>
          ))}
        </ul>
      </section>

      {loading ? <LoadingCard /> : (
        <>
          <section className="rigx-admin-card p-4">
            <h2 className="font-bold">{messages.retailPartnerLedgerTitle}</h2>
            <div className="mt-3 grid gap-2">
              {ledger.length === 0 ? <Message tone="info">{messages.retailNoEarnings}</Message> : ledger.map((entry) => (
                <div key={entry.partnerName} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-[var(--ds-border)] p-3 text-sm">
                  <span className="font-bold">{entry.partnerName}</span>
                  <span dir="ltr">
                    {messages.retailOutstanding} {entry.outstanding.toLocaleString()} · {messages.retailCashDue} {entry.cashDue.toLocaleString()} · {messages.retailRecoverable} {entry.recoverable.toLocaleString()}
                  </span>
                  <SecondaryButton type="button" onClick={() => void settle(entry)} disabled={entry.cashDue <= 0}>{messages.retailSettleCash}</SecondaryButton>
                </div>
              ))}
            </div>
          </section>

          <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
            <h2 className="p-4 font-bold">{messages.retailEarningsTitle}</h2>
            {earnings.length === 0 ? <Message tone="info">{messages.retailNoEarnings}</Message> : earnings.map((item) => (
              <article key={item.id} className="grid gap-1 p-3 text-sm">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span className="font-bold">{item.partnerName} · {item.percent}% · {item.status}</span>
                  <span dir="ltr">{item.amount.toLocaleString()} / {messages.retailPaid} {item.paidAmount.toLocaleString()} / {messages.retailOffset} {item.offsetAmount.toLocaleString()}</span>
                </div>
                <span className="text-xs text-[var(--ds-text-muted)]" dir="ltr">
                  {messages.retailNetProfit} {item.grossProfit.toLocaleString()} · {messages.retailRevenue} {item.revenueAmount.toLocaleString()} · {messages.retailCost} {item.costAmount.toLocaleString()}
                </span>
              </article>
            ))}
          </section>

          <section className="rigx-admin-card p-4">
            <h2 className="font-bold">{messages.retailAdjustmentsTitle}</h2>
            {adjustments.length === 0 ? <Message tone="info">{messages.retailNoAdjustments}</Message> : (
              <ul className="mt-3 grid gap-1 text-sm">
                {adjustments.map((item) => (
                  <li key={item.id} className="flex flex-wrap items-center justify-between gap-2">
                    <span>{item.partnerName} · {item.reason}</span>
                    <span dir="ltr">{item.amount.toLocaleString()} · {messages.retailApplied} {item.appliedAmount.toLocaleString()} · {item.status}</span>
                  </li>
                ))}
              </ul>
            )}
          </section>
          {partnerNames.length === 0 ? null : (
            <p className="text-xs text-[var(--ds-text-muted)]">{messages.retailPartnerHint}</p>
          )}
        </>
      )}
    </div></main>
  );
}
