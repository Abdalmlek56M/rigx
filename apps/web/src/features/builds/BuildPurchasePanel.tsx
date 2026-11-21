"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { QuantityStepper } from "@/components/ui/QuantityStepper";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { BanIcon } from "@/components/ui/icons";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { clampQuantity } from "@/features/cart/cart-rules";
import { buildCartLine, buildTitle, type BuildView } from "./build-view";

interface BuildPurchasePanelProps {
  build: BuildView;
  /** Store WhatsApp number (digits) and the build template from settings; no number = no WhatsApp button. */
  whatsApp: { number: string | null; template: string };
  /** Public URL of this page for the message ("" when the store has no domain set). */
  pageUrl: string;
}

/** Price, copies stepper, buy / add-to-cart, the WhatsApp question and "edit it"; on mobile the actions sit in a sticky bottom bar. */
export function BuildPurchasePanel({ build, whatsApp, pageUrl }: BuildPurchasePanelProps) {
  const t = useTranslations("build");
  const locale = useLocale();
  const [quantity, setQuantity] = useState(1);
  const soldOut = build.copies <= 0;
  const count = clampQuantity({ condition: null, maxQuantity: build.copies }, quantity);
  const line = buildCartLine(build, locale, count);
  const title = buildTitle(build, locale);
  const message = fillTemplate(whatsApp.template, { item: title, code: build.code, price: formatEgp(build.salePrice, locale), link: pageUrl })
    || t("whatsApp", { item: title, code: build.code });
  const whatsAppHref = whatsApp.number ? whatsAppUrl(whatsApp.number, message) : null;
  const whatsAppLabel = soldOut ? t("askWhatsApp") : t("orderWhatsApp");

  return (
    <Card padding="md" className="gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-col gap-0.5">
          <span className="type-label">{count > 1 ? t("total") : t("priceNote")}</span>
          <span className="text-[30px] font-bold tabular-nums md:text-[34px]">{formatEgp(build.salePrice * count, locale)}</span>
        </div>
        {!soldOut && build.copies > 1 ? (
          <QuantityStepper
            value={count}
            max={build.copies}
            onChange={setQuantity}
            label={t("quantity.label")}
            decreaseLabel={t("quantity.decrease")}
            increaseLabel={t("quantity.increase")}
          />
        ) : null}
      </div>
      {soldOut ? (
        <StatusBadge tone="neutral" icon={BanIcon}>{t("soldOut")}</StatusBadge>
      ) : (
        <div className="hidden gap-2.5 md:flex">
          <AddToCartButton line={line} thenOpenCart variant="accent" className="flex-1">{t("buyNow")}</AddToCartButton>
          <AddToCartButton line={line} variant="primary" className="flex-1" />
        </div>
      )}
      {whatsAppHref ? <WhatsAppButton href={whatsAppHref} variant="outline" className="hidden md:flex">{whatsAppLabel}</WhatsAppButton> : null}
      <ButtonLink href={`/builder?from=${build.code}`} variant="secondary" className="hidden md:flex">{t("edit")}</ButtonLink>
      <div data-sticky-bar="md" className="fixed inset-x-0 bottom-0 z-40 flex items-center gap-2 border-t border-line bg-surface px-4 pb-[max(1rem,env(safe-area-inset-bottom))] pt-2.5 md:hidden">
        {whatsAppHref ? <WhatsAppButton href={whatsAppHref} label={whatsAppLabel} className="shrink-0" /> : null}
        {soldOut ? null : (
          <>
            <AddToCartButton line={line} variant="primary" className="flex-1" />
            <AddToCartButton line={line} thenOpenCart variant="accent" className="flex-1">{t("buyNow")}</AddToCartButton>
          </>
        )}
      </div>
    </Card>
  );
}
