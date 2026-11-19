"use client";

import { useLocale, useTranslations } from "next-intl";
import { WhatsAppIcon } from "@/components/ui/icons";
import { whatsAppUrl } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import { useCart } from "./cart-context";
import { cartWhatsAppText } from "./cart-rules";

/** "Order on WhatsApp": opens a chat with the whole cart in the message (orders mostly close on WhatsApp). No store number = nothing. */
export function CartWhatsAppLink({ number, onClick }: { number: string | null; onClick?: () => void }) {
  const t = useTranslations("cart.whatsApp");
  const locale = useLocale();
  const { lines } = useCart();
  if (!number || lines.length === 0) return null;
  const text = cartWhatsAppText(lines, { intro: t("intro"), total: t("total") }, (amount) => formatEgp(amount, locale));

  return (
    <a
      href={whatsAppUrl(number, text)}
      onClick={onClick}
      className="press flex h-[52px] items-center justify-center gap-2 rounded-control border-2 border-ink font-bold"
    >
      <WhatsAppIcon size={20} />
      {t("cta")}
    </a>
  );
}
