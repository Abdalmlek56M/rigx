"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link, getPathname } from "@/i18n/navigation";
import { whatsAppUrl } from "@/lib/contact";
import { formatDateTime, joinValues } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { OrderStatusBadge, PaymentStatusBadge } from "./OrderBadges";
import { OrderItems, OrderTimeline, OrderUnits } from "./OrderBody";
import { OrderBlacklistAction } from "./OrderBlacklistAction";
import { OrderCustomer, OrderDelivery, OrderMoney } from "./OrderCustomer";
import { OrderFulfillmentActions } from "./OrderFulfillmentActions";
import { OrderPaymentActions } from "./OrderPaymentActions";
import { OrderStatusActions } from "./OrderStatusActions";
import { fetchShippingRates } from "../customers/customers-api";
import { fetchMoneyAccounts } from "../expenses/expenses-api";
import { LoadingCard } from "../kit/LoadState";
import { SectionCard } from "../kit/SectionCard";
import { usePermissions } from "../use-permissions";
import { fetchOrder, type Order } from "./orders-api";
import { OrderUnreachableAction } from "./OrderUnreachableAction";
import { canSetPaymentStatus, canTakePayment, canWaivePayment, hasShipping, holdsReservation, orderTrail, orderWhatsAppNumber } from "./order-rules";
import { CheckIcon, CloseIcon, CopyIcon, ExternalIcon, GearIcon, RefreshIcon, TruckIcon, WalletIcon, WhatsAppIcon } from "@/components/ui/icons";
import { cardClass } from "@/components/ui/Card";
import { Button, IconButton, buttonClass, buttonIconSize } from "../kit/Button";

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
        <Link href={`/t/${token}`} target="_blank" className={buttonClass()}><ExternalIcon size={buttonIconSize()} />{t("open")}</Link>
        <Button icon={copied ? CheckIcon : CopyIcon} onClick={() => void copy()}>{copied ? t("copied") : t("copy")}</Button>
      </div>
    </div>
  );
}

/** The selected order, as the content of the order drawer: warnings, then the action cards (status, payment, shipping), then the facts, then the extras. */
export function OrderDetail({ id, onClose, onChanged, onBlacklistChanged }: { id: string; onClose: () => void; onChanged: () => void; onBlacklistChanged: () => void }) {
  const t = useTranslations("admin.orders");
  const locale = useLocale();
  const { has } = usePermissions();
  const canSeeAccounts = has("Finance.ViewVaults");
  const load = useCallback(
    () => Promise.all([fetchOrder(id), fetchShippingRates(), canSeeAccounts ? fetchMoneyAccounts().catch(() => null) : Promise.resolve(null)])
      .then(([detail, rates, accounts]) => ({ ...detail, governorates: rates.governorates, accounts })),
    [id, canSeeAccounts]
  );
  const { state, reload } = useLoad(load);
  // The order as the last action returned it; a 409 drops it and reads the order again.
  const [updated, setUpdated] = useState<Order | null>(null);

  if (state.status === "loading") {
    return <LoadingCard className="h-64" label={t("loading")} />;
  }
  if (state.status === "error") {
    return (
      <div role="alert" className={cardClass({ className: "items-start gap-3" })}>
        <p className="m-0 font-bold">{t("detail.failed")}</p>
        <div className="flex flex-wrap gap-2">
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("detail.retry")}</Button>
          <Button icon={CloseIcon} onClick={onClose}>{t("close")}</Button>
        </div>
      </div>
    );
  }

  const order = updated ?? state.data.order;
  const actionProps = {
    order,
    accounts: state.data.accounts?.filter((account) => account.isActive) ?? null,
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
  const payment = has("Finance.VerifyPayment") && (canTakePayment(order) || canSetPaymentStatus(order) || canWaivePayment(order));
  const whatsApp = orderWhatsAppNumber(order);
  const fulfillment = has("Sales.ApproveOrder") && (holdsReservation(order) || hasShipping(order));

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <h2 className="m-0 text-[17px] font-bold">
            {t("detail.heading")} <span dir="ltr" className="font-mono">#{order.reference}</span>
          </h2>
          <OrderStatusBadge status={order.status} />
          <PaymentStatusBadge status={order.paymentStatus} />
          <span className="text-xs text-ink-muted">{joinValues([t(`channels.${order.channel === "Store" ? "Store" : "Online"}`), t("detail.created", { date: formatDateTime(order.createdAtUtc, locale) })], locale)}</span>
        </div>
        <IconButton icon={CloseIcon} label={t("close")} onClick={onClose} />
      </header>

      {order.isBlacklisted ? <p role="status" className="m-0 rounded-control border border-error px-3 py-2 text-sm font-bold text-error-text">{t("detail.blacklistWarning")}</p> : null}
      {order.reservationExpiredAtUtc ? <p role="status" className="m-0 rounded-control border border-orange px-3 py-2 text-sm font-bold text-orange-text">{t("detail.reservationExpired")}</p> : null}
      <SectionCard title={t("statusCard.title")} icon={CheckIcon}>
        <Trail order={order} />
        <OrderStatusActions {...actionProps} />
      </SectionCard>
      {payment ? (
        <SectionCard title={t("payment.title")} icon={WalletIcon}>
          <OrderPaymentActions key={`payment-${formKey}`} {...actionProps} />
        </SectionCard>
      ) : null}
      {fulfillment ? (
        <SectionCard title={t("fulfillmentCard.title")} icon={TruckIcon}>
          <OrderFulfillmentActions key={`fulfillment-${formKey}`} {...actionProps} />
        </SectionCard>
      ) : null}
      <OrderItems order={order} />
      <OrderUnits units={state.data.units} />
      <OrderCustomer order={order} governorates={state.data.governorates} />
      <OrderDelivery order={order} />
      <OrderMoney order={order} />
      <OrderTimeline order={order} />
      <SectionCard title={t("moreCard.title")} icon={GearIcon}>
        <div className="flex flex-wrap items-center gap-2">
          {whatsApp ? (
            <a href={whatsAppUrl(whatsApp)} target="_blank" rel="noreferrer" className={buttonClass({ variant: "primary" })}>
              <WhatsAppIcon size={buttonIconSize()} />
              {t("actions.whatsapp")}
            </a>
          ) : null}
          <OrderUnreachableAction {...actionProps} />
        </div>
        <OrderBlacklistAction order={order} onAdded={onBlacklistChanged} />
        <Tracking token={order.trackingToken} />
      </SectionCard>
    </div>
  );
}
