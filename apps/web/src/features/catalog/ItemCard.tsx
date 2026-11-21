"use client";

import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { cardClass } from "@/components/ui/Card";
import { BanIcon, ImagePlaceholderIcon, ShieldIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { productPath } from "@/lib/seo/urls";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { itemLine } from "@/features/cart/cart-rules";
import { FavouriteButton } from "@/features/favourites/FavouriteButton";
import { NotifyMeButton } from "@/features/requests/NotifyMeButton";
import { useCatalogPending } from "./use-catalog-pending";

/**
 * One catalog card (catalog grid, home "just arrived"). The photo sits in a square frame and is never cropped; the title
 * is at most two lines and the badge and price share the bottom row so cards in a row line up.
 */
export function ItemCard({ item }: { item: Schemas["StoreItemCard"] }) {
  const t = useTranslations("item");
  const tc = useTranslations("common.condition");
  const locale = useLocale();
  const pending = useCatalogPending();
  const condition = parseCondition(item.condition);
  const soldOut = item.available <= 0;
  const href = productPath(item.slug);

  return (
    <article className={`${cardClass({ padding: "none", interactive: true })} relative h-full overflow-hidden transition-opacity duration-200 starting:opacity-0 motion-reduce:transition-none ${pending ? "opacity-60" : "opacity-100"}`}>
      <Link
        href={href}
        // The accessible name includes the visible badge and "sold out" text (WCAG label-in-name).
        aria-label={[item.modelName, condition ? tc(condition) : null, soldOut ? t("soldOutBadge") : null].filter(Boolean).join(" ")}
        className={`relative flex aspect-square items-center justify-center overflow-hidden bg-surface text-muted ${soldOut ? "grayscale" : ""}`}
      >
        {item.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(item.thumbnailStorageKey)} alt="" width={320} height={320} loading="lazy" className="h-full w-full object-contain p-4" />
        ) : (
          <ImagePlaceholderIcon />
        )}
        {soldOut ? (
          <span className="absolute bottom-3 start-3"><StatusBadge tone="neutral" icon={BanIcon}>{t("soldOutBadge")}</StatusBadge></span>
        ) : null}
      </Link>
      <FavouriteButton code={item.code} modelName={item.modelName} className="absolute end-3 top-3" />
      <div className="flex flex-1 flex-col gap-2 px-4 pb-4 pt-3.5">
        <Link href={href} dir="ltr" className="line-clamp-2 min-h-10 text-start text-sm leading-5 font-bold [unicode-bidi:isolate]">{item.modelName}</Link>
        {item.warrantyLabel ? (
          <span className="flex items-center gap-1.5 text-xs text-ink-muted"><ShieldIcon size={13} />{item.warrantyLabel}</span>
        ) : null}
        <div className="mt-auto flex flex-col gap-2 border-t border-line pt-2.5">
          <div className="flex min-h-7 items-center justify-between gap-2">
            {condition ? <ConditionBadge condition={condition} /> : <span aria-hidden="true" />}
            <span className="text-[18px] font-bold tracking-tight">{formatEgp(item.prices.alone, locale)}</span>
          </div>
          {soldOut ? (
            <NotifyMeButton target={{ itemCode: item.code, modelName: item.modelName }} size="sm" className="w-full" />
          ) : condition ? (
            <AddToCartButton line={itemLine({ ...item, condition }, "Alone", item.prices.alone)} size="sm" className="w-full" />
          ) : null}
        </div>
      </div>
    </article>
  );
}
