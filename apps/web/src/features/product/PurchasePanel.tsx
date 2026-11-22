"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { QuantityStepper } from "@/components/ui/QuantityStepper";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { CpuIcon } from "@/components/ui/icons";
import type { Condition } from "@/lib/condition";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { clampQuantity, itemLine, type PriceOption } from "@/features/cart/cart-rules";
import { NotifyMeButton } from "@/features/requests/NotifyMeButton";
import { defaultOption, isBuildOnly, offeredOptions, priceChoices, priceOf, type Prices } from "./price-options";

export interface PurchaseItem {
  code: string;
  modelName: string;
  condition: Condition;
  warrantyLabel: string | null;
  thumbnailStorageKey: string | null;
  available: number;
  prices: Prices;
  /** The sale prices the category offers, in order; the buyer sees and buys only these. */
  priceOptions: readonly string[];
}

interface PurchasePanelProps {
  item: PurchaseItem;
  /** Store WhatsApp number (digits) and the product template from settings; no number = no WhatsApp button. */
  whatsApp: { number: string | null; template: string };
  /** Public URL of this page for the message ("" when the store has no domain set). */
  pageUrl: string;
}

const optionLabel: Record<PriceOption, "inBuild" | "withBoard" | "alone"> = { InBuild: "inBuild", WithBoard: "withBoard", Alone: "alone" };
const optionHelp: Record<PriceOption, "inBuildHelp" | "withBoardHelp" | "aloneHelp"> = {
  InBuild: "inBuildHelp",
  WithBoard: "withBoardHelp",
  Alone: "aloneHelp"
};

/** Price options, quantity, buy / add-to-cart / notify-me and the WhatsApp question; on mobile the actions sit in a sticky bottom bar. */
export function PurchasePanel({ item, whatsApp, pageUrl }: PurchasePanelProps) {
  const t = useTranslations("product");
  const locale = useLocale();
  const offered = offeredOptions(item.priceOptions);
  const [option, setOption] = useState<PriceOption>(() => defaultOption(offered));
  const [quantity, setQuantity] = useState(1);
  const choices = priceChoices(item.prices, offered);
  const unitPrice = priceOf(item.prices, option);
  const soldOut = item.available <= 0;
  const buildOnly = isBuildOnly(offered);
  const maxQuantity = item.condition === "Used" ? 1 : item.available;
  const count = clampQuantity({ condition: item.condition, maxQuantity }, quantity);
  const line = itemLine(item, option, unitPrice, count);

  const message = fillTemplate(whatsApp.template, { item: item.modelName, code: item.code, price: formatEgp(unitPrice, locale), link: pageUrl })
    || t("whatsApp", { item: item.modelName, code: item.code });
  const whatsAppHref = whatsApp.number ? whatsAppUrl(whatsApp.number, message) : null;
  const whatsAppLabel = soldOut ? t("askWhatsApp") : t("orderWhatsApp");
  const builderHref = `/builder?parts=${encodeURIComponent(item.code)}`;

  return (
    <Card padding="md" className="gap-4">
      {choices.length > 0 ? (
        <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
          <legend className="text-xs font-bold">{t("price.legend")}</legend>
          <p className="m-0 text-xs leading-5 text-ink-muted">{t("price.hint")}</p>
          {choices.map((choice) => {
            const saving = item.prices.alone - choice.price;
            return (
              <label
                key={choice.option}
                className="flex cursor-pointer flex-col gap-1.5 rounded-card border border-line bg-surface px-2.5 py-3 transition-colors hover:border-ink-muted has-checked:border-orange has-checked:bg-orange-soft motion-reduce:transition-none"
              >
                <span className="flex items-center gap-2">
                  <input
                    type="radio"
                    name="price-option"
                    checked={option === choice.option}
                    onChange={() => setOption(choice.option)}
                    className="h-[18px] w-[18px] shrink-0 accent-orange"
                  />
                  <b className="min-w-0 flex-1 text-[13px] md:text-sm">{t(`price.${optionLabel[choice.option]}`)}</b>
                  <span className="flex shrink-0 flex-col items-end gap-1">
                    <b className="whitespace-nowrap text-[17px] font-bold tabular-nums">{formatEgp(choice.price, locale)}</b>
                    {saving > 0 ? <StatusBadge tone="success">{t("price.save", { amount: formatEgp(saving, locale) })}</StatusBadge> : null}
                  </span>
                </span>
                <span className="type-label ps-8">{t(`price.${optionHelp[choice.option]}`)}</span>
              </label>
            );
          })}
        </fieldset>
      ) : null}

      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="flex flex-col gap-0.5">
          <span className="type-label">{count > 1 ? t("price.total") : t("price.price")}</span>
          <span className="text-[30px] font-bold tabular-nums md:text-[34px]">{formatEgp(unitPrice * count, locale)}</span>
        </div>
        {!soldOut && !buildOnly && item.condition === "New" && item.available > 1 ? (
          <div className="shrink-0 pb-1">
            <QuantityStepper
              value={count}
              max={maxQuantity}
              onChange={setQuantity}
              label={t("quantity.label")}
              decreaseLabel={t("quantity.decrease")}
              increaseLabel={t("quantity.increase")}
            />
          </div>
        ) : null}
      </div>

      {buildOnly ? <p className="m-0 text-sm leading-6 text-ink-muted">{t("buildOnly.note")}</p> : null}

      {soldOut ? (
        <NotifyMeButton target={{ itemCode: item.code, modelName: item.modelName }} className="hidden w-full md:flex" />
      ) : buildOnly ? (
        <ButtonLink href={builderHref} variant="accent" icon={CpuIcon} className="hidden w-full md:flex">{t("buildOnly.cta")}</ButtonLink>
      ) : (
        <div className="hidden gap-2.5 md:flex">
          <AddToCartButton line={line} thenOpenCart variant="accent" className="flex-1">{t("buyNow")}</AddToCartButton>
          <AddToCartButton line={line} variant="primary" className="flex-1" />
        </div>
      )}

      {whatsAppHref ? <WhatsAppButton href={whatsAppHref} variant="outline" className="max-md:hidden">{whatsAppLabel}</WhatsAppButton> : null}

      {buildOnly ? null : (
        <ButtonLink href={builderHref} variant="ghost" size="sm" icon={CpuIcon} className="self-start">{t("fitsMyPc")}</ButtonLink>
      )}

      <div data-sticky-bar="md" className="fixed inset-x-0 bottom-0 z-40 flex items-center gap-2 border-t border-line bg-surface px-4 pb-[max(1rem,env(safe-area-inset-bottom))] pt-2.5 md:hidden">
        {whatsAppHref ? <WhatsAppButton href={whatsAppHref} label={whatsAppLabel} className="shrink-0" /> : null}
        {soldOut ? (
          <NotifyMeButton target={{ itemCode: item.code, modelName: item.modelName }} className="flex-1" />
        ) : buildOnly ? (
          <ButtonLink href={builderHref} variant="accent" icon={CpuIcon} className="flex-1">{t("buildOnly.cta")}</ButtonLink>
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
