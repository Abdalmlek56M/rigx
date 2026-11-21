"use client";

import { useLocale, useTranslations } from "next-intl";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
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

  return <WhatsAppButton href={whatsAppUrl(number, text)} variant="outline" className="w-full" onClick={onClick}>{t("cta")}</WhatsAppButton>;
}
