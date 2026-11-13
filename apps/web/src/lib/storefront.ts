import type { Locale } from "@/lib/i18n";

export function currencyLabel(locale: Locale): string {
  return locale === "ar" ? "ج.م" : "EGP";
}

export function formatCatalogPrice(value: number): string {
  return new Intl.NumberFormat("en-EG", {
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  }).format(value);
}

export function configuredWhatsAppNumber(): string | null {
  const digits = (process.env.NEXT_PUBLIC_STORE_WHATSAPP_NUMBER ?? "").replace(/\D/g, "");
  return digits.length >= 8 ? digits : null;
}

export function whatsappChatUrl(phone: string, message: string): string {
  const digits = phone.replace(/\D/g, "");
  return `https://wa.me/${digits}?text=${encodeURIComponent(message)}`;
}

export function productCanonicalUrl(slug: string, origin?: string): string {
  const base = (origin ?? process.env.NEXT_PUBLIC_SITE_URL ?? "").replace(/\/+$/, "");
  return `${base}/products/${encodeURIComponent(slug)}`;
}

export function whatsappProductInquiryUrl(input: {
  phone: string;
  productName: string;
  sku: string;
  canonicalUrl: string;
  locale: Locale;
}): string {
  const message = input.locale === "ar"
    ? `مرحبًا RIGX، أستفسر عن:\n${input.productName}\nSKU: ${input.sku}\n${input.canonicalUrl}`
    : `Hello RIGX, I would like to ask about:\n${input.productName}\nSKU: ${input.sku}\n${input.canonicalUrl}`;
  return `https://wa.me/${input.phone}?text=${encodeURIComponent(message)}`;
}
