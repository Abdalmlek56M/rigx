/** Contact links: platform, value, where each shows, and the display order. */
import type { Schemas } from "@rigx/api-client";

export type ContactLinkSettings = Schemas["ContactLinkSettings"];

export const CONTACT_PLATFORMS = ["WhatsApp", "Phone", "Telegram", "Facebook", "Instagram", "TikTok", "YouTube"] as const;

export interface ContactLinkForm {
  platform: string;
  value: string;
  showOnSite: boolean;
  showOnInvoice: boolean;
  showInFloatingButton: boolean;
}

export interface ContactLinksForm {
  links: ContactLinkForm[];
}

/** WhatsApp and phone links hold a number; the rest hold a URL. */
export const contactLinkIsNumber = (platform: string) => platform === "WhatsApp" || platform === "Phone";

export const emptyContactLink = (): ContactLinkForm => ({
  platform: "WhatsApp",
  value: "",
  showOnSite: true,
  showOnInvoice: false,
  showInFloatingButton: false
});

export const contactLinksForm = (value: ContactLinkSettings): ContactLinksForm => ({
  links: (value.links ?? []).map((link) => ({
    platform: link.platform ?? "",
    value: link.value ?? "",
    showOnSite: link.showOnSite ?? true,
    showOnInvoice: link.showOnInvoice ?? false,
    showInFloatingButton: link.showInFloatingButton ?? false
  }))
});

export const contactLinksValue = (form: ContactLinksForm): ContactLinkSettings => ({
  links: form.links.map((link) => ({ ...link, value: link.value.trim() }))
});
