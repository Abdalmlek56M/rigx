"use client";

import { useLocale, useTranslations } from "next-intl";
import { displayPhone } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import { useMounted } from "@/lib/hooks/use-mounted";
import { useSession } from "@/features/auth/use-session";
import { OrderWhatsAppLink, type OrderWhatsApp } from "@/features/tracking/OrderWhatsAppLink";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { BoxIcon, HelpIcon, TruckIcon, WalletIcon } from "@/components/ui/icons";
import { readLastOrder, type PlacedOrder } from "./last-order";
import { depositOf, isTransfer } from "./order-rules";
import { TransferInstructions } from "./TransferInstructions";

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
    <Card title={t("title")} icon={HelpIcon} padding="md" className="gap-3.5">
      <ol className="m-0 flex list-none flex-col gap-3.5 p-0">
        {steps.map((step, index) => (
          <li key={step.title} className="flex items-start gap-3">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full surface-strong font-mono text-sm font-bold">{index + 1}</span>
            <span className="text-sm leading-7"><b>{step.title}</b> {step.body}</span>
          </li>
        ))}
      </ol>
    </Card>
  );
}

function Summary({ order }: { order: PlacedOrder }) {
  const t = useTranslations("orderSuccess.summary");
  const locale = useLocale();

  return (
    <Card title={t("title")} icon={BoxIcon} padding="md" className="gap-3">
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {order.items.map((item) => (
          <li key={`${item.code}-${item.name}`} className="flex justify-between gap-3 text-sm">
            <span dir="auto" className="min-w-0 [unicode-bidi:isolate]">
              {item.quantity > 1 ? `${item.quantity} × ` : ""}{item.name}
              {item.code ? <span dir="ltr" className="ms-1.5 font-mono text-xs text-ink-muted">{item.code}</span> : null}
            </span>
            <b className="shrink-0 tabular-nums">{formatEgp(item.lineTotal, locale)}</b>
          </li>
        ))}
      </ul>
      <dl className="m-0 flex flex-col gap-2 border-t border-line pt-3">
        {order.couponDiscount > 0 ? (
          <div className="flex justify-between text-sm"><dt className="text-ink-muted">{t("couponDiscount", { code: order.couponCode ?? "" })}</dt><dd className="m-0">− {formatEgp(order.couponDiscount, locale)}</dd></div>
        ) : null}
        {order.fulfillmentMethod === "Delivery" ? (
          <div className="flex justify-between text-sm">
            <dt className="text-ink-muted">{t("shipping")}</dt>
            <dd className={`m-0 ${order.deliveryFee > 0 ? "" : "font-bold text-success"}`}>{order.deliveryFee > 0 ? formatEgp(order.deliveryFee, locale) : t("freeShipping")}</dd>
          </div>
        ) : null}
        <div className="flex items-baseline justify-between"><dt className="font-bold">{t("total")}</dt><dd className="m-0 text-[22px] font-bold tabular-nums">{formatEgp(order.total, locale)}</dd></div>
        {order.upfrontDue > 0 ? (
          <div className="flex justify-between text-sm"><dt className="text-ink-muted">{t("upfront")}</dt><dd className="m-0 font-bold">{formatEgp(order.upfrontDue, locale)}</dd></div>
        ) : null}
      </dl>
    </Card>
  );
}

function TrackCard({ order }: { order: PlacedOrder }) {
  const t = useTranslations("orderSuccess.track");
  const session = useSession();

  return (
    <Card title={t("title")} icon={TruckIcon} padding="md" className="accent-panel gap-3">
      {session.status === "signedOut" ? <p className="m-0 text-sm leading-7 text-ink-muted">{t("accountHint")}</p> : null}
      <ButtonLink href={`/t/${order.trackingToken}`} variant="accent" className="w-full">{t("track")}</ButtonLink>
      {session.status === "signedOut" ? (
        <ButtonLink href="/login?next=/account" variant="secondary" className="w-full">{t("createAccount")}</ButtonLink>
      ) : null}
    </Card>
  );
}

/** The confirmation after checkout, read from the order the checkout stored in this tab. */
export function OrderSuccess({ whatsApp, transferInstructions }: { whatsApp: OrderWhatsApp; transferInstructions: string | null }) {
  const t = useTranslations("orderSuccess");
  const tp = useTranslations("checkout.payment");
  const mounted = useMounted();
  if (!mounted) return null;
  const order = readLastOrder();

  if (!order) {
    return (
      <Card padding="lg" className="items-center gap-3 text-center">
        <p className="m-0 type-section-title">{t("missing")}</p>
        <div className="flex flex-wrap justify-center gap-2">
          <ButtonLink href="/" variant="secondary" size="sm">{t("home")}</ButtonLink>
          <ButtonLink href="/warranty" variant="ghost" size="sm">{t("trackingHelp")}</ButtonLink>
        </div>
      </Card>
    );
  }

  return (
    <div className="mx-auto flex max-w-[760px] flex-col gap-4">
      <PageHeader
        contained={false}
        eyebrow={order.reference}
        title={t("title")}
        lead={t.rich("lead", { phone: () => <bdi dir="ltr" className="font-mono">{displayPhone(order.phoneNumber)}</bdi> })}
        actions={<OrderWhatsAppLink whatsApp={whatsApp} reference={order.reference} label={t("whatsApp")} />}
      />
      {transferInstructions && isTransfer(order.paymentMethod) ? (
        <Card title={tp("transferTitle")} icon={WalletIcon} padding="md" className="gap-2 text-sm leading-7">
          <TransferInstructions text={transferInstructions} titled={false} />
        </Card>
      ) : null}
      <NextSteps order={order} />
      <Summary order={order} />
      <TrackCard order={order} />
    </div>
  );
}
