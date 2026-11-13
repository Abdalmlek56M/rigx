"use client";

import { useLocale } from "@/lib/i18n";
import { configuredWhatsAppNumber, whatsappChatUrl } from "@/lib/storefront";
import { useStoreSettings } from "@/components/store-settings-provider";
import { Icon } from "@/components/icons";

export function WhatsAppFloatingButton() {
  const { locale, messages } = useLocale();
  const { settings } = useStoreSettings();
  const phone = settings?.whatsAppNumber ?? configuredWhatsAppNumber();
  if (!phone) return null;
  const message = settings?.whatsAppMessage?.trim()
    || (locale === "ar"
      ? "مرحبًا RIGX، أريد الاستفسار عن منتجاتكم."
      : "Hello RIGX, I would like to ask about your products.");
  return (
    <a
      href={whatsappChatUrl(phone, message)}
      target="_blank"
      rel="noreferrer"
      className="rigx-whatsapp-fab"
      aria-label={messages.whatsappContact}
      title={messages.whatsappContact}
    >
      <Icon name="whatsapp" className="h-6 w-6" />
    </a>
  );
}
