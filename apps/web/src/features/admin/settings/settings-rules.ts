/** Store settings: the section list, the form props and most section mappers (contact, payment, shipping and controls have their own). Numbers are strings in the form. */
import type { Schemas } from "@rigx/api-client";

export type SettingsView = Schemas["StoreSettingsAdminView"];
export type StoreInfo = Schemas["StoreInfoSettings"];
export type WhatsAppSettings = Schemas["WhatsAppTemplateSettings"];
export type ReviewSettings = Schemas["ReviewSettings"];
export type LanguageSettings = Schemas["LanguageSettings"];
export type InvoiceSettings = Schemas["InvoiceSettings"];
export type LeadSourceSettings = Schemas["LeadSourceSettings"];
export type WarrantyOptionSettings = Schemas["WarrantyOptionSettings"];
export type SalesSettings = Schemas["SalesSettings"];
export type WatermarkSettings = Schemas["WatermarkSettings"];
export type BackupSettings = Schemas["BackupSettings"];

export const SETTINGS_SECTIONS = [
  "store-info",
  "contact-links",
  "shipping",
  "payment",
  "controls",
  "whatsapp-templates",
  "reviews",
  "language",
  "invoice",
  "lead-sources",
  "warranty-options",
  "sales",
  "watermark",
  "backup"
] as const;
export type SettingsSection = (typeof SETTINGS_SECTIONS)[number];

/** The `section` value in the URL; anything unknown opens the store data. */
export function settingsSection(value: string | undefined): SettingsSection {
  return SETTINGS_SECTIONS.find((section) => section === value) ?? "store-info";
}

export const SETTINGS_FILTERS = { section: "store-info" };

/** What every section form gets: the loaded settings, the save result, and "reload after a conflict". */
export interface SectionProps {
  view: SettingsView;
  onSaved: (view: SettingsView) => void;
  onStale: () => void;
}

const text = (value: string | null | undefined) => value ?? "";
const optional = (value: string) => value.trim() || null;
const numberValue = (value: string) => (value.trim() === "" ? 0 : Number(value));

// —— Store data ————————————————————————————————————————————————————————————————
export interface StoreInfoForm {
  name: string;
  domain: string;
  email: string;
  whatsAppHours: string;
  pickupAddress: string;
  pickupHours: string;
  pickupInstructions: string;
  warrantySummary: string;
  serviceSummary: string;
  consultationSummary: string;
}

export const storeInfoForm = (value: StoreInfo): StoreInfoForm => ({
  name: text(value.name),
  domain: text(value.domain),
  email: text(value.email),
  whatsAppHours: text(value.whatsAppHours),
  pickupAddress: text(value.pickupAddress),
  pickupHours: text(value.pickupHours),
  pickupInstructions: text(value.pickupInstructions),
  warrantySummary: text(value.warrantySummary),
  serviceSummary: text(value.serviceSummary),
  consultationSummary: text(value.consultationSummary)
});

export const storeInfoValue = (form: StoreInfoForm): StoreInfo => ({
  name: form.name.trim(),
  domain: optional(form.domain),
  email: optional(form.email),
  whatsAppHours: optional(form.whatsAppHours),
  pickupAddress: optional(form.pickupAddress),
  pickupHours: optional(form.pickupHours),
  pickupInstructions: optional(form.pickupInstructions),
  warrantySummary: optional(form.warrantySummary),
  serviceSummary: optional(form.serviceSummary),
  consultationSummary: optional(form.consultationSummary)
});

// —— WhatsApp templates ———————————————————————————————————————————————————————
export interface WhatsAppForm {
  general: string;
  product: string;
  build: string;
  order: string;
  warranty: string;
  requestReply: string;
  backInStock: string;
}

export const whatsAppForm = (value: WhatsAppSettings): WhatsAppForm => ({
  general: text(value.general),
  product: text(value.product),
  build: text(value.build),
  order: text(value.order),
  warranty: text(value.warranty),
  requestReply: text(value.requestReply),
  backInStock: text(value.backInStock)
});

export const whatsAppValue = (form: WhatsAppForm): WhatsAppSettings => ({
  general: form.general.trim(),
  product: form.product.trim(),
  build: form.build.trim(),
  order: form.order.trim(),
  warranty: form.warranty.trim(),
  requestReply: form.requestReply.trim(),
  backInStock: form.backInStock.trim()
});

// —— Reviews ——————————————————————————————————————————————————————————————————
export interface ReviewsForm {
  enabled: boolean;
  minimumCountToShowStars: string;
}

export const reviewsForm = (value: ReviewSettings): ReviewsForm => ({
  enabled: value.enabled ?? false,
  minimumCountToShowStars: String(value.minimumCountToShowStars ?? 3)
});

export const reviewsValue = (form: ReviewsForm): ReviewSettings => ({
  enabled: form.enabled,
  minimumCountToShowStars: numberValue(form.minimumCountToShowStars)
});

// —— Language —————————————————————————————————————————————————————————————————
export interface LanguageForm {
  defaultLanguage: string;
  englishEnabled: boolean;
}

export const languageForm = (value: LanguageSettings): LanguageForm => ({
  defaultLanguage: value.defaultLanguage ?? "ar",
  englishEnabled: value.englishEnabled ?? true
});

