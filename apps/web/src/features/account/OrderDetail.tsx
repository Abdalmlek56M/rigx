"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { OrderWhatsAppLink, type OrderWhatsApp } from "@/features/tracking/OrderWhatsAppLink";
import { TrackingTimeline } from "@/features/tracking/TrackingTimeline";
import { orderStatusKey, trackingSteps } from "@/features/tracking/tracking-steps";
import { Link } from "@/i18n/navigation";
import { formatDate, formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { paymentMethodKey } from "./account-labels";
import { fetchOrder, type Order } from "./account-api";
import { LoadBoundary } from "./LoadBoundary";
import { useLoad } from "@/lib/hooks/use-load";

const card = "flex flex-col gap-3 rounded-card border border-line bg-surface p-5 md:p-6";

function Row({ label, value, mono, strong }: { label: string; value: string; mono?: boolean; strong?: boolean }) {
  return (
    <div className={`flex justify-between gap-4 text-sm ${strong ? "font-bold" : ""}`}>
      <dt className={strong ? "" : "text-ink-muted"}>{label}</dt>
      <dd dir={mono ? "ltr" : "auto"} className={`m-0 text-end ${mono ? "font-mono" : ""}`}>{value}</dd>
    </div>
  );
}

function Items({ order }: { order: Order }) {
  const t = useTranslations("account.order");
  const locale = useLocale();
  return (
    <section className={card}>
      <h2 className="m-0 text-lg font-bold">{t("items")}</h2>
      <ul className="m-0 flex list-none flex-col p-0">
        {order.items.map((item, index) => (
          <li key={`${item.code ?? item.name}-${index}`} className="flex items-center gap-3 border-b border-line py-3 last:border-b-0">
            <span className="h-[50px] w-16 shrink-0 overflow-hidden rounded bg-sand">
              {item.thumbnailStorageKey ? (
                // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                <img src={mediaUrl(item.thumbnailStorageKey)} alt="" className="h-full w-full object-contain" />
              ) : null}
            </span>
            <span className="flex min-w-0 flex-1 flex-col">
              <span dir="auto" className="truncate text-start text-sm font-bold [unicode-bidi:isolate]">{item.name}</span>
              {item.code ? <span dir="ltr" className="text-start font-mono text-xs text-ink-muted">{item.code}</span> : null}
            </span>
            <span className="flex shrink-0 flex-col items-end text-sm">
              <b>{formatEgp(item.lineTotal, locale)}</b>
              {item.quantity > 1 ? <span dir="ltr" className="font-mono text-xs text-ink-muted">{item.quantity} × {formatEgp(item.unitPrice, locale)}</span> : null}
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}

function Totals({ order }: { order: Order }) {
  const t = useTranslations("account.order");
  const locale = useLocale();
  const open = !["Delivered", "Completed", "Cancelled"].includes(order.status);
  const money = (value: number) => formatEgp(value, locale);
  return (
    <section className={card}>
      <h2 className="m-0 text-lg font-bold">{t("totals")}</h2>
      <dl className="m-0 flex flex-col gap-2.5">
        <Row label={t("subtotal")} value={money(order.subtotal)} />
        {order.couponDiscount > 0 ? <Row label={t("couponDiscount", { code: order.couponCode ?? "" })} value={`− ${money(order.couponDiscount)}`} /> : null}
        {order.fulfillmentMethod === "Delivery" ? <Row label={t("shipping")} value={money(order.deliveryFee)} /> : null}
        <Row label={t("total")} value={money(order.total)} strong />
        <Row label={t("paid")} value={money(order.paidAmount)} />
        {order.refundedAmount > 0 ? <Row label={t("refunded")} value={money(order.refundedAmount)} /> : null}
        {open ? <Row label={t("remaining")} value={money(order.remaining)} /> : null}
        {open && order.upfrontDue > 0 ? <Row label={t("upfrontDue")} value={money(order.upfrontDue)} /> : null}
      </dl>
    </section>
  );
}

function Delivery({ order, pickupAddress }: { order: Order; pickupAddress: string | null }) {
  const t = useTranslations("account.order");
  const tPay = useTranslations("checkout.payment.methods");
  const payment = paymentMethodKey(order.paymentMethod);
  const pickup = order.fulfillmentMethod === "Pickup";
  const address = order.address ? [order.address.governorate, order.address.area, order.address.street, order.address.building, order.address.landmark].filter(Boolean).join("، ") : "";
  return (
    <section className={card}>
      <h2 className="m-0 text-lg font-bold">{pickup ? t("pickupTitle") : t("deliveryTitle")}</h2>
      <dl className="m-0 flex flex-col gap-2.5">
        <Row label={pickup ? t("pickupFrom") : t("address")} value={pickup ? (pickupAddress ?? t("pickupShop")) : address || "—"} />
        {order.deliveryTime && !pickup ? <Row label={t("deliveryTime")} value={order.deliveryTime} /> : null}
        {payment ? <Row label={t("payment")} value={tPay(payment)} /> : null}
        {order.courierName ? <Row label={t("courier")} value={order.courierName} /> : null}
        {order.waybillNumber ? <Row label={t("waybill")} value={order.waybillNumber} mono /> : null}
      </dl>
    </section>
  );
}

function OrderBody({ order, whatsApp, pickupAddress }: { order: Order; whatsApp: OrderWhatsApp; pickupAddress: string | null }) {
  const t = useTranslations("account.order");
  const tStatus = useTranslations("orderStatus");
  const locale = useLocale();
  const status = orderStatusKey(order.status);
  const button = "flex h-11 items-center justify-center gap-2 rounded-control px-5 font-bold";

  return (
    <div className="flex flex-col gap-4">
      <Link href="/account" className="inline-flex min-h-11 items-center text-sm text-orange-text underline hover:text-orange-text-hover">{t("back")}</Link>
      <header className="flex flex-col gap-1">
        <span dir="ltr" className="font-mono text-sm font-semibold text-orange-text">{order.reference}</span>
        <h2 className="m-0 text-2xl font-bold">{tStatus(status)}</h2>
        <span className="text-sm text-ink-muted">{formatDate(order.createdAtUtc, locale)}</span>
      </header>
      <section aria-label={t("timeline")} className="rounded-card border border-line bg-surface p-5 pb-0 md:p-6 md:pb-1">
        <TrackingTimeline steps={trackingSteps(order.status, order.fulfillmentMethod, order.timeline)} />
      </section>
      <Items order={order} />
      <Totals order={order} />
      <Delivery order={order} pickupAddress={pickupAddress} />
      <div className="flex flex-wrap gap-3">
        <OrderWhatsAppLink whatsApp={whatsApp} reference={order.reference} label={t("ask")} className={`${button} bg-carbon text-on-carbon`} />
        <Link href={`/t/${order.trackingToken}`} className={`${button} border border-input-line bg-surface`}>{t("track")}</Link>
      </div>
    </div>
  );
}

/** One of the person's orders in full; a missing order says so. */
export function OrderDetail({ id, whatsApp, pickupAddress }: { id: string; whatsApp: OrderWhatsApp; pickupAddress: string | null }) {
  const t = useTranslations("account.order");
  const load = useCallback(() => fetchOrder(id), [id]);
  const { state, reload } = useLoad(load);

  return (
    <LoadBoundary state={state} reload={reload}>
      {(order) =>
        order ? (
          <OrderBody order={order} whatsApp={whatsApp} pickupAddress={pickupAddress} />
        ) : (
          <div role="status" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
            <p className="m-0 text-lg font-bold">{t("notFound")}</p>
            <Link href="/account" className="text-orange-text underline hover:text-orange-text-hover">{t("back")}</Link>
          </div>
        )
      }
    </LoadBoundary>
  );
}
