import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { EMPTY_FORM, type CheckoutFormState } from "./checkout-request.ts";
import { CHECKOUT_DRAFT_VERSION, draftJson, mergeDraft, parseDraft, toDraft } from "./checkout-draft.ts";

const form = (patch: Partial<CheckoutFormState> = {}): CheckoutFormState => ({
  ...EMPTY_FORM, name: "عمر", phone: "01289186840", email: "omar@example.com", emailConfirm: "omar@example.com",
  fulfillment: "Delivery", governorate: "ALX", area: "العجمي", street: "شارع 9", building: "12", landmark: "بجوار المسجد",
  notes: "اتصل قبل التسليم", usage: "Gaming", usageNote: "", leadSource: "Google", paymentMethod: "CashOnDelivery",
  termsAccepted: true, couponCode: "AHMED10", ...patch
});

describe("checkout draft", () => {
  it("round-trips the form fields it keeps", () => {
    const restored = mergeDraft(parseDraft(draftJson(form())));
    assert.equal(restored.name, "عمر");
    assert.equal(restored.phone, "01289186840");
    assert.equal(restored.emailConfirm, "omar@example.com");
    assert.equal(restored.fulfillment, "Delivery");
    assert.equal(restored.governorate, "ALX");
    assert.equal(restored.usage, "Gaming");
    assert.equal(restored.leadSource, "Google");
    assert.equal(restored.phoneHasWhatsApp, true);
  });

  it("never stores the coupon, payment method or the terms tick", () => {
    const draft = toDraft(form());
    assert.equal("couponCode" in draft, false);
    assert.equal("paymentMethod" in draft, false);
    assert.equal("termsAccepted" in draft, false);
  });

  it("ignores bad JSON, another version and a missing form", () => {
    assert.equal(parseDraft("{not json"), null);
    assert.equal(parseDraft(JSON.stringify({ version: CHECKOUT_DRAFT_VERSION + 1, form: { name: "x" } })), null);
    assert.equal(parseDraft(JSON.stringify({ version: CHECKOUT_DRAFT_VERSION })), null);
    assert.equal(parseDraft(null), null);
  });

  it("keeps a partial draft and drops values the form cannot hold", () => {
    const draft = parseDraft(JSON.stringify({
      version: CHECKOUT_DRAFT_VERSION,
      form: { name: "سارة", fulfillment: "Drone", usage: "Mining", phoneHasWhatsApp: "yes", area: 12 }
    }));
    assert.deepEqual(draft, { name: "سارة" });
    assert.equal(mergeDraft(draft).fulfillment, "Delivery");
    assert.equal(mergeDraft(draft).usage, null);
    assert.equal(mergeDraft(draft).name, "سارة");
  });

  it("starts from the defaults when there is no draft", () => {
    assert.deepEqual(mergeDraft(null), EMPTY_FORM);
  });
});
