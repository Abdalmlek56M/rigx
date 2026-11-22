"use client";

import { useEffect } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState } from "@/components/ui/EmptyState";
import { CartIcon, WalletIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { useMounted } from "@/lib/hooks/use-mounted";
import { useCart } from "./cart-context";
import { CartLineRow } from "./CartLineRow";
import { CartWhatsAppLink } from "./CartWhatsAppLink";

/** The full cart: the same lines as the drawer, with the summary, checkout and "order on WhatsApp". */
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
      <EmptyState
        icon={CartIcon}
        title={t("empty")}
        action={<ButtonLink href="/products" variant="secondary">{t("browse")}</ButtonLink>}
      />
    );
  }

  return (
    <div className="grid items-start gap-5 lg:grid-cols-[1fr_340px]">
      <Card padding="none" className="px-5 py-2 md:px-6">
        {removedNotice ? <p role="status" className="m-0 border-b border-line py-3 text-sm text-ink-muted">{t("removedNotice")}</p> : null}
        <ul className="m-0 list-none p-0">
          {lines.map((line) => (
            <CartLineRow key={`${line.code}:${line.priceOption}`} line={line} />
          ))}
        </ul>
      </Card>
      <Card title={t("summary")} icon={WalletIcon} padding="md" className="gap-3 lg:sticky lg:top-24">
        <dl className="m-0 flex flex-col gap-2.5 text-sm">
          <div className="flex justify-between gap-3">
            <dt className="text-ink-muted">{t("shipping")}</dt>
            <dd className="m-0">{t("shippingNote")}</dd>
          </div>
          <div className="flex items-baseline justify-between gap-3 border-t border-line pt-3">
            <dt className="font-bold">{t("subtotal")}</dt>
            <dd className="m-0 text-[22px] font-bold tabular-nums">{formatEgp(subtotal, locale)}</dd>
          </div>
        </dl>
        <ButtonLink href="/checkout" variant="accent" className="w-full">{t("checkout")}</ButtonLink>
        <CartWhatsAppLink number={whatsAppNumber} />
        <Link href="/products" className="self-center text-sm font-semibold text-orange-text underline hover:text-orange-text-hover">{t("continue")}</Link>
      </Card>
    </div>
  );
}
