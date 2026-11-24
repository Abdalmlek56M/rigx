"use client";

import { useLocale, useTranslations } from "next-intl";
import { TruckIcon } from "@/components/ui/icons";
import { formatEgp } from "@/lib/format";
import { freeShippingHint, freeShippingReached, freeShippingRemaining } from "./free-shipping";

/** The shipping line of a cart summary: "free" once the total reaches the store's minimum, otherwise the usual note. */
export function CartShippingRow({ label, note, minimum, total }: { label: string; note: string; minimum: number | null; total: number }) {
  const t = useTranslations("freeShipping");
  const free = freeShippingReached(minimum, total);
  return (
    <div className="flex justify-between gap-2.5">
      <dt className="text-ink-muted">{label}</dt>
      <dd className={`m-0 ${free ? "font-bold text-success" : ""}`}>{free ? t("free") : note}</dd>
    </div>
  );
}

/** "Add X for free shipping", shown only when the order is close to the minimum. `remaining` is the server's when it has one. */
export function FreeShippingHint({ minimum, total, remaining }: { minimum: number | null; total: number; remaining?: number | null }) {
  const t = useTranslations("freeShipping");
  const locale = useLocale();
  const amount = freeShippingHint(minimum, remaining === undefined ? freeShippingRemaining(minimum, total) : remaining);
  if (amount === null) return null;
  return (
    <p className="m-0 flex items-start gap-2 rounded-control bg-paper px-3 py-2 text-[13px] leading-6 text-ink-muted">
      <span aria-hidden="true" className="mt-1 shrink-0 text-orange-text"><TruckIcon size={16} /></span>
      <span>{t("hint", { amount: formatEgp(amount, locale) })}</span>
    </p>
  );
}
