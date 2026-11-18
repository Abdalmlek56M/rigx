import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  SETTINGS_FILTERS,
  backupForm,
  backupValue,
  invoiceForm,
  invoiceValue,
  languageForm,
  languageValue,
  leadSourcesForm,
  leadSourcesValue,
  reviewsForm,
  reviewsValue,
  salesForm,
  salesValue,
  settingsSection,
  storeInfoForm,
  storeInfoValue,
  warrantyForm,
  warrantyValue,
  watermarkForm,
  watermarkValue,
  whatsAppForm,
  whatsAppValue
} from "./settings-rules.ts";
import { contactLinkIsNumber, contactLinksForm, contactLinksValue } from "./contact-rules.ts";
import { paymentForm, paymentValue } from "./payment-rules.ts";
import { shippingForm, shippingValue, toggleGovernorate, type ShippingGroupForm } from "./shipping-rules.ts";
import { controlsForm, controlsValue } from "./controls-rules.ts";

describe("settings sections", () => {
  it("opens the store data and falls back for unknown sections", () => {
    assert.equal(SETTINGS_FILTERS.section, "store-info");
    assert.equal(settingsSection(undefined), "store-info");
    assert.equal(settingsSection("shipping"), "shipping");
    assert.equal(settingsSection("backup"), "backup");
    assert.equal(settingsSection("nope"), "store-info");
  });
});

describe("store data", () => {
  it("reads nulls as empty and sends empty optional boxes as null", () => {
    const form = storeInfoForm({ name: "RIGX", domain: null, email: null, whatsAppHours: null });
    assert.deepEqual(
      [form.name, form.domain, form.email, form.whatsAppHours, form.pickupAddress],
      ["RIGX", "", "", "", ""]
    );
    const value = storeInfoValue({
      ...form,
      name: "  RIGX  ",
      domain: "  rigxhardware.com ",
      email: " ",
      pickupAddress: "  شارع 9 "
    });
    assert.deepEqual(
      [value.name, value.domain, value.email, value.pickupAddress, value.warrantySummary],
      ["RIGX", "rigxhardware.com", null, "شارع 9", null]
    );
  });
});

describe("contact links", () => {
  it("knows number platforms, defaults the switches and trims the value", () => {
    assert.equal(contactLinkIsNumber("WhatsApp"), true);
    assert.equal(contactLinkIsNumber("Phone"), true);
    assert.equal(contactLinkIsNumber("Facebook"), false);
    const form = contactLinksForm({ links: [{ platform: "WhatsApp", value: " 2010 " }] });
    assert.deepEqual(form.links[0], {
      platform: "WhatsApp",
      value: " 2010 ",
      showOnSite: true,
      showOnInvoice: false,
      showInFloatingButton: false
    });
    assert.equal(contactLinksValue(form).links?.[0]?.value, "2010");
  });
});

describe("payment", () => {
  it("keeps numbers as text and optional fields as null", () => {
    const form = paymentForm({ depositThreshold: 10000, depositPercent: 12.5, instaPayAddress: " pay@insta ", walletNumber: null });
    assert.deepEqual(
      [form.depositThreshold, form.depositPercent, form.instaPayAddress, form.walletNumber],
      ["10000", "12.5", " pay@insta ", ""]
    );
    const value = paymentValue({ ...form, depositPercent: "", instaPayAddress: " ", walletNumber: " 010 " });
    assert.deepEqual(
      [value.depositThreshold, value.depositPercent, value.instaPayAddress, value.walletNumber],
      [10000, 0, null, "010"]
    );
  });
});

describe("text sections", () => {
  it("trims the WhatsApp templates", () => {
    const value = whatsAppValue({
      ...whatsAppForm({}),
      general: "  أهلًا  ",
      product: " {القطعة} ",
      build: "",
      order: " ",
      warranty: "w",
      requestReply: "r",
      backInStock: "b"
    });
    assert.deepEqual(
      [value.general, value.product, value.build, value.order],
      ["أهلًا", "{القطعة}", "", ""]
    );
  });

  it("reads and writes reviews, language and invoice", () => {
    const reviews = reviewsValue({ ...reviewsForm({}), minimumCountToShowStars: "" });
    assert.equal(reviews.minimumCountToShowStars, 0);
    assert.equal(reviewsForm({ enabled: true, minimumCountToShowStars: 4 }).minimumCountToShowStars, "4");

    assert.deepEqual(languageValue({ defaultLanguage: "en", englishEnabled: false }), {
      defaultLanguage: "en",
      englishEnabled: false
    });
    assert.deepEqual(languageForm({ defaultLanguage: "ar", englishEnabled: true }), {
      defaultLanguage: "ar",
      englishEnabled: true
    });

    const invoice = invoiceValue({ ...invoiceForm({}), numberPrefix: " inv ", termsLine: " " });
    assert.equal(invoice.numberPrefix, "INV");
    assert.equal(invoice.termsLine, null);
    assert.equal(invoiceForm({ numberPrefix: "RIGX" }).numberPrefix, "RIGX");
  });
});

