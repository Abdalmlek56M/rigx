import type { ContactLink } from "./storefront";

export const Platform = {
  WhatsApp: "WhatsApp",
  Phone: "Phone",
  Telegram: "Telegram",
  Facebook: "Facebook",
  Instagram: "Instagram",
  TikTok: "TikTok",
  YouTube: "YouTube"
} as const;

export const SOCIAL_PLATFORMS = [
  Platform.WhatsApp,
  Platform.Telegram,
  Platform.Facebook,
  Platform.Instagram,
  Platform.TikTok,
  Platform.YouTube
] as const;

const EGYPT_CODE = "20";

/** Numbers are stored as international digits (e.g. 201289186840); links hold a URL. */
export function contactHref(link: ContactLink): string {
  if (link.platform === Platform.WhatsApp) return `https://wa.me/${link.value}`;
  if (link.platform === Platform.Phone) return `tel:+${link.value}`;
  return link.value;
}

/** Local reading of an Egyptian number: 201289186840 becomes "0128 918 6840". */
export function displayPhone(digits: string): string {
  const local = digits.startsWith(EGYPT_CODE) ? `0${digits.slice(EGYPT_CODE.length)}` : digits;
  return local.length === 11 ? `${local.slice(0, 4)} ${local.slice(4, 7)} ${local.slice(7)}` : local;
}

export function findLink(links: ContactLink[], platform: string): ContactLink | undefined {
  return links.find((link) => link.platform === platform);
}

/** The placeholders of a store WhatsApp template (the owner edits the templates in settings). */
export interface TemplateValues {
  item?: string;
  code?: string;
  price?: string;
  link?: string;
  order?: string;
}

const PLACEHOLDERS: Record<keyof TemplateValues, string> = {
  item: "{القطعة}",
  code: "{الكود}",
  price: "{السعر}",
  link: "{اللينك}",
  order: "{الطلب}"
};

/** Fills a template's placeholders; missing values become empty (with the parentheses around them) and the text is trimmed. Empty template → empty text. */
export function fillTemplate(template: string, values: TemplateValues): string {
  let text = template;
  for (const key of Object.keys(PLACEHOLDERS) as (keyof TemplateValues)[]) text = text.replaceAll(PLACEHOLDERS[key], values[key] ?? "");
  return text.replace(/\s*\(\s*\)/g, "").trim();
}

/** Digits of the store's WhatsApp number, or `null` when none is configured. */
export function whatsAppNumber(links: ContactLink[]): string | null {
  return findLink(links, Platform.WhatsApp)?.value.replace(/\D/g, "") || null;
}

/** `https://wa.me/<number>?text=<text>` (the text is optional). */
export function whatsAppUrl(number: string, text?: string): string {
  return text ? `https://wa.me/${number}?text=${encodeURIComponent(text)}` : `https://wa.me/${number}`;
}

/** Absolute public URL of a storefront path (`/p/rtx-3060-12gb-pny`), or "" when the store's domain is not set. */
export function siteUrl(domain: string | null, path: string): string {
  return domain ? `https://${domain.replace(/^https?:\/\//, "").replace(/\/+$/, "")}${path}` : "";
}
