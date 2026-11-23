"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { formatEgp, joinValues } from "@/lib/format";
import type { MoneyAccount } from "../expenses/expenses-api";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { addOrderPayment, type Order } from "./orders-api";
import { paymentAmountValid, STORE_PAYMENT_METHODS, type StorePaymentMethod } from "./order-rules";
import { useOrderAction } from "./use-order-action";
import { ArrowBackIcon, WalletIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** Record a store payment: the form first, then one review line before the money is written. */
export function RecordPaymentDialog({ order, accounts, onUpdated, onStale, onClose }: {
  order: Order;
  accounts: readonly MoneyAccount[] | null;
  onUpdated: (order: Order) => void;
  onStale: () => void;
  onClose: () => void;
}) {
  const t = useTranslations("admin.orders.payment.record");
  const locale = useLocale();
  const { submit, run } = useOrderAction({ onUpdated, onStale });
  const [step, setStep] = useState<"form" | "confirm">("form");
  const [method, setMethod] = useState<StorePaymentMethod>("Cash");
  const [account, setAccount] = useState("");
  const [amount, setAmount] = useState(String(order.remaining));
  const [note, setNote] = useState("");
  const valid = paymentAmountValid(amount, order.remaining);
  // Store methods share their names with the account kinds, so only the accounts that take this method are offered.
  const matching = accounts?.filter((entry) => entry.kind === method) ?? [];

  async function confirm() {
    await run(async () => {
      const updated = await addOrderPayment(order.id, method, Number(amount), note.trim() || null, order.rowVersion, account || null);
      onClose();
      return updated;
    }, t("done"));
  }

  return (
    <Dialog open onClose={onClose} title={t("title")}>
      {step === "form" ? (
        <form
          className="flex flex-col gap-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (valid) setStep("confirm");
          }}
        >
          <SelectField
            label={t("method")} name="method" value={method} onChange={(value) => { setMethod(value as StorePaymentMethod); setAccount(""); }}
            options={STORE_PAYMENT_METHODS.map((value) => ({ value, label: t(`methods.${value}`) }))} error={submit.errors.fields.method}
          />
          {accounts !== null ? (
            <SelectField
              label={t("account")} name="moneyAccountId" value={account} onChange={setAccount}
              options={[{ value: "", label: t("defaultAccount") }, ...matching.map((entry) => ({ value: entry.id, label: joinValues([entry.name, formatEgp(entry.balance, locale)], locale) }))]}
              error={submit.errors.fields.moneyAccountId}
            />
          ) : null}
          <TextField label={t("amount")} name="amount" type="number" ltr value={amount} onChange={setAmount} error={submit.errors.fields.amount} />
          <p className="m-0 text-xs text-ink-muted">{t("hint", { amount: formatEgp(order.remaining, locale) })}</p>
          <TextField label={t("note")} name="note" value={note} onChange={(value) => setNote(value.slice(0, 500))} error={submit.errors.fields.note} />
          <FormMessage message={submit.errors.general} />
          <div className="flex flex-wrap justify-end gap-2">
            <Button variant="ghost" onClick={onClose}>{t("cancel")}</Button>
            <Button type="submit" variant="primary" icon={WalletIcon} disabled={!valid}>{t("review")}</Button>
          </div>
          {submit.dialog}
        </form>
      ) : (
        <div className="flex flex-col gap-3">
          <p className="m-0 text-sm">{t("confirmLine", { amount: formatEgp(Number(amount), locale), method: t(`adverbs.${method}`), reference: order.reference })}</p>
          <FormMessage message={submit.errors.general} />
          <div className="flex flex-wrap justify-end gap-2">
            <Button icon={ArrowBackIcon} onClick={() => setStep("form")}>{t("back")}</Button>
            <Button variant="primary" icon={WalletIcon} loading={submit.busy} onClick={() => void confirm()}>{t("confirm")}</Button>
          </div>
          {submit.dialog}
        </div>
      )}
    </Dialog>
  );
}
