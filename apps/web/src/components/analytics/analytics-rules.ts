/** Admin and POS pages are staff screens: never measured. */
export function isMeasuredPath(pathname: string): boolean {
  return !/^\/(?:[a-z]{2}\/)?admin(?:\/|$)/.test(pathname);
}

/** A link that opens a WhatsApp chat (the main way an order closes), so a click counts as a lead. */
export function isWhatsAppLink(href: string | null | undefined): boolean {
  if (!href) return false;
  try {
    const host = new URL(href).hostname;
    return host === "wa.me" || host === "api.whatsapp.com" || host === "web.whatsapp.com";
  } catch {
    return false;
  }
}

/** Analytics ids are short public tokens; anything else is treated as not configured. */
export function cleanId(value: string | undefined, pattern: RegExp): string | null {
  const id = value?.trim();
  return id && pattern.test(id) ? id : null;
}

export const GA_ID_PATTERN = /^G-[A-Z0-9]{4,20}$/;
export const META_PIXEL_ID_PATTERN = /^\d{6,20}$/;
