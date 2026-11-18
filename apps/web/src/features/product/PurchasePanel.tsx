"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { WhatsAppIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
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
const primary = "flex h-[54px] flex-1 items-center justify-center rounded-control bg-orange text-[17px] font-bold text-on-orange";
const secondary = "flex h-[54px] flex-1 items-center justify-center rounded-control bg-carbon text-base font-bold text-on-carbon";

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
  const notifyClass = "flex h-[54px] flex-1 items-center justify-center gap-2 rounded-control border-2 border-ink font-bold text-ink";

  return (
    <div className="flex flex-col gap-4">
      {choices.length > 0 ? (
        <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
          <legend className="mb-2 text-[15px] font-bold">{t("price.legend")}</legend>
          {choices.map((choice) => (
            <label
              key={choice.option}
              className="flex min-h-14 cursor-pointer items-center gap-3 rounded-card border border-input-line px-3.5 py-3 has-checked:ring-2 has-checked:ring-ink"
            >
              <input
                type="radio"
                name="price-option"
                checked={option === choice.option}
                onChange={() => setOption(choice.option)}
                className="h-[18px] w-[18px] accent-ink"
              />
              <span className="flex-1 text-[15px] font-bold">{t(`price.${optionLabel[choice.option]}`)}</span>
              <b className="text-[17px]">{formatEgp(choice.price, locale)}</b>
            </label>
          ))}
          <span className="text-[13px] text-ink-muted">{t("price.hint")}</span>
        </fieldset>
      ) : null}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-col">
          {count > 1 ? <span className="text-[13px] text-ink-muted">{t("price.total")}</span> : null}
          <span className="text-[30px] font-bold md:text-[34px]">{formatEgp(unitPrice * count, locale)}</span>
        </div>
        {!soldOut && item.condition === "New" && item.available > 1 ? (
          <div role="group" aria-label={t("quantity.label")} className="flex items-center gap-2">
            <button type="button" aria-label={t("quantity.decrease")} disabled={count <= 1} onClick={() => setQuantity(count - 1)} className="h-11 w-11 rounded-control border border-input-line bg-surface text-lg disabled:opacity-40">−</button>
            <span className="min-w-8 text-center font-mono font-semibold">{count}</span>
            <button type="button" aria-label={t("quantity.increase")} disabled={count >= maxQuantity} onClick={() => setQuantity(count + 1)} className="h-11 w-11 rounded-control border border-input-line bg-surface text-lg disabled:opacity-40">+</button>
          </div>
        ) : null}
      </div>

      {soldOut ? (
        <div className="hidden md:flex">
          <NotifyMeButton target={{ itemCode: item.code, modelName: item.modelName }} className={notifyClass} />
        </div>
      ) : (
        <div className="hidden gap-2.5 md:flex">
          <AddToCartButton line={line} thenOpenCart className={primary}>{t("buyNow")}</AddToCartButton>
          <AddToCartButton line={line} className={secondary} />
        </div>
      )}

      {whatsAppHref ? (
        <a
          href={whatsAppHref}
          target="_blank"
          rel="noopener noreferrer"
          className="press hidden h-[54px] items-center justify-center gap-2 rounded-control border-2 border-ink font-bold md:flex"
        >
          <WhatsAppIcon size={20} />
          {whatsAppLabel}
        </a>
      ) : null}

      <Link href={`/builder?parts=${encodeURIComponent(item.code)}`} className="text-[15px] font-bold text-orange-text underline hover:text-orange-text-hover">
        {t("fitsMyPc")}
      </Link>

      <div data-sticky-bar="md" className="fixed inset-x-0 bottom-0 z-40 flex items-center gap-2 border-t border-line bg-surface px-4 pb-[max(1rem,env(safe-area-inset-bottom))] pt-2.5 md:hidden">
        {whatsAppHref ? (
          <a
            href={whatsAppHref}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={whatsAppLabel}
            className="flex h-[52px] w-[52px] shrink-0 items-center justify-center rounded-card border-[1.5px] border-ink"
          >
            <WhatsAppIcon size={22} />
          </a>
        ) : null}
        {soldOut ? (
          <NotifyMeButton target={{ itemCode: item.code, modelName: item.modelName }} className={`${notifyClass} h-[52px]`} />
        ) : (
          <>
            <AddToCartButton line={line} className="flex h-[52px] flex-1 items-center justify-center rounded-card bg-carbon font-bold text-on-carbon" />
            <AddToCartButton line={line} thenOpenCart className="flex h-[52px] flex-1 items-center justify-center rounded-card bg-orange font-bold text-on-orange">
              {t("buyNow")}
            </AddToCartButton>
          </>
        )}
      </div>
    </div>
  );
}
