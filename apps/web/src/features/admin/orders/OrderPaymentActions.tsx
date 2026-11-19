"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { formatEgp } from "@/lib/format";
import { usePermissions } from "../use-permissions";
import { FormMessage } from "../kit/PageHeader";
import { ReasonDialog } from "./ReasonDialog";
import { addOrderPayment, setPaymentStatus, waivePayment } from "./orders-api";
import {
  canSetPaymentStatus, canTakePayment, canWaivePayment, paymentAmountValid, SETTABLE_PAYMENT_STATUSES, STORE_PAYMENT_METHODS,
  waiverReasonValid, type StorePaymentMethod
} from "./order-rules";
import { useOrderAction, type OrderActionProps } from "./use-order-action";
import { BanIcon, CheckIcon, WalletIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

function RecordPayment(props: OrderActionProps) {
  const { order } = props;
  const t = useTranslations("admin.orders");
  const locale = useLocale();
  const { submit, run } = useOrderAction(props);
  const [method, setMethod] = useState<StorePaymentMethod>("Cash");
  const [amount, setAmount] = useState(String(order.remaining));
  const [note, setNote] = useState("");

  return (
    <form
      className="flex flex-col gap-2"
      onSubmit={(event) => {
        event.preventDefault();
        void run(() => addOrderPayment(order.id, method, Number(amount), note.trim() || null, order.rowVersion), t("payment.record.done"));
      }}
    >
      <h3 className="m-0 text-sm font-bold">{t("payment.record.title")}</h3>
      <SelectField
        label={t("payment.record.method")} name="method" value={method} onChange={setMethod} error={submit.errors.fields.method}
        options={STORE_PAYMENT_METHODS.map((value) => ({ value, label: t(`methods.${value}`) }))}
      />
      <TextField label={t("payment.record.amount")} name="amount" type="number" ltr value={amount} onChange={setAmount} error={submit.errors.fields.amount} />
      <p className="m-0 text-xs text-ink-muted">{t("payment.record.hint", { amount: formatEgp(order.remaining, locale) })}</p>
      <TextField label={t("payment.record.note")} name="note" value={note} onChange={setNote} error={submit.errors.fields.note} />
      <FormMessage message={submit.errors.general} />
      <Button type="submit" icon={WalletIcon} className="self-start" loading={submit.busy} disabled={!paymentAmountValid(amount, order.remaining)}>{t("payment.record.submit")}</Button>
      {submit.dialog}
    </form>
  );
}

function PaymentStatusForm(props: OrderActionProps) {
  const { order } = props;
  const t = useTranslations("admin.orders");
  const { submit, run } = useOrderAction(props);
  const [status, setStatus] = useState<(typeof SETTABLE_PAYMENT_STATUSES)[number]>(SETTABLE_PAYMENT_STATUSES.find((value) => value === order.paymentStatus) ?? "Unpaid");
  const [note, setNote] = useState("");

  return (
    <form
      className="flex flex-col gap-2"
      onSubmit={(event) => {
        event.preventDefault();
        void run(() => setPaymentStatus(order.id, status, note.trim() || null, order.rowVersion), t("payment.status.done"));
      }}
    >
      <h3 className="m-0 text-sm font-bold">{t("payment.status.title")}</h3>
      <SelectField
        label={t("payment.status.label")} name="paymentStatus" value={status} onChange={setStatus} error={submit.errors.fields.paymentStatus}
        options={SETTABLE_PAYMENT_STATUSES.map((value) => ({ value, label: t(`paymentStatuses.${value}`) }))}
      />
      <p className="m-0 text-xs text-ink-muted">{t("payment.status.hint")}</p>
      <TextField label={t("payment.status.note")} name="statusNote" value={note} onChange={setNote} error={submit.errors.fields.notes} />
      <FormMessage message={submit.errors.general} />
      <Button type="submit" icon={CheckIcon} className="self-start" loading={submit.busy}>{t("payment.status.submit")}</Button>
      {submit.dialog}
    </form>
  );
}

/** Record money, set the verified-transfer status, or waive the payment (needs `Finance.VerifyPayment`). */
export function OrderPaymentActions(props: OrderActionProps) {
  const { order } = props;
  const t = useTranslations("admin.orders");
  const { has } = usePermissions();
  const { apply } = useOrderAction(props);
  const [waiving, setWaiving] = useState(false);
  if (!has("Finance.VerifyPayment")) return null;
  const take = canTakePayment(order);
  const setStatus = canSetPaymentStatus(order);
  const waive = canWaivePayment(order);
  if (!take && !setStatus && !waive) return null;

  return (
    <section className="flex flex-col gap-4 border-t border-line pt-4">
      <h3 className="m-0 text-[15px] font-bold">{t("payment.title")}</h3>
      {take ? <RecordPayment {...props} /> : null}
      {setStatus ? <PaymentStatusForm {...props} /> : null}
      {waive ? (
        <>
          <Button icon={BanIcon} className="self-start" onClick={() => setWaiving(true)}>{t("payment.waive.button")}</Button>
          <ReasonDialog
            open={waiving} title={t("payment.waive.title")} body={t("payment.waive.body")} label={t("payment.waive.reason")} field="reason"
            maxLength={500} valid={waiverReasonValid} confirmLabel={t("payment.waive.confirm")} success={t("payment.waive.done")}
            onClose={() => setWaiving(false)} onConfirm={(reason) => apply(() => waivePayment(order.id, reason, order.rowVersion))}
          />
        </>
      ) : null}
    </section>
  );
}
