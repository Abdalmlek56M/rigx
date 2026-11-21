"use client";

import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { IconButton } from "@/components/ui/Button";
import { QuantityStepper } from "@/components/ui/QuantityStepper";
import { TrashIcon } from "@/components/ui/icons";
import { mediaUrl } from "@/lib/media";
import { useCart } from "./cart-context";
import type { CartLine } from "./cart-rules";

/** One cart row: a square picture, the name and condition, a stepper for a countable line, the total and a remove icon. */
export function CartLineRow({ line }: { line: CartLine }) {
  const t = useTranslations("cart");
  const locale = useLocale();
  const { remove, setQuantity } = useCart();

  return (
    <li className="flex items-center gap-3 border-b border-line py-3.5 last:border-b-0">
      <span className="flex aspect-square h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-control bg-paper">
        {line.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(line.thumbnailStorageKey)} alt="" className="h-full w-full object-contain p-1.5" />
        ) : null}
      </span>
      <span className="flex min-w-0 flex-1 flex-col gap-1">
        <span dir="auto" className="truncate text-start text-sm font-bold [unicode-bidi:isolate]">{line.modelName}</span>
        <span className="flex flex-wrap items-center gap-1.5">
          {line.condition ? <ConditionBadge condition={line.condition} /> : null}
          {line.warrantyLabel ? <span className="text-xs text-ink-muted">{line.warrantyLabel}</span> : null}
        </span>
        {line.condition !== "Used" && line.maxQuantity > 1 ? (
          <QuantityStepper
            value={line.quantity}
            max={line.maxQuantity}
            size="sm"
            onChange={(value) => setQuantity(line.code, line.priceOption, value)}
            label={t("quantity")}
            decreaseLabel={t("decrease")}
            increaseLabel={t("increase")}
          />
        ) : null}
      </span>
      <span className="flex shrink-0 flex-col items-end gap-1">
        <b className="text-sm tabular-nums">{formatEgp(line.unitPrice * line.quantity, locale)}</b>
        <IconButton
          icon={TrashIcon}
          label={t("removeAria", { model: line.modelName })}
          variant="ghost"
          size="sm"
          className="text-ink-muted hover:bg-error/10 hover:text-error"
          onClick={() => remove(line.code, line.priceOption)}
        />
      </span>
    </li>
  );
}
