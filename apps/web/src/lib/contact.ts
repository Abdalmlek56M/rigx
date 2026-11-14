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
