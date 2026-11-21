"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { QuantityStepper } from "@/components/ui/QuantityStepper";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { CpuIcon } from "@/components/ui/icons";
import type { Condition } from "@/lib/condition";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { clampQuantity, itemLine, type PriceOption } from "@/features/cart/cart-rules";
import { NotifyMeButton } from "@/features/requests/NotifyMeButton";
import { DEFAULT_OPTION, priceChoices, priceOf, type Prices } from "./price-options";

export interface PurchaseItem {
  code: string;
  modelName: string;
  condition: Condition;
  warrantyLabel: string | null;
  thumbnailStorageKey: string | null;
  available: number;
  prices: Prices;
}

interface PurchasePanelProps {
  item: PurchaseItem;
  /** Store WhatsApp number (digits) and the product template from settings; no number = no WhatsApp button. */
  whatsApp: { number: string | null; template: string };
  /** Public URL of this page for the message ("" when the store has no domain set). */
  pageUrl: string;
}

const optionLabel: Record<PriceOption, "inBuild" | "withBoard" | "alone"> = { InBuild: "inBuild", WithBoard: "withBoard", Alone: "alone" };

/** Price options, quantity, buy / add-to-cart / notify-me and the WhatsApp question; on mobile the actions sit in a sticky bottom bar. */
export function PurchasePanel({ item, whatsApp, pageUrl }: PurchasePanelProps) {
  const t = useTranslations("product");
  const locale = useLocale();
  const [option, setOption] = useState<PriceOption>(DEFAULT_OPTION);
  const [quantity, setQuantity] = useState(1);
  const choices = priceChoices(item.prices);
  const unitPrice = priceOf(item.prices, option);
  const soldOut = item.available <= 0;
  const maxQuantity = item.condition === "Used" ? 1 : item.available;
  const count = clampQuantity({ condition: item.condition, maxQuantity }, quantity);
  const line = itemLine(item, option, unitPrice, count);

  const message = fillTemplate(whatsApp.template, { item: item.modelName, code: item.code, price: formatEgp(unitPrice, locale), link: pageUrl })
    || t("whatsApp", { item: item.modelName, code: item.code });
  const whatsAppHref = whatsApp.number ? whatsAppUrl(whatsApp.number, message) : null;
  const whatsAppLabel = soldOut ? t("askWhatsApp") : t("orderWhatsApp");

  return (
    <Card padding="md" className="gap-4">
      {choices.length > 0 ? (
        <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
          <legend className="mb-2 text-xs font-bold">{t("price.legend")}</legend>
          {choices.map((choice) => (
            <label
              key={choice.option}
              className="flex min-h-14 cursor-pointer items-center gap-3 rounded-card border border-input-line px-3.5 py-3 has-checked:border-ink has-checked:ring-2 has-checked:ring-ink"
            >
              <input
                type="radio"
                name="price-option"
                checked={option === choice.option}
                onChange={() => setOption(choice.option)}
                className="h-[18px] w-[18px] accent-ink"
              />
              <span className="flex-1 text-[15px] font-bold">{t(`price.${optionLabel[choice.option]}`)}</span>
              <b className="text-[17px] tabular-nums">{formatEgp(choice.price, locale)}</b>
            </label>
          ))}
          <span className="text-xs text-ink-muted">{t("price.hint")}</span>
        </fieldset>
      ) : null}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-col gap-0.5">
          <span className="type-label">{count > 1 ? t("price.total") : t("price.alone")}</span>
          <span className="text-[30px] font-bold tabular-nums md:text-[34px]">{formatEgp(unitPrice * count, locale)}</span>
        </div>
        {!soldOut && item.condition === "New" && item.available > 1 ? (
          <QuantityStepper
            value={count}
            max={maxQuantity}
            onChange={setQuantity}
            label={t("quantity.label")}
            decreaseLabel={t("quantity.decrease")}
            increaseLabel={t("quantity.increase")}
          />
        ) : null}
      </div>

      {soldOut ? (
        <NotifyMeButton target={{ itemCode: item.code, modelName: item.modelName }} className="hidden w-full md:flex" />
      ) : (
        <div className="hidden gap-2.5 md:flex">
          <AddToCartButton line={line} thenOpenCart variant="accent" className="flex-1">{t("buyNow")}</AddToCartButton>
          <AddToCartButton line={line} variant="primary" className="flex-1" />
        </div>
      )}

      {whatsAppHref ? <WhatsAppButton href={whatsAppHref} variant="outline" className="hidden md:flex">{whatsAppLabel}</WhatsAppButton> : null}

      <ButtonLink href={`/builder?parts=${encodeURIComponent(item.code)}`} variant="ghost" size="sm" icon={CpuIcon} className="self-start">
        {t("fitsMyPc")}
      </ButtonLink>

      <div data-sticky-bar="md" className="fixed inset-x-0 bottom-0 z-40 flex items-center gap-2 border-t border-line bg-surface px-4 pb-[max(1rem,env(safe-area-inset-bottom))] pt-2.5 md:hidden">
        {whatsAppHref ? <WhatsAppButton href={whatsAppHref} label={whatsAppLabel} className="shrink-0" /> : null}
        {soldOut ? (
          <NotifyMeButton target={{ itemCode: item.code, modelName: item.modelName }} className="flex-1" />
        ) : (
          <>
            <AddToCartButton line={line} variant="primary" className="flex-1" />
            <AddToCartButton line={line} thenOpenCart variant="accent" className="flex-1">{t("buyNow")}</AddToCartButton>
          </>
        )}
      </div>
    </Card>
  );
}
