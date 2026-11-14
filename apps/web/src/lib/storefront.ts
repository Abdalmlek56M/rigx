import "server-only";
import { unwrap, type Schemas } from "@rigx/api-client";
import { createServerApi } from "./api/server";
import type { CategoryNode } from "./categories";

export interface ContactLink {
  platform: string;
  value: string;
  showOnSite: boolean;
  showInFloatingButton: boolean;
}
export type MenuCategory = { name: string; slug: string; children: MenuCategory[] };

export interface PaymentMethods {
  cash: boolean;
  instaPay: boolean;
  wallet: boolean;
}

/** WhatsApp message templates from settings; empty when the owner cleared one (pages then use their own wording). */
export interface WhatsAppTemplates {
  general: string;
  product: string;
  build: string;
}

export interface StorefrontData {
  storeName: string;
  announcementText: string | null;
  englishEnabled: boolean;
  contactLinks: ContactLink[];
  email: string | null;
  domain: string | null;
  whatsAppHours: string | null;
  pickupAddress: string | null;
  pickupEnabled: boolean;
  payment: PaymentMethods;
  whatsAppTemplates: WhatsAppTemplates;
  showSoldOut: boolean;
  categories: MenuCategory[];
  /** Every effectively active category with ids and specs (catalog filters, breadcrumbs, spec names). */
  categoryTree: CategoryNode[];
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
  pickupEnabled: false,
  payment: { cash: false, instaPay: false, wallet: false },
  whatsAppTemplates: { general: "", product: "", build: "" },
  showSoldOut: true,
  categories: [],
  categoryTree: []
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

function toTree(categories: Schemas["CategoryView"][]): CategoryNode[] {
  return categories
    .filter((c) => c.isEffectivelyActive)
    .map((c) => ({
      id: c.id,
      parentId: c.parentId,
      name: c.name,
      slug: c.slug,
      specs: c.specs.map((s) => ({ key: s.key, name: s.name, options: s.options ?? [], unit: s.unit ?? null, isFilter: s.isFilter })),
      children: toTree(c.children)
    }));
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
      pickupEnabled: settings.shipping.pickupEnabled ?? false,
      payment: {
        cash: settings.payment.cashOnDeliveryEnabled ?? false,
        instaPay: settings.payment.instaPayEnabled ?? false,
        wallet: settings.payment.walletEnabled ?? false
      },
      whatsAppTemplates: {
        general: settings.whatsAppTemplates.general?.trim() ?? "",
        product: settings.whatsAppTemplates.product?.trim() ?? "",
        build: settings.whatsAppTemplates.build?.trim() ?? ""
      },
      showSoldOut: settings.controls.showSoldOut ?? true,
      categories: toMenu(categories),
      categoryTree: toTree(categories)
    };
  } catch (error) {
    logOnce(error);
    return FALLBACK;
  }
}
