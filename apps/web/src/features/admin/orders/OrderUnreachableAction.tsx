"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { whatsAppUrl } from "@/lib/contact";
import { PhoneOffIcon } from "@/components/ui/icons";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { IconButton } from "../kit/Button";
import { toFormErrors } from "../kit/form-errors";
import { useToast } from "../kit/Toast";
import { usePermissions } from "../use-permissions";
import { canMarkUnreachable, orderWhatsAppNumber } from "./order-rules";
import { markCustomerUnreachable } from "./orders-api";
import { useOrderAction, type OrderActionProps } from "./use-order-action";

/**
 * "Could not reach the customer": records it on the order, emails the customer (when the order has an email) and opens a WhatsApp chat
 * with a prepared message. The chat tab is opened inside the click so the browser allows it, and pointed at WhatsApp once the server
 * has recorded the call.
 */
export function OrderUnreachableAction(props: OrderActionProps) {
  const { order } = props;
  const t = useTranslations("admin.orders.actions.unreachable");
  const kit = useTranslations("admin.kit");
  const toast = useToast();
  const { has } = usePermissions();
  const { apply } = useOrderAction(props);
  const [open, setOpen] = useState(false);
  const whatsApp = orderWhatsAppNumber(order);
  if (!has("Sales.ApproveOrder") || !canMarkUnreachable(order)) return null;

  async function confirm() {
    const chat = whatsApp ? window.open("", "_blank") : null;
    try {
      await apply(() => markCustomerUnreachable(order.id, order.rowVersion));
    } catch (error) {
      chat?.close();
      toast(toFormErrors(error, kit("failed")).general ?? kit("failed"), "error");
      throw error;
    }
    toast(t("done"));
    if (chat && whatsApp) {
      chat.opener = null;
      chat.location.href = whatsAppUrl(whatsApp, t("whatsappText", { name: order.customerName, reference: order.reference }));
    }
  }

  return (
    <>
      <IconButton icon={PhoneOffIcon} label={t("button")} onClick={() => setOpen(true)} />
      <ConfirmDialog
        open={open} title={t("title")} body={order.customerEmail ? t("bodyWithEmail") : t("bodyNoEmail")}
        confirmLabel={t("confirm")} onConfirm={confirm} onClose={() => setOpen(false)}
      />
    </>
  );
}
