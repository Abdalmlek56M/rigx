import { multipart, type Schemas, unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type SettingsView = Schemas["StoreSettingsAdminView"];
export type ShippingRates = Schemas["ShippingRatesView"];
export type GovernorateRate = Schemas["GovernorateShippingRate"];
export type StoreLogoKind = "store" | "watermark";

export type StoreInfo = Schemas["StoreInfoSettings"];
export type ContactLinks = Schemas["ContactLinkSettings"];
export type Shipping = Schemas["ShippingSettings"];
export type Payment = Schemas["PaymentSettings"];
export type Controls = Schemas["StoreControlSettings"];
export type WhatsAppTemplates = Schemas["WhatsAppTemplateSettings"];
export type Reviews = Schemas["ReviewSettings"];
export type Language = Schemas["LanguageSettings"];
export type Invoice = Schemas["InvoiceSettings"];
export type LeadSources = Schemas["LeadSourceSettings"];
export type WarrantyOptions = Schemas["WarrantyOptionSettings"];
export type Sales = Schemas["SalesSettings"];
export type Watermark = Schemas["WatermarkSettings"];
export type Backup = Schemas["BackupSettings"];

const api = () => browserApi();

export const fetchSettings = () => required(unwrap(api().GET("/api/v1/admin/storefront/settings")));

/** The governorate names behind the codes in the shipping groups (public: the checkout uses the same list). */
export const fetchShippingRates = () => required(unwrap(api().GET("/api/v1/storefront/shipping-rates")));

export const saveStoreInfo = (value: StoreInfo, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/store-info", { body: { value, rowVersion } })));

export const saveContactLinks = (value: ContactLinks, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/contact-links", { body: { value, rowVersion } })));

export const saveShipping = (value: Shipping, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/shipping", { body: { value, rowVersion } })));

export const savePayment = (value: Payment, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/payment", { body: { value, rowVersion } })));

export const saveControls = (value: Controls, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/controls", { body: { value, rowVersion } })));

export const saveWhatsApp = (value: WhatsAppTemplates, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/whatsapp-templates", { body: { value, rowVersion } })));

export const saveReviews = (value: Reviews, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/reviews", { body: { value, rowVersion } })));

export const saveLanguage = (value: Language, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/language", { body: { value, rowVersion } })));

export const saveInvoice = (value: Invoice, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/invoice", { body: { value, rowVersion } })));

export const saveLeadSources = (value: LeadSources, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/lead-sources", { body: { value, rowVersion } })));

export const saveWarrantyOptions = (value: WarrantyOptions, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/warranty-options", { body: { value, rowVersion } })));

export const saveSales = (value: Sales, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/sales", { body: { value, rowVersion } })));

export const saveWatermark = (value: Watermark, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/watermark", { body: { value, rowVersion } })));

export const saveBackup = (value: Backup, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/storefront/settings/backup", { body: { value, rowVersion } })));

export const uploadLogo = (kind: StoreLogoKind, file: File) => {
  const form = new FormData();
  form.set("image", file);
  return required(unwrap(api().PUT("/api/v1/admin/storefront/settings/logos/{kind}", {
    params: { path: { kind } },
    ...multipart(form)
  })));
};

export const deleteLogo = (kind: StoreLogoKind) =>
  required(unwrap(api().DELETE("/api/v1/admin/storefront/settings/logos/{kind}", { params: { path: { kind } } })));

/** Reprocesses every stored photo with the current watermark (accepted 202; the work runs in the background). */
export const reapplyWatermark = () =>
  unwrap(api().POST("/api/v1/admin/storefront/settings/watermark/reapply"));
