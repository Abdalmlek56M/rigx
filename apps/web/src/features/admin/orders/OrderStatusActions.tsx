"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { formatEgp } from "@/lib/format";
import { usePermissions } from "../use-permissions";
import { FormMessage } from "../kit/PageHeader";
import { OrderInvoiceLinks } from "./OrderInvoiceLinks";
import { ReasonDialog } from "./ReasonDialog";
import { completeOrder, setOrderStatus } from "./orders-api";
import { forwardKey } from "./order-labels";
import { canCancel, completeBlock, hasInvoice, nextStatus, nextStatusBlock } from "./order-rules";
import { useOrderAction, type OrderActionProps } from "./use-order-action";

const SECONDARY = "h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold disabled:opacity-50";

/** The forward step, complete, cancel and the invoice. */
export function OrderStatusActions(props: OrderActionProps) {
  const { order } = props;
  const t = useTranslations("admin.orders.actions");
  const locale = useLocale();
  const { has } = usePermissions();
  const { submit, run, apply } = useOrderAction(props);
  const [note, setNote] = useState("");
  const [cancelling, setCancelling] = useState(false);
  const next = nextStatus(order);
  const forward = forwardKey(next);
  const block = nextStatusBlock(order);
  const complete = order.status === "Delivered" && has("Sales.CompleteOrder");
  const completeReason = complete ? completeBlock(order) : null;
  const approve = has("Sales.ApproveOrder");

  return (
    <section className="flex flex-col gap-3">
      {approve && next && forward ? (
        <form
          className="flex flex-col gap-2"
          onSubmit={(event) => {
            event.preventDefault();
            void run(async () => {
              const updated = await setOrderStatus(order.id, next, note.trim() || null, order.rowVersion);
              setNote("");
              return updated;
            }, t("statusDone"));
          }}
        >
          <TextField label={t("noteLabel")} name="notes" value={note} onChange={setNote} error={submit.errors.fields.notes} />
          <button type="submit" disabled={submit.busy || block !== null} className="h-11 rounded-control bg-orange px-4 text-sm font-bold text-on-orange disabled:opacity-50">
            {t(`next.${forward}`)}
          </button>
          {block ? <p className="m-0 text-xs text-error">{t(`blocks.${block}`, { amount: formatEgp(order.upfrontDue, locale) })}</p> : null}
        </form>
      ) : null}
      {complete ? (
        <div className="flex flex-col gap-2">
          <button
            type="button" disabled={submit.busy || completeReason !== null}
            onClick={() => void run(() => completeOrder(order.id, null, order.rowVersion), t("complete.done"))}
            className="h-11 rounded-control bg-success px-4 text-sm font-bold text-surface disabled:opacity-50"
          >
            {t("complete.button")}
          </button>
          {completeReason ? <p className="m-0 text-xs text-error">{t(`complete.blocks.${completeReason}`)}</p> : null}
        </div>
      ) : null}
      <FormMessage message={submit.errors.general} />
      {hasInvoice(order) ? <OrderInvoiceLinks order={order} /> : null}
      {approve && canCancel(order) ? (
        <>
          <button type="button" disabled={submit.busy} onClick={() => setCancelling(true)} className={`${SECONDARY} text-error`}>{t("cancel.button")}</button>
          <ReasonDialog
            open={cancelling} title={t("cancel.title")} body={t("cancel.body")} label={t("cancel.notes")} field="notes" maxLength={500}
            confirmLabel={t("cancel.confirm")} danger success={t("cancel.done")} onClose={() => setCancelling(false)}
            onConfirm={(text) => apply(() => setOrderStatus(order.id, "Cancelled", text || null, order.rowVersion))}
          />
        </>
      ) : null}
      {submit.dialog}
    </section>
  );
}
