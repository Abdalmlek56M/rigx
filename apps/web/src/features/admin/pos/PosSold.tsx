"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { OrderInvoiceLinks } from "../orders/OrderInvoiceLinks";
import { OrderStatusBadge } from "../orders/OrderBadges";
import { setOrderStatus, type Order } from "../orders/orders-api";
import { EyeIcon, PlusIcon, TruckIcon } from "@/components/ui/icons";
import { cardClass } from "@/components/ui/Card";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";

interface PosSoldProps {
  order: Order;
  /** Staff who can move an order along may hand a store sale over right here. */
  canDeliver: boolean;
  onNewSale: () => void;
}

function Figure({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs text-ink-muted">{label}</dt>
      <dd dir={mono ? "ltr" : undefined} className={`m-0 text-start font-bold ${mono ? "font-mono" : ""}`}>{value}</dd>
    </div>
  );
}

/** What the sale became: its numbers, the way to the order, and (for a confirmed pickup sale) "hand over and print the invoice". */
export function PosSold({ order: sold, canDeliver, onNewSale }: PosSoldProps) {
  const t = useTranslations("admin.pos.sold");
  const locale = useLocale();
  const submit = useSubmit();
  const [order, setOrder] = useState<Order>(sold);
  const canHandOver = canDeliver && order.status === "Confirmed" && order.fulfillmentMethod === "Pickup";

  /** Preparing, then Delivered (the server issues the invoice and the warranty then); the order keeps whatever step succeeded. */
  const handOver = () => submit.run(async () => {
    const preparing = await setOrderStatus(order.id, "Preparing", null, order.rowVersion);
    setOrder(preparing);
    setOrder(await setOrderStatus(order.id, "Delivered", null, preparing.rowVersion));
  }, t("delivered"));

  return (
    <section className={cardClass({ className: "gap-4" })}>
      <h2 className="m-0 text-xl font-bold">{sold.status === "New" ? t("pending") : t("confirmed")}</h2>
      <dl className="m-0 grid grid-cols-2 gap-4 sm:grid-cols-5">
        <Figure label={t("reference")} value={order.reference} mono />
        <div className="flex flex-col gap-1">
          <dt className="text-xs text-ink-muted">{t("status")}</dt>
          <dd className="m-0"><OrderStatusBadge status={order.status} /></dd>
        </div>
        <Figure label={t("total")} value={formatEgp(order.total, locale)} mono />
        <Figure label={t("paid")} value={formatEgp(order.paidAmount, locale)} mono />
        <Figure label={t("remaining")} value={formatEgp(order.remaining, locale)} mono />
      </dl>
      <FormMessage message={submit.errors.general} />
      <div className="flex flex-wrap gap-2">
        {canHandOver ? <Button variant="primary" icon={TruckIcon} loading={submit.busy} onClick={() => void handOver()}>{t("deliver")}</Button> : null}
        {order.status === "Delivered" ? <OrderInvoiceLinks order={order} /> : null}
        <Link href={`/admin/orders?id=${order.id}`} className={buttonClass()}><EyeIcon size={buttonIconSize()} />{t("openOrder")}</Link>
        <Button icon={PlusIcon} onClick={onNewSale}>{t("newSale")}</Button>
      </div>
      {submit.dialog}
    </section>
  );
}
