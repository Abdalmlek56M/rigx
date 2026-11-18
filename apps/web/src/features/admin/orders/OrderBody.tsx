"use client";

import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { formatDateTime, formatEgp } from "@/lib/format";
import { methodKey } from "./order-labels";
import { orderEventKey } from "./order-rules";
import type { Order, OrderUnit } from "./orders-api";

const TABLE = "w-full border-collapse text-sm";
const TH = "whitespace-nowrap px-2 py-2 text-start text-[13px] font-bold text-ink-muted";
const TD = "px-2 py-2 align-middle";

/** The lines of the order: what was sold, at what price. */
export function OrderItems({ order }: { order: Order }) {
  const t = useTranslations("admin.orders.detail.items");
  const locale = useLocale();
  return (
    <section className="flex flex-col gap-2">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <div className="overflow-x-auto">
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
    </section>
  );
}

/** The physical pieces reserved or sold for the order. */
export function OrderUnits({ units }: { units: readonly OrderUnit[] }) {
  const t = useTranslations("admin.orders.detail.units");
  if (units.length === 0) return null;
  return (
    <section className="flex flex-col gap-2">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <div className="overflow-x-auto">
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
                <td className={TD}>{unit.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

/** What happened to the order, newest first. */
export function OrderTimeline({ order }: { order: Order }) {
  const t = useTranslations("admin.orders");
  const locale = useLocale();
  const method = (value: string) => {
    const key = methodKey(value);
    return key ? t(`methods.${key}`) : value;
  };

  function label(event: Order["events"][number]): string {
    const key = orderEventKey(event);
    switch (key.key) {
      case "status": return t("detail.timeline.status", { status: t(`statuses.${key.status}`) });
      case "paymentStatus": return t("detail.timeline.paymentStatus", { status: t(`paymentStatuses.${key.status}`) });
      case "payment": return t("detail.timeline.payment", { method: method(key.method), amount: key.amount ?? "" });
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
    <section className="flex flex-col gap-2">
      <h3 className="m-0 text-[15px] font-bold">{t("detail.timeline.title")}</h3>
      <ol className="m-0 flex list-none flex-col p-0">
        {[...order.events].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc)).map((event, index) => (
          <li key={`${event.createdAtUtc}-${index}`} className="flex flex-wrap items-baseline justify-between gap-x-3 border-t border-line py-2 text-sm first:border-t-0">
            <span className="flex flex-col">
              <span className="font-bold">{label(event)}</span>
              {showsNote(event) ? <span dir="auto" className="text-xs text-ink-muted">{event.notes}</span> : null}
            </span>
            <span dir="ltr" className="font-mono text-xs text-ink-muted">{formatDateTime(event.createdAtUtc, locale)}</span>
          </li>
        ))}
      </ol>
    </section>
  );
}
