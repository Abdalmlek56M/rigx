"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { StockDot } from "@/components/ui/StockDot";
import { cardClass } from "@/components/ui/Card";
import { ImagePlaceholderIcon, ShieldIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { productPath } from "@/lib/seo/urls";
import { FavouriteButton } from "@/features/favourites/FavouriteButton";
import { defaultOption, isBuildOnly, offeredOptions, priceOf } from "@/features/product/price-options";
import { ItemAction } from "./ItemAction";
import { QuickView } from "./QuickView";
import { useCatalogPending } from "./use-catalog-pending";

/**
 * One catalog card (catalog grid or list, home "just arrived"). Grid: the photo fills a square frame with the condition on one
 * corner and the heart and quick-view icons on the other; the title starts directly under it, at most two lines, and the price
 * and the one action sit at the bottom so cards in a row line up. List: the same parts in one row, the price and the action on
 * the end side. With a mouse the second photo fades in over the first, and it is only requested after the first pointer-enter.
 */
export function ItemCard({ item, layout = "grid" }: { item: Schemas["StoreItemCard"]; layout?: "grid" | "list" }) {
  const t = useTranslations("item");
  const tc = useTranslations("common.condition");
  const locale = useLocale();
  const pending = useCatalogPending();
  const [secondPhoto, setSecondPhoto] = useState({ requested: false, loaded: false });
  const condition = parseCondition(item.condition);
  const soldOut = item.available <= 0;
  const offered = offeredOptions(item.priceOptions);
  const price = priceOf(item.prices, defaultOption(offered));
  const buildOnly = isBuildOnly(offered);
  const href = productPath(item.slug);
  const list = layout === "list";

  return (
    <article
      onPointerEnter={(event) => {
        if (event.pointerType === "mouse" && item.secondThumbnailStorageKey) setSecondPhoto((current) => ({ ...current, requested: true }));
      }}
      className={`${cardClass({ padding: "none", interactive: true })} relative h-full w-full overflow-hidden transition-opacity duration-200 starting:opacity-0 motion-reduce:transition-none ${list ? "flex-row!" : ""} ${pending ? "opacity-60" : "opacity-100"}`}
    >
      <Link
        href={href}
        // The accessible name includes the visible badge and "sold out" text (WCAG label-in-name).
        aria-label={[item.modelName, condition ? tc(condition) : null, soldOut ? t("soldOutBadge") : null].filter(Boolean).join(" ")}
        className={`photo-frame photo-swap-host relative flex items-center justify-center overflow-hidden bg-surface text-muted ${list ? "min-h-28 w-28 shrink-0 self-stretch sm:w-44" : "aspect-square"} ${soldOut ? "grayscale" : ""}`}
      >
        {item.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(item.thumbnailStorageKey)} alt="" width={320} height={320} loading="lazy" className="main-photo h-full w-full object-cover" />
        ) : (
          <ImagePlaceholderIcon />
        )}
        {secondPhoto.requested && item.secondThumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img
            src={mediaUrl(item.secondThumbnailStorageKey)}
            alt=""
            width={320}
            height={320}
            data-loaded={secondPhoto.loaded ? "" : undefined}
            onLoad={() => setSecondPhoto((current) => ({ ...current, loaded: true }))}
            className="swap-photo absolute inset-0 h-full w-full object-cover"
          />
        ) : null}
        {condition ? <span className="absolute start-3 top-3"><ConditionBadge condition={condition} /></span> : null}
        <span className="absolute bottom-3 start-3"><StockDot available={!soldOut} label={soldOut ? t("soldOutBadge") : t("inStockBadge")} /></span>
      </Link>
      <div className={`absolute end-3 top-3 flex gap-2 ${list ? "" : "flex-col"}`}>
        <FavouriteButton code={item.code} modelName={item.modelName} />
        <QuickView item={item} />
      </div>
      <div className={`flex min-w-0 flex-1 flex-col gap-2 ${list ? "p-3 sm:px-5 sm:py-4" : "px-4 pb-4 pt-3.5"}`}>
        <div className={list ? "pe-24" : undefined}>
          <Link href={href} dir="ltr" className="line-clamp-2 min-h-10 text-start text-sm leading-5 font-bold [unicode-bidi:isolate]">{item.modelName}</Link>
        </div>
        {item.warrantyLabel ? (
          <span className="flex items-center gap-1.5 text-xs text-ink-muted"><ShieldIcon size={13} />{item.warrantyLabel}</span>
        ) : null}
        <div className={`mt-auto flex flex-col gap-2 border-t border-line pt-2.5 ${list ? "sm:flex-row sm:items-center sm:justify-between sm:gap-6" : ""}`}>
          <div className="flex flex-col items-start gap-0.5">
            <span className="whitespace-nowrap text-[17px] font-bold tracking-tight sm:text-[18px]">
              {formatEgp(price, locale)}
            </span>
            {buildOnly ? <span className="text-xs text-ink-muted">{t("inBuildOnly")}</span> : null}
          </div>
          <ItemAction item={item} className={list ? "w-full sm:w-auto sm:min-w-44" : "w-full"} />
        </div>
      </div>
    </article>
  );
}
