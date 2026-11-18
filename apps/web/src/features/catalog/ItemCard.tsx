import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { ImagePlaceholderIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { itemLine } from "@/features/cart/cart-rules";
import { NotifyMeButton } from "@/features/requests/NotifyMeButton";

const buttonBase = "inline-flex h-11 items-center justify-center gap-2 rounded-control px-3 text-[13px] font-bold whitespace-nowrap";

/**
 * One catalog card (catalog grid, home "just arrived"). Priced as the item bought alone; a sold-out item shows "خلص"
 * with the notify-me button instead of add-to-cart.
 */
export function ItemCard({ item }: { item: Schemas["StoreItemCard"] }) {
  const t = useTranslations("item");
  const locale = useLocale();
  const condition = parseCondition(item.condition);
  const soldOut = item.available <= 0;
  const href = `/products/${item.code}`;

  return (
    <article className="lift flex w-full flex-col overflow-hidden rounded-card border border-line bg-surface">
      <Link
        href={href}
        aria-label={item.modelName}
        className={`relative flex h-[170px] items-center justify-center text-muted ${condition === "New" ? "bg-paper" : "bg-sand"} ${soldOut ? "grayscale" : ""}`}
      >
        {item.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(item.thumbnailStorageKey)} alt="" width={250} height={170} loading="lazy" className="h-full w-full object-contain" />
        ) : (
          <ImagePlaceholderIcon />
        )}
        {condition ? (
          <span className="absolute start-3 top-3">
            <ConditionBadge condition={condition} />
          </span>
        ) : null}
        {/* The page direction places the code (opposite the condition badge); the inner span keeps it left-to-right. */}
        <span className="absolute end-3 top-3 font-mono text-xs text-ink-muted"><span dir="ltr">{item.code}</span></span>
        {soldOut ? (
          <span className="absolute bottom-3 start-3 rounded bg-paper px-2 py-[3px] text-xs font-bold text-ink-muted">{t("soldOutBadge")}</span>
        ) : null}
      </Link>
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
              <span className="text-[17px] font-bold">{formatEgp(item.prices.alone, locale)}</span>
              {condition ? (
                <AddToCartButton
                  line={itemLine({ ...item, condition }, "Alone", item.prices.alone)}
                  className={`${buttonBase} bg-orange text-on-orange`}
                />
              ) : null}
            </>
          )}
        </div>
      </div>
    </article>
  );
}
