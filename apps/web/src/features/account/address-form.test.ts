import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { addressErrors, addressLine, emptyAddressForm, governorateOptions, toAddressRequest, toUpdateRequest } from "./address-form.ts";

const filled = {
  ...emptyAddressForm({ name: "منى", phone: "01012345678" }, false),
  label: "البيت", governorate: "الإسكندرية", city: "العجمي", street: " شارع 9 ", floor: "3"
};

describe("addressErrors", () => {
  it("flags every missing required field", () => {
    assert.deepEqual(addressErrors(emptyAddressForm({ name: "", phone: "" }, false)), ["label", "recipientName", "phone", "governorate", "city", "street"]);
  });
  it("accepts a complete form and rejects a bad phone", () => {
    assert.deepEqual(addressErrors(filled), []);
    assert.deepEqual(addressErrors({ ...filled, phone: "12345" }), ["phone"]);
  });
});

describe("toAddressRequest", () => {
  it("trims, normalizes the phone and nulls empty optionals", () => {
    const request = toAddressRequest({ ...filled, phone: "+201012345678" });
    assert.equal(request.phoneNumber, "01012345678");
    assert.equal(request.street, "شارع 9");
    assert.equal(request.building, null);
    assert.equal(request.floor, "3");
    assert.equal(request.countryCode, "EG");
    assert.equal(request.countryCallingCode, "+20");
  });
  it("adds the row version for an update", () => {
    assert.equal(toUpdateRequest(filled, 4).rowVersion, 4);
  });
});

describe("governorateOptions", () => {
  it("keeps a saved governorate that left the list", () => {
    const rates = [{ name: "القاهرة", nameEn: "Cairo" }];
    assert.deepEqual(governorateOptions(rates, "الجيزة", "ar").map((o) => o.value), ["الجيزة", "القاهرة"]);
    assert.deepEqual(governorateOptions(rates, "القاهرة", "ar").map((o) => o.value), ["القاهرة"]);
  });
  it("labels the options in the site language but keeps the stored Arabic value", () => {
    const rates = [{ name: "القاهرة", nameEn: "Cairo" }, { name: "الجيزة", nameEn: "" }];
    assert.deepEqual(governorateOptions(rates, "", "en"), [
      { value: "القاهرة", label: "Cairo" },
      { value: "الجيزة", label: "الجيزة" }
    ]);
    assert.deepEqual(governorateOptions(rates, "", "ar"), [
      { value: "القاهرة", label: "القاهرة" },
      { value: "الجيزة", label: "الجيزة" }
    ]);
  });
});

describe("addressLine", () => {
  it("joins the parts that exist", () => {
    const parts = { city: "العجمي", street: "شارع 9", building: null, floor: "3", apartment: null, landmark: null };
    assert.equal(addressLine(parts, { floor: "دور", apartment: "شقة" }), "العجمي، شارع 9، دور 3");
  });
});
