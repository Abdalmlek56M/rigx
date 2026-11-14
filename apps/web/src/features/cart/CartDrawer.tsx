"use client";

import { useLocale, useTranslations } from "next-intl";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { CloseIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { useCart } from "./cart-context";
import { CartLineRow } from "./CartLineRow";

export function CartDrawer() {
  const t = useTranslations("cart");
  const tc = useTranslations("common");
  const locale = useLocale();
  const { lines, count, subtotal, isOpen, close, removedNotice } = useCart();

  return (
    <ModalSheet open={isOpen} onClose={close} label={t("title")} edge="end">
      <div className="flex max-h-[88dvh] flex-col md:h-full md:max-h-none">
        <div className="flex items-center justify-between border-b border-line px-5 py-4">
          <b className="text-lg">{t("titleWithCount", { count })}</b>
          <button
            type="button"
            aria-label={tc("close")}
            onClick={close}
            className="flex h-11 w-11 items-center justify-center rounded-control border border-input-line bg-surface"
          >
            <CloseIcon size={18} />
          </button>
        </div>

        {removedNotice ? (
          <p role="status" className="bg-paper px-5 py-2 text-sm text-ink-muted">{t("removedNotice")}</p>
        ) : null}

        {lines.length === 0 ? (
          <div className="flex flex-1 flex-col items-center justify-center gap-3 p-8 text-center">
            <p className="text-lg font-bold">{t("empty")}</p>
            <Link href="/products" onClick={close} className="text-orange-text underline hover:text-orange-text-hover">
              {t("browse")}
            </Link>
          </div>
        ) : (
          <>
            <ul className="min-h-0 flex-1 overflow-y-auto px-5 py-2">
              {lines.map((line) => (
                <CartLineRow key={`${line.code}:${line.priceOption}`} line={line} />
              ))}
            </ul>
            <div className="flex flex-col gap-2.5 border-t border-line px-5 py-4">
              <div className="flex justify-between gap-2.5 text-sm">
                <span className="text-ink-muted">{t("shipping")}</span>
                <span>{t("shippingNote")}</span>
              </div>
              <div className="flex items-baseline justify-between border-t border-line pt-2.5">
                <span className="font-bold">{t("total")}</span>
                <span className="text-[22px] font-bold">{formatEgp(subtotal, locale)}</span>
              </div>
              <Link
                href="/checkout"
                onClick={close}
                className="flex h-[52px] items-center justify-center rounded-control bg-orange font-bold text-on-orange"
              >
                {t("checkout")}
              </Link>
              <Link
                href="/cart"
                onClick={close}
                className="flex h-11 items-center justify-center rounded-control border border-input-line bg-surface text-sm font-bold"
              >
                {t("viewCart")}
              </Link>
            </div>
          </>
        )}
      </div>
    </ModalSheet>
  );
}
