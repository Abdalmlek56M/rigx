"use client";

import { useCallback } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { whatsAppUrl } from "@/lib/contact";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchOrderInvoice, type Order } from "./orders-api";
import { orderWhatsAppNumber } from "./order-rules";
import { DocumentIcon, WhatsAppIcon } from "@/components/ui/icons";
import { buttonClass, buttonIconSize } from "../kit/Button";

/** Open the delivered order's invoice, or send its public link to the customer on WhatsApp. Nothing shows until the invoice loads. */
export function OrderInvoiceLinks({ order }: { order: Pick<Order, "id" | "phoneNumber" | "phoneHasWhatsApp" | "whatsAppNumber"> }) {
  const t = useTranslations("admin.orders.actions.invoice");
  const { state } = useLoad(useCallback(() => fetchOrderInvoice(order.id), [order.id]));
  if (state.status !== "ready") return null;
  const { invoice, invoiceUrl } = state.data;
  const whatsApp = orderWhatsAppNumber(order);
  return (
    <>
      <Link href={`/i/${invoice.accessToken}`} target="_blank" className={buttonClass()}><DocumentIcon size={buttonIconSize()} />{t("open")}</Link>
      {whatsApp ? (
        <a href={whatsAppUrl(whatsApp, invoiceUrl)} target="_blank" rel="noreferrer" className={buttonClass()}><WhatsAppIcon size={buttonIconSize()} />{t("whatsapp")}</a>
      ) : null}
    </>
  );
}
