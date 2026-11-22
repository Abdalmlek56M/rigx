"use client";

import { useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ButtonLink, type ButtonSize } from "@/components/ui/Button";
import { CpuIcon } from "@/components/ui/icons";
import { parseCondition } from "@/lib/condition";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { itemLine } from "@/features/cart/cart-rules";
import { defaultOption, isBuildOnly, offeredOptions, priceOf } from "@/features/product/price-options";
import { NotifyMeButton } from "@/features/requests/NotifyMeButton";

/** The one action an item offers on a card or in the quick view: add to cart, "notify me" when sold out, or the builder for a build-only part. */
export function ItemAction({ item, size = "sm", className = "w-full" }: { item: Schemas["StoreItemCard"]; size?: ButtonSize; className?: string }) {
  const t = useTranslations("item");
  const condition = parseCondition(item.condition);
  const offered = offeredOptions(item.priceOptions);
  const option = defaultOption(offered);

  if (item.available <= 0) return <NotifyMeButton target={{ itemCode: item.code, modelName: item.modelName }} size={size} className={className} />;
  if (isBuildOnly(offered)) {
    return <ButtonLink href={`/builder?parts=${encodeURIComponent(item.code)}`} variant="secondary" size={size} icon={CpuIcon} className={className}>{t("buildIt")}</ButtonLink>;
  }
  if (!condition) return null;
  return <AddToCartButton line={itemLine({ ...item, condition }, option, priceOf(item.prices, option))} size={size} className={className} />;
}
