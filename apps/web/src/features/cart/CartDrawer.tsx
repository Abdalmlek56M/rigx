"use client";

import { useLocale, useTranslations } from "next-intl";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { ButtonLink, IconButton } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { CartIcon, CloseIcon, WalletIcon } from "@/components/ui/icons";
import { formatEgp } from "@/lib/format";
import { useCart } from "./cart-context";
import { CartShippingRow, FreeShippingHint } from "./FreeShipping";
import { CartLineRow } from "./CartLineRow";
import { CartWhatsAppLink } from "./CartWhatsAppLink";

/** The cart drawer; `whatsAppNumber` is the store number for "order on WhatsApp" (none = no button). */
export function CartDrawer({ whatsAppNumber, freeShippingMinimum }: { whatsAppNumber: string | null; freeShippingMinimum: number | null }) {
  const t = useTranslations("cart");
  const tc = useTranslations("common");
  const locale = useLocale();
  const { lines, count, subtotal, isOpen, close, removedNotice } = useCart();

  return (
    <ModalSheet open={isOpen} onClose={close} label={t("title")} edge="end">
      <div className="flex max-h-[88dvh] flex-col md:h-full md:max-h-none">
        <div className="flex items-center justify-between border-b border-line px-5 py-4">
          <b className="type-section-title">{t("titleWithCount", { count })}</b>
          <IconButton icon={CloseIcon} label={tc("close")} variant="secondary" onClick={close} />
        </div>

        {removedNotice ? (
          <p role="status" className="m-0 bg-paper px-5 py-2 text-sm text-ink-muted">{t("removedNotice")}</p>
        ) : null}

        {lines.length === 0 ? (
          <div className="flex flex-1 flex-col items-center justify-center gap-3 p-8 text-center">
            <span aria-hidden="true" className="flex h-14 w-14 items-center justify-center rounded-full bg-paper text-ink-muted"><CartIcon size={26} /></span>
            <p className="m-0 type-section-title">{t("empty")}</p>
            <ButtonLink href="/products" variant="secondary" onClick={close}>{t("browse")}</ButtonLink>
          </div>
        ) : (
          <>
            <ul className="min-h-0 flex-1 overflow-y-auto overscroll-contain px-5 py-2">
              {lines.map((line) => (
                <CartLineRow key={`${line.code}:${line.priceOption}`} line={line} />
              ))}
            </ul>
            <div className="border-t border-line p-5">
              <Card title={t("summary")} icon={WalletIcon} padding="sm" className="gap-3 border-0 p-0">
                <dl className="m-0 flex flex-col gap-2.5 text-sm">
                  <CartShippingRow label={t("shipping")} note={t("shippingNote")} minimum={freeShippingMinimum} total={subtotal} />
                  <div className="flex items-baseline justify-between gap-2.5 border-t border-line pt-2.5">
                    <dt className="font-bold">{t("total")}</dt>
                    <dd className="m-0 text-[22px] font-bold tabular-nums">{formatEgp(subtotal, locale)}</dd>
                  </div>
                </dl>
                <FreeShippingHint minimum={freeShippingMinimum} total={subtotal} />
                <ButtonLink href="/checkout" variant="accent" onClick={close} className="w-full">{t("checkout")}</ButtonLink>
                <CartWhatsAppLink number={whatsAppNumber} onClick={close} />
                <ButtonLink href="/cart" variant="secondary" onClick={close} className="w-full">{t("viewCart")}</ButtonLink>
              </Card>
            </div>
          </>
        )}
      </div>
    </ModalSheet>
  );
}
