import { useTranslations } from "next-intl";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import type { ButtonSize } from "@/components/ui/Button";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";

export interface OrderWhatsApp {
  /** Store WhatsApp number (digits); none = no link. */
  number: string | null;
  /** The owner's order template from settings (empty = the plain message). */
  template: string;
}

interface OrderWhatsAppLinkProps {
  whatsApp: OrderWhatsApp;
  reference: string;
  label: string;
  variant?: "solid" | "outline";
  size?: ButtonSize;
  className?: string;
}

/** "Ask about this order" on WhatsApp, with the order reference in the message. */
export function OrderWhatsAppLink({ whatsApp, reference, label, variant = "solid", size = "md", className }: OrderWhatsAppLinkProps) {
  const t = useTranslations("tracking");
  if (!whatsApp.number) return null;
  const text = fillTemplate(whatsApp.template, { order: reference }) || t("whatsAppMessage", { reference });

  return <WhatsAppButton href={whatsAppUrl(whatsApp.number, text)} variant={variant} size={size} className={className}>{label}</WhatsAppButton>;
}
