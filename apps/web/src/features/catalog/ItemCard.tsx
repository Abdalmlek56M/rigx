"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { StockDot } from "@/components/ui/StockDot";
import { cardClass } from "@/components/ui/Card";
import { SpecIcon } from "@/components/ui/SpecIcon";
import { ImagePlaceholderIcon } from "@/components/ui/icons";
import { WarrantyTag } from "@/components/ui/WarrantyTag";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { productPath } from "@/lib/seo/urls";
import { FavouriteButton } from "@/features/favourites/FavouriteButton";
import { defaultOption, isBuildOnly, offeredOptions, priceOf } from "@/features/product/price-options";
import { useBrandName, useCardSpecs } from "./category-tree-context";
import { ItemAction } from "./ItemAction";
import { QuickView } from "./QuickView";
import { useCatalogPending } from "./use-catalog-pending";

/**
 * One catalog card (catalog grid or list, home "just arrived"). Grid: the photo fills a square frame (cropped to fit, never
 * letterboxed) with the stock state on one corner and the heart and quick-view icons on the other; under it, packed together with no
 * empty band, the brand in small orange letters, the title (a fixed two-line area, so every card's specs start at the same height), two or three key specs with their icons (two on a phone's
 * narrow grid), and the condition and warranty; the price and the one action sit at the bottom so cards in a row line up (any spare height
 * goes above the price). A sold-out card dims its photo, says "نفدت الكمية" on it, mutes the price and offers "أبلغني عند التوفر". List: the
 * same parts in one row, the price and the action on the end side. With a mouse the second photo fades in over the first, and it is only
 * requested after the first pointer-enter.
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
  const specs = useCardSpecs(item);
  const brand = useBrandName(item.brandId);

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
        className={`photo-swap-host relative flex items-center justify-center overflow-hidden bg-surface text-muted ${list ? "min-h-28 w-28 shrink-0 self-stretch sm:w-44" : "aspect-square"} ${soldOut ? "sold-out-photo" : ""}`}
      >
        {item.thumbnailStorageKey ? (
          <span className="main-photo absolute inset-0">
            {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
            <img src={mediaUrl(item.thumbnailStorageKey)} alt="" width={320} height={320} loading="lazy" className="product-photo h-full w-full object-cover" />
          </span>
        ) : (
          <ImagePlaceholderIcon />
        )}
        {secondPhoto.requested && item.secondThumbnailStorageKey ? (
          <span data-loaded={secondPhoto.loaded ? "" : undefined} className="swap-photo absolute inset-0">
            {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
            <img
              src={mediaUrl(item.secondThumbnailStorageKey)}
              alt=""
              width={320}
              height={320}
              onLoad={() => setSecondPhoto((current) => ({ ...current, loaded: true }))}
              className="product-photo h-full w-full object-cover"
            />
          </span>
        ) : null}
        {soldOut ? (
          <span className="absolute inset-x-0 bottom-3 flex justify-center">
            <span className="inline-flex h-7 items-center rounded-full bg-carbon/85 px-3 text-xs font-bold text-on-carbon shadow-soft backdrop-blur-sm">{t("soldOutBadge")}</span>
          </span>
        ) : (
          <span className="absolute bottom-3 start-3"><StockDot available label={t("inStockBadge")} /></span>
        )}
      </Link>
      <div className={`absolute end-3 top-3 flex gap-2 ${list ? "" : "flex-col"}`}>
        <FavouriteButton code={item.code} modelName={item.modelName} />
        <QuickView item={item} />
      </div>
      <div className={`flex min-w-0 flex-1 flex-col gap-2 ${list ? "p-3 sm:px-5 sm:py-4" : "px-3 pb-3.5 pt-3 sm:px-4 sm:pb-4 sm:pt-3.5"}`}>
        <div className={`flex flex-col gap-0.5 ${list ? "pe-24" : ""}`}>
          <span dir="ltr" className="min-h-4 truncate text-start text-[11px] font-bold uppercase leading-4 tracking-wide text-orange-text [unicode-bidi:isolate]">{brand}</span>
          <Link href={href} dir="ltr" className={`line-clamp-2 text-start text-sm leading-5 font-bold [unicode-bidi:isolate] ${list ? "" : "min-h-10"}`}>{item.modelName}</Link>
        </div>
        {specs.length > 0 ? (
          <ul className="m-0 flex list-none flex-wrap gap-1 p-0">
            {specs.map((spec) => (
              <li key={spec.key} title={spec.name} className={`h-6 items-center gap-1 rounded-full border border-line bg-paper px-1.5 text-xs font-semibold text-ink sm:px-2 ${list ? "inline-flex" : "inline-flex nth-[n+3]:hidden sm:nth-[n+3]:inline-flex"}`}>
                <SpecIcon specKey={spec.key} size={13} />
                <span className="sr-only">{spec.name}: </span>
                <span dir="ltr" className="[unicode-bidi:isolate]">{spec.value}</span>
              </li>
            ))}
          </ul>
        ) : null}
        {condition || item.warrantyLabel ? (
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
            {condition ? <ConditionBadge condition={condition} /> : null}
            <WarrantyTag label={item.warrantyLabel} />
          </div>
        ) : null}
        <div className={`mt-auto flex flex-col gap-2 border-t border-line pt-2.5 ${list ? "sm:flex-row sm:items-center sm:justify-between sm:gap-6" : ""}`}>
          <div className="flex flex-col items-start gap-0.5">
            <span className={`whitespace-nowrap text-[17px] font-bold tracking-tight sm:text-[18px] ${soldOut ? "text-ink-muted" : ""}`}>
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
