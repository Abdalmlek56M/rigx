import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { ImagePlaceholderIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { productPath } from "@/lib/seo/urls";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { itemLine } from "@/features/cart/cart-rules";
import { FavouriteButton } from "@/features/favourites/FavouriteButton";
import { NotifyMeButton } from "@/features/requests/NotifyMeButton";

const buttonBase = "inline-flex h-10 items-center justify-center gap-2 rounded-full px-4 text-[13px] font-bold whitespace-nowrap";

/**
 * One catalog card (catalog grid, home "just arrived"). Priced as the item bought alone; a sold-out item shows "خلص"
 * with the notify-me button instead of add-to-cart. The heart sits on the photo's top end corner (the badge is on the start).
 */
export function ItemCard({ item }: { item: Schemas["StoreItemCard"] }) {
  const t = useTranslations("item");
  const tc = useTranslations("common.condition");
  const locale = useLocale();
  const condition = parseCondition(item.condition);
  const soldOut = item.available <= 0;
  const href = productPath(item.slug);

  return (
    <article className="lift relative flex w-full flex-col overflow-hidden rounded-card border border-line bg-surface">
      <Link
        href={href}
        // The accessible name includes the visible badge and "sold out" text (WCAG label-in-name).
        aria-label={[item.modelName, condition ? tc(condition) : null, soldOut ? t("soldOutBadge") : null].filter(Boolean).join(" ")}
        className={`relative flex aspect-[4/3] items-center justify-center overflow-hidden bg-paper text-muted ${soldOut ? "grayscale" : ""}`}
      >
        {item.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(item.thumbnailStorageKey)} alt="" width={320} height={240} loading="lazy" className="h-full w-full object-contain p-3" />
        ) : (
          <ImagePlaceholderIcon />
        )}
        {condition ? (
          <span className="absolute start-3 top-3">
            <ConditionBadge condition={condition} />
          </span>
        ) : null}
        {soldOut ? (
          <span className="absolute bottom-3 start-3 rounded-full bg-surface px-2.5 py-1 text-xs font-bold text-ink-muted shadow-soft">{t("soldOutBadge")}</span>
        ) : null}
      </Link>
      <FavouriteButton code={item.code} modelName={item.modelName} className="absolute end-3 top-3" />
      <div className="flex flex-1 flex-col gap-2 px-4 pb-4 pt-3.5">
        <Link href={href} dir="ltr" className="text-start text-[15px] font-bold [unicode-bidi:isolate]">{item.modelName}</Link>
        {item.warrantyLabel ? <span className="text-xs text-ink-muted">{item.warrantyLabel}</span> : null}
        <div className="mt-auto flex flex-wrap items-center justify-between gap-2 border-t border-line pt-2.5">
          {soldOut ? (
            <>
              <span className="text-[13px] text-ink-muted">{t("soldOut")}</span>
              <NotifyMeButton
                target={{ itemCode: item.code, modelName: item.modelName }}
                className={`${buttonBase} border-[1.5px] border-ink text-ink`}
              />
            </>
          ) : (
            <>
              <span className="text-[18px] font-bold tracking-tight">{formatEgp(item.prices.alone, locale)}</span>
              {condition ? (
                <AddToCartButton
                  line={itemLine({ ...item, condition }, "Alone", item.prices.alone)}
                  className={`${buttonBase} bg-orange text-on-orange transition-[filter] hover:brightness-105`}
                />
              ) : null}
            </>
          )}
        </div>
      </div>
    </article>
  );
}
