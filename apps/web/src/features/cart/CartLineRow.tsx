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
 * One cart row in a fixed grid, so every line reads the same: the square photo on one side; the name with the remove icon at its
 * end; the condition and warranty under it with the line total at the end; the quantity stepper last, for a countable line only.
 */
export function CartLineRow({ line }: { line: CartLine }) {
  const t = useTranslations("cart");
  const locale = useLocale();
  const { remove, setQuantity } = useCart();
  const countable = line.condition !== "Used" && line.maxQuantity > 1;

  return (
    <li className="grid grid-cols-[4rem_minmax(0,1fr)_auto] items-center gap-x-3 gap-y-1.5 border-b border-line py-4 last:border-b-0">
      <span className={`flex aspect-square w-16 items-center justify-center self-start overflow-hidden rounded-control border border-line bg-paper ${countable ? "row-span-3" : "row-span-2"}`}>
        {line.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(line.thumbnailStorageKey)} alt="" className="product-photo h-full w-full object-cover" />
        ) : null}
      </span>
      <span dir="auto" className="line-clamp-2 text-start text-sm font-bold leading-5 [unicode-bidi:isolate]">{line.modelName}</span>
      <IconButton
        icon={TrashIcon}
        label={t("removeAria", { model: line.modelName })}
        variant="ghost"
        size="sm"
        className="-me-1.5 self-start justify-self-end text-ink-muted hover:bg-error/10 hover:text-error-text"
        onClick={() => remove(line.code, line.priceOption)}
      />
      <span className="flex min-w-0 flex-wrap items-center gap-x-2.5 gap-y-1">
        {line.condition ? <ConditionBadge condition={line.condition} /> : null}
        {line.warrantyLabel ? (
          <span className="flex items-center gap-1 text-xs font-medium text-ink-muted">
            <span aria-hidden="true" className="inline-flex text-orange-text"><ShieldIcon size={13} /></span>
            {line.warrantyLabel}
          </span>
        ) : null}
      </span>
      <b className="justify-self-end text-[15px] tabular-nums">{formatEgp(line.unitPrice * line.quantity, locale)}</b>
      {countable ? (
        <span className="col-start-2 col-end-4">
          <QuantityStepper
            value={line.quantity}
            max={line.maxQuantity}
            size="sm"
            onChange={(value) => setQuantity(line.code, line.priceOption, value)}
            label={t("quantity")}
            decreaseLabel={t("decrease")}
            increaseLabel={t("increase")}
          />
        </span>
      ) : null}
    </li>
  );
}
