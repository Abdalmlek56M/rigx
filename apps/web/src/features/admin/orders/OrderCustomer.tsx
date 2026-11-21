"use client";

import type { ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime, formatEgp } from "@/lib/format";
import { whatsAppUrl } from "@/lib/contact";
import { orderAddressText, orderWhatsAppNumber } from "./order-rules";
import { governorateName } from "../customers/customer-rules";
import { fulfillmentKey, methodKey, usageKey } from "./order-labels";
import type { Order } from "./orders-api";
import { PhoneIcon, TruckIcon, UserIcon, WalletIcon, WhatsAppIcon } from "@/components/ui/icons";
import { SectionCard } from "../kit/SectionCard";
import { buttonClass, buttonIconSize } from "../kit/Button";

function Field({ label, value, mono = false, wide = false, children }: {
  label: string;
  value?: string | null;
  mono?: boolean;
  wide?: boolean;
  children?: ReactNode;
}) {
  if (!value && !children) return null;
  return (
    <div className={`flex min-w-0 flex-col gap-0.5 ${wide ? "sm:col-span-2" : ""}`}>
      <dt className="text-xs text-ink-muted">{label}</dt>
      <dd className="m-0 flex min-w-0 flex-wrap items-center gap-2 text-sm font-bold">
        {children ?? <span dir={mono ? "ltr" : "auto"} className={`[unicode-bidi:isolate] ${mono ? "font-mono" : ""}`}>{value}</span>}
      </dd>
    </div>
  );
}

/** Who ordered: contact details, how they found us, the address and their notes. */
export function OrderCustomer({ order, governorates }: { order: Order; governorates: readonly { code: string; name: string }[] }) {
  const t = useTranslations("admin.orders");
  const usages = useTranslations("checkout.extras.usages");
  const usageName = order.usage ? usageKey(order.usage) : null;
  const usage = usageName ? usages(usageName) : order.usage;
  const address = orderAddressText(order.address, (code) => governorateName(governorates, code) ?? code);
  const whatsApp = orderWhatsAppNumber(order);
  const rating = order.satisfactionScore !== null ? `${order.satisfactionScore}/5${order.satisfactionComment ? ` · ${order.satisfactionComment}` : ""}` : null;

  return (
    <SectionCard title={t("detail.customer.title")} icon={UserIcon}>
      <dl className="m-0 grid gap-x-6 gap-y-3 sm:grid-cols-2">
        <Field label={t("detail.customer.name")} value={order.customerName} />
        <Field label={t("detail.customer.phone")} mono>
          <span dir="ltr" className="font-mono">{order.phoneNumber}</span>
          <a href={`tel:${order.phoneNumber}`} aria-label={t("detail.customer.call")} data-tip={t("detail.customer.call")} className={`tip ${buttonClass({ variant: "ghost", size: "sm", iconOnly: true })}`}>
            <PhoneIcon size={buttonIconSize("sm")} />
          </a>
          {whatsApp ? (
            <a href={whatsAppUrl(whatsApp)} target="_blank" rel="noreferrer" aria-label={t("detail.customer.whatsapp")} data-tip={t("detail.customer.whatsapp")} className={`tip ${buttonClass({ variant: "ghost", size: "sm", iconOnly: true })}`}>
              <WhatsAppIcon size={buttonIconSize("sm")} />
            </a>
          ) : null}
        </Field>
        <Field label={t("detail.customer.email")} value={order.customerEmail} mono />
        <Field label={t("detail.customer.leadSource")} value={order.leadSource} />
        <Field label={t("detail.customer.usage")} value={[usage, order.usageNote].filter(Boolean).join(" · ")} />
        <Field label={t("detail.customer.satisfaction")} value={rating} />
        <Field label={t("detail.customer.address")} value={address} wide />
        <Field label={t("detail.customer.notes")} value={order.notes} wide />
      </dl>
    </SectionCard>
  );
}

