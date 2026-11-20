"use client";

import { useRef, type ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import { FormSection } from "@/features/checkout/form-parts";
import { formatEgp } from "@/lib/format";
import { STORE_PAYMENT_METHODS, type StorePaymentMethod } from "../orders/order-rules";
import { MoneyInput, usePosError, type SetForm } from "./pos-parts";
import { fillRemaining, moneyValid, paidTotal, paymentState, type PaymentRow, type PosField, type PosForm } from "./pos-rules";
import { CloseIcon, PlusIcon, WalletIcon } from "@/components/ui/icons";
import { Button, IconButton } from "../kit/Button";

interface PosPaymentsProps {
  form: PosForm;
  set: SetForm;
  errors: readonly PosField[];
  /** The server's total for the cart (after the discount); null before the first preview. */
  total: number | null;
}

/** One picture per method, so the cashier reads a row at a glance. */
const METHOD_ICON: Record<StorePaymentMethod, string> = { Cash: "💵", InstaPay: "⚡", Wallet: "📱", BankTransfer: "🏦" };

function Line({ icon, label, children, tone }: { icon: string; label: string; children: ReactNode; tone?: "due" | "done" | "error" }) {
  const color = tone === "due" ? "text-orange" : tone === "done" ? "text-success" : tone === "error" ? "text-error" : "";
  return (
    <div className="flex min-h-11 items-center justify-between gap-3">
      <dt className="flex items-center gap-2 text-sm text-ink-muted"><span aria-hidden className="text-base">{icon}</span>{label}</dt>
      <dd className={`m-0 text-end font-bold tabular-nums ${color}`}>{children}</dd>
    </div>
  );
}

/** How the customer pays: one row per method (several rows split the payment), the discount, and paid / remaining against the total. */
export function PosPayments({ form, set, errors, total }: PosPaymentsProps) {
  const t = useTranslations("admin.pos.payments");
  const methods = useTranslations("admin.orders.methods");
  const locale = useLocale();
  const error = usePosError(errors);
  const nextKey = useRef(1);
  const rows = form.payments;
  const paid = paidTotal(rows);
  const state = paymentState(total, paid);
  const egp = (amount: number) => <span dir="ltr">{formatEgp(amount, locale)}</span>;
  const setRows = (next: PaymentRow[]) => set("payments", next);
  const patch = (key: string, change: Partial<PaymentRow>) => setRows(rows.map((row) => (row.key === key ? { ...row, ...change } : row)));
  const problems = (["payments", "overpaid", "discount"] as const).map(error).filter((message) => message !== null);

  return (
    <FormSection title={t("title")}>
      <ul aria-label={t("methods")} className="m-0 flex list-none flex-col gap-2 p-0">
        {rows.map((row) => (
          <li key={row.key} className="flex flex-wrap items-center gap-2 rounded-control border border-line bg-paper p-2">
            <span aria-hidden className="flex h-11 w-11 shrink-0 items-center justify-center rounded-control bg-surface text-xl">{METHOD_ICON[row.method]}</span>
            <select
              aria-label={t("method")} value={row.method} onChange={(event) => patch(row.key, { method: event.target.value as PaymentRow["method"] })}
              className="h-11 min-w-0 flex-1 rounded-control border border-input-line bg-surface px-2.5 text-sm font-bold sm:flex-none sm:basis-48"
            >
              {STORE_PAYMENT_METHODS.map((method) => <option key={method} value={method}>{methods(method)}</option>)}
            </select>
            <MoneyInput
              label={t("amount")} placeholder={t("amount")} value={row.amount} onChange={(amount) => patch(row.key, { amount })}
              invalid={row.amount.trim() !== "" && !moneyValid(row.amount)} className="min-w-0 flex-1"
            />
            {rows.length > 1 ? (
              <IconButton icon={CloseIcon} label={t("remove")} variant="ghost" onClick={() => setRows(rows.filter((other) => other.key !== row.key))} />
            ) : null}
          </li>
        ))}
      </ul>
      <div className="flex flex-wrap gap-2">
        <Button size="sm" icon={PlusIcon} onClick={() => setRows([...rows, { key: `p${nextKey.current++}`, method: "Cash", amount: "" }])}>{t("add")}</Button>
        {state.kind === "due" ? <Button size="sm" icon={WalletIcon} onClick={() => setRows(fillRemaining(rows, total ?? 0))}>{t("fill")}</Button> : null}
      </div>

      <dl aria-label={t("summary")} className="m-0 flex flex-col divide-y divide-line rounded-card border border-line px-4 py-1">
        <Line icon="🏷️" label={t("discount")}>
          <MoneyInput label={t("discount")} placeholder="0" value={form.discount} onChange={(value) => set("discount", value)} invalid={error("discount") !== null} className="w-32 font-normal" />
        </Line>
        <Line icon="🧾" label={t("total")}>
          {total === null ? <span className="text-sm font-normal text-ink-muted">{t("unpriced")}</span> : egp(total)}
        </Line>
        <Line icon="✅" label={t("paid")}>{egp(paid)}</Line>
        {state.kind === "due" ? <Line icon="⏳" label={t("remaining")} tone="due">{egp(state.amount)}</Line> : null}
        {state.kind === "settled" ? <Line icon="🎉" label={t("remaining")} tone="done">{t("settled")}</Line> : null}
        {state.kind === "over" ? <Line icon="⚠️" label={t("over")} tone="error">{egp(state.amount)}</Line> : null}
      </dl>
      {problems.map((message) => <p key={message} role="alert" className="m-0 text-xs text-error">{message}</p>)}
    </FormSection>
  );
}
