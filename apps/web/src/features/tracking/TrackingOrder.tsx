import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { Card } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { PageHeader } from "@/components/ui/PageHeader";
import { BoxIcon, InfoIcon, TruckIcon } from "@/components/ui/icons";
import { formatEgp } from "@/lib/format";
import { OrderWhatsAppLink, type OrderWhatsApp } from "./OrderWhatsAppLink";
import { RatingForm } from "./RatingForm";
import { TrackingTimeline } from "./TrackingTimeline";
import { orderStatusKey, trackingSteps } from "./tracking-steps";

/**
 * What the tracking link shows: status, timeline, delivery details (the phone only masked), items and the rating. Long after delivery the API
 * sends `detailsHidden` and the page keeps only the status and the timeline.
 */
export function TrackingOrder({ view, token, whatsApp }: { view: Schemas["TrackingView"]; token: string; whatsApp: OrderWhatsApp }) {
  const t = useTranslations("tracking");
  const locale = useLocale();
  const status = orderStatusKey(view.status);
  const open = status !== "Delivered" && status !== "Cancelled";
  const showRemaining = open && view.remaining > 0;

  return (
    <div className="mx-auto flex max-w-[720px] flex-col gap-4">
      <PageHeader
        contained={false}
        eyebrow={view.reference}
        title={t(`headings.${status}`)}
        lead={open && view.deliveryTime ? t("expected", { time: view.deliveryTime }) : undefined}
      />

      <Card title={t("timeline")} icon={TruckIcon} padding="md">
        <TrackingTimeline steps={trackingSteps(view.status, view.fulfillmentMethod, view.timeline)} />
      </Card>

      {view.detailsHidden ? (
        <Card title={t("expiredTitle")} icon={InfoIcon} padding="md" className="gap-3">
          <p className="m-0 text-sm leading-7 text-ink-muted">{t("expired")}</p>
          <OrderWhatsAppLink whatsApp={whatsApp} reference={view.reference} label={t("ask")} variant="solid" className="self-start" />
        </Card>
      ) : (
        <>
      <Card title={t("details")} icon={InfoIcon} padding="md" className="gap-3">
        <InfoGrid columns={2} className="gap-y-3">
          {view.maskedPhone ? <InfoField label={t("phone")} ltr value={view.maskedPhone} /> : null}
          {view.courierName ? <InfoField label={t("courier")} value={view.courierName} /> : null}
          {view.waybillNumber ? <InfoField label={t("waybill")} ltr value={view.waybillNumber} /> : null}
          {view.area ? <InfoField label={t("address")} value={view.area} wide /> : null}
          {showRemaining ? <InfoField label={t("remaining")} value={formatEgp(view.remaining, locale)} /> : null}
          {open && view.upfrontDue > 0 ? <InfoField label={t("upfrontDue")} value={formatEgp(view.upfrontDue, locale)} /> : null}
        </InfoGrid>
        <OrderWhatsAppLink whatsApp={whatsApp} reference={view.reference} label={t("ask")} variant="solid" className="self-start" />
      </Card>

      <Card title={t("items")} icon={BoxIcon} padding="md" className="gap-3">
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
      </Card>

      <RatingForm token={token} canRate={view.canRate} score={view.satisfactionScore} />
        </>
      )}
      <p className="m-0 text-center text-xs text-ink-muted">{t("privateNote")}</p>
    </div>
  );
}
