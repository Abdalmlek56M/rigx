"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import type { MoneyAccount } from "../expenses/expenses-api";
import { usePermissions } from "../use-permissions";
import { FormMessage } from "../kit/PageHeader";
import { ReasonDialog } from "./ReasonDialog";
import { RecordPaymentDialog } from "./RecordPaymentDialog";
import { setPaymentStatus, waiveDeposit, waivePayment } from "./orders-api";
import {
  canSetPaymentStatus, canTakePayment, canWaiveDeposit, canWaivePayment, SETTABLE_PAYMENT_STATUSES, waiverReasonValid
} from "./order-rules";
import { useOrderAction, type OrderActionProps } from "./use-order-action";
import { BanIcon, CheckIcon, WalletIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

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

/** Record money in a review dialog, set the verified-transfer status, or waive the payment (needs `Finance.VerifyPayment`). */
export function OrderPaymentActions(props: OrderActionProps & { accounts: readonly MoneyAccount[] | null }) {
  const { order, accounts } = props;
  const t = useTranslations("admin.orders");
  const { has } = usePermissions();
  const { apply } = useOrderAction(props);
  const [recording, setRecording] = useState(false);
  const [waiving, setWaiving] = useState(false);
  const [waivingDeposit, setWaivingDeposit] = useState(false);
  if (!has("Finance.VerifyPayment")) return null;
  const take = canTakePayment(order);
  const setStatus = canSetPaymentStatus(order);
  const waive = canWaivePayment(order);
  const waiveDepositOnly = canWaiveDeposit(order);
  if (!take && !setStatus && !waive && !waiveDepositOnly) return null;

  return (
    <div className="flex flex-col gap-4">
      {take ? <Button variant="primary" icon={WalletIcon} className="self-start" onClick={() => setRecording(true)}>{t("payment.record.button")}</Button> : null}
      {recording ? (
        <RecordPaymentDialog
          order={order} accounts={accounts} onUpdated={props.onUpdated} onStale={props.onStale} onClose={() => setRecording(false)}
        />
      ) : null}
      {setStatus ? <PaymentStatusForm {...props} /> : null}
      {waiveDepositOnly ? (
        <>
          <Button icon={BanIcon} className="self-start" onClick={() => setWaivingDeposit(true)}>{t("payment.waiveDeposit.button")}</Button>
          <ReasonDialog
            open={waivingDeposit} title={t("payment.waiveDeposit.title")} body={t("payment.waiveDeposit.body", { amount: order.depositAmount ?? 0 })}
            label={t("payment.waiveDeposit.reason")} field="reason" maxLength={500} valid={waiverReasonValid}
            confirmLabel={t("payment.waiveDeposit.confirm")} success={t("payment.waiveDeposit.done")}
            onClose={() => setWaivingDeposit(false)} onConfirm={(reason) => apply(() => waiveDeposit(order.id, reason, order.rowVersion))}
          />
        </>
      ) : null}
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
    </div>
  );
}
