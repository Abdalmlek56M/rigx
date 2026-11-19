"use client";

import { useRef } from "react";
import { useLocale, useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { FormSection } from "@/features/checkout/form-parts";
import { formatEgp } from "@/lib/format";
import { STORE_PAYMENT_METHODS } from "../orders/order-rules";
import { MoneyInput, usePosError, type SetForm } from "./pos-parts";
import { fillRemaining, moneyValid, paidTotal, type PaymentRow, type PosField, type PosForm } from "./pos-rules";
import { CloseIcon, PlusIcon, WalletIcon } from "@/components/ui/icons";
import { Button, IconButton } from "../kit/Button";

interface PosPaymentsProps {
  form: PosForm;
  set: SetForm;
  errors: readonly PosField[];
  /** The server's total for the cart; null before the first preview. */
  total: number | null;
}

/** The discount and how the customer pays: one row per method (several rows split the payment). */
export function PosPayments({ form, set, errors, total }: PosPaymentsProps) {
  const t = useTranslations("admin.pos.payments");
  const methods = useTranslations("admin.orders.methods");
  const locale = useLocale();
  const error = usePosError(errors);
  const nextKey = useRef(1);
  const rows = form.payments;
  const paid = paidTotal(rows);
  const setRows = (next: PaymentRow[]) => set("payments", next);
  const patch = (key: string, change: Partial<PaymentRow>) => setRows(rows.map((row) => (row.key === key ? { ...row, ...change } : row)));
  const problems = (["payments", "overpaid"] as const).map(error).filter((message) => message !== null);

  return (
    <FormSection title={t("title")}>
      <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
        {rows.map((row) => (
          <li key={row.key} className="flex flex-wrap items-center gap-2">
            <select
              aria-label={t("method")} value={row.method} onChange={(event) => patch(row.key, { method: event.target.value as PaymentRow["method"] })}
              className="h-11 min-w-0 flex-1 rounded-control border border-input-line bg-surface px-2.5 text-sm sm:flex-none sm:basis-48"
            >
              {STORE_PAYMENT_METHODS.map((method) => <option key={method} value={method}>{methods(method)}</option>)}
            </select>
            <MoneyInput label={t("amount")} value={row.amount} onChange={(amount) => patch(row.key, { amount })} invalid={row.amount.trim() !== "" && !moneyValid(row.amount)} className="min-w-0 flex-1 sm:flex-none sm:basis-40" />
            {rows.length > 1 ? (
              <IconButton icon={CloseIcon} label={t("remove")} variant="secondary" onClick={() => setRows(rows.filter((other) => other.key !== row.key))} />
            ) : null}
          </li>
        ))}
      </ul>
      <div className="flex flex-wrap gap-2">
        <Button icon={PlusIcon} onClick={() => setRows([...rows, { key: `p${nextKey.current++}`, method: "Cash", amount: "" }])}>{t("add")}</Button>
        {total !== null ? <Button icon={WalletIcon} onClick={() => setRows(fillRemaining(rows, total))}>{t("fill")}</Button> : null}
      </div>
      <dl className="m-0 grid grid-cols-2 gap-3 text-sm">
        <div className="flex flex-col gap-0.5">
          <dt className="text-xs text-ink-muted">{t("paid")}</dt>
          <dd dir="ltr" className="m-0 text-start font-bold">{formatEgp(paid, locale)}</dd>
        </div>
        <div className="flex flex-col gap-0.5">
          <dt className="text-xs text-ink-muted">{t("remaining")}</dt>
          <dd dir="ltr" className="m-0 text-start font-bold">{total === null ? "—" : formatEgp(Math.max(0, Math.round((total - paid) * 100) / 100), locale)}</dd>
        </div>
      </dl>
      {problems.map((message) => <p key={message} role="alert" className="m-0 text-xs text-error">{message}</p>)}
      <div className="max-w-60">
        <TextField label={t("discount")} name="discount" ltr value={form.discount} onChange={(value) => set("discount", value)} error={error("discount")} />
      </div>
    </FormSection>
  );
}
