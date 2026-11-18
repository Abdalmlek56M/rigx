import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { formatEgp } from "@/lib/format";
import { OrderWhatsAppLink, type OrderWhatsApp } from "./OrderWhatsAppLink";
import { RatingForm } from "./RatingForm";
import { TrackingTimeline } from "./TrackingTimeline";
import { orderStatusKey, trackingSteps } from "./tracking-steps";

function Detail({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex justify-between gap-4 text-sm">
      <dt className="text-ink-muted">{label}</dt>
      <dd dir={mono ? "ltr" : "auto"} className={`m-0 text-end ${mono ? "font-mono" : ""}`}>{value}</dd>
    </div>
  );
}

/** What the tracking link shows: status, timeline, delivery details, items and the rating. */
export function TrackingOrder({ view, token, whatsApp }: { view: Schemas["TrackingView"]; token: string; whatsApp: OrderWhatsApp }) {
  const t = useTranslations("tracking");
  const locale = useLocale();
  const status = orderStatusKey(view.status);
  const open = status !== "Delivered" && status !== "Cancelled";
  const address = [view.area, view.street].filter(Boolean).join("، ");
  const showRemaining = open && view.remaining > 0;

  return (
    <div className="mx-auto flex max-w-[640px] flex-col gap-4">
      <header className="flex flex-col gap-2">
        <span className="font-mono text-sm font-semibold text-orange-text" dir="ltr">{view.reference}</span>
        <h1 className="m-0 text-3xl font-bold md:text-[38px]">{t(`headings.${status}`)}</h1>
        {open && view.deliveryTime ? <p className="m-0 text-base text-ink-muted">{t("expected", { time: view.deliveryTime })}</p> : null}
      </header>

      <section aria-label={t("timeline")} className="rounded-[10px] border border-line bg-surface p-5 pb-0 md:p-6 md:pb-1">
        <TrackingTimeline steps={trackingSteps(view.status, view.fulfillmentMethod, view.timeline)} />
      </section>

      <section className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5 md:p-6">
        <dl className="m-0 flex flex-col gap-2.5">
          {view.courierName ? <Detail label={t("courier")} value={view.courierName} /> : null}
          {view.waybillNumber ? <Detail label={t("waybill")} value={view.waybillNumber} mono /> : null}
          {address ? <Detail label={t("address")} value={address} /> : null}
          {showRemaining ? <Detail label={t("remaining")} value={formatEgp(view.remaining, locale)} /> : null}
          {open && view.upfrontDue > 0 ? <Detail label={t("upfrontDue")} value={formatEgp(view.upfrontDue, locale)} /> : null}
        </dl>
        <OrderWhatsAppLink
          whatsApp={whatsApp} reference={view.reference} label={t("ask")}
          className="press flex h-[52px] items-center justify-center gap-2 rounded-control bg-carbon font-bold text-on-carbon"
        />
      </section>

      <section className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5 md:p-6">
        <h2 className="m-0 text-lg font-bold">{t("items")}</h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {view.items.map((item) => (
            <li key={`${item.code}-${item.name}`} className="flex justify-between gap-3 text-sm">
              <span dir="auto" className="min-w-0 [unicode-bidi:isolate]">
                {item.name}
                {item.code ? <span dir="ltr" className="ms-1.5 font-mono text-xs text-ink-muted">{item.code}</span> : null}
              </span>
              {item.quantity > 1 ? <span dir="ltr" className="shrink-0 font-mono">× {item.quantity}</span> : null}
            </li>
          ))}
        </ul>
      </section>

      <RatingForm token={token} canRate={view.canRate} score={view.satisfactionScore} />
      <p className="m-0 text-center text-[13px] text-ink-muted">{t("privateNote")}</p>
    </div>
  );
}
