"use client";

import { useLocale, useTranslations } from "next-intl";
import { formatDateTime, formatEgp } from "@/lib/format";
import { governorateName } from "../customers/customer-rules";
import { fulfillmentKey, methodKey, usageKey } from "./order-labels";
import type { Order } from "./orders-api";

function Field({ label, value, mono = false }: { label: string; value: string | null | undefined; mono?: boolean }) {
  if (!value) return null;
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs text-ink-muted">{label}</dt>
      <dd dir={mono ? "ltr" : "auto"} className={`m-0 text-sm ${mono ? "text-start font-mono" : "font-bold"}`}>{value}</dd>
    </div>
  );
}

/** Who ordered, how it is delivered and what they said. */
export function OrderCustomer({ order, governorates }: { order: Order; governorates: readonly { code: string; name: string }[] }) {
  const t = useTranslations("admin.orders");
  const usages = useTranslations("checkout.extras.usages");
  const fulfillment = fulfillmentKey(order.fulfillmentMethod);
  const usageName = order.usage ? usageKey(order.usage) : null;
  const usage = usageName ? usages(usageName) : order.usage;
  const address = order.address ? [governorateName(governorates, order.address.governorate), order.address.area, order.address.street, order.address.building, order.address.landmark].filter(Boolean).join(" · ") : null;
  const rating = order.satisfactionScore !== null ? `${order.satisfactionScore}/5${order.satisfactionComment ? ` · ${order.satisfactionComment}` : ""}` : null;

  return (
    <section className="flex flex-col gap-2">
      <h3 className="m-0 text-[15px] font-bold">{t("detail.customer.title")}</h3>
      <dl className="m-0 grid gap-3 sm:grid-cols-2">
        <Field label={t("detail.customer.name")} value={order.customerName} />
        <Field label={t("detail.customer.phone")} value={order.phoneNumber} mono />
        <Field label={t("detail.customer.email")} value={order.customerEmail} mono />
        <Field label={t("detail.customer.fulfillment")} value={fulfillment ? t(`fulfillmentKinds.${fulfillment}`) : order.fulfillmentMethod} />
        <Field label={t("detail.customer.address")} value={address} />
        <Field label={t("detail.customer.leadSource")} value={order.leadSource} />
        <Field label={t("detail.customer.usage")} value={[usage, order.usageNote].filter(Boolean).join(" · ")} />
        <Field label={t("detail.customer.notes")} value={order.notes} />
        <Field label={t("detail.customer.satisfaction")} value={rating} />
      </dl>
    </section>
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
  const rows: { label: string; amount: number; strong: boolean }[] = [
    { label: t("detail.money.subtotal"), amount: order.subtotal, strong: false },
    { label: t("detail.money.deliveryFee"), amount: order.deliveryFee, strong: false },
    { label: t("detail.money.total"), amount: order.total, strong: true },
    { label: t("detail.money.upfrontDue"), amount: order.upfrontDue, strong: false },
    { label: t("detail.money.paid"), amount: order.paidAmount, strong: false },
    { label: t("detail.money.remaining"), amount: order.remaining, strong: true },
    { label: t("detail.money.refunded"), amount: order.refundedAmount, strong: false }
  ];

  return (
    <section className="flex flex-col gap-2">
      <h3 className="m-0 text-[15px] font-bold">{t("detail.money.title")}</h3>
      <dl className="m-0 grid gap-x-6 sm:grid-cols-2">
        {rows.filter((row) => row.strong || row.amount > 0).map((row) => (
          <div key={row.label} className="flex justify-between gap-3 border-b border-line py-1.5 text-sm">
            <dt className="text-ink-muted">{row.label}</dt>
            <dd className={`m-0 whitespace-nowrap ${row.strong ? "font-bold" : ""}`}>{formatEgp(row.amount, locale)}</dd>
          </div>
        ))}
      </dl>
      <p className="m-0 text-sm"><span className="text-ink-muted">{t("detail.money.method")}: </span><span className="font-bold">{method(order.paymentMethod)}</span></p>
      {order.paymentWaivedAtUtc ? (
        <p className="m-0 rounded-control bg-paper p-3 text-sm">
          {t("detail.money.waived", { date: formatDateTime(order.paymentWaivedAtUtc, locale), reason: order.paymentWaiverReason ?? "" })}
        </p>
      ) : null}
      <h4 className="m-0 mt-2 text-sm font-bold">{t("detail.payments.title")}</h4>
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
    </section>
  );
}
