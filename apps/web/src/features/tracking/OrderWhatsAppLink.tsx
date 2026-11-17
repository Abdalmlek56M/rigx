import { useTranslations } from "next-intl";
import { WhatsAppIcon } from "@/components/ui/icons";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";

export interface OrderWhatsApp {
  /** Store WhatsApp number (digits); none = no link. */
  number: string | null;
  /** The owner's order template from settings (empty = the plain message). */
  template: string;
}

/** "Ask about this order" on WhatsApp, with the order reference in the message. */
export function OrderWhatsAppLink({ whatsApp, reference, label, className }: { whatsApp: OrderWhatsApp; reference: string; label: string; className: string }) {
  const t = useTranslations("tracking");
  if (!whatsApp.number) return null;
  const text = fillTemplate(whatsApp.template, { order: reference }) || t("whatsAppMessage", { reference });

  return (
    <a href={whatsAppUrl(whatsApp.number, text)} target="_blank" rel="noopener noreferrer" className={className}>
      <WhatsAppIcon size={18} />
      {label}
    </a>
  );
}
