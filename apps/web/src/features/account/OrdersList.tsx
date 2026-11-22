"use client";

import { BoxIcon, EyeIcon, TruckIcon } from "@/components/ui/icons";
import { useLocale, useTranslations } from "next-intl";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { orderTone } from "@/components/ui/status-badge-rules";
import { formatDate, formatEgp } from "@/lib/format";
import { orderStatusKey } from "@/features/tracking/tracking-steps";
import { itemsPreview } from "./account-labels";
import { fetchOrders, type Order } from "./account-api";
import { CouponHistory } from "./CouponHistory";
import { OrderItemsAccordion } from "./OrderItemsAccordion";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { OrderProgress } from "./OrderProgress";
import { RowLink } from "./RowAction";
import { useLoad } from "@/lib/hooks/use-load";

const PREVIEW_ITEMS = 2;

function OrderCard({ order }: { order: Order }) {
  const t = useTranslations("account.orders");
  const tStatus = useTranslations("orderStatus");
  const locale = useLocale();
  const { shown, more } = itemsPreview(order.items.map((item) => item.name), PREVIEW_ITEMS);
  const status = orderStatusKey(order.status);
  const count = order.items.reduce((sum, item) => sum + item.quantity, 0);

  return (
    <Card as="li" padding="md" className="gap-3">
      <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2">
        <span dir="ltr" className="font-mono text-sm font-semibold text-orange-text">{order.reference}</span>
        <div className="flex flex-wrap items-center gap-2">
          <StatusBadge tone={orderTone(status)}>{tStatus(status)}</StatusBadge>
          <span className="text-sm text-ink-muted">{formatDate(order.createdAtUtc, locale)}</span>
        </div>
      </div>
      <OrderProgress order={order} />
      <p dir="auto" className="m-0 text-sm text-ink-muted [unicode-bidi:isolate]">
        {shown.join("، ")}
        {more > 0 ? ` ${t("more", { count: more })}` : ""}
      </p>
      <OrderItemsAccordion order={order} />
      <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 border-t border-line pt-3">
        <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
          <b className="tabular-nums">{formatEgp(order.total, locale)}</b>
          <span className="text-sm text-ink-muted">{t("itemCount", { count })}</span>
        </div>
        <div className="flex items-center gap-1.5">
          <RowLink href={`/account/orders/${order.id}`} label={t("details")} icon={EyeIcon} />
          <RowLink href={`/t/${order.trackingToken}`} label={t("track")} icon={TruckIcon} />
        </div>
      </div>
    </Card>
  );
}

/** "طلباتي": the person's orders, newest first (the API's order), each with its status, progress and actions. */
export function OrdersList() {
  const t = useTranslations("account.orders");
  const { state, reload } = useLoad(fetchOrders);

  return (
    <div className="flex flex-col gap-4">
      <h2 className="sr-only">{t("title")}</h2>
      <LoadBoundary state={state} reload={reload}>
        {(orders) =>
          orders.length === 0 ? (
            <EmptyState icon={BoxIcon} message={t("empty")} action={<ButtonLink href="/products" variant="secondary" size="sm">{t("browse")}</ButtonLink>} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {orders.map((order) => <OrderCard key={order.id} order={order} />)}
            </ul>
          )
        }
      </LoadBoundary>
      <CouponHistory />
    </div>
  );
}
