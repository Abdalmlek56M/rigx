import "server-only";
import { unwrap, type Schemas } from "@rigx/api-client";
import { createServerApi } from "./api/server";

export interface ContactLink {
  platform: string;
  value: string;
  showOnSite: boolean;
  showInFloatingButton: boolean;
}
export type MenuCategory = { name: string; slug: string; children: MenuCategory[] };

export interface StorefrontData {
  storeName: string;
  announcementText: string | null;
  englishEnabled: boolean;
  contactLinks: ContactLink[];
  email: string | null;
  domain: string | null;
  whatsAppHours: string | null;
  pickupAddress: string | null;
  categories: MenuCategory[];
}

const FALLBACK: StorefrontData = {
  storeName: "RIGX",
  announcementText: null,
  englishEnabled: false,
  contactLinks: [],
  email: null,
  domain: null,
  whatsAppHours: null,
  pickupAddress: null,
  categories: []
};

let failureLogged = false;

function logOnce(error: unknown) {
  if (failureLogged) return;
  failureLogged = true;
  console.warn("Storefront data unavailable, using defaults:", error instanceof Error ? error.message : error);
}

function toMenu(categories: Schemas["CategoryView"][]): MenuCategory[] {
  return categories
    .filter((c) => c.showInMenu && c.isEffectivelyActive)
    .map((c) => ({ name: c.name, slug: c.slug, children: toMenu(c.children) }));
}

function toContactLinks(links: Schemas["ContactLink"][]): ContactLink[] {
  return links.flatMap((l) =>
    l.platform && l.value
      ? [{ platform: l.platform, value: l.value, showOnSite: l.showOnSite ?? true, showInFloatingButton: l.showInFloatingButton ?? false }]
      : []
  );
}

/** Settings and menu for the layout. Never throws: when the API is down the safe defaults are returned. */
export async function loadStorefront(): Promise<StorefrontData> {
  try {
    const api = createServerApi();
    const [settings, categories] = await Promise.all([
      unwrap(api.GET("/api/v1/storefront/settings")),
      unwrap(api.GET("/api/v1/catalog/categories"))
    ]);
    if (!settings || !categories) throw new Error("Empty storefront response.");
    const info = settings.storeInfo;
    return {
      storeName: info.name || FALLBACK.storeName,
      announcementText: settings.controls.announcementText?.trim() || null,
      englishEnabled: settings.language.englishEnabled ?? false,
      contactLinks: toContactLinks(settings.contactLinks),
      email: info.email?.trim() || null,
      domain: info.domain?.trim() || null,
      whatsAppHours: info.whatsAppHours?.trim() || null,
      pickupAddress: info.pickupAddress?.trim() || null,
      categories: toMenu(categories)
    };
  } catch (error) {
    logOnce(error);
    return FALLBACK;
  }
}
