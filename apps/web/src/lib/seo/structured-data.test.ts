import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { breadcrumbJsonLd, faqJsonLd, organizationJsonLd, productJsonLd, RETURN_WINDOW_DAYS, serializeJsonLd, websiteJsonLd } from "./structured-data.ts";

const base = {
  name: "RTX 3060 12GB PNY", url: "https://rigxhardware.com/p/rtx-3060-12gb-pny", images: [], price: 9500,
  organizationId: "https://rigxhardware.com/#organization"
};

describe("structured data", () => {
  it("describes a product offer in EGP with availability and condition", () => {
    const offer = productJsonLd({ ...base, available: true, condition: "Used", sku: "RX-0001" }).offers as Record<string, unknown>;
    assert.equal(offer.price, "9500.00");
    assert.equal(offer.priceCurrency, "EGP");
    assert.equal(offer.availability, "https://schema.org/InStock");
    assert.equal(offer.itemCondition, "https://schema.org/UsedCondition");
  });

  it("marks sold-out items and leaves out what is unknown", () => {
    assert.deepEqual(productJsonLd({ ...base, available: true, brand: "MSI" }).brand, { "@type": "Brand", name: "MSI" });
    const product = productJsonLd({ ...base, available: false });
    const offer = product.offers as Record<string, unknown>;
    assert.equal(offer.availability, "https://schema.org/SoldOut");
    assert.equal("itemCondition" in offer, false);
    assert.equal("image" in product, false);
  });

  it("numbers breadcrumbs and lets the current page go without a URL", () => {
    const list = breadcrumbJsonLd([{ name: "الرئيسية", url: "https://rigxhardware.com" }, { name: "RTX 3060" }]).itemListElement as Record<string, unknown>[];
    assert.deepEqual(list.map((crumb) => crumb.position), [1, 2]);
    assert.equal("item" in list[1]!, false);
  });

  it("gives the store a service area and no street address", () => {
    const org = organizationJsonLd({
      id: "x", name: "RIGX", url: "u", logo: "l", description: "d", email: null, telephone: "+201289186840", sameAs: []
    });
    assert.equal(org["@type"], "OnlineStore");
    assert.deepEqual(org.areaServed, { "@type": "Country", name: "Egypt" });
    assert.equal("address" in org, false);
    assert.equal("email" in org, false);
  });

  it("names the site and its Arabic alternate name", () => {
    const site = websiteJsonLd({ name: "RIGX Hardware", alternateName: "ريج اكس هاردوير", url: "https://rigxhardware.com" });
    assert.equal(site["@type"], "WebSite");
    assert.equal(site.name, "RIGX Hardware");
    assert.equal(site.alternateName, "ريج اكس هاردوير");
  });

  it("gives the organization an alternate name only when one is passed", () => {
    const data = { id: "x", name: "RIGX Hardware", url: "u", logo: "l", description: "d", email: null, telephone: null, sameAs: [] };
    assert.equal("alternateName" in organizationJsonLd(data), false);
    assert.equal(organizationJsonLd({ ...data, alternateName: "ريج اكس هاردوير" }).alternateName, "ريج اكس هاردوير");
  });

  it("adds the return window to the offer only when a returns page is given", () => {
    const without = productJsonLd({ ...base, available: true }).offers as Record<string, unknown>;
    assert.equal("hasMerchantReturnPolicy" in without, false);
    const offer = productJsonLd({ ...base, available: true, returnPolicyUrl: "https://rigxhardware.com/policies/returns" }).offers as Record<string, unknown>;
    const policy = offer.hasMerchantReturnPolicy as Record<string, unknown>;
    assert.equal(policy.merchantReturnDays, RETURN_WINDOW_DAYS);
    assert.equal(policy.applicableCountry, "EG");
    assert.equal(policy.merchantReturnLink, "https://rigxhardware.com/policies/returns");
  });

  it("lists FAQ questions with their answers", () => {
    const faq = faqJsonLd([{ question: "بتشحنوا؟", answer: "لكل المحافظات." }]);
    assert.deepEqual(faq.mainEntity, [{ "@type": "Question", name: "بتشحنوا؟", acceptedAnswer: { "@type": "Answer", text: "لكل المحافظات." } }]);
  });

  it("never lets text close the script tag", () => {
    assert.equal(serializeJsonLd({ name: "</script><b>" }).includes("<"), false);
  });
});
