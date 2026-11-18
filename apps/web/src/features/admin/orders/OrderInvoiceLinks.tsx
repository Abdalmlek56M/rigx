"use client";

import { useCallback } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { whatsAppUrl } from "@/lib/contact";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchOrderInvoice, type Order } from "./orders-api";
import { customerWhatsAppNumber } from "./order-rules";

const LINK = "inline-flex h-11 items-center justify-center rounded-control border border-input-line bg-surface px-4 text-sm font-bold";

/** Open the delivered order's invoice, or send its public link to the customer on WhatsApp. Nothing shows until the invoice loads. */
export function OrderInvoiceLinks({ order }: { order: Pick<Order, "id" | "phoneNumber" | "phoneHasWhatsApp"> }) {
  const t = useTranslations("admin.orders.actions.invoice");
  const { state } = useLoad(useCallback(() => fetchOrderInvoice(order.id), [order.id]));
  if (state.status !== "ready") return null;
  const { invoice, invoiceUrl } = state.data;
  return (
    <>
      <Link href={`/i/${invoice.accessToken}`} target="_blank" className={LINK}>{t("open")}</Link>
      {order.phoneHasWhatsApp ? (
        <a href={whatsAppUrl(customerWhatsAppNumber(order.phoneNumber), invoiceUrl)} target="_blank" rel="noreferrer" className={LINK}>{t("whatsapp")}</a>
      ) : null}
    </>
  );
}
