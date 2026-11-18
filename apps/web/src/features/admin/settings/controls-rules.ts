/** Store controls: the page switches, the announcement and the homepage section order. */
import type { Schemas } from "@rigx/api-client";

export type StoreControlSettings = Schemas["StoreControlSettings"];

/** The keys the server accepts, in the board's order; each must appear exactly once. */
export const HOMEPAGE_SECTION_KEYS = ["hero", "categories", "brands", "offers", "discovery", "guide", "latest", "closing"] as const;

export interface HomepageSectionForm {
  key: string;
  visible: boolean;
}

export interface ControlsForm {
  maintenanceMode: boolean;
  announcementText: string;
  showSoldOut: boolean;
  showOfferCountdown: boolean;
  guestCheckoutEnabled: boolean;
  minimumOrderAmount: string;
  homepageSections: HomepageSectionForm[];
}

export const controlsForm = (value: StoreControlSettings): ControlsForm => ({
  maintenanceMode: value.maintenanceMode ?? false,
  announcementText: value.announcementText ?? "",
  showSoldOut: value.showSoldOut ?? true,
  showOfferCountdown: value.showOfferCountdown ?? true,
  guestCheckoutEnabled: value.guestCheckoutEnabled ?? false,
  minimumOrderAmount: String(value.minimumOrderAmount ?? 0),
  homepageSections: (value.homepageSections ?? []).map((section) => ({ key: section.key ?? "", visible: section.visible ?? true }))
});

export const controlsValue = (form: ControlsForm): StoreControlSettings => ({
  maintenanceMode: form.maintenanceMode,
  announcementText: form.announcementText.trim() || null,
  showSoldOut: form.showSoldOut,
  showOfferCountdown: form.showOfferCountdown,
  guestCheckoutEnabled: form.guestCheckoutEnabled,
  minimumOrderAmount: form.minimumOrderAmount.trim() === "" ? 0 : Number(form.minimumOrderAmount),
  homepageSections: form.homepageSections.map((section) => ({ key: section.key, visible: section.visible }))
});