/** How the order reaches the customer: delivery or pickup, courier, waybill and the reservation hold. */
export function OrderDelivery({ order }: { order: Order }) {
  const t = useTranslations("admin.orders");
  const locale = useLocale();
  const fulfillment = fulfillmentKey(order.fulfillmentMethod);

  return (
    <SectionCard title={t("detail.delivery.title")} icon={TruckIcon}>
      <dl className="m-0 grid gap-x-6 gap-y-3 sm:grid-cols-2">
        <Field label={t("detail.delivery.fulfillment")} value={fulfillment ? t(`fulfillmentKinds.${fulfillment}`) : order.fulfillmentMethod} />
        <Field label={t("detail.delivery.time")} value={order.deliveryTime} />
        <Field label={t("detail.delivery.courier")} value={order.courierName} />
        <Field label={t("detail.delivery.waybill")} value={order.waybillNumber} mono />
        {order.reservedUntilUtc ? (
          <Field
            label={t("detail.delivery.reservedUntil")}
            value={formatDateTime(order.reservedUntilUtc, locale)}
            wide
          />
        ) : null}
        {order.reservationAutoRelease ? <Field label={t("detail.delivery.autoRelease")} value={t("detail.delivery.releaseOnEnd")} wide /> : null}
      </dl>
    </SectionCard>
  );
}

/** The totals, what was paid, and every payment recorded. */
export function OrderMoney({ order }: { order: Order }) {
  const t = useTranslations("admin.orders");
  const locale = useLocale();
  const method = (value: string) => {
    const key = methodKey(value);
    return key ? t(`methods.${key}`) : value;
  };
  const rows: { label: string; amount: number; strong: boolean; minus?: boolean; orange?: boolean; always?: boolean }[] = [
    { label: t("detail.money.subtotal"), amount: order.subtotal, strong: false, always: true },
    { label: t("detail.money.couponDiscount", { code: order.couponCode ?? "" }), amount: order.couponDiscount, strong: false, minus: true },
    { label: t("detail.money.deliveryFee"), amount: order.deliveryFee, strong: false },
    { label: t("detail.money.total"), amount: order.total, strong: true, always: true },
    { label: t("detail.money.upfrontDue"), amount: order.upfrontDue, strong: false },
    { label: t("detail.money.paid"), amount: order.paidAmount, strong: false, always: true },
    { label: t("detail.money.remaining"), amount: order.remaining, strong: true, orange: order.remaining > 0, always: true },
    { label: t("detail.money.refunded"), amount: order.refundedAmount, strong: false }
  ];

  return (
    <SectionCard title={t("detail.money.title")} icon={WalletIcon}>
      <dl className="m-0 flex flex-col">
        {rows.filter((row) => row.always || row.strong || row.amount > 0).map((row) => (
          <div key={row.label} className="flex items-center justify-between gap-3 border-t border-line py-2 text-sm first:border-t-0">
            <dt className="text-ink-muted">{row.label}</dt>
            <dd className={`m-0 whitespace-nowrap ${row.strong ? "font-bold" : ""} ${row.orange ? "text-orange-text" : ""}`}>
              {row.minus ? "− " : ""}{formatEgp(row.amount, locale)}
            </dd>
          </div>
        ))}
      </dl>
      <p className="m-0 text-sm"><span className="text-ink-muted">{t("detail.money.method")}: </span><span className="font-bold">{method(order.paymentMethod)}</span></p>
      {order.paymentWaivedAtUtc ? (
        <p className="m-0 rounded-control bg-paper p-3 text-sm">
          {t("detail.money.waived", { date: formatDateTime(order.paymentWaivedAtUtc, locale), reason: order.paymentWaiverReason ?? "" })}
        </p>
      ) : null}
      <div className="flex flex-col gap-2 border-t border-line pt-3">
        <h3 className="m-0 text-sm font-bold">{t("detail.payments.title")}</h3>
        {order.payments.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("detail.payments.empty")}</p> : (
          <ul className="m-0 flex list-none flex-col p-0">
            {order.payments.map((payment, index) => (
              <li key={`${payment.createdAtUtc}-${index}`} className="flex flex-wrap items-baseline justify-between gap-x-3 border-t border-line py-2 text-sm first:border-t-0">
                <span className="flex flex-col">
                  <span className="font-bold">{method(payment.method)} · {formatEgp(payment.amount, locale)}</span>
                  {payment.note ? <span dir="auto" className="text-xs text-ink-muted">{payment.note}</span> : null}
                </span>
                <span dir="ltr" className="font-mono text-xs text-ink-muted">{formatDateTime(payment.createdAtUtc, locale)}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </SectionCard>
  );
}
