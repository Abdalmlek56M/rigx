"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { WhatsAppIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
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

const primary = "flex h-[54px] flex-1 items-center justify-center rounded-control bg-orange text-[17px] font-bold text-on-orange";
const secondary = "flex h-[54px] flex-1 items-center justify-center rounded-control bg-carbon text-base font-bold text-on-carbon";
const stepper = "h-11 w-11 rounded-control border border-input-line bg-surface text-lg disabled:opacity-40";

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

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-col">
          <span className="text-[13px] text-ink-muted">{count > 1 ? t("total") : t("priceNote")}</span>
          <span className="text-[30px] font-bold md:text-[34px]">{formatEgp(build.salePrice * count, locale)}</span>
        </div>
        {!soldOut && build.copies > 1 ? (
          <div role="group" aria-label={t("quantity.label")} className="flex items-center gap-2">
            <button type="button" aria-label={t("quantity.decrease")} disabled={count <= 1} onClick={() => setQuantity(count - 1)} className={stepper}>−</button>
            <span className="min-w-8 text-center font-mono font-semibold">{count}</span>
            <button type="button" aria-label={t("quantity.increase")} disabled={count >= build.copies} onClick={() => setQuantity(count + 1)} className={stepper}>+</button>
          </div>
        ) : null}
      </div>
      {soldOut ? (
        <p role="status" className="m-0 rounded-card bg-paper p-3 text-[15px] font-bold text-ink-muted">{t("soldOut")}</p>
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
          className="hidden h-[50px] items-center justify-center gap-2 rounded-control border-2 border-ink font-bold md:flex"
        >
          <WhatsAppIcon size={18} />
          {t("askWhatsApp", { code: build.code })}
        </a>
      ) : null}
      <Link
        href={`/builder?from=${build.code}`}
        className="flex h-[50px] items-center justify-center rounded-control border-[1.5px] border-ink font-bold"
      >
        {t("edit")}
      </Link>
      <div className="fixed inset-x-0 bottom-0 z-40 flex items-center gap-2 border-t border-line bg-surface px-4 pb-4 pt-2.5 md:hidden">
        {whatsAppHref ? (
          <a
            href={whatsAppHref}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={t("askWhatsApp", { code: build.code })}
            className="flex h-[52px] w-[52px] shrink-0 items-center justify-center rounded-card border-[1.5px] border-ink"
          >
            <WhatsAppIcon size={22} />
          </a>
        ) : null}
        {soldOut ? null : (
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
