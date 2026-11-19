"use client";

import { useLocale, useTranslations } from "next-intl";
import { displayPhone } from "@/lib/contact";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { useMounted } from "@/lib/hooks/use-mounted";
import { useSession } from "@/features/auth/use-session";
import { OrderWhatsAppLink, type OrderWhatsApp } from "@/features/tracking/OrderWhatsAppLink";
import { readLastOrder, type PlacedOrder } from "./last-order";
import { depositOf } from "./order-rules";

const linkClass = "text-orange-text underline hover:text-orange-text-hover";

function NextSteps({ order }: { order: PlacedOrder }) {
  const t = useTranslations("orderSuccess.steps");
  const locale = useLocale();
  const pickup = order.fulfillmentMethod === "Pickup";
  const deposit = depositOf(order.upfrontAmount, order.deliveryFee);
  const confirmation = [
    t("confirm"),
    !pickup && order.deliveryFee > 0 ? t("confirmShipping") : "",
    deposit > 0 ? t("confirmDeposit", { amount: formatEgp(deposit, locale) }) : ""
  ].filter(Boolean).join(" ");
  const steps = [
    { title: t("confirmTitle"), body: confirmation },
    { title: t("prepareTitle"), body: t("prepare") },
    { title: pickup ? t("pickupTitle") : t("shipTitle"), body: pickup ? t("pickup") : t("ship") }
  ];

  return (
    <section className="flex flex-col gap-3.5 rounded-card border border-line bg-surface p-5 md:p-6">
      <h2 className="m-0 text-lg font-bold">{t("title")}</h2>
      <ol className="m-0 flex list-none flex-col gap-3.5 p-0">
        {steps.map((step, index) => (
          <li key={step.title} className="flex items-start gap-3">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-carbon font-mono text-sm font-bold text-on-carbon">{index + 1}</span>
            <span className="text-sm leading-7"><b>{step.title}</b> {step.body}</span>
          </li>
        ))}
      </ol>
    </section>
  );
}

function Summary({ order }: { order: PlacedOrder }) {
  const t = useTranslations("orderSuccess.summary");
  const locale = useLocale();

  return (
    <section className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5 md:p-6">
      <h2 className="m-0 text-lg font-bold">{t("title")}</h2>
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {order.items.map((item) => (
          <li key={`${item.code}-${item.name}`} className="flex justify-between gap-3 text-sm">
            <span dir="auto" className="min-w-0 [unicode-bidi:isolate]">
              {item.quantity > 1 ? `${item.quantity} × ` : ""}{item.name}
              {item.code ? <span dir="ltr" className="ms-1.5 font-mono text-xs text-ink-muted">{item.code}</span> : null}
            </span>
            <b className="shrink-0">{formatEgp(item.lineTotal, locale)}</b>
          </li>
        ))}
      </ul>
      <div className="flex flex-col gap-2 border-t border-line pt-3">
        {order.deliveryFee > 0 ? (
          <div className="flex justify-between text-sm"><span className="text-ink-muted">{t("shipping")}</span><span>{formatEgp(order.deliveryFee, locale)}</span></div>
        ) : null}
        <div className="flex items-baseline justify-between"><b>{t("total")}</b><b className="text-[22px]">{formatEgp(order.total, locale)}</b></div>
        {order.upfrontDue > 0 ? (
          <div className="flex justify-between text-sm"><span className="text-ink-muted">{t("upfront")}</span><b>{formatEgp(order.upfrontDue, locale)}</b></div>
        ) : null}
      </div>
    </section>
  );
}

function TrackCard({ order }: { order: PlacedOrder }) {
  const t = useTranslations("orderSuccess.track");
  const session = useSession();

  return (
    <section className="flex flex-col gap-3 rounded-card border border-feature-line bg-feature p-5 text-on-carbon md:p-6">
      <h2 className="m-0 text-lg font-bold">{t("title")}</h2>
      {session.status === "signedOut" ? <p className="m-0 text-sm leading-7 text-footer-text">{t("accountHint")}</p> : null}
      <Link href={`/t/${order.trackingToken}`} className="flex h-12 items-center justify-center rounded-control bg-orange font-bold text-on-orange">
        {t("track")}
      </Link>
      {session.status === "signedOut" ? (
        <Link href="/login?next=/account" className="flex h-12 items-center justify-center rounded-control border border-footer-line font-bold">
          {t("createAccount")}
        </Link>
      ) : null}
    </section>
  );
}

/** The confirmation after checkout, read from the order the checkout stored in this tab. */
export function OrderSuccess({ whatsApp }: { whatsApp: OrderWhatsApp }) {
  const t = useTranslations("orderSuccess");
  const mounted = useMounted();
  if (!mounted) return null;
  const order = readLastOrder();

  if (!order) {
    return (
      <div role="status" className="flex flex-col items-center gap-3 rounded-card border border-line bg-surface p-10 text-center">
        <p className="m-0 text-lg font-bold">{t("missing")}</p>
        <Link href="/" className={linkClass}>{t("home")}</Link>
        <Link href="/warranty" className={linkClass}>{t("trackingHelp")}</Link>
      </div>
    );
  }

  return (
    <div className="mx-auto flex max-w-[760px] flex-col gap-4">
      <header className="flex flex-col gap-2">
        <span dir="ltr" className="font-mono text-sm font-semibold text-orange-text">{order.reference}</span>
        <h1 className="m-0 text-3xl font-bold md:text-[40px]">{t("title")}</h1>
        <p className="m-0 text-base leading-7 text-ink-muted">
          {t.rich("lead", { phone: () => <bdi dir="ltr" className="font-mono">{displayPhone(order.phoneNumber)}</bdi> })}
        </p>
      </header>
      <OrderWhatsAppLink
        whatsApp={whatsApp} reference={order.reference} label={t("whatsApp")}
        className="press flex h-[54px] items-center justify-center gap-2 rounded-control bg-orange font-bold text-on-orange"
      />
      <NextSteps order={order} />
      <Summary order={order} />
      <TrackCard order={order} />
    </div>
  );
}
