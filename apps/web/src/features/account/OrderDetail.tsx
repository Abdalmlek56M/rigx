"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { OrderWhatsAppLink, type OrderWhatsApp } from "@/features/tracking/OrderWhatsAppLink";
import { TrackingTimeline } from "@/features/tracking/TrackingTimeline";
import { orderStatusKey, trackingSteps } from "@/features/tracking/tracking-steps";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { orderTone } from "@/components/ui/status-badge-rules";
import { ArrowBackIcon, BoxIcon, ClockIcon, MapPinIcon, TruckIcon, WalletIcon } from "@/components/ui/icons";
import { formatDate, formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { paymentMethodKey } from "./account-labels";
import { fetchOrder, type Order } from "./account-api";
import { LoadBoundary } from "./LoadBoundary";
import { useLoad } from "@/lib/hooks/use-load";

function Items({ order }: { order: Order }) {
  const t = useTranslations("account.order");
  const locale = useLocale();
  return (
    <Card title={t("items")} icon={BoxIcon} padding="md" className="gap-3">
      <ul className="m-0 flex list-none flex-col p-0">
        {order.items.map((item, index) => (
          <li key={`${item.code ?? item.name}-${index}`} className="flex items-center gap-3 border-b border-line py-3 last:border-b-0">
            <span className="flex aspect-square h-14 w-14 shrink-0 items-center justify-center overflow-hidden rounded-control bg-paper">
              {item.thumbnailStorageKey ? (
                // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                <img src={mediaUrl(item.thumbnailStorageKey)} alt="" className="h-full w-full object-contain p-1" />
              ) : null}
            </span>
            <span className="flex min-w-0 flex-1 flex-col">
              <span dir="auto" className="truncate text-start text-sm font-bold [unicode-bidi:isolate]">{item.name}</span>
              {item.code ? <span dir="ltr" className="text-start font-mono text-xs text-ink-muted">{item.code}</span> : null}
            </span>
            <span className="flex shrink-0 flex-col items-end text-sm">
              <b className="tabular-nums">{formatEgp(item.lineTotal, locale)}</b>
              {item.quantity > 1 ? <span dir="ltr" className="font-mono text-xs text-ink-muted">{item.quantity} × {formatEgp(item.unitPrice, locale)}</span> : null}
            </span>
          </li>
        ))}
      </ul>
    </Card>
  );
}

function Totals({ order }: { order: Order }) {
  const t = useTranslations("account.order");
  const locale = useLocale();
  const open = !["Delivered", "Completed", "Cancelled"].includes(order.status);
  const money = (value: number) => formatEgp(value, locale);
  return (
    <Card title={t("totals")} icon={WalletIcon} padding="md" className="gap-3">
      <InfoGrid columns={2} className="gap-y-3">
        <InfoField label={t("subtotal")} value={money(order.subtotal)} />
        {order.couponDiscount > 0 ? <InfoField label={t("couponDiscount", { code: order.couponCode ?? "" })} value={`− ${money(order.couponDiscount)}`} /> : null}
        {order.fulfillmentMethod === "Delivery" ? <InfoField label={t("shipping")} value={money(order.deliveryFee)} /> : null}
        <InfoField label={t("total")} value={money(order.total)} />
        <InfoField label={t("paid")} value={money(order.paidAmount)} />
        {order.refundedAmount > 0 ? <InfoField label={t("refunded")} value={money(order.refundedAmount)} /> : null}
        {open ? <InfoField label={t("remaining")} value={money(order.remaining)} /> : null}
        {open && order.upfrontDue > 0 ? <InfoField label={t("upfrontDue")} value={money(order.upfrontDue)} /> : null}
      </InfoGrid>
    </Card>
  );
}

function Delivery({ order, pickupAddress }: { order: Order; pickupAddress: string | null }) {
  const t = useTranslations("account.order");
  const tPay = useTranslations("checkout.payment.methods");
  const payment = paymentMethodKey(order.paymentMethod);
  const pickup = order.fulfillmentMethod === "Pickup";
  const address = order.address
    ? [order.address.governorate, order.address.area, order.address.street, order.address.building, order.address.landmark].filter(Boolean).join("، ")
    : "";
  return (
    <Card title={pickup ? t("pickupTitle") : t("deliveryTitle")} icon={MapPinIcon} padding="md" className="gap-3">
      <InfoGrid columns={2} className="gap-y-3">
        <InfoField
          label={pickup ? t("pickupFrom") : t("address")}
          value={pickup ? (pickupAddress ?? t("pickupShop")) : address || undefined}
          wide={!pickup}
        />
        {order.deliveryTime && !pickup ? <InfoField label={t("deliveryTime")} icon={ClockIcon} value={order.deliveryTime} /> : null}
        {payment ? <InfoField label={t("payment")} value={tPay(payment)} /> : null}
        {order.courierName ? <InfoField label={t("courier")} value={order.courierName} /> : null}
        {order.waybillNumber ? <InfoField label={t("waybill")} ltr value={order.waybillNumber} /> : null}
      </InfoGrid>
    </Card>
  );
}

function OrderBody({ order, whatsApp, pickupAddress }: { order: Order; whatsApp: OrderWhatsApp; pickupAddress: string | null }) {
  const t = useTranslations("account.order");
  const tStatus = useTranslations("orderStatus");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const status = orderStatusKey(order.status);
  const count = order.items.reduce((sum, item) => sum + item.quantity, 0);

  return (
    <div className="flex flex-col gap-4">
      <ButtonLink href="/account" variant="ghost" size="sm" icon={ArrowBackIcon} className="self-start">{t("back")}</ButtonLink>
      <Card title={t("timeline")} icon={TruckIcon} padding="md" className="gap-4">
        <div className="grid gap-4 md:grid-cols-2">
          <TrackingTimeline steps={trackingSteps(order.status, order.fulfillmentMethod, order.timeline)} />
          <InfoGrid columns={2} className="gap-y-3 border-t border-line pt-4 md:border-s md:border-t-0 md:ps-4 md:pt-0">
            <InfoField
              label={t("reference")} ltr value={order.reference}
              copy={{ value: order.reference, label: tCommon("copy"), copiedLabel: tCommon("copied") }}
            />
            <InfoField label={t("status")} value={<StatusBadge tone={orderTone(status)}>{tStatus(status)}</StatusBadge>} />
            <InfoField label={t("date")} value={formatDate(order.createdAtUtc, locale)} />
            <InfoField label={t("itemCount", { count })} value={formatEgp(order.total, locale)} />
          </InfoGrid>
        </div>
      </Card>
      <Items order={order} />
      <Totals order={order} />
      <Delivery order={order} pickupAddress={pickupAddress} />
      <div className="flex flex-wrap gap-3">
        <OrderWhatsAppLink whatsApp={whatsApp} reference={order.reference} label={t("ask")} />
        <ButtonLink href={`/t/${order.trackingToken}`} variant="secondary">{t("track")}</ButtonLink>
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
          <Card padding="md" className="items-start gap-3">
            <p className="m-0 type-section-title">{t("notFound")}</p>
            <ButtonLink href="/account" variant="secondary" size="sm">{t("back")}</ButtonLink>
          </Card>
        )
      }
    </LoadBoundary>
  );
}
