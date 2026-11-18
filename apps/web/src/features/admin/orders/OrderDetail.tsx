"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link, getPathname } from "@/i18n/navigation";
import { whatsAppUrl } from "@/lib/contact";
import { formatDateTime } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { OrderStatusBadge, PaymentStatusBadge } from "./OrderBadges";
import { OrderItems, OrderTimeline, OrderUnits } from "./OrderBody";
import { OrderBlacklistAction } from "./OrderBlacklistAction";
import { OrderCustomer, OrderMoney } from "./OrderCustomer";
import { OrderFulfillmentActions } from "./OrderFulfillmentActions";
import { OrderPaymentActions } from "./OrderPaymentActions";
import { OrderStatusActions } from "./OrderStatusActions";
import { fetchShippingRates } from "../customers/customers-api";
import { fetchOrder, type Order } from "./orders-api";
import { customerWhatsAppNumber, orderTrail } from "./order-rules";

function Trail({ order }: { order: Order }) {
  const t = useTranslations("admin.orders");
  const steps = orderTrail(order);
  const current = steps.findIndex((step) => step === order.status);
  if (order.status === "Cancelled") return <p role="status" className="m-0 rounded-control bg-error px-3 py-2 text-sm font-bold text-surface">{t("detail.cancelled")}</p>;
  return (
    <ol aria-label={t("detail.trailLabel")} className="m-0 flex list-none flex-wrap gap-1.5 p-0">
      {steps.map((step, index) => (
        <li
          key={step} aria-current={index === current ? "step" : undefined}
          className={`min-w-20 flex-1 rounded-control px-1 py-2 text-center text-xs font-bold ${index === current ? "bg-carbon text-on-carbon" : index < current ? "bg-orange/15 text-orange-text" : "bg-paper text-ink-muted"}`}
        >
          {t(`statuses.${step}`)}
        </li>
      ))}
    </ol>
  );
}

function Tracking({ token }: { token: string }) {
  const t = useTranslations("admin.orders.detail.tracking");
  const locale = useLocale();
  const [copied, setCopied] = useState(false);
  const path = getPathname({ href: `/t/${token}`, locale });
  async function copy() {
    try {
      await navigator.clipboard.writeText(`${window.location.origin}${path}`);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  }
  return (
    <div className="flex flex-col gap-2">
      <h3 className="m-0 text-sm font-bold">{t("title")}</h3>
      <div className="flex flex-wrap gap-2">
        <Link href={`/t/${token}`} target="_blank" className="inline-flex h-11 items-center rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{t("open")}</Link>
        <button type="button" onClick={() => void copy()} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{copied ? t("copied") : t("copy")}</button>
      </div>
    </div>
  );
}

/** The selected order: its content on the left, and on the right (under it on phones) every action staff may take. */
export function OrderDetail({ id, onClose, onChanged, onBlacklistChanged }: { id: string; onClose: () => void; onChanged: () => void; onBlacklistChanged: () => void }) {
  const t = useTranslations("admin.orders");
  const locale = useLocale();
  const load = useCallback(
    () => Promise.all([fetchOrder(id), fetchShippingRates()]).then(([detail, rates]) => ({ ...detail, governorates: rates.governorates })),
    [id]
  );
  const { state, reload } = useLoad(load);
  // The order as the last action returned it; a 409 drops it and reads the order again.
  const [updated, setUpdated] = useState<Order | null>(null);

  if (state.status === "loading") {
    return <div role="status" aria-label={t("loading")} className="h-64 animate-pulse rounded-[10px] border border-line bg-surface" />;
  }
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-[10px] border border-line bg-surface p-5">
        <p className="m-0 font-bold">{t("detail.failed")}</p>
        <div className="flex flex-wrap gap-2">
          <button type="button" onClick={reload} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("detail.retry")}</button>
          <button type="button" onClick={onClose} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("close")}</button>
        </div>
      </div>
    );
  }

  const order = updated ?? state.data.order;
  const actionProps = {
    order,
    onUpdated: (next: Order) => {
      setUpdated(next);
      onChanged();
    },
    onStale: () => {
      setUpdated(null);
      reload();
    }
  };
  // Forms start from the order's values, so a new version of the order gives them a fresh start.
  const formKey = order.rowVersion;

  return (
    <section className="flex min-w-0 flex-col gap-4 rounded-[10px] border border-line bg-surface p-5">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <h2 className="m-0 text-[17px] font-bold">
            {t("detail.heading")} <span dir="ltr" className="font-mono">#{order.reference}</span>
          </h2>
          <OrderStatusBadge status={order.status} />
          <PaymentStatusBadge status={order.paymentStatus} />
          <span className="text-xs text-ink-muted">{t(`channels.${order.channel === "Store" ? "Store" : "Online"}`)} · {t("detail.created", { date: formatDateTime(order.createdAtUtc, locale) })}</span>
        </div>
        <button type="button" onClick={onClose} aria-label={t("close")} className="h-11 w-11 rounded-control text-xl leading-none text-ink-muted hover:bg-line">×</button>
      </header>

      <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_400px]">
        <div className="flex min-w-0 flex-col gap-5">
          <Trail order={order} />
          {order.isBlacklisted ? <p role="status" className="m-0 rounded-control border border-error px-3 py-2 text-sm font-bold text-error">{t("detail.blacklistWarning")}</p> : null}
          {order.reservationExpiredAtUtc ? <p role="status" className="m-0 rounded-control border border-orange px-3 py-2 text-sm font-bold text-orange-text">{t("detail.reservationExpired")}</p> : null}
          <OrderItems order={order} />
          <OrderUnits units={state.data.units} />
          <OrderCustomer order={order} governorates={state.data.governorates} />
          <OrderMoney order={order} />
          <OrderTimeline order={order} />
        </div>
        <aside className="flex min-w-0 flex-col gap-4 rounded-control bg-paper p-4">
          <h3 className="m-0 text-[15px] font-bold">{t("actions.title")}</h3>
          <OrderStatusActions {...actionProps} />
          <OrderFulfillmentActions key={`fulfillment-${formKey}`} {...actionProps} />
          <OrderPaymentActions key={`payment-${formKey}`} {...actionProps} />
          <div className="flex flex-col gap-2 border-t border-line pt-4">
            {order.phoneHasWhatsApp ? (
              <a href={whatsAppUrl(customerWhatsAppNumber(order.phoneNumber))} target="_blank" rel="noreferrer" className="inline-flex h-11 items-center justify-center rounded-control bg-orange px-4 text-sm font-bold text-on-orange">
                {t("actions.whatsapp")}
              </a>
            ) : null}
            <OrderBlacklistAction order={order} onAdded={onBlacklistChanged} />
            <Tracking token={order.trackingToken} />
          </div>
        </aside>
      </div>
    </section>
  );
}