export const languageValue = (form: LanguageForm): LanguageSettings => ({
  defaultLanguage: form.defaultLanguage,
  englishEnabled: form.englishEnabled
});

// —— Invoice ——————————————————————————————————————————————————————————————————
export interface InvoiceForm {
  numberPrefix: string;
  showBackgroundLogo: boolean;
  showWarrantyQr: boolean;
  termsLine: string;
}

export const invoiceForm = (value: InvoiceSettings): InvoiceForm => ({
  numberPrefix: value.numberPrefix ?? "INV",
  showBackgroundLogo: value.showBackgroundLogo ?? true,
  showWarrantyQr: value.showWarrantyQr ?? true,
  termsLine: text(value.termsLine)
});

export const invoiceValue = (form: InvoiceForm): InvoiceSettings => ({
  numberPrefix: form.numberPrefix.trim().toUpperCase(),
  showBackgroundLogo: form.showBackgroundLogo,
  showWarrantyQr: form.showWarrantyQr,
  termsLine: optional(form.termsLine)
});

// —— Lead sources —————————————————————————————————————————————————————————————
export interface LeadSourcesForm {
  sources: string[];
}

export const leadSourcesForm = (value: LeadSourceSettings): LeadSourcesForm => ({ sources: [...(value.sources ?? [])] });

/** Empty rows are not choices; the server rejects an empty list or a repeated label. */
export const leadSourcesValue = (form: LeadSourcesForm): LeadSourceSettings => ({
  sources: form.sources.map((source) => source.trim()).filter(Boolean)
});

// —— Warranty options —————————————————————————————————————————————————————————
export interface WarrantyOptionForm {
  label: string;
  months: string;
}

export interface WarrantyForm {
  options: WarrantyOptionForm[];
}

export const emptyWarrantyOption = (): WarrantyOptionForm => ({ label: "", months: "" });

export const warrantyForm = (value: WarrantyOptionSettings): WarrantyForm => ({
  options: (value.options ?? []).map((option) => ({
    label: text(option.label),
    months: option.months == null ? "" : String(option.months)
  }))
});

/** An empty months box is the manufacturer's warranty; a blank label row is dropped. */
export const warrantyValue = (form: WarrantyForm): WarrantyOptionSettings => ({
  options: form.options
    .filter((option) => option.label.trim())
    .map((option) => ({
      label: option.label.trim(),
      months: option.months.trim() ? Number(option.months) : null
    }))
});

// —— Sales ————————————————————————————————————————————————————————————————————
export interface SalesForm {
  reservationHours: string;
  reservationAutoRelease: boolean;
  posMaxDiscountPercent: string;
  soldFeedEnabled: boolean;
  soldFeedShowGovernorate: boolean;
}

export const salesForm = (value: SalesSettings): SalesForm => ({
  reservationHours: String(value.reservationHours ?? 24),
  reservationAutoRelease: value.reservationAutoRelease ?? false,
  posMaxDiscountPercent: String(value.posMaxDiscountPercent ?? 10),
  soldFeedEnabled: value.soldFeedEnabled ?? true,
  soldFeedShowGovernorate: value.soldFeedShowGovernorate ?? false
});

export const salesValue = (form: SalesForm): SalesSettings => ({
  reservationHours: numberValue(form.reservationHours),
  reservationAutoRelease: form.reservationAutoRelease,
  posMaxDiscountPercent: numberValue(form.posMaxDiscountPercent),
  soldFeedEnabled: form.soldFeedEnabled,
  soldFeedShowGovernorate: form.soldFeedShowGovernorate
});

// —— Watermark ————————————————————————————————————————————————————————————————
export const WATERMARK_POSITIONS = ["BottomLeft", "BottomRight", "TopLeft", "TopRight", "Center"] as const;

export interface WatermarkForm {
  enabled: boolean;
  position: string;
  opacityPercent: string;
  sizePercent: string;
  applyToArticles: boolean;
}

export const watermarkForm = (value: WatermarkSettings): WatermarkForm => ({
  enabled: value.enabled ?? false,
  position: value.position ?? "BottomRight",
  opacityPercent: String(value.opacityPercent ?? 40),
  sizePercent: String(value.sizePercent ?? 20),
  applyToArticles: value.applyToArticles ?? false
});

export const watermarkValue = (form: WatermarkForm): WatermarkSettings => ({
  enabled: form.enabled,
  position: form.position,
  opacityPercent: numberValue(form.opacityPercent),
  sizePercent: numberValue(form.sizePercent),
  applyToArticles: form.applyToArticles
});

// —— Backup ———————————————————————————————————————————————————————————————————
export interface BackupForm {
  enabled: boolean;
  hourCairo: string;
  retentionDays: string;
}

export const backupForm = (value: BackupSettings): BackupForm => ({
  enabled: value.enabled ?? true,
  hourCairo: String(value.hourCairo ?? 3),
  retentionDays: String(value.retentionDays ?? 14)
});

export const backupValue = (form: BackupForm): BackupSettings => ({
  enabled: form.enabled,
  hourCairo: numberValue(form.hourCairo),
  retentionDays: numberValue(form.retentionDays)
});
