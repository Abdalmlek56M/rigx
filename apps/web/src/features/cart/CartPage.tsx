"use client";

import { useEffect } from "react";
import { useLocale, useTranslations } from "next-intl";
import { EmptyIcon } from "@/components/ui/EmptyIcon";
import { CartIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { useMounted } from "@/lib/hooks/use-mounted";
import { useCart } from "./cart-context";
import { CartLineRow } from "./CartLineRow";
import { CartWhatsAppLink } from "./CartWhatsAppLink";

/** The full cart: the same lines as the drawer, with the subtotal, checkout and "order on WhatsApp". */
export function CartPage({ whatsAppNumber }: { whatsAppNumber: string | null }) {
  const t = useTranslations("cartPage");
  const locale = useLocale();
  const mounted = useMounted();
  const { lines, subtotal, refresh, removedNotice } = useCart();

  useEffect(() => {
    void refresh();
  }, [refresh]);

  if (!mounted) return null;
  if (lines.length === 0) {
    return (
      <div role="status" className="flex flex-col items-center gap-3 rounded-card border border-line bg-surface p-10 text-center">
        <EmptyIcon icon={CartIcon} />
        <p className="m-0 text-lg font-bold">{t("empty")}</p>
        <Link href="/products" className="text-orange-text underline hover:text-orange-text-hover">{t("browse")}</Link>
      </div>
    );
  }

  return (
    <div className="grid items-start gap-5 md:grid-cols-[1fr_340px]">
      <div className="rounded-card border border-line bg-surface px-5 py-2 md:px-6">
        {removedNotice ? <p role="status" className="m-0 border-b border-line py-3 text-sm text-ink-muted">{t("removedNotice")}</p> : null}
        <ul className="m-0 list-none p-0">
          {lines.map((line) => (
            <CartLineRow key={`${line.code}:${line.priceOption}`} line={line} />
          ))}
        </ul>
      </div>
      <aside className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5 md:sticky md:top-24 md:p-6">
        <div className="flex justify-between gap-3 text-sm">
          <span className="text-ink-muted">{t("shipping")}</span>
          <span>{t("shippingNote")}</span>
        </div>
        <div className="flex items-baseline justify-between border-t border-line pt-3">
          <b>{t("subtotal")}</b>
          <b className="text-[22px]">{formatEgp(subtotal, locale)}</b>
        </div>
        <Link href="/checkout" className="flex h-[52px] items-center justify-center rounded-control bg-orange font-bold text-on-orange">
          {t("checkout")}
        </Link>
        <CartWhatsAppLink number={whatsAppNumber} />
        <Link href="/products" className="flex h-11 items-center justify-center rounded-control border border-input-line bg-surface text-sm font-bold">
          {t("continue")}
        </Link>
      </aside>
    </div>
  );
}
