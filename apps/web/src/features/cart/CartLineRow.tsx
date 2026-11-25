"use client";

import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { IconButton } from "@/components/ui/Button";
import { QuantityStepper } from "@/components/ui/QuantityStepper";
import { ShieldIcon, TrashIcon } from "@/components/ui/icons";
import { mediaUrl } from "@/lib/media";
import { useCart } from "./cart-context";
import type { CartLine } from "./cart-rules";

/**
 * One cart line as a single block: the square photo on the start side; beside it the name (bold, on the reading edge, the model
 * itself read left to right), the condition and warranty as two small pills, and a bottom row with the quantity on one side and
 * the line total on the other. The remove icon sits in the block's top corner, the same place on every line.
 */
export function CartLineRow({ line }: { line: CartLine }) {
  const t = useTranslations("cart");
  const locale = useLocale();
  const { remove, setQuantity } = useCart();
  const countable = line.condition !== "Used" && line.maxQuantity > 1;

  return (
    <li className="relative flex gap-3 border-b border-line py-4 last:border-b-0">
      <span className="flex size-[72px] shrink-0 items-center justify-center overflow-hidden rounded-control border border-line bg-paper">
        {line.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(line.thumbnailStorageKey)} alt="" className="product-photo h-full w-full object-cover" />
        ) : null}
      </span>
      <div className="flex min-w-0 flex-1 flex-col gap-2">
        <p className="m-0 line-clamp-2 pe-9 text-start text-sm font-bold leading-5">
          <bdi dir="ltr">{line.modelName}</bdi>
        </p>
        <div className="flex flex-wrap items-center gap-1.5">
          {line.condition ? <ConditionBadge condition={line.condition} /> : null}
          {line.warrantyLabel ? (
            <span className="inline-flex h-6 items-center gap-1 rounded-full border border-line bg-paper px-2 text-xs font-semibold text-ink-muted">
              <span aria-hidden="true" className="inline-flex text-orange-text"><ShieldIcon size={12} /></span>
              {line.warrantyLabel}
            </span>
          ) : null}
        </div>
        <div className="mt-auto flex min-h-9 items-center justify-between gap-3">
          {countable ? (
            <QuantityStepper
              value={line.quantity}
              max={line.maxQuantity}
              size="sm"
              onChange={(value) => setQuantity(line.code, line.priceOption, value)}
              label={t("quantity")}
              decreaseLabel={t("decrease")}
              increaseLabel={t("increase")}
            />
          ) : (
            <span className="text-xs text-ink-muted">{t("onePiece")}</span>
          )}
          <b className="text-[15px] tabular-nums">{formatEgp(line.unitPrice * line.quantity, locale)}</b>
        </div>
      </div>
      <IconButton
        icon={TrashIcon}
        label={t("removeAria", { model: line.modelName })}
        variant="ghost"
        size="sm"
        className="absolute end-0 top-3 text-ink-muted hover:bg-error/10 hover:text-error-text"
        onClick={() => remove(line.code, line.priceOption)}
      />
    </li>
  );
}
