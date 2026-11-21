"use client";

import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { formatDateTime, formatEgp } from "@/lib/format";
import { unitStatusKey } from "../inventory/inventory-rules";
import { methodKey } from "./order-labels";
import { eventPaymentAmount, orderEventKey, orderTimeline } from "./order-rules";
import type { Order, OrderUnit } from "./orders-api";
import { BoxIcon, CartIcon, ClockIcon } from "@/components/ui/icons";
import { SectionCard } from "../kit/SectionCard";

const TABLE = "w-full min-w-[600px] border-collapse text-sm";
const TH = "whitespace-nowrap px-2 py-2 text-start text-[13px] font-bold text-ink-muted";
const TD = "px-2 py-2 align-middle";

/** The lines of the order: what was sold, at what price. */
export function OrderItems({ order }: { order: Order }) {
  const t = useTranslations("admin.orders.detail.items");
  const locale = useLocale();
  return (
    <SectionCard title={t("title")} icon={CartIcon}>
      <div className="-mx-2 overflow-x-auto">
        <table className={TABLE}>
          <thead>
            <tr>
              <th scope="col" className={TH}>{t("name")}</th>
              <th scope="col" className={TH}>{t("code")}</th>
              <th scope="col" className={TH}>{t("quantity")}</th>
              <th scope="col" className={TH}>{t("price")}</th>
              <th scope="col" className={TH}>{t("discount")}</th>
              <th scope="col" className={TH}>{t("total")}</th>
            </tr>
          </thead>
          <tbody>
            {order.items.map((item) => (
              <tr key={item.id} className="border-t border-line">
                <td className={TD}><span dir="auto" className="[unicode-bidi:isolate]">{item.name}</span></td>
                <td className={TD}><span dir="ltr" className="font-mono text-xs">{item.code}</span></td>
                <td className={TD}>{item.quantity}</td>
                <td className={`${TD} whitespace-nowrap`}>{formatEgp(item.unitPrice, locale)}</td>
                <td className={`${TD} whitespace-nowrap`}>{item.discountAmount > 0 ? formatEgp(item.discountAmount, locale) : "—"}</td>
                <td className={`${TD} whitespace-nowrap font-bold`}>{formatEgp(item.lineTotal, locale)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </SectionCard>
  );
}

/** The physical pieces reserved or sold for the order. */
export function OrderUnits({ units }: { units: readonly OrderUnit[] }) {
  const t = useTranslations("admin.orders.detail.units");
  const statuses = useTranslations("admin.inventory.units.status");
  if (units.length === 0) return null;
  return (
    <SectionCard title={t("title")} icon={BoxIcon}>
      <div className="-mx-2 overflow-x-auto">
        <table className={TABLE}>
          <thead>
            <tr>
              <th scope="col" className={TH}>{t("code")}</th>
              <th scope="col" className={TH}>{t("name")}</th>
              <th scope="col" className={TH}>{t("condition")}</th>
              <th scope="col" className={TH}>{t("serial")}</th>
              <th scope="col" className={TH}>{t("status")}</th>
            </tr>
          </thead>
          <tbody>
            {units.map((unit) => (
              <tr key={unit.unitId} className="border-t border-line">
                <td className={TD}><span dir="ltr" className="font-mono text-xs">{unit.stockItemCode}</span></td>
                <td className={TD}><span dir="auto" className="[unicode-bidi:isolate]">{unit.name}</span></td>
                <td className={TD}>{unit.condition === "New" || unit.condition === "Used" ? <ConditionBadge condition={unit.condition} /> : unit.condition}</td>
                <td className={TD}><span dir="ltr" className="font-mono text-xs">{unit.serialNumber ?? "—"}</span></td>
                <td className={TD}><span className="whitespace-nowrap">{statuses(unitStatusKey(unit.status))}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </SectionCard>
  );
}

/** What happened to the order, newest first: a vertical line with one dot per event. */
export function OrderTimeline({ order }: { order: Order }) {
  const t = useTranslations("admin.orders");
  const locale = useLocale();
  const method = (value: string) => {
    const key = methodKey(value);
    return key ? t(`detail.timeline.eventMethods.${key}`) : value;
  };

  function label(event: Order["events"][number]): string {
    const key = orderEventKey(event);
    switch (key.key) {
      case "status": return t("detail.timeline.status", { status: t(`statuses.${key.status}`) });
      case "paymentStatus": return t("detail.timeline.paymentStatus", { status: t(`paymentStatuses.${key.status}`) });
      case "payment": {
        const amount = eventPaymentAmount(key.amount);
        return t("detail.timeline.payment", { method: method(key.method), amount: amount === null ? key.amount ?? "" : formatEgp(amount, locale) });
      }
      case "other": return t("detail.timeline.other", { type: key.type });
      case "created": return t("detail.timeline.created");
      default: return t(`detail.timeline.${key.key}`);
    }
  }
  const showsNote = (event: Order["events"][number]) => {
    const { key } = orderEventKey(event);
    return Boolean(event.notes) && (key === "status" || key === "paymentStatus" || key === "other");
  };

  return (
    <SectionCard title={t("detail.timeline.title")} icon={ClockIcon}>
      {order.events.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("detail.timeline.empty")}</p> : (
        <ol className="relative m-0 flex list-none flex-col gap-4 p-0 ps-5 before:absolute before:bottom-1 before:start-[6px] before:top-1 before:w-px before:bg-line">
          {orderTimeline(order.events).map((event, index) => (
            <li key={`${event.createdAtUtc}-${index}`} className="relative min-w-0">
              <span aria-hidden="true" className="absolute -start-[18px] top-1.5 size-2.5 rounded-full bg-orange-text ring-4 ring-surface" />
              <p className="m-0 text-sm font-bold">{label(event)}</p>
              {showsNote(event) ? <p dir="auto" className="m-0 mt-0.5 text-xs text-ink-muted [unicode-bidi:isolate]">{event.notes}</p> : null}
              <p className="m-0 mt-0.5 text-xs text-ink-muted"><time dir="ltr" dateTime={event.createdAtUtc}>{formatDateTime(event.createdAtUtc, locale)}</time></p>
            </li>
          ))}
        </ol>
      )}
    </SectionCard>
  );
}
