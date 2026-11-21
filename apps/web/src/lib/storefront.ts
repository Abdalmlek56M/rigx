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
/** A menu category; `image` is the uploaded category picture (often a transparent PNG), null when there is none. */
export type MenuCategory = { id: string; name: string; nameEn?: string | null; slug: string; image: string | null; children: MenuCategory[] };
/** An active brand (catalog filter, product page). */
export type Brand = { id: string; name: string; slug: string };

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
  order: string;
  warranty: string;
}

/** Checkout rules from settings: who may order, the minimum, lead-source chips and what is paid upfront. */
export interface CheckoutSettings {
  guestCheckoutEnabled: boolean;
  minimumOrderAmount: number;
  leadSources: string[];
  /** Orders above this subtotal also pay a deposit of `depositPercent` percent upfront. */
  depositThreshold: number;
  depositPercent: number;
  shippingPaidUpfront: boolean;
  instaPayAddress: string | null;
  walletNumber: string | null;
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
  checkout: CheckoutSettings;
  showSoldOut: boolean;
  categories: MenuCategory[];
  /** Every effectively active category with ids and specs (catalog filters, breadcrumbs, spec names). */
  categoryTree: CategoryNode[];
  /** The active brands, by name. */
  brands: Brand[];
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
  whatsAppTemplates: { general: "", product: "", build: "", order: "", warranty: "" },
  checkout: {
    guestCheckoutEnabled: false, minimumOrderAmount: 0, leadSources: [], depositThreshold: 0, depositPercent: 0,
    shippingPaidUpfront: false, instaPayAddress: null, walletNumber: null
  },
  showSoldOut: true,
  categories: [],
  categoryTree: [],
  brands: []
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
    .map((c) => ({ id: c.id, name: c.name, nameEn: c.nameEn ?? null, slug: c.slug, image: c.iconStorageKey ?? null, children: toMenu(c.children) }));
}

function toTree(categories: Schemas["CategoryView"][]): CategoryNode[] {
  return categories
    .filter((c) => c.isEffectivelyActive)
    .map((c) => ({
      id: c.id,
      parentId: c.parentId,
      name: c.name,
      nameEn: c.nameEn ?? null,
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
    const [settings, categories, brands] = await Promise.all([
      unwrap(api.GET("/api/v1/storefront/settings")),
      unwrap(api.GET("/api/v1/catalog/categories")),
      unwrap(api.GET("/api/v1/catalog/brands"))
    ]);
    if (!settings || !categories || !brands) throw new Error("Empty storefront response.");
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
        build: settings.whatsAppTemplates.build?.trim() ?? "",
        order: settings.whatsAppTemplates.order?.trim() ?? "",
        warranty: settings.whatsAppTemplates.warranty?.trim() ?? ""
      },
      checkout: {
        guestCheckoutEnabled: settings.controls.guestCheckoutEnabled ?? false,
        minimumOrderAmount: settings.controls.minimumOrderAmount ?? 0,
        leadSources: settings.leadSources,
        depositThreshold: settings.payment.depositThreshold ?? 0,
        depositPercent: settings.payment.depositPercent ?? 0,
        shippingPaidUpfront: settings.payment.shippingPaidUpfront ?? false,
        instaPayAddress: settings.payment.instaPayAddress?.trim() || null,
        walletNumber: settings.payment.walletNumber?.trim() || null
      },
      showSoldOut: settings.controls.showSoldOut ?? true,
      categories: toMenu(categories),
      categoryTree: toTree(categories),
      brands: brands.map((brand) => ({ id: brand.id, name: brand.name, slug: brand.slug }))
    };
  } catch (error) {
    logOnce(error);
    return FALLBACK;
  }
}
