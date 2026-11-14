"use client";

import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { mediaUrl } from "@/lib/media";
import { useCart } from "./cart-context";
import type { CartLine } from "./cart-rules";

export function CartLineRow({ line }: { line: CartLine }) {
  const t = useTranslations("cart");
  const locale = useLocale();
  const { remove, setQuantity } = useCart();
  const stepper = "h-9 w-9 rounded-control border border-input-line bg-surface text-lg leading-none disabled:opacity-40";

  return (
    <li className="flex items-center gap-3 border-b border-line py-3.5">
      <span className="h-[50px] w-16 shrink-0 overflow-hidden rounded bg-sand">
        {line.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(line.thumbnailStorageKey)} alt="" className="h-full w-full object-contain" />
        ) : null}
      </span>
      <span className="flex min-w-0 flex-1 flex-col gap-1">
        <span dir="auto" className="truncate text-start font-bold [unicode-bidi:isolate]">{line.modelName}</span>
        <span className="flex items-center gap-1.5">
          {line.condition ? <ConditionBadge condition={line.condition} /> : null}
          {line.warrantyLabel ? <span className="text-xs text-ink-muted">{line.warrantyLabel}</span> : null}
        </span>
        {line.condition !== "Used" && line.maxQuantity > 1 ? (
          <span role="group" aria-label={t("quantity")} className="mt-1 flex items-center gap-2">
            <button
              type="button"
              aria-label={t("decrease")}
              className={stepper}
              disabled={line.quantity <= 1}
              onClick={() => setQuantity(line.code, line.priceOption, line.quantity - 1)}
            >
              −
            </button>
            <span className="min-w-6 text-center font-mono font-semibold">{line.quantity}</span>
            <button
              type="button"
              aria-label={t("increase")}
              className={stepper}
              disabled={line.quantity >= line.maxQuantity}
              onClick={() => setQuantity(line.code, line.priceOption, line.quantity + 1)}
            >
              +
            </button>
          </span>
        ) : null}
      </span>
      <span className="flex flex-col items-end gap-1.5">
        <b className="text-sm">{formatEgp(line.unitPrice * line.quantity, locale)}</b>
        <button
          type="button"
          aria-label={t("removeAria", { model: line.modelName })}
          className="min-h-9 text-xs text-orange-text underline hover:text-orange-text-hover"
          onClick={() => remove(line.code, line.priceOption)}
        >
          {t("remove")}
        </button>
      </span>
    </li>
  );
}
