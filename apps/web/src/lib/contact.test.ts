import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { fillTemplate, siteUrl, whatsAppNumber, whatsAppUrl } from "./contact.ts";

describe("whatsapp helpers", () => {
  it("fills the placeholders and trims a missing link", () => {
    const text = fillTemplate("أهلًا، عايز أسأل على {القطعة} ({الكود}) بسعر {السعر}.\n{اللينك}", { item: "RTX 3060", code: "RX-1", price: "100 ج.م" });
    assert.equal(text, "أهلًا، عايز أسأل على RTX 3060 (RX-1) بسعر 100 ج.م.");
  });
  it("returns empty text for an empty template", () => {
    assert.equal(fillTemplate("  ", { item: "x" }), "");
  });
  it("builds a wa.me link with the encoded text", () => {
    assert.equal(whatsAppUrl("201289186840", "a b"), "https://wa.me/201289186840?text=a%20b");
    assert.equal(whatsAppUrl("201289186840"), "https://wa.me/201289186840");
  });
  it("reads the number from the contact links", () => {
    const link = { platform: "WhatsApp", value: "+20 128 918 6840", showOnSite: true, showInFloatingButton: false };
    assert.equal(whatsAppNumber([link]), "201289186840");
    assert.equal(whatsAppNumber([]), null);
  });
  it("builds a public URL from the domain", () => {
    assert.equal(siteUrl("rigxhardware.com", "/products/RX-1"), "https://rigxhardware.com/products/RX-1");
    assert.equal(siteUrl("https://rigxhardware.com/", "/x"), "https://rigxhardware.com/x");
    assert.equal(siteUrl(null, "/x"), "");
  });
});