describe("list sections", () => {
  it("drops blank lead sources", () => {
    assert.deepEqual(leadSourcesValue({ sources: [" فيسبوك ", "", "   ", "جوجل"] }), { sources: ["فيسبوك", "جوجل"] });
    assert.deepEqual(leadSourcesForm({ sources: ["أ"] }).sources, ["أ"]);
  });

  it("keeps an empty warranty months as the agent's warranty and drops blank labels", () => {
    const value = warrantyValue({
      options: [
        { label: " شهر ", months: "1" },
        { label: "سنة", months: "" },
        { label: "  ", months: "12" }
      ]
    });
    assert.deepEqual(value.options, [
      { label: "شهر", months: 1 },
      { label: "سنة", months: null }
    ]);
    const form = warrantyForm({ options: [{ label: "شهر", months: 1 }, { label: "وكيل" }] });
    assert.deepEqual(form.options.map((option) => option.months), ["1", ""]);
  });

  it("reads and writes sales, watermark and backup numbers", () => {
    const sales = salesValue({
      reservationHours: "",
      reservationAutoRelease: true,
      posMaxDiscountPercent: "7.5"
    });
    assert.deepEqual([sales.reservationHours, sales.posMaxDiscountPercent], [0, 7.5]);
    assert.equal(salesForm({}).posMaxDiscountPercent, "10");

    const watermark = watermarkValue({
      ...watermarkForm({}),
      opacityPercent: "",
      sizePercent: "25",
      position: "Center"
    });
    assert.deepEqual([watermark.opacityPercent, watermark.sizePercent, watermark.position], [0, 25, "Center"]);

    const backup = backupValue({ ...backupForm({}), enabled: false, hourCairo: "23", retentionDays: "30" });
    assert.deepEqual([backup.enabled, backup.hourCairo, backup.retentionDays], [false, 23, 30]);
  });
});

describe("shipping", () => {
  it("reads groups and sends trimmed names and nullable delivery times", () => {
    const form = shippingForm({
      courierName: "Aramex",
      pickupEnabled: true,
      defaultFee: 50,
      defaultDeliveryTime: null,
      groups: [{ name: " القاهرة ", fee: 30, deliveryTime: "يومين", governorates: ["C", "GZ"] }]
    });
    assert.deepEqual([form.defaultFee, form.defaultDeliveryTime], ["50", ""]);
    assert.deepEqual(form.groups[0], { name: " القاهرة ", fee: "30", deliveryTime: "يومين", governorates: ["C", "GZ"] });

    const value = shippingValue({ ...form, courierName: " ", groups: [{ ...form.groups[0]!, fee: "", deliveryTime: " " }] });
    assert.equal(value.courierName, null);
    assert.deepEqual(value.groups?.[0], { name: "القاهرة", fee: 0, deliveryTime: null, governorates: ["C", "GZ"] });
  });

  it("adds and removes one governorate without touching the others", () => {
    const group: ShippingGroupForm = { name: "دلتا", fee: "35", deliveryTime: "", governorates: ["BH"] };
    assert.deepEqual(toggleGovernorate(group, "GH").governorates, ["BH", "GH"]);
    assert.deepEqual(toggleGovernorate(group, "BH").governorates, []);
    assert.deepEqual(group.governorates, ["BH"]);
  });
});

describe("controls", () => {
  it("keeps the homepage order and trims the announcement", () => {
    const form = controlsForm({
      maintenanceMode: true,
      announcementText: null,
      minimumOrderAmount: 500,
      homepageSections: [{ key: "offers", visible: false }, { key: "hero", visible: true }]
    });
    assert.deepEqual(form.homepageSections, [{ key: "offers", visible: false }, { key: "hero", visible: true }]);
    const value = controlsValue({ ...form, announcementText: "  خصم  ", minimumOrderAmount: "" });
    assert.equal(value.announcementText, "خصم");
    assert.equal(value.minimumOrderAmount, 0);
    assert.deepEqual(value.homepageSections, [{ key: "offers", visible: false }, { key: "hero", visible: true }]);
  });
});
