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
import { ArrowEndIcon, BanIcon, CheckIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

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
          <Button type="submit" variant="primary" icon={ArrowEndIcon} loading={submit.busy} disabled={block !== null}>
            {t(`next.${forward}`)}
          </Button>
          {block ? <p className="m-0 text-xs text-error-text">{t(`blocks.${block}`, { amount: formatEgp(order.upfrontDue, locale) })}</p> : null}
        </form>
      ) : null}
      {complete ? (
        <div className="flex flex-col gap-2">
          <Button
            variant="primary" icon={CheckIcon} loading={submit.busy} disabled={completeReason !== null}
            onClick={() => void run(() => completeOrder(order.id, null, order.rowVersion), t("complete.done"))}
          >
            {t("complete.button")}
          </Button>
          {completeReason ? <p className="m-0 text-xs text-error-text">{t(`complete.blocks.${completeReason}`)}</p> : null}
        </div>
      ) : null}
      <FormMessage message={submit.errors.general} />
      {hasInvoice(order) ? <OrderInvoiceLinks order={order} /> : null}
      {approve && canCancel(order) ? (
        <>
          <Button variant="danger" icon={BanIcon} disabled={submit.busy} onClick={() => setCancelling(true)}>{t("cancel.button")}</Button>
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
