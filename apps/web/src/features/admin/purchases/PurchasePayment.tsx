"use client";

import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { formatEgp } from "@/lib/format";
import { hasMoney, hasTradeIn, isOverpaid, PAYMENT_METHODS, purchaseTotal, remainingAmount, withMethod, type PurchaseForm } from "./purchase-rules";

/** What the seller is paid: method, money and trade-in, with the total and what is left (the server stays the authority). */
export function PurchasePayment({ form, errors, missing, submitted, onChange }: {
  form: PurchaseForm;
  errors: Record<string, string>;
  missing: readonly string[];
  submitted: boolean;
  onChange: (form: PurchaseForm) => void;
}) {
  const t = useTranslations("admin.purchases.payment");
  const common = useTranslations("admin.purchases");
  const locale = useLocale();
  const set = (patch: Partial<PurchaseForm>) => onChange({ ...form, ...patch });
  const errorFor = (name: string) => errors[name] ?? (submitted && missing.includes(name) ? common("required") : undefined);
  const total = purchaseTotal(form.lines);
  const paid = Number(form.paidAmount) || 0;
  const tradeIn = Number(form.tradeInValue) || 0;

  return (
    <section data-field="payment" className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      <dl className="m-0 grid grid-cols-2 gap-3 rounded-control bg-paper p-3 text-sm">
        <div className="flex flex-col gap-0.5">
          <dt className="text-ink-muted">{t("total")}</dt>
          <dd className="m-0 text-lg font-bold">{formatEgp(total, locale)}</dd>
        </div>
        <div className="flex flex-col gap-0.5">
          <dt className="text-ink-muted">{t("remaining")}</dt>
          <dd className="m-0 text-lg font-bold">{formatEgp(remainingAmount(total, paid, tradeIn), locale)}</dd>
        </div>
      </dl>
      <div className="grid gap-3 sm:grid-cols-2">
        <SelectField
          label={t("method")} name="paymentMethod" value={form.paymentMethod} onChange={(method) => onChange(withMethod(form, method))} error={errors.paymentMethod}
          options={PAYMENT_METHODS.map((method) => ({ value: method, label: t(`methods.${method}`) }))}
        />
        {hasMoney(form.paymentMethod) ? <TextField label={t("paid")} name="paidAmount" type="number" ltr value={form.paidAmount} onChange={(paidAmount) => set({ paidAmount })} error={errorFor("paidAmount")} /> : null}
        {hasTradeIn(form.paymentMethod) ? <TextField label={t("tradeInValue")} name="tradeInValue" type="number" ltr value={form.tradeInValue} onChange={(tradeInValue) => set({ tradeInValue })} error={errorFor("tradeInValue")} /> : null}
        <TextField label={t("date")} name="purchaseDate" type="date" ltr value={form.purchaseDate} onChange={(purchaseDate) => set({ purchaseDate })} error={errors.purchaseDate} />
      </div>
      {hasTradeIn(form.paymentMethod) ? (
        <TextAreaField label={t("tradeInNote")} name="tradeInNote" value={form.tradeInNote} onChange={(tradeInNote) => set({ tradeInNote })} rows={2} maxLength={500} hint={t("tradeInHint")} error={errorFor("tradeInNote")} />
      ) : null}
      {isOverpaid(form) ? <p role="alert" className="m-0 text-sm font-bold text-error">{t("overpaid")}</p> : null}
      <TextAreaField label={t("notes")} name="notes" value={form.notes} onChange={(notes) => set({ notes })} rows={2} maxLength={2000} error={errors.notes} />
    </section>
  );
}
